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
using System.Data;

namespace SMGI.Plugin.CartographicGeneralization
{
    /// <summary>
    /// 要素分层检查
    /// </summary>
    public class CheckFCFieldValueCmd : SMGICommand
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
            if (m_Application.MapControl.Map.ReferenceScale == 0)
            {
                MessageBox.Show("请先设置参考比例尺！");
                return;
            }

            //读取配置表，获取需检查的内容
            string mdbPath = m_Application.Template.Root + "\\质检\\质检内容配置.xlsx";
            if (!System.IO.File.Exists(mdbPath))
            {
                MessageBox.Show(string.Format("未找到配置文件:{0}!", mdbPath));
                return;
            }

            string tabaleName = "要素分层检查_";
            DataTable dataTable = CheckHelper.getConfigTable(mdbPath, tabaleName, (int)m_Application.MapControl.Map.ReferenceScale);
            if (dataTable == null)
                return;

            string outputFileName = OutputSetup.GetDir() + string.Format("\\要素分层检查_{0}.shp", m_Application.MapControl.Map.ReferenceScale);

            string err = "";
            using (var wo = m_Application.SetBusy())
            {
                Dictionary<IFeatureClass, Dictionary<string, string>> checkItems = new Dictionary<IFeatureClass, Dictionary<string, string>>();
                foreach (DataRow dr in dataTable.Rows)
                {
                    string fcName = dr["FCName"].ToString();
                    string fieldName = dr["FieldName"].ToString();
                    string domain = dr["DomainCheck"].ToString();

                    var lyr = m_Application.Workspace.LayerManager.GetLayer(new LayerManager.LayerChecker(l =>
                    {
                        return (l is IGeoFeatureLayer) && ((l as IGeoFeatureLayer).FeatureClass.AliasName.ToUpper() == fcName.ToUpper());
                    })).FirstOrDefault() as IGeoFeatureLayer;
                    if (lyr == null)
                    {
                        continue;
                    }
                    IFeatureClass fc = lyr.FeatureClass;
                    if (fc == null)
                        continue;

                    if (checkItems.ContainsKey(fc))
                    {
                        checkItems[fc].Add(fieldName, domain);
                    }
                    else
                    {
                        Dictionary<string, string> fn2Domain = new Dictionary<string, string>();
                        fn2Domain.Add(fieldName, domain);

                        checkItems.Add(fc, fn2Domain);
                    }
                }

                err = DoCheck(outputFileName, checkItems, wo);
            }

            if (err == "")
            {
                if (File.Exists(outputFileName))
                {
                    IFeatureClass errFC = CheckHelper.OpenSHPFile(outputFileName);

                    if (MessageBox.Show("是否加载检查结果数据到地图？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        CheckHelper.AddTempLayerToMap(m_Application.Workspace.LayerManager.Map, errFC);
                }
                else
                {
                    MessageBox.Show("检查完毕,没有发现异常！");
                }
            }
            else
            {
                MessageBox.Show(err);
            }
        }

        public static string DoCheck(string resultSHPFileName, Dictionary<IFeatureClass, Dictionary<string, string>> checkItems, WaitOperation wo = null)
        {
            string err = "";

            try
            {
                ShapeFileWriter resultFile = null;


                foreach (var kv in checkItems)
                {
                    IFeatureClass fc = kv.Key;
                    Dictionary<string, string> fn2Domain = kv.Value;

                    Dictionary<int, List<string>> oid2FieldList = new Dictionary<int, List<string>>();
                    foreach (var kv2 in fn2Domain)
                    {
                        if (wo != null)
                            wo.SetText(string.Format("正在对要素类【{0}】中的字段【{1}】进行检查......", fc.AliasName, kv2.Key));

                        #region 检查
                        IQueryFilter qf = new QueryFilterClass();
                        if (fc.HasCollabField())
                            qf.WhereClause = cmdUpdateRecord.CurFeatureFilter;
                        if (kv2.Value != "")
                        {
                            if (qf.WhereClause != "")
                                qf.WhereClause += " and ";
                            qf.WhereClause += string.Format("({0})", kv2.Value);
                        }
                        IFeatureCursor feCursor = fc.Search(qf, true);
                        IFeature fe = null;
                        while ((fe = feCursor.NextFeature()) != null)
                        {
                            if(oid2FieldList.ContainsKey(fe.OID))
                            {
                                oid2FieldList[fe.OID].Add(kv2.Key);
                            }
                            else
                            {
                                List<string> fieldList = new List<string>();
                                fieldList.Add(kv2.Key);

                                oid2FieldList.Add(fe.OID, fieldList);
                            }
                        }
                        Marshal.ReleaseComObject(feCursor);
                        #endregion
                    }

                    if (oid2FieldList.Count > 0)
                    {
                        if (resultFile == null)
                        {
                            //新建结果文件
                            resultFile = new ShapeFileWriter();
                            Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                            fieldName2Len.Add("要素类名", 16);
                            fieldName2Len.Add("要素编号", 16);
                            fieldName2Len.Add("不合法字段", 256);
                            fieldName2Len.Add("检查项", 32);

                            resultFile.createErrorResutSHPFile(resultSHPFileName, (fc as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPoint, fieldName2Len);
                        }

                        //写入结果文件
                        foreach (var item in oid2FieldList)
                        {
                            IFeature fe = fc.GetFeature(item.Key);

                            Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                            fieldName2FieldValue.Add("要素类名", fc.AliasName);
                            fieldName2FieldValue.Add("要素编号", item.Key.ToString());
                            string oidErrString = "";
                            foreach (var fd in item.Value)
                            {
                                if (oidErrString == "")
                                {
                                    oidErrString = string.Format("{0}", fd);
                                }
                                else
                                {
                                    oidErrString += string.Format(",{0}", fd);
                                }
                            }
                            fieldName2FieldValue.Add("不合法字段", oidErrString);
                            fieldName2FieldValue.Add("检查项", "要素分层检查");
                            IPoint geo = null;
                            try
                            {
                                if (fc.ShapeType == esriGeometryType.esriGeometryPoint)
                                {
                                    geo = fe.ShapeCopy as IPoint;
                                }
                                else if (fc.ShapeType == esriGeometryType.esriGeometryPolyline)
                                {
                                    geo = new PointClass();
                                    (fe.Shape as ICurve).QueryPoint(esriSegmentExtension.esriNoExtension, 0.5, true, geo);
                                }
                                else if (fc.ShapeType == esriGeometryType.esriGeometryPolygon)
                                {
                                    geo = (fe.Shape as IArea).LabelPoint;
                                }
                            }
                            catch
                            {
                                //求几何中心点失败，空几何返回
                            }
                            resultFile.addErrorGeometry(geo, fieldName2FieldValue);

                            Marshal.ReleaseComObject(fe);

                            //内存监控
                            if (Environment.WorkingSet > DCDHelper.MaxMem)
                            {
                                GC.Collect();
                            }
                        }

                    }//oid2FieldList.Count > 0

                }

                //保存结果文件
                if(resultFile != null)
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
        
    }
}
