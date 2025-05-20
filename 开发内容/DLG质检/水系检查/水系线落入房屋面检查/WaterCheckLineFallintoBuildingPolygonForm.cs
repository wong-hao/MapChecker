using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using SMGI.Common;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.TrackingAnalyst;

namespace SMGI.Plugin.CartographicGeneralization
{
    public partial class WaterCheckLineFallintoBuildingPolygonForm : Form
    {
        public enum CheckType
        {
            INTERSECTS,//线落入面
            NOTWITHIN//线未被面完全包含
        }

        #region 属性
        public CheckType CheckLineFallintoPolygonType
        {
            get
            {
                return ((KeyValuePair<CheckType, string>)cmbCheckType.SelectedItem).Key;
            }
        }

        public IFeatureClass LineFeatureClass
        {
            get
            {
                return _lineFeatureClass;
            }
        }
        private IFeatureClass _lineFeatureClass;

        public string LineFilterString
        {
            get
            {
                return tbLineFilterString.Text;
            }
        }

        public IFeatureClass AreaFeatureClass
        {
            get
            {
                return _areaFeatureClass;
            }
        }
        private IFeatureClass _areaFeatureClass;

        public string AreaFilterString
        {
            get
            {
                return tbAreaFilterString.Text;
            }
        }

        public string ResultOutputFilePath
        {
            get
            {
                return tbOutFilePath.Text;
            }
        }
        #endregion

        private GApplication _app;
        public WaterCheckLineFallintoBuildingPolygonForm(GApplication app)
        {
            InitializeComponent();

            _app = app;
        }

        private void CheckLineFallintoPolygonForm_Load(object sender, EventArgs e)
        {
            //初始化检查类型
            cmbCheckType.ValueMember = "Key";
            cmbCheckType.DisplayMember = "Value";
            cmbCheckType.Items.Add(new KeyValuePair<CheckType, string>(CheckType.INTERSECTS, "线落入面"));
            cmbCheckType.Items.Add(new KeyValuePair<CheckType, string>(CheckType.NOTWITHIN, "线未被面完全包含"));
            cmbCheckType.SelectedIndex = 0;

            //初始化线图层
            cbLineLayerName.ValueMember = "Key";
            cbLineLayerName.DisplayMember = "Value";
            var lineLayers = _app.Workspace.LayerManager.GetLayer(l => (l is IGeoFeatureLayer) && ((l as IGeoFeatureLayer).FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline));
            foreach (var lyr in lineLayers)
            {
                IFeatureLayer feLayer = lyr as IFeatureLayer;
                if (feLayer.FeatureClass == null)
                    continue;//空图层

                if ((feLayer.FeatureClass as IDataset).Workspace.PathName != _app.Workspace.EsriWorkspace.PathName)
                    continue;//临时数据

                cbLineLayerName.Items.Add(new KeyValuePair<ILayer, string>(feLayer, feLayer.Name));
            }
            if (cbLineLayerName.Items.Count > 0)
            {
                cbLineLayerName.SelectedIndex = 0;
            }


            //初始化面图层
            cbAreaLayerName.ValueMember = "Key";
            cbAreaLayerName.DisplayMember = "Value";
            var areaLayers = _app.Workspace.LayerManager.GetLayer(l => (l is IGeoFeatureLayer) && ((l as IGeoFeatureLayer).FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon));
            foreach (var lyr in areaLayers)
            {
                IFeatureLayer feLayer = lyr as IFeatureLayer;
                if (feLayer.FeatureClass == null)
                    continue;//空图层

                if ((feLayer.FeatureClass as IDataset).Workspace.PathName != _app.Workspace.EsriWorkspace.PathName)
                    continue;//临时数据

                cbAreaLayerName.Items.Add(new KeyValuePair<ILayer, string>(feLayer, feLayer.Name));
            }
            if (cbAreaLayerName.Items.Count > 0)
            {
                cbAreaLayerName.SelectedIndex = 0;
            }

            tbOutFilePath.Text = OutputSetup.GetDir();
        }

        private void btnLineQueryBuilder_Click(object sender, EventArgs e)
        {
            if (cbLineLayerName.SelectedIndex == -1)
            {
                MessageBox.Show("请先指定线图层！");
                return;
            }

            var item = (KeyValuePair<ILayer, string>)cbLineLayerName.SelectedItem;
            IQueryBuilder qBuilder = new QueryBuilderClass();
            qBuilder.Layer = item.Key;
            qBuilder.WhereClause = tbLineFilterString.Text;
            qBuilder.DoModal(this.Handle.ToInt32());

            tbLineFilterString.Text = qBuilder.WhereClause;

        }

        private void btnAreaQueryBuilder_Click(object sender, EventArgs e)
        {
            if (cbAreaLayerName.SelectedIndex == -1)
            {
                MessageBox.Show("请先指定线图层！");
                return;
            }

            var item = (KeyValuePair<ILayer, string>)cbAreaLayerName.SelectedItem;
            IQueryBuilder qBuilder = new QueryBuilderClass();
            qBuilder.Layer = item.Key;
            qBuilder.WhereClause = tbAreaFilterString.Text;
            qBuilder.DoModal(this.Handle.ToInt32());

            tbAreaFilterString.Text = qBuilder.WhereClause;
        }

        private void btnOutputPath_Click(object sender, EventArgs e)
        {
            var fd = new FolderBrowserDialog();
            if (fd.ShowDialog() == DialogResult.OK && fd.SelectedPath.Length > 0)
            {
                btnOutputPath.Text = fd.SelectedPath;
            }
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            if (cbLineLayerName.SelectedIndex == -1)
            {
                MessageBox.Show("请指定线图层！");
                return;
            }
            var item = (KeyValuePair<ILayer, string>)cbLineLayerName.SelectedItem;
            _lineFeatureClass = (item.Key as IFeatureLayer).FeatureClass;

            if (cbAreaLayerName.SelectedIndex == -1)
            {
                MessageBox.Show("请指定面图层！");
                return;
            }
            item = (KeyValuePair<ILayer, string>)cbAreaLayerName.SelectedItem;
            _areaFeatureClass = (item.Key as IFeatureLayer).FeatureClass;

            //验证过滤条件是否合法
            if (tbLineFilterString.Text != "")
            {
                IQueryFilter qf = new QueryFilterClass();
                qf.WhereClause = tbLineFilterString.Text;

                try
                {
                    int feCount = _lineFeatureClass.FeatureCount(qf);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    System.Diagnostics.Trace.WriteLine(ex.Source);
                    System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                    MessageBox.Show(string.Format("线图层的过滤条件输入不合法：{0}", ex.Message));
                    return;
                }
            }
            if (tbAreaFilterString.Text != "")
            {
                IQueryFilter qf = new QueryFilterClass();
                qf.WhereClause = tbAreaFilterString.Text;

                try
                {
                    int feCount = _areaFeatureClass.FeatureCount(qf);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    System.Diagnostics.Trace.WriteLine(ex.Source);
                    System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                    MessageBox.Show(string.Format("面图层的过滤条件输入不合法：{0}", ex.Message));
                    return;
                }
            }

            if (tbOutFilePath.Text == "")
            {
                MessageBox.Show("请指定检查结果输出路径！");
                return;
            }

            DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }

    
}
