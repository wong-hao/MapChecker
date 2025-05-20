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
using ESRI.ArcGIS.ADF;

namespace SMGI.Plugin.CartographicGeneralization
{
    /// <summary>
    /// 沟渠符号流向一致检查
    /// </summary>
    public class DitchFlowDirectionMatchCmd : SMGICommand
    {
        private List<Tuple<string, string, string, string, string>> tts = new List<Tuple<string, string, string, string, string>>(); //配置信息表（单行）
        public static ISpatialReference srf;
        public static List<List<Tuple<IGeometry, string, int, string, int>>> errList = new List<List<Tuple<IGeometry, string, int, string, int>>>();
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
            outputFileName = OutputSetup.GetDir() + string.Format("\\{0}.shp", "沟渠符号流向一致检查");

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
        /// 沟渠符号流向一致检查
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
                        plQF.WhereClause = ptFC.HasCollabField() ? plSQL + " and " + cmdUpdateRecord.CurFeatureFilter : plSQL;

                        errList.Add(CheckFlowDirectionConsistency(ptFC, ptQF, plFC, plQF));
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
                        resultFile.createErrorResutSHPFile(outputFileName, srf, esriGeometryType.esriGeometryPolyline, fieldName2Len);
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
                            fieldName2FieldValue.Add("检查项", "沟渠符号流向一致检查");

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

        public static List<Tuple<IGeometry, string, int, string, int>> CheckFlowDirectionConsistency(
            IFeatureClass hfclFC,
            IQueryFilter hyclQF,
            IFeatureClass hydlFC,
            IQueryFilter hydlQF)
        {
            List<Tuple<IGeometry, string, int, string, int>> inconsistentPairs = new List<Tuple<IGeometry, string, int, string, int>>();

            IFeatureCursor hyclCursor = hfclFC.Search(hyclQF, false);

            IFeature hyclFeature = null;
            while ((hyclFeature = hyclCursor.NextFeature()) != null)
            {
                IPolyline hyclLine = hyclFeature.Shape as IPolyline;
                if (hyclLine == null) continue;

                IGeometry bufferGeom = ((ITopologicalOperator)hyclLine).Buffer(100);
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = bufferGeom;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
                spatialFilter.GeometryField = hydlFC.ShapeFieldName;
                spatialFilter.WhereClause = hydlQF.WhereClause;

                IFeatureCursor hydlCursor = hydlFC.Search(spatialFilter, false);

                double minDistance = double.MaxValue;
                IFeature nearestHydlFeature = null;
                IFeature hydlFeature = null;

                while ((hydlFeature = hydlCursor.NextFeature()) != null)
                {
                    IPolyline hydlLine = hydlFeature.Shape as IPolyline;
                    if (hydlLine == null) continue;

                    double distance = ((IProximityOperator)hyclLine).ReturnDistance(hydlLine as IGeometry);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestHydlFeature = hydlFeature;
                    }
                }

                if (nearestHydlFeature != null)
                {
                    IPolyline nearestLine = nearestHydlFeature.Shape as IPolyline;
                    if (!IsSameDirection(hyclLine, nearestLine))
                    {
                        inconsistentPairs.Add(
                            new Tuple<IGeometry, string, int, string, int>(hyclFeature.Shape, hydlFC.AliasName, hyclFeature.OID, hydlFC.AliasName, nearestHydlFeature.OID));
                    }
                }

                Marshal.ReleaseComObject(hydlCursor);
            }

            Marshal.ReleaseComObject(hyclCursor);

            return inconsistentPairs;
        }

        private static bool IsSameDirection(IPolyline hyclLine, IPolyline hydlLine)
        {
            // 获取 hycl 的方向向量
            IPoint hyclFrom = hyclLine.FromPoint;
            IPoint hyclTo = hyclLine.ToPoint;
            double dx1 = hyclTo.X - hyclFrom.X;
            double dy1 = hyclTo.Y - hyclFrom.Y;

            double hyclAngle = Math.Atan2(dy1, dx1); // 方向角

            // 获取 hyclLine 的中点
            IPoint hyclMidPoint = new PointClass();
            double midFraction = 0.5;
            hyclLine.QueryPoint(esriSegmentExtension.esriNoExtension, midFraction, false, hyclMidPoint);

            // 使用中点作为查找参考点
            IProximityOperator proxOp = (IProximityOperator)hydlLine;
            IPoint nearestPoint = proxOp.ReturnNearestPoint(hyclMidPoint, esriSegmentExtension.esriNoExtension);

            // 获取 nearestPoint 在线上的位置
            IPoint outPoint = new PointClass();
            double outDistance = 0, outFraction = 0;
            bool bRightSide = false;
            hydlLine.QueryPointAndDistance(esriSegmentExtension.esriNoExtension, nearestPoint, false, outPoint, ref outDistance, ref outFraction, ref bRightSide);

            // 获取 hydl 上最近点附近的一小段线方向
            const double step = 0.01; // 比如取前后 1% 范围
            double f1 = Math.Max(0, outFraction - step);
            double f2 = Math.Min(1, outFraction + step);

            IPoint p1 = new PointClass();
            IPoint p2 = new PointClass();
            hydlLine.QueryPoint(esriSegmentExtension.esriNoExtension, f1, false, p1);
            hydlLine.QueryPoint(esriSegmentExtension.esriNoExtension, f2, false, p2);

            double dx2 = p2.X - p1.X;
            double dy2 = p2.Y - p1.Y;
            double hydlAngle = Math.Atan2(dy2, dx2);

            // 计算方向夹角差（单位：弧度）
            double angleDiff = Math.Abs(hydlAngle - hyclAngle);
            if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;

            return angleDiff < Math.PI / 6; // 小于 30° 视为同向
        }

        //读取质检内容配置表
        private void ReadConfig()
        {
            tts.Clear();
            string dbPath = GApplication.Application.Template.Root + @"\质检\质检内容配置.xlsx";
            string tableName = "沟渠符号流向一致检查";
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
