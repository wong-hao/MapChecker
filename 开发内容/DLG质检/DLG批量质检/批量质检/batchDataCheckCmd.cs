using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SMGI.Common;

namespace SMGI.Plugin.CartographicGeneralization
{
    public class batchDataCheckCmd : SMGI.Common.SMGICommand
    {
        public override bool Enabled
        {
            get
            {
                return true;
            }
        }
        public override void OnClick()
        {
            string schemesPath = String.Join("\\", GApplication.Application.Template.Root, "质检");

            var frm = new batchDataCheckForm(m_Application, schemesPath);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();
        }
    }
}
