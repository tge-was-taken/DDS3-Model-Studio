using System.Collections.Generic;
using System.ComponentModel;
using DDS3ModelLibrary.Models;
using JetBrains.Annotations;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public class ModelPackEffectInfoNode : DataNode<ModelPackEffectInfo>
    {
        public override DataNodeDisplayHint DisplayHint
            => DataNodeDisplayHint.Leaf;

        public override DataNodeAction SupportedActions
            => DataNodeAction.Delete | DataNodeAction.Export | DataNodeAction.Replace | DataNodeAction.Move;


        [DisplayName( "ID")]
        public short Id
        {
            get => GetDataProperty<short>();
            set => SetDataProperty( value );
        }

        public List<short> Fields 
            => GetDataProperty<List<short>>();

        public ModelPackEffectInfoNode( [NotNull] string name, [NotNull] ModelPackEffectInfo data ) : base( name, data )
        {
        }

        protected override void OnInitialize()
        {
            //RegisterExportHandler<ModelPackEffectInfo>( Data.Save );
            //RegisterReplaceHandler<ModelPackEffectInfo>( ( filePath ) => new ModelPackInfo( filePath ) );
        }
    }
}