using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.DataSourcesGDB;
using SMGI.Common;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;

namespace SMGI.Plugin.CartographicGeneralization
{
    public partial class CheckPolygonGapFrm : Form
    {       

        private GApplication _app;

        //面图层名
        private List<string> _polygonLayerNames;
        public List<string> PolygonLayerNames
        {
            get
            {
                return _polygonLayerNames;
            }
        }

        /// <summary>
        /// 输出路径
        /// </summary>
        public string OutFilePath
        {
            get
            {
                return tbOutFilePath.Text;
            }
        }


        public CheckPolygonGapFrm(GApplication app)
        {
            InitializeComponent();

            _app = app;
            _polygonLayerNames = new List<string>();
        }


        private void CheckPolygonGapFrm_Load(object sender, EventArgs e)
        {
            List<string> lyNames = new List<string>();
            var pPolygonLayers = _app.Workspace.LayerManager.GetLayer(new SMGI.Common.LayerManager.LayerChecker(l =>
                (l is IGeoFeatureLayer) && ((l as IGeoFeatureLayer).FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon))).ToArray();
            for (int i = 0; i < pPolygonLayers.Length; i++)
            {
                IFeatureLayer layer = pPolygonLayers[i] as IFeatureLayer;
                if ((layer.FeatureClass as IDataset).Workspace.PathName != _app.Workspace.EsriWorkspace.PathName)//临时数据不参与
                    continue;

                //if (!layer.FeatureClass.AliasName.Contains("BOUA") && !layer.FeatureClass.AliasName.Contains("HYDA"))
                if (layer.FeatureClass.AliasName[3] !='A')
                    //continue;

                lyNames.Add(layer.Name.ToUpper());
            }
            chkLayerlist.Items.AddRange(lyNames.ToArray());

            tbOutFilePath.Text = OutputSetup.GetDir();
        }

        private void btn_All_Click(object sender, EventArgs e)
        {
            for (var i = 0; i < chkLayerlist.Items.Count; i++)
            {
                chkLayerlist.SetItemChecked(i, true);
            }
        }

        private void btn_Clear_Click(object sender, EventArgs e)
        {
            for (var i = 0; i < chkLayerlist.Items.Count; i++)
            {
                chkLayerlist.SetItemChecked(i, false);
            }
        }

        private void btnFilePath_Click(object sender, EventArgs e)
        {
            var fd = new FolderBrowserDialog();
            if (fd.ShowDialog() == DialogResult.OK && fd.SelectedPath.Length > 0)
            {
                tbOutFilePath.Text = fd.SelectedPath;
            }
        }


        private void btOK_Click(object sender, EventArgs e)
        {
            foreach (var ln in chkLayerlist.CheckedItems)
            {
                _polygonLayerNames.Add(ln as string);
            }
            if (_polygonLayerNames.Count == 0)
            {
                MessageBox.Show("请指定需检查的面图层！");
                return;
            }

            if (tbOutFilePath.Text == "")
            {
                MessageBox.Show("请指定输出路径！");
                return;
            }

            DialogResult = DialogResult.OK;
        }

        

    }
}
