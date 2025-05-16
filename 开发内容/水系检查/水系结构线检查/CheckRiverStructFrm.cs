using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SMGI.Plugin.CartographicGeneralization
{
    public partial class CheckRiverStructFrm : Form
    {
        public bool BNonRiverStruct
        {
            get
            {
                return cbCheckNonStruct.Checked;
            }
        }

        public bool BRiverStruct
        {
            get
            {
                return cbCheckStruct.Checked;
            }
        }

        public bool BIgnoreSmall
        {
            get
            {
                return cbCheckIgnoreSmall.Checked;
            }
        }

        public CheckRiverStructFrm()
        {
            InitializeComponent();
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
