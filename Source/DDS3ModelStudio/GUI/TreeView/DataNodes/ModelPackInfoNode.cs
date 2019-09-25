using DDS3ModelLibrary.Models;
using JetBrains.Annotations;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public class ModelPackInfoNode : ResourceNode<ModelPackInfo>
    {
        public override DataNodeDisplayHint DisplayHint 
            => DataNodeDisplayHint.Branch;

        public override DataNodeAction SupportedActions
            => DataNodeAction.Delete | DataNodeAction.Export | DataNodeAction.Replace | DataNodeAction.Move;


        public short Field1A
        {
            get => GetDataProperty<short>();
            set => SetDataProperty( value );
        }

        public short Field22
        {
            get => GetDataProperty<short>();
            set => SetDataProperty( value );
        }

        public ListNode<ModelPackEffectInfo> EffectInfos { get; private set; }

        public ModelPackInfoNode( [NotNull] string name, [NotNull] ModelPackInfo data ) : base( name, data )
        {
        }

        protected override void OnInitialize()
        {
            RegisterExportHandler<ModelPackInfo>( Data.Save );
            //RegisterReplaceHandler<ModelPackInfo>( ( filePath ) => new ModelPackInfo( filePath ) );
            RegisterSyncHandler( () =>
            {
                Data.EffectInfos.Clear();
                Data.EffectInfos.AddRange( EffectInfos.Data );
                return Data;
            });
        }

        protected override void OnInitializeView()
        {
            EffectInfos = AddNode( new ListNode<ModelPackEffectInfo>( "Effect infos", Data.EffectInfos, ( i, x ) => $"Effect info {i}" ) );
        }
    }
}