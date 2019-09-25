using System.IO;
using JetBrains.Annotations;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public class StreamNode : DataNode<Stream>
    {
        public override DataNodeDisplayHint DisplayHint => DataNodeDisplayHint.Leaf;

        public override DataNodeAction SupportedActions => DataNodeAction.Delete | DataNodeAction.Export | DataNodeAction.Move |
                                                           DataNodeAction.Rename | DataNodeAction.Replace;

        public StreamNode( [NotNull] string name, [NotNull] Stream data ) : base( name, data )
        {
        }
    }
}
