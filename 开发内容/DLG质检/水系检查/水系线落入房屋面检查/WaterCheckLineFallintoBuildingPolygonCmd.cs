using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SMGI.Common;
using ESRI.ArcGIS.Geodatabase;
using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesFile;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Carto;
using System.IO;

namespace SMGI.Plugin.CartographicGeneralization
{
    /// <summary>
    /// 水系线落入房屋面检查
    /// </summary>
    public class WaterCheckLineFallintoBuildingPolygonCmd : SMGICommand
    {
        public override bool Enabled
        {
            get
            {
                return m_Application != null &&
                       m_Application.Workspace != null;
            }
        }

        public override void OnClick()
        {
            var frm = new WaterCheckLineFallintoBuildingPolygonForm(m_Application);
            frm.StartPosition = FormStartPosition.CenterParent;

            if (frm.ShowDialog() != DialogResult.OK)
                return;

            string outputFileName = OutputSetup.GetDir() + string.Format("\\水系线落入房屋面检查_{0}.shp", frm.LineFeatureClass.AliasName);


            string err = "";
            using (var wo = m_Application.SetBusy())
            {
                err = DoCheck(outputFileName, frm.CheckLineFallintoPolygonType, frm.LineFeatureClass, frm.LineFilterString, frm.AreaFeatureClass, frm.AreaFilterString, wo);
            }

            if (err == "")
            {
                if (File.Exists(outputFileName))
                {
                    if (MessageBox.Show("是否加载检查结果数据到地图？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        IFeatureClass errFC = CheckHelper.OpenSHPFile(outputFileName);
                        CheckHelper.AddTempLayerToMap(m_Application.Workspace.LayerManager.Map, errFC);
                    }
                }
                else
                {
                    MessageBox.Show("检查完毕，没有发现非法线要素！");
                }
            }
            else
            {
                MessageBox.Show(err);
            }

        }
        
        /// <summary>
        /// 水系线落入房屋面检查
        /// </summary>
        /// <param name="resultSHPFileName"></param>
        /// <param name="checkType"></param>
        /// <param name="lineFC"></param>
        /// <param name="lineFilter"></param>
        /// <param name="areaFC"></param>
        /// <param name="areaFilter"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static string DoCheck(string resultSHPFileName, WaterCheckLineFallintoBuildingPolygonForm.CheckType checkType, IFeatureClass lineFC, string lineFilter, IFeatureClass areaFC, string areaFilter, WaitOperation wo = null)
        {
            string err = "";

            try
            {
                IQueryFilter lineQF = new QueryFilterClass();
                lineQF.WhereClause = lineFilter;
                if (lineFC.HasCollabField())
                {
                    if(lineQF.WhereClause != "")
                        lineQF.WhereClause = string.Format("({0}) and ", lineQF.WhereClause);
                    lineQF.WhereClause += cmdUpdateRecord.CurFeatureFilter;
                }

                IQueryFilter areaQF = new QueryFilterClass();
                areaQF.WhereClause = areaFilter;
                if (areaFC.HasCollabField())
                {
                    if (areaQF.WhereClause != "")
                        areaQF.WhereClause = string.Format("({0}) and ", areaQF.WhereClause);
                    areaQF.WhereClause += cmdUpdateRecord.CurFeatureFilter;
                }

                //核查并输出结果
                ShapeFileWriter resultFile = null;
                if (checkType == WaterCheckLineFallintoBuildingPolygonForm.CheckType.INTERSECTS)
                {
                    #region 线非法落入面
                    Dictionary<int, string> errList = CheckLineFallintoPolygon(lineFC, lineQF, areaFC, areaQF, wo);
                    if (errList.Count > 0)
                    {
                        if (resultFile == null)
                        {
                            //建立结果文件
                            resultFile = new ShapeFileWriter();
                            Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                            fieldName2Len.Add("线图层名", lineFC.AliasName.Count());
                            fieldName2Len.Add("线要素编号", 10);
                            fieldName2Len.Add("面图层名", areaFC.AliasName.Count());
                            fieldName2Len.Add("检查项", 16);
                            fieldName2Len.Add("说明", 64);
                            resultFile.createErrorResutSHPFile(resultSHPFileName, (lineFC as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPolyline, fieldName2Len);
                        }

                        //写入结果文件
                        foreach (var kv in errList)
                        {
                            Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                            fieldName2FieldValue.Add("线图层名", lineFC.AliasName);
                            fieldName2FieldValue.Add("线要素编号", kv.Key.ToString());
                            fieldName2FieldValue.Add("面图层名", areaFC.AliasName);
                            fieldName2FieldValue.Add("检查项", "线非法落入面");
                            fieldName2FieldValue.Add("说明", kv.Value);

                            IFeature fe = lineFC.GetFeature(kv.Key);
                            resultFile.addErrorGeometry(fe.ShapeCopy, fieldName2FieldValue);
                            Marshal.ReleaseComObject(fe);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 线未被面完全包含
                    List<int> errList = CheckHelper.LineNotBeContainedByPolygon(lineFC, lineQF, areaFC, areaQF, wo);
                    if (errList.Count > 0)
                    {
                        if (resultFile == null)
                        {
                            //建立结果文件
                            resultFile = new ShapeFileWriter();
                            Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                            fieldName2Len.Add("线图层名", lineFC.AliasName.Count());
                            fieldName2Len.Add("线要素编号", 10);
                            fieldName2Len.Add("面图层名", areaFC.AliasName.Count());
                            fieldName2Len.Add("检查项", 16);
                            resultFile.createErrorResutSHPFile(resultSHPFileName, (lineFC as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPolyline, fieldName2Len);
                        }

                        //写入结果文件
                        foreach (var item in errList)
                        {
                            Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                            fieldName2FieldValue.Add("线图层名", lineFC.AliasName);
                            fieldName2FieldValue.Add("线要素编号", item.ToString());
                            fieldName2FieldValue.Add("面图层名", areaFC.AliasName);
                            fieldName2FieldValue.Add("检查项", "线未被面完全包含");

                            IFeature fe = lineFC.GetFeature(item);
                            resultFile.addErrorGeometry(fe.ShapeCopy, fieldName2FieldValue);
                            Marshal.ReleaseComObject(fe);
                        }
                    }
                    #endregion
                }

                //保存结果文件
                if (resultFile != null)
                    resultFile.saveErrorResutSHPFile();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.Source);
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                err = ex.Message;
            }

            return err;
        }

        /// <summary>
        /// 检查线落入面内的异常情况
        /// </summary>
        /// <param name="lineFC"></param>
        /// <param name="lineQF"></param>
        /// <param name="areaFC"></param>
        /// <param name="areaQF"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<int, string> CheckLineFallintoPolygon(IFeatureClass lineFC, IQueryFilter lineQF, IFeatureClass areaFC, IQueryFilter areaQF, WaitOperation wo = null)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();

            IFeatureCursor feCursor = lineFC.Search(lineQF, true);
            IFeature fe;
            while ((fe = feCursor.NextFeature()) != null)
            {
                if (fe.Shape == null || fe.Shape.IsEmpty)
                    continue;

                if (wo != null)
                    wo.SetText(string.Format("正在检查要素类【{0}】中的要素【{1}】......", lineFC.AliasName, fe.OID));

                ISpatialFilter sf = new SpatialFilter();
                sf.Geometry = fe.Shape;
                sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                sf.WhereClause = areaQF.WhereClause;

                IFeatureCursor areaFeCursor = areaFC.Search(sf, true);
                IFeature areaFe = null;
                while ((areaFe = areaFeCursor.NextFeature()) != null)
                {
                    ITopologicalOperator to = areaFe.Shape as ITopologicalOperator;
                    IPolyline interGeo = to.Intersect(fe.Shape, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                    if (interGeo != null && interGeo.Length > 0)
                    {
                        if (CheckHelper.ExistsWaterFacilities(fe.ShapeCopy, (lineFC as IDataset).Workspace as IFeatureWorkspace)) continue;

                        if (result.ContainsKey(fe.OID))
                        {
                            if (result[fe.OID].Count() > 50)
                            {
                                result[fe.OID] += string.Format("...");
                                break;
                            }
                            else
                            {
                                result[fe.OID] += string.Format(",{0}", areaFe.OID);
                            }
                        }
                        else
                        {
                            result.Add(fe.OID, string.Format("{0}|{1}", areaFC.AliasName, areaFe.OID));
                        }
                    }
                }
                Marshal.ReleaseComObject(areaFeCursor);

            }
            Marshal.ReleaseComObject(feCursor);

            return result;
        }

        
    }
}
