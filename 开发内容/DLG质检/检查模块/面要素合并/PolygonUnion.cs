using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.ADF.BaseClasses;
using SMGI.Common;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geoprocessing;
using System.Runtime.InteropServices;
using System.Collections;

namespace SMGI.Plugin.CartographicGeneralization
{
    public partial class PolygonUnion : Form
    {
      
        private GApplication _app;
        private Dictionary<string, ILayer> _layerDictionary = new Dictionary<string, ILayer>();
        public ILayer pSelectLayer;
        public esriGeometryType GeoTypeFilter{get;set;}

        private Dictionary<string, List<string>> fieldsDic = new Dictionary<string, List<string>>();
        public ArrayList FieldArray { get; set; }

        public PolygonUnion(GApplication app)
        {
            InitializeComponent();
            _app = app;
            FieldArray = new ArrayList();
        }

        private void GetLayers(ILayer pLayer, esriGeometryType pGeoType, Dictionary<string, ILayer> LayerList)
        {
            if (pLayer is IGroupLayer)
            {
                ICompositeLayer pGroupLayer = (ICompositeLayer)pLayer;
                for (int i = 0; i < pGroupLayer.Count; i++)
                {
                    ILayer SubLayer = pGroupLayer.get_Layer(i);
                    GetLayers(SubLayer, pGeoType, LayerList);
                }
            }
            else
            {
                if (pLayer is IFeatureLayer)
                {
                    IFeatureLayer pFeatLayer = (IFeatureLayer)pLayer;
                    IFeatureClass pFeatClass = pFeatLayer.FeatureClass;
                    if (pFeatClass.ShapeType == pGeoType)
                    {
                        fieldsDic.Add(pLayer.Name, GetFields(pFeatClass));
                        LayerList.Add(pLayer.Name, pLayer);
                    }
                }
            }
        }

        private List<string> GetFields(IFeatureClass fc)
        {
            List<string> ruleNames = new List<string>();
            for (int i = 0; i < fc.Fields.FieldCount; i++)
            {
                var field = fc.Fields.get_Field(i);
                if (field.Type!= esriFieldType.esriFieldTypeGeometry && field.Type!= esriFieldType.esriFieldTypeOID)
                {
                    ruleNames.Add(field.Name);
                }
            }
            return ruleNames;
        }
       
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (clbLayerList.SelectedItem.ToString().Trim() == "")
            {
                MessageBox.Show("图层名不能为空");
                return;
            }

            pSelectLayer = _layerDictionary[clbLayerList.SelectedItem.ToString()];

            for (int i = 0; i < checkedFieldNames.CheckedItems.Count; i++)
            {
                var item = checkedFieldNames.CheckedItems[i];
                FieldArray.Add(item.ToString());
            }

            this.Close();
        }
      

        private void PolygonUnion_Load(object sender, EventArgs e)
        {
            var acv = _app.ActiveView;
            var map = acv.FocusMap;

            for (int i = 0; i < map.LayerCount; i++)
            {
                ILayer pLayer = map.get_Layer(i);
                GetLayers(pLayer, GeoTypeFilter, _layerDictionary);
            }

            foreach (var layerName in _layerDictionary.Keys)
            {
                this.clbLayerList.Items.Add(layerName);
            }

            this.clbLayerList.SelectedIndex = this.clbLayerList.Items.Count > 0 ? 0 : -1;
        }

        private void clbLayerList_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            string layerName = clbLayerList.SelectedItem.ToString();
            checkedFieldNames.Items.Clear();
            checkedFieldNames.Items.AddRange(fieldsDic[layerName].ToArray());
        } 
    }
}
