using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DDS3ModelStudio.GUI.TreeView
{
    public delegate void DataNodeExportHandler(string filePath);

    /// <summary>
    /// The handler used
    /// </summary>
    /// <param name="filePath"></param>
    public delegate void DataNodeImportHandler(string filePath);

    /// <summary>
    /// The handler used to perform additional processing required to synchronize the data object with the view model.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public delegate T DataNodeSyncHandler<out T>();

    /// <summary>
    /// The handler used to create a new data object that will replace the current.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public delegate T DataNodeReplaceHandler<out T>(string filePath);

    [Flags]
    public enum DataNodeAction
    {
        Export = 1 << 0,
        Replace = 1 << 1,
        Add = 1 << 2,
        Move = 1 << 3,
        Rename = 1 << 4,
        Delete = 1 << 5,
    }

    /// <summary>
    /// Encapsulates the user visible properties and user interaction logic of a data object.
    /// </summary>
    public abstract class DataNode : IEnumerable<DataNode>, INotifyPropertyChanged
    {
        // -- Private fields --
        private string mName;
        private readonly List<DataNode> mNodes;
        private object mData;
        private bool mSynced;

        // handlers
        private readonly Dictionary<Type, DataNodeExportHandler> mExportHandlers;
        private readonly List<(string[] MenuPathParts, ToolStripMenuItem Item)> mCustomHandlers;
        private bool mHasUnsavedChanges;
        private DataNode mParent;

        // -- Protected properties
        protected bool IsInitialized { get; private set; }

        protected bool IsInitializingView { get; private set; }

        protected bool IsViewInitialized { get; private set; }

        protected bool IsSyncing { get; private set; }

        // -- Public properties --
        [NotNull]
        [Browsable(false)]
        public object Data
        {
            get
            {
                if (!Synced && !IsSyncing)
                {
                    // Sync data with view model
                    SyncData();
                }

                return mData;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                mData = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets if the view model is synchronized with the data object.
        /// </summary>
#if !DEBUG
        [Browsable( false )]
#endif
        public bool Synced
        {
            get => mSynced;
            set
            {
                if (value != mSynced)
                {
                    mSynced = value;
                    NotifyPropertyChanged();
                    Log($"Synced = {value}");

                    if (!mSynced && Parent != null)
                    {
                        // If a parent's child gets desynchronized, then the parent itself will be too.
                        Parent.Synced = false;
                    }
                    else if (mSynced)
                    {
                        Debug.Assert(IsSyncing, "Synced should not be manually set to true outside of the sync process");
                    }
                }
            }
        }

        /// <summary>
        /// Gets if the data has ever changed since it was initialized.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => mHasUnsavedChanges;
            private set
            {
                if (mHasUnsavedChanges != value)
                {
                    mHasUnsavedChanges = value;
                    NotifyPropertyChanged();
                }
            }
        }

#if !DEBUG
        [Browsable( false )]
#endif
        public abstract Type DataType { get; }

#if !DEBUG
        [Browsable( false )]
#endif
        public abstract DataNodeDisplayHint DisplayHint { get; }

#if !DEBUG
        [Browsable( false )]
#endif
        public abstract DataNodeAction SupportedActions { get; }

        [Browsable(false)]
        public DataNode Parent
        {
            get => mParent;
            private set
            {
                mParent = value;
                NotifyPropertyChanged();
            }
        }

        public IReadOnlyList<DataNode> Nodes => mNodes;

        [Browsable(false)]
        public ContextMenuStrip ContextMenuStrip { get; private set; }

        [NotNull]
        public virtual string Name
        {
            get => mName;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value != mName)
                {
                    var oldName = mName;
                    mName = value;
                    OnRenamed(oldName, mName);
                    NotifyPropertyChanged();
                }
            }
        }

        // -- Events --
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataNodePropertyChangedEventArgs<string>> Renamed;
        public event EventHandler<DataNodeEventArgs<DataNode>> NodeAdded;
        public event EventHandler<DataNodeEventArgs<DataNode>> NodeRemoved;
        public event EventHandler<DataNodeMovedEventArgs> NodeMoved;
        public event EventHandler<DataNodeEventArgs<object>> DataReplaced;
        public event EventHandler<DataNodeEventArgs<DataNode>> ParentChanged;
        public event EventHandler<DataNodeEventArgs> BeforeViewInitialized;
        public event EventHandler<DataNodeEventArgs> AfterViewInitialized;

        // -- Callbacks --
        public Action StartRename { get; set; }

        protected DataNode([NotNull] string name, [NotNull] object data)
        {
            // Basic init
            mName = name;
            mData = data;
            mNodes = new List<DataNode>();
            mCustomHandlers = new List<(string[] MenuPathParts, ToolStripMenuItem Item)>();
            mExportHandlers = new Dictionary<Type, DataNodeExportHandler>();
            mSynced = true;
            Log("Created");
        }

        // -- Public methods --
        public virtual T AddNode<T>([NotNull] T node) where T : DataNode
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Log($"Add child: {node.Name}");

            // Disown if necessary
            node.Parent?.RemoveNode(node);
            node.Parent = this;
            mNodes.Add(node);
            OnNodeAdded(node);
            return node;
        }

        public virtual void RemoveNode([NotNull] DataNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Log($"Remove child: {node.Name}");

            // Disown
            mNodes.Remove(node);
            node.Parent = null;
            OnNodeRemoved(node);
        }

        public virtual void ClearNodes()
        {
            Log("Clearing nodes");
            mNodes.Clear();
            Synced = false;
        }

        public IEnumerable<DataNode> EnumerateNodes() => mNodes;

        public virtual void Remove()
        {
            Parent?.RemoveNode(this);
        }

        public virtual void MoveDown()
        {
            if (Parent == null)
                return;

            var index = Parent.mNodes.IndexOf(this);
            if (index == Parent.mNodes.Count - 1)
                return;

            Parent.mNodes.RemoveAt(index);
            Parent.mNodes.Insert(index + 1, this);
            Parent.OnNodeMoved(index, index + 1, this);
        }

        public virtual void MoveUp()
        {
            if (Parent == null)
                return;

            var index = Parent.mNodes.IndexOf(this);
            if (index == 0)
                return;

            Parent.mNodes.RemoveAt(index);
            Parent.mNodes.Insert(index - 1, this);
            Parent.OnNodeMoved(index, index - 1, this);
        }

        //
        // Export methods
        //

        /// <summary>
        /// Selects a file path to export to, and exports the data to the specified file path. The type of data to export is inferred from the file path.
        /// </summary>
        /// <returns>The path the file was exported to.</returns>
        public virtual string Export()
        {
            if (mExportHandlers.Count == 0)
                return null;

            using (var dialog = new SaveFileDialog())
            {
                dialog.AutoUpgradeEnabled = true;
                dialog.CheckPathExists = true;
                dialog.FileName = Name;
                //dialog.Filter             = ModuleFilterGenerator.GenerateFilter( FormatModuleUsageFlags.Export, mExportHandlers.Keys.ToArray() );
                dialog.OverwritePrompt = true;
                dialog.Title = "Select a file to export to.";
                dialog.ValidateNames = true;
                dialog.AddExtension = true;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return null;

                Export(dialog.FileName);
                return dialog.FileName;
            }
        }

        /// <summary>
        /// Exports the data to the specified file path. The type of data to export is inferred from the file path.
        /// </summary>
        /// <param name="filePath">Path to the file to export to.</param>
        public virtual void Export(string filePath)
        {
            if (mExportHandlers.Count == 0)
                return;

            //var type         = GetTypeFromPath( filePath, mExportHandlers.Keys );
            //var exportAction = mExportHandlers[type];

            //Log( $"Export as {type} to {filePath}" );

            //exportAction( filePath );
        }

        // -- Protected methods
        public void Initialize()
        {
            if (IsInitialized)
                return;

            Log("Initializing");

            // initialize the derived view model
            OnInitialize();

            // initialize the context menu strip if the view model isn't initialized yet
            InitializeContextMenuStrip();

            // set initialization flag
            IsInitialized = true;
        }

        /// <summary>
        /// Called once when the node is first initialized.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Populates the view -- which is the current node's child nodes and/or any other properties
        /// </summary>
        protected internal void InitializeView(bool force = false)
        {
            // Don't initialize the view if it is already initialized, or if the view is still in sync with the data unless forced
            if (IsInitializingView || (!force && Synced && IsViewInitialized))
                return;

            OnBeforeInitializeView();

            IsInitializingView = true;
            Log("Initializing view");

            // rebuild for initializing view, if necessary
            if (!Synced)
                SyncData();

            if (DisplayHint.HasFlag(DataNodeDisplayHint.Branch))
            {
                // let the derived view model populate the view
                OnInitializeView();
            }

            IsInitializingView = false;
            IsViewInitialized = true;

            OnAfterInitializeView();
        }

        /// <summary>
        /// Invoked before the view is initialized.
        /// </summary>
        protected virtual void OnBeforeInitializeView()
        {
            InvokeEvent(BeforeViewInitialized);
        }

        /// <summary>
        /// Invoked whenever the view has to be (re-)populated
        /// </summary>
        protected virtual void OnInitializeView() { }

        /// <summary>
        /// Invokes after the view is initialized.
        /// </summary>
        protected virtual void OnAfterInitializeView()
        {
            InvokeEvent(AfterViewInitialized);
        }

        /// <summary>
        /// Gets a property from the data object. Name is inferred from name of current method or property when not specified.
        /// </summary>
        /// <remarks>
        /// Use this to enforce naming consistency between the view model and model.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected T GetDataProperty<T>([CallerMemberName] string propertyName = default)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var prop = DataType.GetProperty(propertyName, typeof(T));
            if (prop == null)
            {
                var field = DataType
                            .GetFields()
                            .Where(x => x.Name == propertyName)
                            .FirstOrDefault(x => x.FieldType == typeof(T));

                if (field == null)
                    throw new MissingMemberException(DataType.Name, propertyName);

                return (T)field.GetValue(Data);
            }

            return (T)prop.GetValue(Data);
        }

        /// <summary>
        /// Sets a property of data object. Name is inferred from name of current method or property when not specified.
        /// </summary>
        /// <remarks>
        /// Use this to enforce naming consistency between the view model and model.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected void SetDataProperty<T>(T value, [CallerMemberName] string propertyName = default)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var propertyType = typeof(T);

            var property = DataType.GetProperty(propertyName, propertyType);
            if (property == null)
                throw new MissingMemberException(DataType.Name, nameof(property));

            if (propertyType.IsValueType)
            {
                // Value is the same, so no need to update it.
                if (Equals(property.GetValue(Data, null), value))
                    return;
            }

            Log($"Setting {propertyName} to {value}");
            property.SetValue(Data, value);

            if (property.GetCustomAttribute<RequireSyncAttribute>() != null)
            {
                // This property requires to data object to be resynchronized
                // Example: the data presented in the view model isn't representative of the underlying data in the data object
                // And/or the process by which the data has to be synchronized with the data object is too expensive to perform
                // every time the property value is changed
                Synced = false;
            }

            if (Parent != null)
            {
                // Child was modified, so parent is no longer synchronized
                Parent.Synced = false;
            }

            // ReSharper disable once ExplicitCallerInfoArgument
            NotifyPropertyChanged(propertyName);
        }

        protected void RegisterExportHandler<T>(DataNodeExportHandler handler)
            => mExportHandlers[typeof(T)] = handler;

        protected void RegisterCustomHandler(string menuPath, Action action, Keys shortcutKeys = Keys.None)
        {
            var menuPathParts = menuPath.Split('/');
            var itemName = menuPathParts[menuPathParts.Length - 1];
            Array.Resize(ref menuPathParts, menuPathParts.Length - 1);
            mCustomHandlers.Add((menuPathParts, CreateToolstripMenuItem(itemName, action, shortcutKeys)));
        }

        private ToolStripMenuItem CreateToolstripMenuItem(string name, Action action, Keys shortcutKeys)
            => new ToolStripMenuItem(name, null, CreateContextMenuStripEventHandler(action), shortcutKeys) { Name = name };

        // -- Protected event handlers --
        protected virtual void OnRenamed([NotNull] string oldName, [NotNull] string newName)
        {
            Synced = false;
            InvokeEvent(Renamed, oldName, newName);
        }

        protected virtual void OnNodeAdded([NotNull] DataNode node)
        {
            if (!IsInitializingView)
                Synced = false;

            InvokeEvent(NodeAdded, node);
        }

        protected virtual void OnNodeRemoved([NotNull] DataNode node)
        {
            Synced = false;
            InvokeEvent(NodeRemoved, node);
        }

        protected virtual void OnParentChanged(DataNode parent)
        {
            if (parent != null)
                parent.Synced = false;

            InvokeEvent(ParentChanged, parent);
        }

        protected virtual void OnNodeMoved(int oldIndex, int nodeIndex, DataNode movedNode)
        {
            Synced = false;
            NodeMoved?.Invoke(this, new DataNodeMovedEventArgs(this, oldIndex, nodeIndex, movedNode));
        }

        /// <summary>
        /// Performs the actual data synchronisation.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        protected abstract object SyncDataCore();


        [Conditional("DEBUG")]
        protected void Log(string message) => Debug.WriteLine($"DataNode<{DataType.Name}> '{Name}': {message}");

        // -- Private methods
        private void SyncData()
        {
            Log("Syncing");
            IsSyncing = true;
            Data = SyncDataCore();
            Synced = true;
            IsSyncing = false;
        }

        /// <summary>
        /// Populates the context menu strip, should be called after the context menu options have been set.
        /// </summary>
        private void InitializeContextMenuStrip()
        {
            ContextMenuStrip = new ContextMenuStrip();

            var addSeperator = false;
            foreach (var handler in mCustomHandlers.Where(x => x.MenuPathParts.Length == 0))
            {
                ContextMenuStrip.Items.Add(handler.Item);
                addSeperator = true;
            }

            if (addSeperator)
                ContextMenuStrip.Items.Add(new ToolStripSeparator());

            if (SupportedActions.HasFlag(DataNodeAction.Export))
            {
                ContextMenuStrip.Items.Add(new ToolStripMenuItem("&Export", null, CreateContextMenuStripEventHandler(() => Export()), Keys.Control | Keys.E)
                {
                    Name = "Export"
                });
            }

            //if ( SupportedActions.HasFlag( DataNodeAction.Replace ) )
            //{
            //    ContextMenuStrip.Items.Add( new ToolStripMenuItem( "&Replace", null, CreateContextMenuStripEventHandler( Replace ), Keys.Control | Keys.R )
            //    {
            //        Name = "Replace"
            //    } );

            //    if ( !SupportedActions.HasFlag( DataNodeAction.Add ) )
            //        ContextMenuStrip.Items.Add( new ToolStripSeparator() );
            //}

            //if ( SupportedActions.HasFlag( DataNodeAction.Add ) )
            //{
            //    ContextMenuStrip.Items.Add( new ToolStripMenuItem( "&Add", null, CreateContextMenuStripEventHandler( Add ),
            //                                                       Keys.Control | Keys.A )
            //    { Name = "Add" } );

            //    if ( SupportedActions.HasFlag( DataNodeAction.Move ) )
            //        ContextMenuStrip.Items.Add( new ToolStripSeparator() );
            //}

            if (SupportedActions.HasFlag(DataNodeAction.Move))
            {
                ContextMenuStrip.Items.Add(new ToolStripMenuItem("Move &Up", null, CreateContextMenuStripEventHandler(MoveUp), Keys.Control | Keys.Up)
                {
                    Name = "Move Up"
                });
                ContextMenuStrip.Items.Add(new ToolStripMenuItem("Move &Down", null, CreateContextMenuStripEventHandler(MoveDown), Keys.Control | Keys.Down)
                {
                    Name = "Move Down"
                });
            }

            if (SupportedActions.HasFlag(DataNodeAction.Rename))
            {
                ContextMenuStrip.Items.Add(new ToolStripMenuItem("Re&name", null, CreateContextMenuStripEventHandler(StartRename), Keys.Control | Keys.N)
                {
                    Name = "Rename"
                });
            }

            foreach ((string[] menuPathParts, ToolStripMenuItem item) in mCustomHandlers.Where(x => x.MenuPathParts.Length > 0))
            {
                // Add custom handlers with a menu path
                ToolStripMenuItem parentMenuItem = null;

                foreach (string menu in menuPathParts)
                {
                    var menuItem = (ToolStripMenuItem)(parentMenuItem == null ? ContextMenuStrip.Items.Find(menu, false).FirstOrDefault()
                                                                                  : parentMenuItem.DropDownItems.Find(menu, false).FirstOrDefault());

                    var isNewMenuItem = false;
                    if (menuItem == null)
                    {
                        isNewMenuItem = true;
                        menuItem = new ToolStripMenuItem(menu) { Name = menu };
                    }

                    if (isNewMenuItem)
                    {
                        if (parentMenuItem == null)
                            ContextMenuStrip.Items.Add(menuItem);
                        else
                            parentMenuItem.DropDownItems.Add(menuItem);
                    }

                    parentMenuItem = menuItem;
                }

                if (parentMenuItem == null)
                    ContextMenuStrip.Items.Add(item);
                else
                    parentMenuItem.DropDownItems.Add(item);
            }

            if (SupportedActions.HasFlag(DataNodeAction.Delete))
            {
                ContextMenuStrip.Items.Add(new ToolStripMenuItem("&Delete", null, CreateContextMenuStripEventHandler(Remove), Keys.Control | Keys.Delete)
                {
                    Name = "Delete"
                });
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (IsInitialized && !IsInitializingView)
                HasUnsavedChanges = true;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InvokeEvent(EventHandler<DataNodeEventArgs> eventHandler) =>
            eventHandler?.Invoke(this, new DataNodeEventArgs(this));

        private void InvokeEvent<T>(EventHandler<DataNodeEventArgs<T>> eventHandler, T value) =>
            eventHandler?.Invoke(this, new DataNodeEventArgs<T>(this, value));

        private void InvokeEvent<T>(EventHandler<DataNodePropertyChangedEventArgs<T>> eventHandler, T oldValue, T newValue) =>
            eventHandler?.Invoke(this, new DataNodePropertyChangedEventArgs<T>(this, oldValue, newValue));

        private EventHandler CreateContextMenuStripEventHandler(Action action)
        {
            return (s, e) =>
            {
                // Annoyingly, the context menu strip stays visible when a dialog is opened while a 
                // drop down menu is visible. That's why we explicitly hide it here.
                ContextMenuStrip.Visible = false;

                action();
            };
        }

        //
        // Helpers
        //
        //private static Type GetTypeFromPath( string filePath, IEnumerable<Type> types )
        //{
        //    var extension = Path.GetExtension( filePath );
        //    if ( string.IsNullOrEmpty( extension ) )
        //        extension = string.Empty;
        //    else
        //        extension = extension.Substring( 1 );

        //    bool isBaseType = false;

        //    var modulesWithType = FormatModuleRegistry.Modules.Where( x => types.Contains( x.ModelType ) );
        //    if ( !modulesWithType.Any() )
        //    {
        //        modulesWithType = FormatModuleRegistry.Modules.Where( x => types.Any( y => x.ModelType.IsSubclassOf( y ) ) );
        //        isBaseType = true;
        //    }

        //    List<IFormatModule> modules;
        //    if ( extension.Length > 0 )
        //    {
        //        modules = modulesWithType.Where( x =>
        //                                             ( x.Extensions.Any( ext => ext == "*" ) ||
        //                                               x.Extensions.Contains( extension, StringComparer.InvariantCultureIgnoreCase ) ) )
        //                                 .ToList();
        //    }
        //    else
        //    {
        //        modules = modulesWithType.Where( x => x.Extensions.Any( ext => ext == "*" ) )
        //                                 .ToList();
        //    }

        //    // remove wild card modules if we have more than 1 module
        //    if ( modules.Count > 1 )
        //    {
        //        modules.RemoveAll( x => x.Extensions.Contains( "*" ) );

        //        if ( modules.Count == 0 )
        //            throw new Exception( "Only suitable modules are multiple modules with wild cards?" );
        //    }

        //    if ( modules.Count == 0 )
        //    {
        //        throw new Exception( "No suitable modules for format found." );
        //    }

        //    if ( modules.Count != 1 )
        //        throw new Exception( "Ambigious module match. Multiple suitable modules format found." );

        //    if ( !isBaseType )
        //        return modules[0].ModelType;
        //    else
        //        return modules[0].ModelType.BaseType;
        //}

        // -- IEnumerable --
        public IEnumerator<DataNode> GetEnumerator() => mNodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Encapsulates the user visible properties and user interaction logic of a typed data object.
    /// </summary>
    /// <typeparam name="T">The type of the data object.</typeparam>
    public abstract class DataNode<T> : DataNode
    {
        private DataNodeSyncHandler<T> mSyncHandler;
        private readonly Dictionary<Type, DataNodeReplaceHandler<T>> mReplaceHandlers;

        [Browsable(false)]
        public new T Data
        {
            get => (T)base.Data;
            set => base.Data = value;
        }

#if !DEBUG
        [ Browsable( false ) ]
#endif
        public override Type DataType => typeof(T);

        protected DataNode([NotNull] string name, [NotNull] T data) : base(name, data)
        {
            mReplaceHandlers = new Dictionary<Type, DataNodeReplaceHandler<T>>();
        }

        protected void RegisterSyncHandler(DataNodeSyncHandler<T> handler)
            => mSyncHandler = handler;

        protected void RegisterReplaceHandler<TReplacement>(DataNodeReplaceHandler<T> handler)
            => mReplaceHandlers[typeof(TReplacement)] = handler;

        protected override object SyncDataCore()
        {
            if (mSyncHandler == null)
            {
                // Sync was required but no handler exists
                // Likely a bug
                Log("Sync was required but no sync handler is registered");
                return Data;
            }

            // Perform actual synchronization
            return Data = mSyncHandler();
        }
    }
}
