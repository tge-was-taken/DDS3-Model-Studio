using DDS3ModelLibrary.Models;
using DDS3ModelStudio.GUI.TreeView;
using System.IO;
using System.Windows.Forms;

namespace DDS3ModelStudio.GUI.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            mDataTreeView.AfterSelect += HandleDataTreeViewAfterSelect;
            mDataTreeView.NodeUpdated += HandleDataTreeViewNodeUpdated;
        }

        private void HandleDataTreeViewNodeUpdated(object sender, TreeViewEventArgs e)
        {
            var node = (DataTreeNode)e.Node;
            mPropertyGrid.SelectedObject = node.DataNode;
        }

        private void HandleDataTreeViewAfterSelect(object sender, TreeViewEventArgs e)
        {
            var node = (DataTreeNode)e.Node;
            mPropertyGrid.SelectedObject = node.DataNode;
        }

        private void MOpenToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                var modelPackNode = DataNodeFactory.Create(Path.GetFileName(dlg.FileName), new ModelPack(dlg.FileName));
                mDataTreeView.TopNode = new DataTreeNode(modelPackNode);
            }
        }
    }
}
