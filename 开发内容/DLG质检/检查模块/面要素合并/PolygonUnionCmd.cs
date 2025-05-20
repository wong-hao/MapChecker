using System;
using System.Collections.Generic;
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
using System.Data;
using SMGI.Common;
using ESRI.ArcGIS.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geoprocessing;
using System.Collections;
namespace SMGI.Plugin.CartographicGeneralization
{
    public class PolygonUnionCmd : SMGI.Common.SMGICommand
    {
        public PolygonUnionCmd()
        {
            m_category = "面要素合并";
            m_caption = "面要素合并";
        }

        public override bool Enabled
        {
            get
            {
                return m_Application != null && m_Application.Workspace != null && 
                    m_Application.EngineEditor.EditState == esriEngineEditState.esriEngineStateEditing;
            }
        }
        public override void OnClick()
        {
            //FrmPolygonUnion frm = new FrmPolygonUnion();
            //frm.ShowDialog();

            var layerSelector = new PolygonUnion(m_Application);
            layerSelector.GeoTypeFilter = esriGeometryType.esriGeometryPolygon;
            if (layerSelector.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            IFeatureLayer inputFC = layerSelector.pSelectLayer as IFeatureLayer;
            var gp = m_Application.GPTool;

            //打开计时器
            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();



            // using (var wo = m_Application.SetBusy())
            //   {
            //ProcessPseudo(m_Application.Workspace.EsriWorkspace, inputFC, layerSelector.FieldArray);
            ProcessUnion(m_Application.Workspace.EsriWorkspace, inputFC, layerSelector.FieldArray);

        }
        private void ProcessUnion(IWorkspace ws, IFeatureLayer lyr, ArrayList fieldArray)
        {
            //Process pro = new Process();
            WaitOperation wo = m_Application.SetBusy();
            bool isEditing = m_Application.EngineEditor.EditState == esriEngineEditState.esriEngineStateEditing;
            if (!isEditing)
            {
                MessageBox.Show("请先开启编辑！");
                return;
            }

            if (isEditing)
            {
                m_Application.EngineEditor.StartOperation();
            }
            //pro.Show();
            try
            {
                List<int> deleteIDs = new List<int>();
                List<IGeometry> geoList = new List<IGeometry>();
                IQueryFilter qf = new QueryFilterClass();
                qf.WhereClause = (lyr as IFeatureLayerDefinition).DefinitionExpression;
                IFeatureCursor pFeatCursor = lyr.FeatureClass.Search(qf, false);
                IFeature pFeature = pFeatCursor.NextFeature();
                while (pFeature != null)
                {
                    //pro.label1.Text = "正在处理要素" + pFeature.OID;
                    wo.SetText("正在处理要素" + pFeature.OID);
                    System.Windows.Forms.Application.DoEvents();
                    if (deleteIDs.Contains(pFeature.OID))
                    {
                        pFeature = pFeatCursor.NextFeature();
                        continue;
                    }

                    IPolygon Checkpolygon = pFeature.Shape as IPolygon;


                    geoList.Clear();
                    //ITopologicalOperator topo;
                    //topo = Checkpolygon as ITopologicalOperator;
                  
                    ISpatialFilter pFilter = new SpatialFilterClass();
                    pFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    pFilter.WhereClause = (lyr as IFeatureLayerDefinition).DefinitionExpression;
                    pFilter.GeometryField = lyr.FeatureClass.ShapeFieldName;
                    pFilter.Geometry = Checkpolygon;
                    IFeatureCursor pSelectCursor = lyr.FeatureClass.Search(pFilter, false);
                    IFeature SelectFeature = null;
                    while ((SelectFeature = pSelectCursor.NextFeature()) != null)
                    {
                        if (deleteIDs.Contains(SelectFeature.OID))
                        {
                            continue;
                        }

                        if (pFeature.OID != SelectFeature.OID)
                        {
                            bool judge = false;
                            for (int i = 0; i < fieldArray.Count; i++)
                            {
                                int FieldIndex = lyr.FeatureClass.FindField(fieldArray[i].ToString());
                                if (FieldIndex != -1)
                                {
                                    string psfeature = pFeature.get_Value(FieldIndex).ToString().Trim();
                                    string psecfeature = SelectFeature.get_Value(FieldIndex).ToString().Trim();
                                    if (psfeature != psecfeature)
                                    {
                                        judge = true;
                                        break;
                                    }
                                }
                            }
                            if (judge == false)//属性相同
                            {
                                deleteIDs.Add(SelectFeature.OID);
                                geoList.Add(SelectFeature.ShapeCopy);
                                SelectFeature.Delete();
                            }
                        }
                       
                    }
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(pSelectCursor);
                    if (geoList.Count > 0)
                    {
                        foreach (var geo in geoList)
                        {
                            pFeature.Shape = (pFeature.Shape as ITopologicalOperator).Union(geo);
                        }
                        pFeature.Store();

                        continue;//新的要素，继续判断
                    }
                    else
                    {
                        pFeature = pFeatCursor.NextFeature();
                    }
                }
                Marshal.ReleaseComObject(pFeatCursor);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.Source);
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                if (isEditing)
                {
                    MessageBox.Show(ex.Message);

                    m_Application.EngineEditor.AbortOperation();
                }
            }

            if (isEditing)
            {
                m_Application.EngineEditor.StopOperation("面合并处理");
            }
            //pro.Close();
            wo.Dispose();
        }


    }
}
