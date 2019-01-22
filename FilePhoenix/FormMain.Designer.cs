namespace FilePhoenix
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.quickStartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.directoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flattenAndReloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsTabPage = new System.Windows.Forms.TabPage();
            this.fileSplitterPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.mainTabPage = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.workingDirectoryListView = new System.Windows.Forms.ListView();
            this.FileHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.fileFragmentInfoBox = new System.Windows.Forms.RichTextBox();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.fileMergerTabPage = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.fileMergerListBox = new System.Windows.Forms.ListBox();
            this.menuStrip2 = new System.Windows.Forms.MenuStrip();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileMergerPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.menuStrip1.SuspendLayout();
            this.optionsTabPage.SuspendLayout();
            this.mainTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.fileMergerTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.menuStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Checked = true;
            this.fileToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.quickStartToolStripMenuItem,
            this.toolStripSeparator1,
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // quickStartToolStripMenuItem
            // 
            this.quickStartToolStripMenuItem.Name = "quickStartToolStripMenuItem";
            this.quickStartToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.quickStartToolStripMenuItem.Text = "Quick Start...";
            this.quickStartToolStripMenuItem.Click += new System.EventHandler(this.quickStartToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(138, 6);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem1,
            this.directoryToolStripMenuItem});
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.openToolStripMenuItem.Text = "Open";
            // 
            // fileToolStripMenuItem1
            // 
            this.fileToolStripMenuItem1.Name = "fileToolStripMenuItem1";
            this.fileToolStripMenuItem1.Size = new System.Drawing.Size(131, 22);
            this.fileToolStripMenuItem1.Text = "File...";
            this.fileToolStripMenuItem1.Click += new System.EventHandler(this.fileToolStripMenuItem_Click);
            // 
            // directoryToolStripMenuItem
            // 
            this.directoryToolStripMenuItem.Name = "directoryToolStripMenuItem";
            this.directoryToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.directoryToolStripMenuItem.Text = "Directory...";
            this.directoryToolStripMenuItem.Click += new System.EventHandler(this.directoryToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.flattenAndReloadToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // flattenAndReloadToolStripMenuItem
            // 
            this.flattenAndReloadToolStripMenuItem.Enabled = false;
            this.flattenAndReloadToolStripMenuItem.Name = "flattenAndReloadToolStripMenuItem";
            this.flattenAndReloadToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.flattenAndReloadToolStripMenuItem.Text = "Flatten and Reload";
            this.flattenAndReloadToolStripMenuItem.Click += new System.EventHandler(this.flattenAndReloadToolStripMenuItem_Click);
            // 
            // optionsTabPage
            // 
            this.optionsTabPage.Controls.Add(this.fileSplitterPropertyGrid);
            this.optionsTabPage.Location = new System.Drawing.Point(4, 22);
            this.optionsTabPage.Name = "optionsTabPage";
            this.optionsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.optionsTabPage.Size = new System.Drawing.Size(792, 336);
            this.optionsTabPage.TabIndex = 1;
            this.optionsTabPage.Text = "Options";
            this.optionsTabPage.UseVisualStyleBackColor = true;
            // 
            // fileSplitterPropertyGrid
            // 
            this.fileSplitterPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileSplitterPropertyGrid.LineColor = System.Drawing.SystemColors.ControlDark;
            this.fileSplitterPropertyGrid.Location = new System.Drawing.Point(3, 3);
            this.fileSplitterPropertyGrid.Name = "fileSplitterPropertyGrid";
            this.fileSplitterPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.fileSplitterPropertyGrid.Size = new System.Drawing.Size(786, 330);
            this.fileSplitterPropertyGrid.TabIndex = 0;
            // 
            // mainTabPage
            // 
            this.mainTabPage.Controls.Add(this.splitContainer1);
            this.mainTabPage.Location = new System.Drawing.Point(4, 22);
            this.mainTabPage.Name = "mainTabPage";
            this.mainTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.mainTabPage.Size = new System.Drawing.Size(792, 336);
            this.mainTabPage.TabIndex = 0;
            this.mainTabPage.Text = "Main";
            this.mainTabPage.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.workingDirectoryListView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.fileFragmentInfoBox);
            this.splitContainer1.Size = new System.Drawing.Size(786, 330);
            this.splitContainer1.SplitterDistance = 413;
            this.splitContainer1.TabIndex = 0;
            // 
            // workingDirectoryListView
            // 
            this.workingDirectoryListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FileHeader});
            this.workingDirectoryListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workingDirectoryListView.GridLines = true;
            this.workingDirectoryListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.workingDirectoryListView.Location = new System.Drawing.Point(0, 0);
            this.workingDirectoryListView.MultiSelect = false;
            this.workingDirectoryListView.Name = "workingDirectoryListView";
            this.workingDirectoryListView.ShowGroups = false;
            this.workingDirectoryListView.Size = new System.Drawing.Size(413, 330);
            this.workingDirectoryListView.TabIndex = 0;
            this.workingDirectoryListView.UseCompatibleStateImageBehavior = false;
            this.workingDirectoryListView.View = System.Windows.Forms.View.Details;
            this.workingDirectoryListView.SelectedIndexChanged += new System.EventHandler(this.workingDirectoryListView_SelectedIndexChanged);
            this.workingDirectoryListView.Resize += new System.EventHandler(this.workingDirectoryListView_Resize);
            // 
            // FileHeader
            // 
            this.FileHeader.Text = "Filename";
            this.FileHeader.Width = 409;
            // 
            // fileFragmentInfoBox
            // 
            this.fileFragmentInfoBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileFragmentInfoBox.Location = new System.Drawing.Point(0, 0);
            this.fileFragmentInfoBox.Name = "fileFragmentInfoBox";
            this.fileFragmentInfoBox.ReadOnly = true;
            this.fileFragmentInfoBox.Size = new System.Drawing.Size(369, 330);
            this.fileFragmentInfoBox.TabIndex = 0;
            this.fileFragmentInfoBox.Text = "";
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.mainTabPage);
            this.tabControl.Controls.Add(this.optionsTabPage);
            this.tabControl.Controls.Add(this.fileMergerTabPage);
            this.tabControl.Location = new System.Drawing.Point(0, 24);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(800, 362);
            this.tabControl.TabIndex = 1;
            // 
            // fileMergerTabPage
            // 
            this.fileMergerTabPage.Controls.Add(this.splitContainer2);
            this.fileMergerTabPage.Location = new System.Drawing.Point(4, 22);
            this.fileMergerTabPage.Name = "fileMergerTabPage";
            this.fileMergerTabPage.Size = new System.Drawing.Size(792, 336);
            this.fileMergerTabPage.TabIndex = 2;
            this.fileMergerTabPage.Text = "FileMerger";
            this.fileMergerTabPage.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.fileMergerListBox);
            this.splitContainer2.Panel1.Controls.Add(this.menuStrip2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.fileMergerPropertyGrid);
            this.splitContainer2.Size = new System.Drawing.Size(792, 336);
            this.splitContainer2.SplitterDistance = 365;
            this.splitContainer2.TabIndex = 0;
            // 
            // fileMergerListBox
            // 
            this.fileMergerListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileMergerListBox.FormattingEnabled = true;
            this.fileMergerListBox.Location = new System.Drawing.Point(0, 24);
            this.fileMergerListBox.Name = "fileMergerListBox";
            this.fileMergerListBox.Size = new System.Drawing.Size(365, 312);
            this.fileMergerListBox.TabIndex = 0;
            this.fileMergerListBox.SelectedIndexChanged += new System.EventHandler(this.fileMergerListBox_SelectedIndexChanged);
            // 
            // menuStrip2
            // 
            this.menuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.removeToolStripMenuItem});
            this.menuStrip2.Location = new System.Drawing.Point(0, 0);
            this.menuStrip2.Name = "menuStrip2";
            this.menuStrip2.Size = new System.Drawing.Size(365, 24);
            this.menuStrip2.TabIndex = 1;
            this.menuStrip2.Text = "menuStrip2";
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // fileMergerPropertyGrid
            // 
            this.fileMergerPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileMergerPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.fileMergerPropertyGrid.Name = "fileMergerPropertyGrid";
            this.fileMergerPropertyGrid.Size = new System.Drawing.Size(423, 336);
            this.fileMergerPropertyGrid.TabIndex = 0;
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar1.Location = new System.Drawing.Point(0, 392);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(800, 23);
            this.progressBar1.Step = 1;
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 2;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 415);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.Text = "FilePhoenix";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.optionsTabPage.ResumeLayout(false);
            this.mainTabPage.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.fileMergerTabPage.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.menuStrip2.ResumeLayout(false);
            this.menuStrip2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem quickStartToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem directoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem flattenAndReloadToolStripMenuItem;
        private System.Windows.Forms.TabPage optionsTabPage;
        private System.Windows.Forms.PropertyGrid fileSplitterPropertyGrid;
        private System.Windows.Forms.TabPage mainTabPage;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView workingDirectoryListView;
        private System.Windows.Forms.ColumnHeader FileHeader;
        private System.Windows.Forms.RichTextBox fileFragmentInfoBox;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TabPage fileMergerTabPage;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox fileMergerListBox;
        private System.Windows.Forms.PropertyGrid fileMergerPropertyGrid;
        private System.Windows.Forms.MenuStrip menuStrip2;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
    }
}

