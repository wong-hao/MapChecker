namespace SMGI.Plugin.CartographicGeneralization
{
    partial class CheckRiverStructFrm
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
            this.cbCheckIgnoreSmall = new System.Windows.Forms.CheckBox();
            this.cbCheckNonStruct = new System.Windows.Forms.CheckBox();
            this.cbCheckStruct = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btOK);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.btCancel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 111);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(4);
            this.panel1.Size = new System.Drawing.Size(302, 31);
            this.panel1.TabIndex = 9;
            // 
            // btOK
            // 
            this.btOK.Dock = System.Windows.Forms.DockStyle.Right;
            this.btOK.Location = new System.Drawing.Point(161, 4);
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
            this.panel2.Location = new System.Drawing.Point(225, 4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(9, 23);
            this.panel2.TabIndex = 6;
            // 
            // btCancel
            // 
            this.btCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancel.Dock = System.Windows.Forms.DockStyle.Right;
            this.btCancel.Location = new System.Drawing.Point(234, 4);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(64, 23);
            this.btCancel.TabIndex = 5;
            this.btCancel.Text = "取消";
            this.btCancel.UseVisualStyleBackColor = true;
            // 
            // cbCheckIgnoreSmall
            // 
            this.cbCheckIgnoreSmall.AutoSize = true;
            this.cbCheckIgnoreSmall.Checked = true;
            this.cbCheckIgnoreSmall.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbCheckIgnoreSmall.Location = new System.Drawing.Point(12, 79);
            this.cbCheckIgnoreSmall.Name = "cbCheckIgnoreSmall";
            this.cbCheckIgnoreSmall.Size = new System.Drawing.Size(132, 16);
            this.cbCheckIgnoreSmall.TabIndex = 10;
            this.cbCheckIgnoreSmall.Text = "忽略小于容差的部分";
            this.cbCheckIgnoreSmall.UseVisualStyleBackColor = true;
            // 
            // cbCheckNonStruct
            // 
            this.cbCheckNonStruct.AutoSize = true;
            this.cbCheckNonStruct.Checked = true;
            this.cbCheckNonStruct.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbCheckNonStruct.Location = new System.Drawing.Point(12, 12);
            this.cbCheckNonStruct.Name = "cbCheckNonStruct";
            this.cbCheckNonStruct.Size = new System.Drawing.Size(180, 16);
            this.cbCheckNonStruct.TabIndex = 10;
            this.cbCheckNonStruct.Text = "检查水系面中的非水系结构线";
            this.cbCheckNonStruct.UseVisualStyleBackColor = true;
            // 
            // cbCheckStruct
            // 
            this.cbCheckStruct.AutoSize = true;
            this.cbCheckStruct.Checked = true;
            this.cbCheckStruct.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbCheckStruct.Location = new System.Drawing.Point(12, 44);
            this.cbCheckStruct.Name = "cbCheckStruct";
            this.cbCheckStruct.Size = new System.Drawing.Size(168, 16);
            this.cbCheckStruct.TabIndex = 10;
            this.cbCheckStruct.Text = "检查水系面外的水系结构线";
            this.cbCheckStruct.UseVisualStyleBackColor = true;
            // 
            // CheckRiverStructFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(302, 142);
            this.Controls.Add(this.cbCheckNonStruct);
            this.Controls.Add(this.cbCheckStruct);
            this.Controls.Add(this.cbCheckIgnoreSmall);
            this.Controls.Add(this.panel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CheckRiverStructFrm";
            this.Text = "水系线面检查";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.CheckBox cbCheckIgnoreSmall;
        private System.Windows.Forms.CheckBox cbCheckNonStruct;
        private System.Windows.Forms.CheckBox cbCheckStruct;
    }
}