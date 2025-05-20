using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SMGI.Common;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessor;
using System.Windows.Forms;

namespace SMGI.Plugin.CartographicGeneralization
{
    public class SchemeConfigurationCmd : SMGICommand
    {
        private Geoprocessor gp;

        public SchemeConfigurationCmd()
        {
            m_caption = "方案管理";
        }
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

            try
            {
                SchemeConfiguration form = new SchemeConfiguration(schemesPath);
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                   
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
