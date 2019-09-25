using DDS3ModelLibrary.IO.Common;
using JetBrains.Annotations;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public class BinarySerializableNode : DataNode<IBinarySerializable>
    {
        public override DataNodeDisplayHint DisplayHint => DataNodeDisplayHint.Leaf;

        public override DataNodeAction SupportedActions => DataNodeAction.Delete | DataNodeAction.Export | DataNodeAction.Move | DataNodeAction.Replace;

        public BinarySerializableNode( [NotNull] string name, [NotNull] IBinarySerializable data ) : base( name, data )
        {
        }
    }
}
