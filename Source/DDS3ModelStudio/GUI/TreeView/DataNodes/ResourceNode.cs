using System.ComponentModel;
using DDS3ModelLibrary.IO;
using JetBrains.Annotations;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public class ResourceNode<T> : DataNode<T> where T : Resource
    {
        public override DataNodeDisplayHint DisplayHint 
            => DataNodeDisplayHint.Leaf;

        public override DataNodeAction SupportedActions 
            => DataNodeAction.Delete | DataNodeAction.Export | DataNodeAction.Move | DataNodeAction.Replace;

        [DisplayName( "User ID")]
        public ushort UserId
        {
            get => GetDataProperty<ushort>();
            set => SetDataProperty( value );
        }

        public ResourceNode( [NotNull] string name, [NotNull] T data ) : base( name, data )
        {
        }
    }
}