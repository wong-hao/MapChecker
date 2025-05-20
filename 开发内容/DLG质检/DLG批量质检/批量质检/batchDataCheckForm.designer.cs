namespace SMGI.Plugin.CartographicGeneralization
{
    partial class batchDataCheckForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.btOK = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btCancel = new System.Windows.Forms.Button();
            this.cbx_Scale = new System.Windows.Forms.ComboBox();
            this.btnGDB = new System.Windows.Forms.Button();
            this.tbGDBFilePath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tbOutFilePath = new System.Windows.Forms.TextBox();
            this.btnOutputPath = new System.Windows.Forms.Button();
            this.SchemesComboBox = new System.Windows.Forms.ComboBox();
            this.SchemeLabel = new System.Windows.Forms.Label();
            this.CheckerProgressBar = new System.Windows.Forms.ProgressBar();
            this.CheckerProgressLabel = new System.Windows.Forms.Label();
            this.chkToolTreeView = new System.Windows.Forms.TreeView();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btOK);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.btCancel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 544);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(4);
            this.panel1.Size = new System.Drawing.Size(430, 31);
            this.panel1.TabIndex = 14;
            // 
            // btOK
            // 
            this.btOK.Dock = System.Windows.Forms.DockStyle.Right;
            this.btOK.Location = new System.Drawing.Point(289, 4);
            this.btOK.Name = "btOK";
            this.btOK.Size = new System.Drawing.Size(64, 23);
            this.btOK.TabIndex = 7;
            this.btOK.Text = "确定";
            this.btOK.UseVisualStyleBackColor = true;
            this.btOK.Click += new System.EventHandler(this.btOK_Click);
            // 
            // panel2
            // 
            this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel2.Location = new System.Drawing.Point(353, 4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(9, 23);
            this.panel2.TabIndex = 6;
            // 
            // btCancel
            // 
            this.btCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancel.Dock = System.Windows.Forms.DockStyle.Right;
            this.btCancel.Location = new System.Drawing.Point(362, 4);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(64, 23);
            this.btCancel.TabIndex = 5;
            this.btCancel.Text = "取消";
            this.btCancel.UseVisualStyleBackColor = true;
            this.btCancel.Click += new System.EventHandler(this.btCancel_Click);
            // 
            // cbx_Scale
            // 
            this.cbx_Scale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Scale.FormattingEnabled = true;
            this.cbx_Scale.Location = new System.Drawing.Point(14, 72);
            this.cbx_Scale.Name = "cbx_Scale";
            this.cbx_Scale.Size = new System.Drawing.Size(359, 20);
            this.cbx_Scale.TabIndex = 21;
            // 
            // btnGDB
            // 
            this.btnGDB.Location = new System.Drawing.Point(379, 24);
            this.btnGDB.Name = "btnGDB";
            this.btnGDB.Size = new System.Drawing.Size(39, 23);
            this.btnGDB.TabIndex = 20;
            this.btnGDB.Text = "打开";
            this.btnGDB.UseVisualStyleBackColor = true;
            this.btnGDB.Click += new System.EventHandler(this.btnGDB_Click);
            // 
            // tbGDBFilePath
            // 
            this.tbGDBFilePath.Location = new System.Drawing.Point(14, 24);
            this.tbGDBFilePath.Name = "tbGDBFilePath";
            this.tbGDBFilePath.ReadOnly = true;
            this.tbGDBFilePath.Size = new System.Drawing.Size(359, 21);
            this.tbGDBFilePath.TabIndex = 19;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 169);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 17;
            this.label2.Text = "待检查规则";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 12);
            this.label3.TabIndex = 18;
            this.label3.Text = "比例尺";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 16;
            this.label1.Text = "待检查数据库";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 429);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 12);
            this.label5.TabIndex = 27;
            this.label5.Text = "输出路径";
            // 
            // tbOutFilePath
            // 
            this.tbOutFilePath.Location = new System.Drawing.Point(14, 444);
            this.tbOutFilePath.Name = "tbOutFilePath";
            this.tbOutFilePath.ReadOnly = true;
            this.tbOutFilePath.Size = new System.Drawing.Size(359, 21);
            this.tbOutFilePath.TabIndex = 28;
            // 
            // btnOutputPath
            // 
            this.btnOutputPath.Location = new System.Drawing.Point(382, 442);
            this.btnOutputPath.Name = "btnOutputPath";
            this.btnOutputPath.Size = new System.Drawing.Size(39, 23);
            this.btnOutputPath.TabIndex = 20;
            this.btnOutputPath.Text = "选择";
            this.btnOutputPath.UseVisualStyleBackColor = true;
            this.btnOutputPath.Click += new System.EventHandler(this.btnOutputPath_Click);
            // 
            // SchemesComboBox
            // 
            this.SchemesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SchemesComboBox.FormattingEnabled = true;
            this.SchemesComboBox.Location = new System.Drawing.Point(14, 129);
            this.SchemesComboBox.Name = "SchemesComboBox";
            this.SchemesComboBox.Size = new System.Drawing.Size(359, 20);
            this.SchemesComboBox.TabIndex = 30;
            this.SchemesComboBox.SelectedIndexChanged += new System.EventHandler(this.SchemesComboBox_SelectedIndexChanged);
            // 
            // SchemeLabel
            // 
            this.SchemeLabel.AutoSize = true;
            this.SchemeLabel.Location = new System.Drawing.Point(12, 114);
            this.SchemeLabel.Name = "SchemeLabel";
            this.SchemeLabel.Size = new System.Drawing.Size(53, 12);
            this.SchemeLabel.TabIndex = 29;
            this.SchemeLabel.Text = "质检方案";
            // 
            // CheckerProgressBar
            // 
            this.CheckerProgressBar.Location = new System.Drawing.Point(14, 505);
            this.CheckerProgressBar.Name = "CheckerProgressBar";
            this.CheckerProgressBar.Size = new System.Drawing.Size(359, 23);
            this.CheckerProgressBar.TabIndex = 31;
            // 
            // CheckerProgressLabel
            // 
            this.CheckerProgressLabel.AutoSize = true;
            this.CheckerProgressLabel.Location = new System.Drawing.Point(12, 490);
            this.CheckerProgressLabel.Name = "CheckerProgressLabel";
            this.CheckerProgressLabel.Size = new System.Drawing.Size(29, 12);
            this.CheckerProgressLabel.TabIndex = 32;
            this.CheckerProgressLabel.Text = "进度";
            // 
            // chkToolTreeView
            // 
            this.chkToolTreeView.CheckBoxes = true;
            this.chkToolTreeView.Location = new System.Drawing.Point(14, 184);
            this.chkToolTreeView.Name = "chkToolTreeView";
            this.chkToolTreeView.Size = new System.Drawing.Size(359, 230);
            this.chkToolTreeView.TabIndex = 33;
            this.chkToolTreeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.chkToolTreeView_AfterCheck);
            // 
            // batchDataCheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(430, 575);
            this.Controls.Add(this.chkToolTreeView);
            this.Controls.Add(this.CheckerProgressLabel);
            this.Controls.Add(this.CheckerProgressBar);
            this.Controls.Add(this.SchemesComboBox);
            this.Controls.Add(this.SchemeLabel);
            this.Controls.Add(this.tbOutFilePath);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbx_Scale);
            this.Controls.Add(this.btnOutputPath);
            this.Controls.Add(this.btnGDB);
            this.Controls.Add(this.tbGDBFilePath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "batchDataCheckForm";
            this.Text = "批量质检对话框";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.batchDataCheckForm_FormClosing);
            this.Load += new System.EventHandler(this.batchDataCheckForm_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.ComboBox cbx_Scale;
        private System.Windows.Forms.Button btnGDB;
        private System.Windows.Forms.TextBox tbGDBFilePath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbOutFilePath;
        private System.Windows.Forms.Button btnOutputPath;
        private System.Windows.Forms.ComboBox SchemesComboBox;
        private System.Windows.Forms.Label SchemeLabel;
        private System.Windows.Forms.ProgressBar CheckerProgressBar;
        private System.Windows.Forms.Label CheckerProgressLabel;
        private System.Windows.Forms.TreeView chkToolTreeView;


    }
}