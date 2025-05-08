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
    /// 跨图层相交线检查
    /// </summary>
    public class CheckCrossLayerNoLineCrossCmd : SMGICommand
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
            var frm = new CheckLayerSelectForm(m_Application, false, true, false);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.Text = "跨图层相交线检查";

            if (frm.ShowDialog() != DialogResult.OK)
                return;

            string outputFileName;
            if (frm.CheckFeatureLayerList.Count > 1)
            {
                outputFileName = OutputSetup.GetDir() + string.Format("\\{0}.shp", frm.Text);
            }
            else
            {
                outputFileName = OutputSetup.GetDir() + string.Format("\\{0}_{1}.shp", frm.Text, frm.CheckFeatureLayerList.First().Name);
            }


            string err = "";
            using (var wo = m_Application.SetBusy())
            {
                List<IFeatureClass> fcList = new List<IFeatureClass>();
                foreach (var layer in frm.CheckFeatureLayerList)
                {
                    IFeatureClass fc = layer.FeatureClass;
                    if(!fcList.Contains(fc))
                        fcList.Add(fc);
                }

                if (fcList.Count != 2)
                {
                    MessageBox.Show("目前仅支持两个图层检测!");
                    return;
                }

                err = DoCheck(outputFileName, fcList, string.Empty, string.Empty, wo);
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
                    MessageBox.Show("检查完毕！");
                }
            }
            else
            {
                MessageBox.Show(err);
            }

        }
        
        /// <summary>
        /// 跨图层线交叉检查
        /// </summary>
        /// <param name="resultSHPFileName"></param>
        /// <param name="fcList"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static string DoCheck(string resultSHPFileName, List<IFeatureClass> fcList, string lineFilter1, string lineFilter2, WaitOperation wo = null)
        {
            string err = "";

            try
            {
                IQueryFilter lineQF1 = new QueryFilterClass();
                lineQF1.WhereClause = lineFilter1;
                if (fcList[0].HasCollabField())
                {
                    if (lineQF1.WhereClause != "")
                        lineQF1.WhereClause = string.Format("({0}) and ", lineQF1.WhereClause);
                    lineQF1.WhereClause += cmdUpdateRecord.CurFeatureFilter;
                }

                IQueryFilter lineQF2 = new QueryFilterClass();
                lineQF2.WhereClause = lineFilter2;
                if (fcList[1].HasCollabField())
                {
                    if (lineQF2.WhereClause != "")
                        lineQF2.WhereClause = string.Format("({0}) and ", lineQF2.WhereClause);
                    lineQF2.WhereClause += cmdUpdateRecord.CurFeatureFilter;
                }

                //核查并输出结果
                ShapeFileWriter resultFile = null;

                var errList = CheckHelper.CrossLayerLineNoCross(fcList[0], lineQF1, fcList[1], lineQF2, wo);
                if (errList.Count > 0)
                {
                    if (resultFile == null)
                    {
                        //建立结果文件
                        resultFile = new ShapeFileWriter();
                        Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                        fieldName2Len.Add("线图层名", fcList[0].AliasName.Count());
                        fieldName2Len.Add("线要素编号", 10);
                        fieldName2Len.Add("目标图层名", fcList[1].AliasName.Count());
                        fieldName2Len.Add("交要素编号", 10);
                        fieldName2Len.Add("检查项", 16);
                        resultFile.createErrorResutSHPFile(resultSHPFileName, (fcList[0] as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPolyline, fieldName2Len);
                    }

                    //写入结果文件
                    foreach (var item in errList)
                    {
                        Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                        fieldName2FieldValue.Add("线图层名", item.Item2.ToString());
                        fieldName2FieldValue.Add("线要素编号", item.Item1.ToString());
                        fieldName2FieldValue.Add("目标图层名", item.Item4.ToString());
                        fieldName2FieldValue.Add("交要素编号", item.Item3.ToString());
                        fieldName2FieldValue.Add("检查项", "跨图层线交叉");

                        IFeature fe = fcList[0].GetFeature(item.Item1);
                        resultFile.addErrorGeometry(fe.ShapeCopy, fieldName2FieldValue);
                        Marshal.ReleaseComObject(fe);
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
