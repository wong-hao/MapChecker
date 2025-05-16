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

namespace SMGI.Plugin.CartographicGeneralization
{
    public class WaterCheckPseudoCmd : SMGICommand
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
            var layerSelector = new LayerSelectWithFiedsForm(m_Application)
            {
                GeoTypeFilter = esriGeometryType.esriGeometryPolyline
            };

            if (layerSelector.ShowDialog() != DialogResult.OK)
                return;

            if (layerSelector.pSelectLayer == null)
                return;

            var lyr = layerSelector.pSelectLayer as IFeatureLayer;
            IFeatureClass fc = lyr.FeatureClass;


            string outPutFileName = OutputSetup.GetDir() + string.Format("\\水系伪节点检查_{0}.shp", fc.AliasName);


            string err = "";
            using (var wo = m_Application.SetBusy())
            {
                Dictionary<IFeatureClass, List<string>> fc2FieldNames = new Dictionary<IFeatureClass, List<string>>();
                fc2FieldNames.Add(fc, layerSelector.FieldArray);

                err = DoCheck(outPutFileName, fc2FieldNames, wo);
            }

            if (err == "")
            {
                IFeatureClass errFC = CheckHelper.OpenSHPFile(outPutFileName);
                int count = errFC.FeatureCount(null);
                if (count > 0)
                {
                    if (MessageBox.Show("是否加载检查结果数据到地图？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        CheckHelper.AddTempLayerToMap(m_Application.Workspace.LayerManager.Map, errFC);
                }
                else
                {
                    MessageBox.Show("检查完毕！");
                }
            }
            else
            {
                MessageBox.Show(err);
            }

        }

        /// <summary>
        /// 水系伪节点检查
        /// </summary>
        /// <param name="resultSHPFileName"></param>
        /// <param name="fc"></param>
        /// <param name="fieldNames"></param>
        /// <param name="rangeGeometry"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static string DoCheck(string resultSHPFileName, Dictionary<IFeatureClass, List<string>> fc2FieldNames, WaitOperation wo = null)
        {
            string err = "";

            if (fc2FieldNames == null || fc2FieldNames.Count == 0)
                return err;

            try
            {
                ShapeFileWriter resultFile = null;

                foreach (var kv in fc2FieldNames)
                {
                    IFeatureClass fc = kv.Key;
                    List<string> fieldNames = kv.Value;

                    if (fc.ShapeType != esriGeometryType.esriGeometryPolyline)
                        continue;

                    if (resultFile == null)
                    {
                        //建立结果文件
                        resultFile = new ShapeFileWriter();
                        Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                        fieldName2Len.Add("图层名", 20);
                        fieldName2Len.Add("要素编号", 32);
                        fieldName2Len.Add("检查项", 32);
                        resultFile.createErrorResutSHPFile(resultSHPFileName, (fc as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPoint, fieldName2Len);
                    }

                    IQueryFilter qf = new QueryFilterClass();
                    if (fc.HasCollabField())
                        qf.WhereClause = cmdUpdateRecord.CurFeatureFilter;

                    Dictionary<IPoint, KeyValuePair<int, int>> pseudoList = new Dictionary<IPoint, KeyValuePair<int, int>>();
                    #region 构建拓扑，检查伪节点(不考虑属性、不考虑范围)
                    if (wo != null)
                        wo.SetText(string.Format("正在对要素类【{0}】进行伪节点预处理......", fc.AliasName));

                    pseudoList = CheckHelper.NotHavePseudonodes(fc, qf, PseudoCmd.Tolerance);
                    #endregion

                    //核查伪节点
                    int count = pseudoList.Count;
                    foreach (var item in pseudoList)
                    {
                        count--;

                        IPoint p = item.Key;
                        int fid1 = item.Value.Key;
                        int fid2 = item.Value.Value;

                        IFeature fe1 = fc.GetFeature(fid1);
                        IFeature fe2 = fc.GetFeature(fid2);

                        if (wo != null)
                            wo.SetText(string.Format("要素类【{0}】伪节点核查：剩余检查节点数【{1}】......", fc.AliasName, count));

                        //属性判断
                        bool judge = false;//是否不是伪节点
                        for (int i = 0; i < fieldNames.Count; i++)
                        {
                            int FieldIndex = fc.FindField(fieldNames[i]);
                            if (FieldIndex != -1)
                            {
                                string valString1 = fe1.get_Value(FieldIndex).ToString().Trim();
                                string valString2 = fe2.get_Value(FieldIndex).ToString().Trim();
                                if (valString1 != valString2)
                                {
                                    judge = true;
                                    break;
                                }
                            }
                        }

                        if (judge == false)//伪节点
                        {
                            Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                            fieldName2FieldValue.Add("图层名", fc.AliasName);
                            fieldName2FieldValue.Add("要素编号", string.Format("{0},{1}", fe1.OID, fe2.OID));
                            fieldName2FieldValue.Add("检查项", "水系伪节点检查");

                            resultFile.addErrorGeometry(p, fieldName2FieldValue);
                        }

                    }
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
