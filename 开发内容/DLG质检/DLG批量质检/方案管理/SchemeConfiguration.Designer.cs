namespace SMGI.Plugin.CartographicGeneralization
{
    partial class SchemeConfiguration
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
            this.AddOperatorButton = new System.Windows.Forms.Button();
            this.RemoveOperatorButton = new System.Windows.Forms.Button();
            this.EditOperatorButton = new System.Windows.Forms.Button();
            this.OperatorLabel = new System.Windows.Forms.Label();
            this.RuleLabel = new System.Windows.Forms.Label();
            this.SchemeLabel = new System.Windows.Forms.Label();
            this.SchemesComboBox = new System.Windows.Forms.ComboBox();
            this.DeleteSchemeButton = new System.Windows.Forms.Button();
            this.AddSchemeTextBox = new System.Windows.Forms.TextBox();
            this.AddSchemeLabel = new System.Windows.Forms.Label();
            this.AddSchemeButton = new System.Windows.Forms.Button();
            this.OperatorsTreeView = new System.Windows.Forms.TreeView();
            this.RulesTreeView = new System.Windows.Forms.TreeView();
            this.YesButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // AddOperatorButton
            // 
            this.AddOperatorButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddOperatorButton.Location = new System.Drawing.Point(25, 280);
            this.AddOperatorButton.Name = "AddOperatorButton";
            this.AddOperatorButton.Size = new System.Drawing.Size(75, 23);
            this.AddOperatorButton.TabIndex = 2;
            this.AddOperatorButton.Text = "添加算子";
            this.AddOperatorButton.UseVisualStyleBackColor = true;
            this.AddOperatorButton.Click += new System.EventHandler(this.AddOperatorButton_Click);
            // 
            // RemoveOperatorButton
            // 
            this.RemoveOperatorButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.RemoveOperatorButton.Location = new System.Drawing.Point(327, 280);
            this.RemoveOperatorButton.Name = "RemoveOperatorButton";
            this.RemoveOperatorButton.Size = new System.Drawing.Size(75, 23);
            this.RemoveOperatorButton.TabIndex = 3;
            this.RemoveOperatorButton.Text = "移除算子";
            this.RemoveOperatorButton.UseVisualStyleBackColor = true;
            this.RemoveOperatorButton.Click += new System.EventHandler(this.RemoveOperatorButton_Click);
            // 
            // EditOperatorButton
            // 
            this.EditOperatorButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.EditOperatorButton.Location = new System.Drawing.Point(408, 280);
            this.EditOperatorButton.Name = "EditOperatorButton";
            this.EditOperatorButton.Size = new System.Drawing.Size(75, 23);
            this.EditOperatorButton.TabIndex = 4;
            this.EditOperatorButton.Text = "编辑算子";
            this.EditOperatorButton.UseVisualStyleBackColor = true;
            this.EditOperatorButton.Click += new System.EventHandler(this.EditOperatorButton_Click);
            // 
            // OperatorLabel
            // 
            this.OperatorLabel.AutoSize = true;
            this.OperatorLabel.Location = new System.Drawing.Point(23, 72);
            this.OperatorLabel.Name = "OperatorLabel";
            this.OperatorLabel.Size = new System.Drawing.Size(53, 12);
            this.OperatorLabel.TabIndex = 5;
            this.OperatorLabel.Text = "检查算子";
            // 
            // RuleLabel
            // 
            this.RuleLabel.AutoSize = true;
            this.RuleLabel.Location = new System.Drawing.Point(325, 72);
            this.RuleLabel.Name = "RuleLabel";
            this.RuleLabel.Size = new System.Drawing.Size(53, 12);
            this.RuleLabel.TabIndex = 6;
            this.RuleLabel.Text = "检查规则";
            // 
            // SchemeLabel
            // 
            this.SchemeLabel.AutoSize = true;
            this.SchemeLabel.Location = new System.Drawing.Point(23, 9);
            this.SchemeLabel.Name = "SchemeLabel";
            this.SchemeLabel.Size = new System.Drawing.Size(53, 12);
            this.SchemeLabel.TabIndex = 7;
            this.SchemeLabel.Text = "检查方案";
            // 
            // SchemesComboBox
            // 
            this.SchemesComboBox.FormattingEnabled = true;
            this.SchemesComboBox.Location = new System.Drawing.Point(25, 27);
            this.SchemesComboBox.Name = "SchemesComboBox";
            this.SchemesComboBox.Size = new System.Drawing.Size(121, 20);
            this.SchemesComboBox.TabIndex = 8;
            this.SchemesComboBox.SelectedIndexChanged += new System.EventHandler(this.SchemesComboBox_SelectedIndexChanged);
            // 
            // DeleteSchemeButton
            // 
            this.DeleteSchemeButton.Location = new System.Drawing.Point(152, 26);
            this.DeleteSchemeButton.Name = "DeleteSchemeButton";
            this.DeleteSchemeButton.Size = new System.Drawing.Size(75, 23);
            this.DeleteSchemeButton.TabIndex = 9;
            this.DeleteSchemeButton.Text = "删除方案";
            this.DeleteSchemeButton.UseVisualStyleBackColor = true;
            this.DeleteSchemeButton.Click += new System.EventHandler(this.DeleteSchemeButton_Click);
            // 
            // AddSchemeTextBox
            // 
            this.AddSchemeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddSchemeTextBox.Location = new System.Drawing.Point(327, 26);
            this.AddSchemeTextBox.Name = "AddSchemeTextBox";
            this.AddSchemeTextBox.Size = new System.Drawing.Size(100, 21);
            this.AddSchemeTextBox.TabIndex = 10;
            // 
            // AddSchemeLabel
            // 
            this.AddSchemeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddSchemeLabel.AutoSize = true;
            this.AddSchemeLabel.Location = new System.Drawing.Point(325, 9);
            this.AddSchemeLabel.Name = "AddSchemeLabel";
            this.AddSchemeLabel.Size = new System.Drawing.Size(53, 12);
            this.AddSchemeLabel.TabIndex = 11;
            this.AddSchemeLabel.Text = "添加方案";
            // 
            // AddSchemeButton
            // 
            this.AddSchemeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddSchemeButton.Location = new System.Drawing.Point(432, 25);
            this.AddSchemeButton.Name = "AddSchemeButton";
            this.AddSchemeButton.Size = new System.Drawing.Size(75, 23);
            this.AddSchemeButton.TabIndex = 12;
            this.AddSchemeButton.Text = "添加方案";
            this.AddSchemeButton.UseVisualStyleBackColor = true;
            this.AddSchemeButton.Click += new System.EventHandler(this.AddSchemeButton_Click);
            // 
            // OperatorsTreeView
            // 
            this.OperatorsTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OperatorsTreeView.Location = new System.Drawing.Point(25, 87);
            this.OperatorsTreeView.Name = "OperatorsTreeView";
            this.OperatorsTreeView.Size = new System.Drawing.Size(244, 187);
            this.OperatorsTreeView.TabIndex = 13;
            // 
            // RulesTreeView
            // 
            this.RulesTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RulesTreeView.Location = new System.Drawing.Point(327, 87);
            this.RulesTreeView.Name = "RulesTreeView";
            this.RulesTreeView.Size = new System.Drawing.Size(254, 187);
            this.RulesTreeView.TabIndex = 14;
            // 
            // YesButton
            // 
            this.YesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.YesButton.Location = new System.Drawing.Point(506, 280);
            this.YesButton.Name = "YesButton";
            this.YesButton.Size = new System.Drawing.Size(75, 23);
            this.YesButton.TabIndex = 15;
            this.YesButton.Text = "确定";
            this.YesButton.UseVisualStyleBackColor = true;
            this.YesButton.Click += new System.EventHandler(this.YesButton_Click);
            // 
            // SchemeConfiguration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 315);
            this.Controls.Add(this.YesButton);
            this.Controls.Add(this.RulesTreeView);
            this.Controls.Add(this.OperatorsTreeView);
            this.Controls.Add(this.AddSchemeButton);
            this.Controls.Add(this.AddSchemeLabel);
            this.Controls.Add(this.AddSchemeTextBox);
            this.Controls.Add(this.DeleteSchemeButton);
            this.Controls.Add(this.SchemesComboBox);
            this.Controls.Add(this.SchemeLabel);
            this.Controls.Add(this.RuleLabel);
            this.Controls.Add(this.OperatorLabel);
            this.Controls.Add(this.EditOperatorButton);
            this.Controls.Add(this.RemoveOperatorButton);
            this.Controls.Add(this.AddOperatorButton);
            this.Name = "SchemeConfiguration";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "方案管理";
            this.Load += new System.EventHandler(this.Configuration_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button AddOperatorButton;
        private System.Windows.Forms.Button RemoveOperatorButton;
        private System.Windows.Forms.Button EditOperatorButton;
        private System.Windows.Forms.Label OperatorLabel;
        private System.Windows.Forms.Label RuleLabel;
        private System.Windows.Forms.Label SchemeLabel;
        private System.Windows.Forms.ComboBox SchemesComboBox;
        private System.Windows.Forms.Button DeleteSchemeButton;
        private System.Windows.Forms.TextBox AddSchemeTextBox;
        private System.Windows.Forms.Label AddSchemeLabel;
        private System.Windows.Forms.Button AddSchemeButton;
        private System.Windows.Forms.TreeView OperatorsTreeView;
        private System.Windows.Forms.TreeView RulesTreeView;
        private System.Windows.Forms.Button YesButton;
    }
}