using System.ComponentModel;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Motions;
using JetBrains.Annotations;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public class ModelPackNode : DataNode<ModelPack>
    {
        public override DataNodeDisplayHint DisplayHint
            => DataNodeDisplayHint.Branch;

        public override DataNodeAction SupportedActions
            => DataNodeAction.Export | DataNodeAction.Move | DataNodeAction.Replace | DataNodeAction.Rename;

        [Browsable( false )]
        public ModelPackInfoNode Info { get; private set; }

        [Browsable( false )]
        public TexturePackNode TexturePack { get; private set; }

        [Browsable( false )]
        public ListNode<Resource> Effects { get; private set; }

        [Browsable( false )]
        public ListNode<Model> Models { get; private set; }

        [Browsable( false )]
        public ListNode<MotionPack> MotionPacks { get; private set; }

        public ModelPackNode( [NotNull] string name, [NotNull] ModelPack data ) : base( name, data )
        {
        }

        protected override void OnInitialize()
        {
            RegisterExportHandler<ModelPack>( Data.Save );
            RegisterReplaceHandler<ModelPack>( filePath => new ModelPack( filePath ) );
            RegisterSyncHandler( () =>
            {
                var modelPack = new ModelPack();
                modelPack.Info = Info.Data;
                modelPack.TexturePack = TexturePack.Data;
                modelPack.Effects.AddRange( Effects.Data );
                modelPack.Models.AddRange( Models.Data );
                modelPack.MotionPacks.AddRange( MotionPacks.Data );
                return modelPack;
            } );
        }

        protected override void OnInitializeView()
        {
            Info = AddNode( new ModelPackInfoNode( "Info", Data.Info ) );
            TexturePack = AddNode( new TexturePackNode( "Textures", Data.TexturePack ) );
            Effects = AddNode( new ListNode<Resource>( "Effects", Data.Effects, ( i, x ) => $"Effect {i}" ) );
            Models = AddNode( new ListNode<Model>( "Models", Data.Models, ( i, x ) => $"Model {i}" ) );
            MotionPacks = AddNode( new ListNode<MotionPack>( "Motion packs", Data.MotionPacks, ( i, x ) => $"Motion pack {i}" ) );
        }
    }
}
