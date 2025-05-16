using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.DataSourcesGDB;
using SMGI.Common;
using ESRI.ArcGIS.Geometry;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;

namespace SMGI.Plugin.CartographicGeneralization
{
    public class CheckPolygonGapCmd : SMGI.Common.SMGICommand
    {
        public override bool Enabled
        {
            get
            {
                return m_Application.Workspace != null && m_Application.EngineEditor.EditState == esriEngineEditState.esriEngineStateNotEditing;
            }
        }
        public override void OnClick()
        {
            CheckPolygonGapFrm frm = new CheckPolygonGapFrm(m_Application);
            if (DialogResult.OK == frm.ShowDialog())
            {
                List<string> resultFileNames = new List<string>();

                List<string> lnList = frm.PolygonLayerNames;
                string err = "";
                foreach (var layerName in lnList)
                {
                    IFeatureClass pFC = (m_Application.Workspace.LayerManager.GetLayer(
                        l => (l is IGeoFeatureLayer) && ((l as IGeoFeatureLayer).Name.ToUpper() == layerName.ToUpper())).FirstOrDefault() as IFeatureLayer).FeatureClass;
                    string outPutFileName = frm.OutFilePath + string.Format("\\面缝隙检查_{0}.shp", pFC.AliasName);

                    
                    using (var wo = m_Application.SetBusy())
                    {

                        CheckPolygonGap ck = new CheckPolygonGap();
                        string r = ck.DoCheck(outPutFileName, pFC, wo);
                        if (r != "")
                        {
                            err += r + "\n";
                        }
                    }

                    IFeatureClass fc = CheckHelper.OpenSHPFile(outPutFileName);
                    int count = fc.FeatureCount(null);
                    if(count > 0)
                    {
                        resultFileNames.Add(outPutFileName);
                    }
                }

                if (err != "")
                {
                    MessageBox.Show(err);
                }

                if (resultFileNames.Count > 0)
                {
                    if (MessageBox.Show("是否加载检查结果数据到地图？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        foreach(var outPutFileName in resultFileNames)
                        {
                            CheckHelper.AddTempLayerFromSHPFile(m_Application.Workspace.LayerManager.Map, outPutFileName);
                        }
                    }
                }

                MessageBox.Show("检查完毕！");
                
            }
        }

       
    }
}
