namespace DDS3ModelStudio.GUI.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mMainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.mFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mOpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mContentPanel = new System.Windows.Forms.Panel();
            this.mDataTreeView = new DDS3ModelStudio.GUI.TreeView.DataTreeView();
            this.mPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.mMainMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mMainMenuStrip
            // 
            this.mMainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mFileToolStripMenuItem});
            this.mMainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mMainMenuStrip.Name = "mMainMenuStrip";
            this.mMainMenuStrip.Size = new System.Drawing.Size(949, 24);
            this.mMainMenuStrip.TabIndex = 0;
            this.mMainMenuStrip.Text = "mMainMenuStrip";
            // 
            // mFileToolStripMenuItem
            // 
            this.mFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mOpenToolStripMenuItem});
            this.mFileToolStripMenuItem.Name = "mFileToolStripMenuItem";
            this.mFileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.mFileToolStripMenuItem.Text = "File";
            // 
            // mOpenToolStripMenuItem
            // 
            this.mOpenToolStripMenuItem.Name = "mOpenToolStripMenuItem";
            this.mOpenToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.mOpenToolStripMenuItem.Text = "Open";
            // 
            // mContextMenuStrip
            // 
            this.mContextMenuStrip.Name = "mContextMenuStrip";
            this.mContextMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // mContentPanel
            // 
            this.mContentPanel.Location = new System.Drawing.Point(12, 27);
            this.mContentPanel.Name = "mContentPanel";
            this.mContentPanel.Size = new System.Drawing.Size(575, 663);
            this.mContentPanel.TabIndex = 2;
            // 
            // mDataTreeView
            // 
            this.mDataTreeView.Location = new System.Drawing.Point(593, 30);
            this.mDataTreeView.Name = "mDataTreeView";
            this.mDataTreeView.Size = new System.Drawing.Size(344, 315);
            this.mDataTreeView.TabIndex = 0;
            // 
            // mPropertyGrid
            // 
            this.mPropertyGrid.Location = new System.Drawing.Point(593, 351);
            this.mPropertyGrid.Name = "mPropertyGrid";
            this.mPropertyGrid.Size = new System.Drawing.Size(344, 339);
            this.mPropertyGrid.TabIndex = 3;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(949, 702);
            this.Controls.Add(this.mPropertyGrid);
            this.Controls.Add(this.mDataTreeView);
            this.Controls.Add(this.mContentPanel);
            this.Controls.Add(this.mMainMenuStrip);
            this.MainMenuStrip = this.mMainMenuStrip;
            this.Name = "MainForm";
            this.Text = "DDS3 Model Studio";
            this.mMainMenuStrip.ResumeLayout(false);
            this.mMainMenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mMainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem mFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mOpenToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip mContextMenuStrip;
        private System.Windows.Forms.Panel mContentPanel;
        private DDS3ModelStudio.GUI.TreeView.DataTreeView mDataTreeView;
        private System.Windows.Forms.PropertyGrid mPropertyGrid;
    }
}

