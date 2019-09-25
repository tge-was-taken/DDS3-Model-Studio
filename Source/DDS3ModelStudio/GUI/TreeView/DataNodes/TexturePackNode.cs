using DDS3ModelLibrary.Textures;
using JetBrains.Annotations;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public class TexturePackNode : ResourceNode<TexturePack>
    {
        public override DataNodeDisplayHint DisplayHint
            => DataNodeDisplayHint.Leaf;

        public override DataNodeAction SupportedActions
            => DataNodeAction.Delete | DataNodeAction.Export | DataNodeAction.Replace | DataNodeAction.Add | DataNodeAction.Move;

        public TexturePackNode( [NotNull] string name, [NotNull] TexturePack data ) : base( name, data )
        {
        }

        protected override void OnInitialize()
        {
            RegisterExportHandler<TexturePack>( Data.Save );
            //RegisterReplaceHandler<ModelPackInfo>( ( filePath ) => new ModelPackInfo( filePath ) );
        }
    }
}