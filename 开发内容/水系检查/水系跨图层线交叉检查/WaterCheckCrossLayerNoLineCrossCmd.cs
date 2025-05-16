using System;
using System.Collections.Generic;
using System.Data;
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
    /// 跨图层水系相交线检查
    /// </summary>
    public class WaterCheckCrossLayerNoLineCrossCmd : SMGICommand
    {
        private List<Tuple<string, string, string, string, string>> tts = new List<Tuple<string, string, string, string, string>>(); //配置信息表（单行）
        public static ISpatialReference srf;
        public static List<List<Tuple<IPoint, int, string, int, string>>> errList = new List<List<Tuple<IPoint, int, string, int, string>>>();
        public static string outputFileName = string.Empty;

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
            outputFileName = OutputSetup.GetDir() + string.Format("\\{0}.shp", "跨图层水系相交线检查");

            IWorkspace workspace = m_Application.Workspace.EsriWorkspace;
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;

            //读取检查配置文件
            ReadConfig();

            using (var wo = m_Application.SetBusy())
            {
                var resultMessage = DoCheck(featureWorkspace, tts, wo);

                if (resultMessage.stat == ResultState.Ok)
                {
                    if (File.Exists(outputFileName))
                    {
                        IFeatureClass errFC = CheckHelper.OpenSHPFile(outputFileName);

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
                    MessageBox.Show(resultMessage.msg);
                    return;
                }
            }
        }
        
        /// <summary>
        /// 跨图层线交叉检查
        /// </summary>
        /// <param name="resultSHPFileName"></param>
        /// <param name="fcList"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static ResultMessage DoCheck(IFeatureWorkspace featureWorkspace, List<Tuple<string, string, string, string, string>> tts, WaitOperation wo = null)
        {
            string err = "";
            errList.Clear();
            
            try
            {
                #region 逐项检查
                foreach (var tt in tts)
                {
                    string ptName = tt.Item1;
                    string ptSQL = tt.Item2;
                    string plName = tt.Item3;
                    string plSQL = tt.Item4;
                    string beizhu = tt.Item5;
                    IFeatureClass ptFC = null;
                    IFeatureClass plFC = null;

                    try
                    {
                        ptFC = featureWorkspace.OpenFeatureClass(ptName);
                        plFC = featureWorkspace.OpenFeatureClass(plName);
                    }
                    catch (Exception ex)
                    {
                        return new ResultMessage { stat = ResultState.Failed, msg = ex.Message };
                    }

                    if (ptFC == null || plFC == null)
                    {
                        return new ResultMessage { stat = ResultState.Failed, msg = String.Format("{0} {1} 有空图层", ptName, plName) };
                    }

                    ISpatialReference ptSRF = (ptFC as IGeoDataset).SpatialReference;
                    if (srf == null)
                    {
                        srf = ptSRF;
                    }

                    try
                    {
                        IQueryFilter ptQF = new QueryFilterClass();
                        ptQF.WhereClause = ptFC.HasCollabField() ? ptSQL + " and " + cmdUpdateRecord.CurFeatureFilter : ptSQL;

                        IQueryFilter plQF = new QueryFilterClass();
                        ptQF.WhereClause = ptFC.HasCollabField() ? plSQL + " and " + cmdUpdateRecord.CurFeatureFilter : plSQL;

                        errList.Add(CheckHelper.CrossLayerLineNoCross(ptFC, ptQF, plFC, plQF, wo));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.Message);
                        System.Diagnostics.Trace.WriteLine(ex.Source);
                        System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                        return new ResultMessage { stat = ResultState.Failed, msg = ex.Message };
                    }
                }
                #endregion

                //核查并输出结果
                ShapeFileWriter resultFile = null;

                if (errList.Count > 0)
                {
                    if (resultFile == null)
                    {
                        //建立结果文件
                        resultFile = new ShapeFileWriter();
                        Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                        fieldName2Len.Add("线图层名", 40);
                        fieldName2Len.Add("线要素编号", 10);
                        fieldName2Len.Add("目标图层名", 40);
                        fieldName2Len.Add("交要素编号", 10);
                        fieldName2Len.Add("检查项", 16);
                        resultFile.createErrorResutSHPFile(outputFileName, srf, esriGeometryType.esriGeometryPoint, fieldName2Len);
                    }

                    //写入结果文件
                    foreach (var item in errList)
                    {
                        foreach (var itemInter in item)
                        {
                            Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                            fieldName2FieldValue.Add("线图层名", itemInter.Item3.ToString());
                            fieldName2FieldValue.Add("线要素编号", itemInter.Item2.ToString());
                            fieldName2FieldValue.Add("目标图层名", itemInter.Item5.ToString());
                            fieldName2FieldValue.Add("交要素编号", itemInter.Item4.ToString());
                            fieldName2FieldValue.Add("检查项", "跨图层线交叉");

                            //IFeature fe = featureWorkspace.OpenFeatureClass(itemInter.Item3).GetFeature(itemInter.Item2);
                            resultFile.addErrorGeometry(itemInter.Item1, fieldName2FieldValue);
                            //Marshal.ReleaseComObject(fe);
                        }
                    }
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

            return new ResultMessage { stat = ResultState.Ok };
        }

        //读取质检内容配置表
        private void ReadConfig()
        {
            tts.Clear();
            string dbPath = GApplication.Application.Template.Root + @"\质检\质检内容配置.xlsx";
            string tableName = "水系跨图层线交叉检查";
            DataTable ruleDataTable = CommonMethods.ReadToDataTable(dbPath, tableName);
            if (ruleDataTable == null)
            {
                return;
            }
            for (int i = 0; i < ruleDataTable.Rows.Count; i++)
            {
                string ptName = (ruleDataTable.Rows[i]["线层名称"]).ToString();
                string ptSQL = (ruleDataTable.Rows[i]["线层条件"]).ToString();
                string relName = (ruleDataTable.Rows[i]["关联层名称"]).ToString();
                string relSQL = (ruleDataTable.Rows[i]["关联层条件"]).ToString();
                string beizhu = (ruleDataTable.Rows[i]["备注"]).ToString();
                Tuple<string, string, string, string, string> tt = new Tuple<string, string, string, string, string>(ptName, ptSQL, relName, relSQL, beizhu);
                tts.Add(tt);
            }
        }
    }
}
