using System;
using System.Linq;
using System.Windows.Forms;

namespace DDS3ModelStudio.GUI.TreeView
{
    public class DataNodeCallbacks
    {
        public Action Rename { get; }
    }

    /// <summary>
    /// Adapts a <see cref="GUI.TreeView.DataNode"/> to the interface of <see cref="TreeNode"/>.
    /// </summary>
    public class DataTreeNode : TreeNode
    {
        public new DataTreeView TreeView => ( DataTreeView )base.TreeView;

        public DataNode DataNode { get; }

        public new string Name
        {
            get => DataNode.Name;
            set => DataNode.Name = value;
        }
        public new string Text
        {
            get => DataNode.Name;
            set => DataNode.Name = value;
        }

        public override ContextMenuStrip ContextMenuStrip => DataNode.ContextMenuStrip;

        public event EventHandler<(DataTreeNode Node, DataTreeNode AddedNode)> NodeAdded;
        public event EventHandler<(DataTreeNode Node, DataTreeNode RemovedNode)> RemovedNode;
        public event EventHandler<(DataTreeNode Node, int OldIndex, int NewIndex, DataTreeNode MovedNode)> NodeMoved;
        public event EventHandler<DataTreeNode> DataChanged;

        public DataTreeNode( DataNode dataNode )
        {
            DataNode = dataNode;
            SetBaseName( dataNode.Name );

            // TODO icon
            DataNode.StartRename = StartRename;
            DataNode.Renamed += ( o, e ) => SetBaseName( e.NewValue );

            if ( dataNode.DisplayHint.HasFlag( DataNodeDisplayHint.Branch ) )
            {
                DataNode.NodeAdded += HandleNodeAdded;
                DataNode.NodeRemoved += HandleNodeRemoved;
                DataNode.NodeMoved += HandleNodeMoved;
                DataNode.DataReplaced += HandleDataReplaced;
                DataNode.PropertyChanged += HandleDataNodePropertyChanged;

                // Make it look like we have a child node if its hinted that it will
                // receive child nodes
                // This allows lists to appear as lists before they are fully initialized
                Nodes.Add( new TreeNode() );
            }
        }

        public virtual void OnBeforeExpand( TreeViewCancelEventArgs e)
        {
            InitializeView();
        }

        /// <summary>
        /// Sets the name and display name of the underlying tree node.
        /// </summary>
        /// <param name="name"></param>
        private void SetBaseName( string name )
        {
            base.Name = base.Text = name;
        }

        // Rename methods
        private void StartRename()
        {
            // renaming can only be done through the treeview that owns this node
            if ( TreeView == null )
                return;

            // start editing the name
            TreeView.LabelEdit = true;
            BeginEdit();

            // subscribe to the label edit event, so we can finish 
            TreeView.AfterLabelEdit += EndRename;
        }

        private void EndRename( object sender, NodeLabelEditEventArgs e )
        {
            // stop editing the name
            EndEdit( false );

            bool textWasEdited = !e.CancelEdit && e.Label != null && e.Label != Text;
            if ( textWasEdited )
                DataNode.Name = e.Label;

            // unsubscribe from the event
            if ( TreeView != null )
                TreeView.AfterLabelEdit -= EndRename;
        }

        private void HandleDataNodePropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            DataChanged?.Invoke( this, this );
        }

        private void HandleNodeAdded( object sender, DataNodeEventArgs<DataNode> e )
        {
            var child = new DataTreeNode( e.Value );
            Nodes.Add( child );
            NodeAdded?.Invoke( this, ( this, child ) );
        }

        private void HandleNodeRemoved( object sender, DataNodeEventArgs<DataNode> e )
        {
            var child = Nodes.OfType<DataTreeNode>().FirstOrDefault( x => x.DataNode == e.Value );

            if ( child != null )
                Nodes.Remove( child );

            RemovedNode?.Invoke( this, ( this, child ) );
        }

        private void HandleNodeMoved( object sender, DataNodeMovedEventArgs e )
        {
            var child = ( DataTreeNode )Nodes[e.OldIndex];
            Nodes.RemoveAt( e.OldIndex );
            Nodes.Insert( e.NewIndex, child );
            NodeMoved?.Invoke( this, ( this, e.OldIndex, e.NewIndex, child ) );
        }

        private void HandleDataReplaced( object sender, DataNodeEventArgs<object> e )
        {
            InitializeView();
        }

        private void InitializeView()
        {
            Nodes.Clear();
            DataNode.InitializeView();
        }

        internal virtual void OnBeforeSelect( TreeViewCancelEventArgs e )
        {
            DataNode.Initialize();
        }
    }
}