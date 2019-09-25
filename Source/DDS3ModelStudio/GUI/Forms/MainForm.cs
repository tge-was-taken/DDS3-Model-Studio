using System.Windows.Forms;
using DDS3ModelLibrary.Models;
using DDS3ModelStudio.GUI.TreeView;
using DDS3ModelStudio.GUI.TreeView.DataNodes;

namespace DDS3ModelStudio.GUI.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            mDataTreeView.AfterSelect += HandleDataTreeViewAfterSelect;
            mDataTreeView.NodeUpdated += HandleDataTreeViewNodeUpdated;
            var modelPackNode = DataNodeFactory.Create( "player_a.PB", new ModelPack( @"D:\Modding\DDS3\Nocturne\DDS3_OUT\model\field\player_a.PB" ) );
            mDataTreeView.TopNode = new DataTreeNode( modelPackNode );
        }

        private void HandleDataTreeViewNodeUpdated( object sender, TreeViewEventArgs e )
        {
            var node = ( DataTreeNode )e.Node;
            mPropertyGrid.SelectedObject = node.DataNode;
        }

        private void HandleDataTreeViewAfterSelect( object sender, TreeViewEventArgs e )
        {
            var node = ( DataTreeNode )e.Node;
            mPropertyGrid.SelectedObject = node.DataNode;
        }
    }
}
