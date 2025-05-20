namespace SMGI.Plugin.CartographicGeneralization
{
    partial class OperatorConfiguration
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
            this.OperatorsDataGridView = new System.Windows.Forms.DataGridView();
            this.SaveButton = new System.Windows.Forms.Button();
            this.OperatorsLabel = new System.Windows.Forms.Label();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.OperatorLabel = new System.Windows.Forms.Label();
            this.FCName1TextBox = new System.Windows.Forms.TextBox();
            this.FilterString1TextBox = new System.Windows.Forms.TextBox();
            this.Addin1TextBox = new System.Windows.Forms.TextBox();
            this.FCName2TextBox = new System.Windows.Forms.TextBox();
            this.FilterString2TextBox = new System.Windows.Forms.TextBox();
            this.Addin2TextBox = new System.Windows.Forms.TextBox();
            this.RelationshipTextBox = new System.Windows.Forms.TextBox();
            this.NotesTextBox = new System.Windows.Forms.TextBox();
            this.MinWidthTextBox = new System.Windows.Forms.TextBox();
            this.AddButton = new System.Windows.Forms.Button();
            this.FCName1Label = new System.Windows.Forms.Label();
            this.FilterString1Label = new System.Windows.Forms.Label();
            this.Addin1Label = new System.Windows.Forms.Label();
            this.FCName2Label = new System.Windows.Forms.Label();
            this.FilterString2Label = new System.Windows.Forms.Label();
            this.Addin2Label = new System.Windows.Forms.Label();
            this.MinWidthTextBoxLabel = new System.Windows.Forms.Label();
            this.NotesTextBoxLabel = new System.Windows.Forms.Label();
            this.RelationshipLabel = new System.Windows.Forms.Label();
            this.EditButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.OperatorsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // OperatorsDataGridView
            // 
            this.OperatorsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OperatorsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.OperatorsDataGridView.Location = new System.Drawing.Point(26, 131);
            this.OperatorsDataGridView.Name = "OperatorsDataGridView";
            this.OperatorsDataGridView.RowTemplate.Height = 23;
            this.OperatorsDataGridView.Size = new System.Drawing.Size(908, 544);
            this.OperatorsDataGridView.TabIndex = 0;
            this.OperatorsDataGridView.SelectionChanged += new System.EventHandler(this.OperatorsDataGridView_SelectionChanged);
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.Location = new System.Drawing.Point(861, 684);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(75, 23);
            this.SaveButton.TabIndex = 1;
            this.SaveButton.Text = "保存";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.YesButton_Click);
            // 
            // OperatorsLabel
            // 
            this.OperatorsLabel.AutoSize = true;
            this.OperatorsLabel.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.OperatorsLabel.ForeColor = System.Drawing.Color.Red;
            this.OperatorsLabel.Location = new System.Drawing.Point(24, 109);
            this.OperatorsLabel.Name = "OperatorsLabel";
            this.OperatorsLabel.Size = new System.Drawing.Size(89, 19);
            this.OperatorsLabel.TabIndex = 2;
            this.OperatorsLabel.Text = "算子属性";
            // 
            // DeleteButton
            // 
            this.DeleteButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.DeleteButton.Location = new System.Drawing.Point(230, 684);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(75, 23);
            this.DeleteButton.TabIndex = 3;
            this.DeleteButton.Text = "删除";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // OperatorLabel
            // 
            this.OperatorLabel.AutoSize = true;
            this.OperatorLabel.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.OperatorLabel.ForeColor = System.Drawing.Color.Red;
            this.OperatorLabel.Location = new System.Drawing.Point(22, 7);
            this.OperatorLabel.Name = "OperatorLabel";
            this.OperatorLabel.Size = new System.Drawing.Size(49, 19);
            this.OperatorLabel.TabIndex = 4;
            this.OperatorLabel.Text = "算子";
            // 
            // FCName1TextBox
            // 
            this.FCName1TextBox.Location = new System.Drawing.Point(121, 28);
            this.FCName1TextBox.Name = "FCName1TextBox";
            this.FCName1TextBox.Size = new System.Drawing.Size(100, 21);
            this.FCName1TextBox.TabIndex = 5;
            // 
            // FilterString1TextBox
            // 
            this.FilterString1TextBox.Location = new System.Drawing.Point(121, 55);
            this.FilterString1TextBox.Name = "FilterString1TextBox";
            this.FilterString1TextBox.Size = new System.Drawing.Size(100, 21);
            this.FilterString1TextBox.TabIndex = 6;
            // 
            // Addin1TextBox
            // 
            this.Addin1TextBox.Location = new System.Drawing.Point(121, 82);
            this.Addin1TextBox.Name = "Addin1TextBox";
            this.Addin1TextBox.Size = new System.Drawing.Size(100, 21);
            this.Addin1TextBox.TabIndex = 7;
            // 
            // FCName2TextBox
            // 
            this.FCName2TextBox.Location = new System.Drawing.Point(498, 31);
            this.FCName2TextBox.Name = "FCName2TextBox";
            this.FCName2TextBox.Size = new System.Drawing.Size(100, 21);
            this.FCName2TextBox.TabIndex = 9;
            // 
            // FilterString2TextBox
            // 
            this.FilterString2TextBox.Location = new System.Drawing.Point(498, 58);
            this.FilterString2TextBox.Name = "FilterString2TextBox";
            this.FilterString2TextBox.Size = new System.Drawing.Size(100, 21);
            this.FilterString2TextBox.TabIndex = 10;
            // 
            // Addin2TextBox
            // 
            this.Addin2TextBox.Location = new System.Drawing.Point(498, 85);
            this.Addin2TextBox.Name = "Addin2TextBox";
            this.Addin2TextBox.Size = new System.Drawing.Size(100, 21);
            this.Addin2TextBox.TabIndex = 11;
            // 
            // RelationshipTextBox
            // 
            this.RelationshipTextBox.Location = new System.Drawing.Point(834, 28);
            this.RelationshipTextBox.Name = "RelationshipTextBox";
            this.RelationshipTextBox.Size = new System.Drawing.Size(100, 21);
            this.RelationshipTextBox.TabIndex = 12;
            // 
            // NotesTextBox
            // 
            this.NotesTextBox.Location = new System.Drawing.Point(834, 55);
            this.NotesTextBox.Name = "NotesTextBox";
            this.NotesTextBox.Size = new System.Drawing.Size(100, 21);
            this.NotesTextBox.TabIndex = 13;
            // 
            // MinWidthTextBox
            // 
            this.MinWidthTextBox.Location = new System.Drawing.Point(834, 82);
            this.MinWidthTextBox.Name = "MinWidthTextBox";
            this.MinWidthTextBox.Size = new System.Drawing.Size(100, 21);
            this.MinWidthTextBox.TabIndex = 14;
            // 
            // AddButton
            // 
            this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddButton.Location = new System.Drawing.Point(28, 684);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 15;
            this.AddButton.Text = "增加";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // FCName1Label
            // 
            this.FCName1Label.AutoSize = true;
            this.FCName1Label.Location = new System.Drawing.Point(26, 31);
            this.FCName1Label.Name = "FCName1Label";
            this.FCName1Label.Size = new System.Drawing.Size(53, 12);
            this.FCName1Label.TabIndex = 16;
            this.FCName1Label.Text = "FCName1:";
            // 
            // FilterString1Label
            // 
            this.FilterString1Label.AutoSize = true;
            this.FilterString1Label.Location = new System.Drawing.Point(26, 58);
            this.FilterString1Label.Name = "FilterString1Label";
            this.FilterString1Label.Size = new System.Drawing.Size(89, 12);
            this.FilterString1Label.TabIndex = 17;
            this.FilterString1Label.Text = "FilterString1:";
            // 
            // Addin1Label
            // 
            this.Addin1Label.AutoSize = true;
            this.Addin1Label.Location = new System.Drawing.Point(26, 85);
            this.Addin1Label.Name = "Addin1Label";
            this.Addin1Label.Size = new System.Drawing.Size(47, 12);
            this.Addin1Label.TabIndex = 18;
            this.Addin1Label.Text = "Addin1:";
            // 
            // FCName2Label
            // 
            this.FCName2Label.AutoSize = true;
            this.FCName2Label.Location = new System.Drawing.Point(403, 34);
            this.FCName2Label.Name = "FCName2Label";
            this.FCName2Label.Size = new System.Drawing.Size(53, 12);
            this.FCName2Label.TabIndex = 19;
            this.FCName2Label.Text = "FCName2:";
            // 
            // FilterString2Label
            // 
            this.FilterString2Label.AutoSize = true;
            this.FilterString2Label.Location = new System.Drawing.Point(403, 58);
            this.FilterString2Label.Name = "FilterString2Label";
            this.FilterString2Label.Size = new System.Drawing.Size(89, 12);
            this.FilterString2Label.TabIndex = 20;
            this.FilterString2Label.Text = "FilterString2:";
            // 
            // Addin2Label
            // 
            this.Addin2Label.AutoSize = true;
            this.Addin2Label.Location = new System.Drawing.Point(403, 88);
            this.Addin2Label.Name = "Addin2Label";
            this.Addin2Label.Size = new System.Drawing.Size(47, 12);
            this.Addin2Label.TabIndex = 21;
            this.Addin2Label.Text = "Addin2:";
            // 
            // MinWidthTextBoxLabel
            // 
            this.MinWidthTextBoxLabel.AutoSize = true;
            this.MinWidthTextBoxLabel.Location = new System.Drawing.Point(713, 85);
            this.MinWidthTextBoxLabel.Name = "MinWidthTextBoxLabel";
            this.MinWidthTextBoxLabel.Size = new System.Drawing.Size(101, 12);
            this.MinWidthTextBoxLabel.TabIndex = 24;
            this.MinWidthTextBoxLabel.Text = "MinWidthTextBox:";
            // 
            // NotesTextBoxLabel
            // 
            this.NotesTextBoxLabel.AutoSize = true;
            this.NotesTextBoxLabel.Location = new System.Drawing.Point(713, 58);
            this.NotesTextBoxLabel.Name = "NotesTextBoxLabel";
            this.NotesTextBoxLabel.Size = new System.Drawing.Size(113, 12);
            this.NotesTextBoxLabel.TabIndex = 23;
            this.NotesTextBoxLabel.Text = "NotesTextBoxLabel:";
            // 
            // RelationshipLabel
            // 
            this.RelationshipLabel.AutoSize = true;
            this.RelationshipLabel.Location = new System.Drawing.Point(713, 31);
            this.RelationshipLabel.Name = "RelationshipLabel";
            this.RelationshipLabel.Size = new System.Drawing.Size(83, 12);
            this.RelationshipLabel.TabIndex = 22;
            this.RelationshipLabel.Text = "Relationship:";
            // 
            // EditButton
            // 
            this.EditButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.EditButton.Location = new System.Drawing.Point(451, 684);
            this.EditButton.Name = "EditButton";
            this.EditButton.Size = new System.Drawing.Size(75, 23);
            this.EditButton.TabIndex = 25;
            this.EditButton.Text = "修改";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
            // 
            // OperatorConfiguration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(967, 716);
            this.Controls.Add(this.EditButton);
            this.Controls.Add(this.MinWidthTextBoxLabel);
            this.Controls.Add(this.NotesTextBoxLabel);
            this.Controls.Add(this.RelationshipLabel);
            this.Controls.Add(this.Addin2Label);
            this.Controls.Add(this.FilterString2Label);
            this.Controls.Add(this.FCName2Label);
            this.Controls.Add(this.Addin1Label);
            this.Controls.Add(this.FilterString1Label);
            this.Controls.Add(this.FCName1Label);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.MinWidthTextBox);
            this.Controls.Add(this.NotesTextBox);
            this.Controls.Add(this.RelationshipTextBox);
            this.Controls.Add(this.Addin2TextBox);
            this.Controls.Add(this.FilterString2TextBox);
            this.Controls.Add(this.FCName2TextBox);
            this.Controls.Add(this.Addin1TextBox);
            this.Controls.Add(this.FilterString1TextBox);
            this.Controls.Add(this.FCName1TextBox);
            this.Controls.Add(this.OperatorLabel);
            this.Controls.Add(this.DeleteButton);
            this.Controls.Add(this.OperatorsLabel);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.OperatorsDataGridView);
            this.Name = "OperatorConfiguration";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "算子配置";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OperatorConfiguration_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.OperatorsDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView OperatorsDataGridView;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.Label OperatorsLabel;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Label OperatorLabel;
        private System.Windows.Forms.TextBox FCName1TextBox;
        private System.Windows.Forms.TextBox FilterString1TextBox;
        private System.Windows.Forms.TextBox Addin1TextBox;
        private System.Windows.Forms.TextBox FCName2TextBox;
        private System.Windows.Forms.TextBox FilterString2TextBox;
        private System.Windows.Forms.TextBox Addin2TextBox;
        private System.Windows.Forms.TextBox RelationshipTextBox;
        private System.Windows.Forms.TextBox NotesTextBox;
        private System.Windows.Forms.TextBox MinWidthTextBox;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Label FCName1Label;
        private System.Windows.Forms.Label FilterString1Label;
        private System.Windows.Forms.Label Addin1Label;
        private System.Windows.Forms.Label FCName2Label;
        private System.Windows.Forms.Label FilterString2Label;
        private System.Windows.Forms.Label Addin2Label;
        private System.Windows.Forms.Label MinWidthTextBoxLabel;
        private System.Windows.Forms.Label NotesTextBoxLabel;
        private System.Windows.Forms.Label RelationshipLabel;
        private System.Windows.Forms.Button EditButton;
    }
}