using System;
using System.Windows.Forms;

namespace DDS3ModelStudio.GUI.TreeView
{
    public class DataTreeView : System.Windows.Forms.TreeView
    {
        public event EventHandler<TreeViewEventArgs> NodeUpdated;

        public new DataTreeNode TopNode
        {
            get
            {
                if ( !DesignMode )
                {
                    return Nodes.Count > 0 ? ( DataTreeNode )Nodes[0] : null;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if ( Nodes.Count > 0 )
                    Nodes[0] = value;
                else if ( value != null )
                    Nodes.Add( value );

                if ( value != null )
                    SubscribeToEvents( value );
            }
        }

        protected override void OnBeforeSelect( TreeViewCancelEventArgs e )
        {
            var node = ( DataTreeNode )e.Node;
            node.OnBeforeSelect( e );
            base.OnBeforeSelect( e );
        }

        protected override void OnBeforeExpand( TreeViewCancelEventArgs e )
        {
            var node = ( DataTreeNode ) e.Node;
            node.OnBeforeExpand( e );
            base.OnBeforeExpand( e );
        }

        protected override void OnNodeMouseClick( TreeNodeMouseClickEventArgs e )
        {
            if ( e.Button == MouseButtons.Right )
                SelectedNode = e.Node;
        }

        private void SubscribeToEvents( DataTreeNode value )
        {
            value.NodeAdded   += HandleNodeAdded;
            value.RemovedNode += HandleNodeRemoved;
            value.NodeMoved   += HandleNodeMoved;
            value.DataChanged += HandleDataChanged;
        }

        private void UnsubscribeToEvents( DataTreeNode value )
        {
            value.NodeAdded   -= HandleNodeAdded;
            value.RemovedNode -= HandleNodeRemoved;
            value.NodeMoved   -= HandleNodeMoved;
            value.DataChanged -= HandleDataChanged;
        }

        private void HandleDataChanged( object sender, DataTreeNode e )
        {
            NodeUpdated?.Invoke( this, new TreeViewEventArgs( e ) );
        }

        private void HandleNodeMoved( object sender, (DataTreeNode Node, int OldIndex, int NewIndex, DataTreeNode MovedNode) e )
        {
        }

        private void HandleNodeRemoved( object sender, (DataTreeNode Node, DataTreeNode RemovedNode) e )
        {
            UnsubscribeToEvents( e.RemovedNode );
        }

        private void HandleNodeAdded( object sender, (DataTreeNode Node, DataTreeNode AddedNode) e )
        {
            SubscribeToEvents( e.AddedNode );
        }

    }
}