using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Carto;
using SMGI.Common;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.AnalysisTools;
using ESRI.ArcGIS.DataManagementTools;

namespace SMGI.Plugin.CartographicGeneralization
{
    /// <summary>
    /// 检查水系结构线是否合理
    /// 2022.4.1 1、图面容差maxDist改为0.12mm对应的图上距离
    ///          2、解决一线跨多面的问题
    /// </summary>
    public class CheckRiverStructCmd : SMGI.Common.SMGICommand
    {
        public override bool Enabled
        {
            get
            {
                return m_Application.Workspace != null;
            }
        }

        public override void OnClick()
        {
            if (m_Application.MapControl.Map.ReferenceScale == 0)
            {
                MessageBox.Show("请先设置参考比例尺！");
                return;
            }

            CheckRiverStructFrm frm = new CheckRiverStructFrm();
            if (frm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            double maxDist = 0.12 * m_Application.MapControl.Map.ReferenceScale * 0.001; //0.12mm为图面限差

            IGeoFeatureLayer hydlLyr = m_Application.Workspace.LayerManager.GetLayer(new LayerManager.LayerChecker(l =>
            {
                return (l is IGeoFeatureLayer) && ((l as IGeoFeatureLayer).FeatureClass.AliasName.ToUpper() == "HYDL");
            })).ToArray().First() as IGeoFeatureLayer;
            if (hydlLyr == null)
            {
                MessageBox.Show("缺少HYDL要素类！");
                return;
            }
            IFeatureClass hydlFC = hydlLyr.FeatureClass;

            IGeoFeatureLayer hydaLyr = m_Application.Workspace.LayerManager.GetLayer(new LayerManager.LayerChecker(l =>
            {
                return (l is IGeoFeatureLayer) && ((l as IGeoFeatureLayer).FeatureClass.AliasName.ToUpper() == "HYDA");
            })).ToArray().First() as IGeoFeatureLayer;
            if (hydaLyr == null)
            {
                MessageBox.Show("缺少HYDA要素类！");
                return;
            }
            IFeatureClass hydaFC = hydaLyr.FeatureClass;


            string outPutFileName = OutputSetup.GetDir() + string.Format("\\水系结构线检查.shp");


            string err = "";
            using (var wo = m_Application.SetBusy())
            {
                if (!frm.BIgnoreSmall)
                    maxDist = 0.1;
                err = DoCheck(outPutFileName, hydlFC, hydaFC, frm.BNonRiverStruct, frm.BRiverStruct, frm.BIgnoreSmall,maxDist, wo);
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
        /// 检查水系结构线位置是否合理：
        /// 1.非水系结构线在水系面存在相交关系，或在水系面内；
        /// 2.水系结构线与水系面存在相交关系、或在水系面外；
        /// 3.水系交叉阈值。
        /// </summary>
        /// <param name="resultSHPFileName"></param>
        /// <param name="hydlFC"></param>
        /// <param name="hydaFC"></param>
        /// <param name="bNonStruct">是否检查完全包含于自然河流面内的非水系结构线</param>
        /// <param name="bStruct">是否检查与水系面相交（横跨）的水系结构线</param>
        /// <param name="bIgnoreSmall">是否使用小阈值</param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static string DoCheck(string resultSHPFileName, IFeatureClass hydlFC, IFeatureClass hydaFC, 
            bool bNonStruct, bool bStruct, bool bIgnoreSmall, double maxDistance,WaitOperation wo = null)
        {
            string err = "";

            int hgbIndex = hydlFC.FindField("GB");
            if (hgbIndex == -1)
            {
                err = string.Format("要素类【{0}】中没有找到GB字段！", hydlFC.AliasName);
                return err;
            }

            int hydaGBIndex = hydaFC.FindField("GB");
            if (hydaGBIndex == -1)
            {
                err = string.Format("要素类【{0}】中没有找到GB字段！", hydaFC.AliasName);
                return err;
            }
                        
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            List<Tuple<string, string, IGeometry>> errList = new List<Tuple<string, string, IGeometry>>();

            if (wo != null)
                wo.SetText("1-水系面获取和处理......");
            //hydaFC2
            #region 水面合并
            IFeatureClass hydaFC2 = null;
            try
            {
                string outFCLyr = hydaFC.AliasName + "_Layer";
                string outFC = hydaFC.AliasName + "_diss";
                Geoprocessor gp = new Geoprocessor();
                gp.OverwriteOutput = true;
                gp.SetEnvironmentValue("workspace", ws.PathName);

                MakeFeatureLayer makeFeatureLayer = new MakeFeatureLayer();
                SelectLayerByAttribute selectLayerByAttribute = new SelectLayerByAttribute();

                makeFeatureLayer.in_features = hydaFC;
                makeFeatureLayer.out_layer = outFCLyr;
                SMGI.Common.Helper.ExecuteGPTool(gp, makeFeatureLayer, null);
                selectLayerByAttribute.in_layer_or_view = outFCLyr;
                selectLayerByAttribute.where_clause = "GB <= 240101";
                if (hydaFC.HasCollabField())
                    selectLayerByAttribute.where_clause += " and "+ cmdUpdateRecord.CurFeatureFilter;
                SMGI.Common.Helper.ExecuteGPTool(gp, selectLayerByAttribute, null);

                ESRI.ArcGIS.DataManagementTools.Dissolve diss = new ESRI.ArcGIS.DataManagementTools.Dissolve();
                diss.in_features = outFCLyr;
                diss.out_feature_class = outFC;
                diss.multi_part = "SINGLE_PART";
                SMGI.Common.Helper.ExecuteGPTool(gp, diss, null);

                hydaFC2 = fws.OpenFeatureClass(outFC);

            }
            catch (Exception ex)
            {
                return "合并HYDA失败！";
            }
            #endregion
            if (bStruct)
            {
                if (wo != null)
                    wo.SetText("2-水系结构线与水系面检查......");
                #region 水系结构线
                try
                {
                    Geoprocessor gp = new Geoprocessor();
                    gp.OverwriteOutput = true;
                    gp.SetEnvironmentValue("workspace", ws.PathName);

                    string outFCLyr = hydlFC.AliasName + "_Layer";

                    MakeFeatureLayer makeFeatureLayer = new MakeFeatureLayer();
                    makeFeatureLayer.in_features = hydlFC;
                    makeFeatureLayer.where_clause = "GB=210400";
                    if (hydlFC.HasCollabField())
                        makeFeatureLayer.where_clause += " and " + cmdUpdateRecord.CurFeatureFilter;
                    makeFeatureLayer.out_layer = outFCLyr;
                    SMGI.Common.Helper.ExecuteGPTool(gp, makeFeatureLayer, null);

                    Erase erase = new Erase();
                    erase.in_features = outFCLyr;
                    erase.erase_features = hydaFC2;
                    string hydlErase = hydlFC.AliasName + "_Erase";
                    erase.out_feature_class = hydlErase;
                    SMGI.Common.Helper.ExecuteGPTool(gp, erase, null);

                    IFeatureClass hydlFCErase = fws.OpenFeatureClass(hydlErase);


                    IFeatureCursor hydlEraseCursor = hydlFCErase.Search(null, true);
                    int guidindex = hydlFCErase.FindField(cmdUpdateRecord.CollabGUID);
                    string guid;
                    string info = "结构线在水系面外";

                    IFeature hydlFe = null;
                    while ((hydlFe = hydlEraseCursor.NextFeature()) != null)
                    {
                        guid = "";
                        if (guidindex > -1)
                            guid = hydlFe.get_Value(guidindex).ToString();
                        IList<IGeometry> geoList = GetGeoParts(hydlFe.ShapeCopy, maxDistance);
                        if (geoList.Count > 0)
                        {
                            foreach (IGeometry geoTemp in geoList)
                            {
                                Tuple<string, string, IGeometry> tp = new Tuple<string, string, IGeometry>(guid, info, geoTemp);
                                errList.Add(tp);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return "水系面擦除结构线失败！";
                }
                #endregion
            }
            if (bNonStruct)
            {
                if (wo != null)
                    wo.SetText("3-非水系结构线与水系面检查......");
                #region 非水系结构线
                try
                {
                    Geoprocessor gp = new Geoprocessor();
                    gp.OverwriteOutput = true;
                    gp.SetEnvironmentValue("workspace", ws.PathName);

                    string outFCLyr = hydlFC.AliasName + "_Layer";

                    MakeFeatureLayer makeFeatureLayer = new MakeFeatureLayer();
                    makeFeatureLayer.in_features = hydlFC;
                    makeFeatureLayer.where_clause = "GB<>210400";
                    if (hydlFC.HasCollabField())
                        makeFeatureLayer.where_clause += " and " + cmdUpdateRecord.CurFeatureFilter;
                    makeFeatureLayer.out_layer = outFCLyr;
                    SMGI.Common.Helper.ExecuteGPTool(gp, makeFeatureLayer, null);

                    ESRI.ArcGIS.AnalysisTools.Clip clip = new ESRI.ArcGIS.AnalysisTools.Clip();
                    clip.in_features = outFCLyr;
                    clip.clip_features = hydaFC2;
                    string hydlClip = hydlFC.AliasName + "_Clip";
                    clip.out_feature_class = hydlClip;
                    SMGI.Common.Helper.ExecuteGPTool(gp, clip, null);

                    IFeatureClass hydlFCClip = fws.OpenFeatureClass(hydlClip);

                    IFeatureCursor hydlClipCursor = hydlFCClip.Search(null, true);
                    IFeature hydlFe = null;
                    int guidindex = hydlFCClip.FindField(cmdUpdateRecord.CollabGUID);
                    string guid;

                    string info = "非结构线在水系面内";
                    while ((hydlFe = hydlClipCursor.NextFeature()) != null)
                    {
                        IList<IGeometry> geoList = GetGeoParts(hydlFe.ShapeCopy, maxDistance);
                        if (geoList.Count > 0)
                        {
                            guid = "";
                            if (guidindex > -1)
                                guid = hydlFe.get_Value(guidindex).ToString();
                            foreach (IGeometry geoTemp in geoList)
                            {
                                Tuple<string, string, IGeometry> tp = new Tuple<string, string, IGeometry>(guid, info, geoTemp);
                                errList.Add(tp);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return "水系面裁切非结构线失败！";
                }
                #endregion
            }

            
            {
                if (wo != null)
                    wo.SetText("4-正在输出检查结果......");

                ShapeFileWriter resultFile = new ShapeFileWriter();
                Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                fieldName2Len.Add("图层名", 20);
                fieldName2Len.Add("GUID", 64);
                fieldName2Len.Add("说明", 40);
                fieldName2Len.Add("检查项", 20);
                resultFile.createErrorResutSHPFile(resultSHPFileName, (hydlFC as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPolyline, fieldName2Len);
                if (errList.Count > 0)
                {
                    foreach (var item in errList)
                    {
                        Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                        fieldName2FieldValue.Add("图层名", hydlFC.AliasName);
                        fieldName2FieldValue.Add("GUID", item.Item1);
                        fieldName2FieldValue.Add("说明", item.Item2);
                        fieldName2FieldValue.Add("检查项", "水系结构线检查");
                        IGeometry geo = item.Item3;
                        resultFile.addErrorGeometry(geo, fieldName2FieldValue);
                    }
                } 
            }

            /*
            try
            {
                Dictionary<int, string> oid2ErrInfo = new Dictionary<int, string>();

                #region 检查完全包含于自然河流面内的非水系结构线（检查河流面中的非水系结构线（有横跨/有包含））
                if (bNonStruct)
                {
                    ESRI.ArcGIS.AnalysisTools.Clip clip = new ESRI.ArcGIS.AnalysisTools.Clip();

                    IQueryFilter hydaQF = new QueryFilterClass();
                    hydaQF.WhereClause = "GB <= 240101 ";//水面，排除河道干河
                    if (hydaFC.HasCollabField())
                        hydaQF.WhereClause += " and " + cmdUpdateRecord.CurFeatureFilter;
                    IFeatureCursor hydaCursor = hydaFC.Search(hydaQF, true);
                    IFeature hydaFe = null;
                    while ((hydaFe = hydaCursor.NextFeature()) != null)
                    {
                        if (wo != null)
                            wo.SetText(string.Format("正在检查河流面【{0}】内的非水系结构线......", hydaFe.OID));

                        ISpatialFilter sf = new SpatialFilter();
                        sf.Geometry = hydaFe.Shape;
                        sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains; //1-在水面内
                        sf.WhereClause = "GB <> 210400";//非水系结构线
                        if (hydlFC.HasCollabField())
                            sf.WhereClause += " and " + cmdUpdateRecord.CurFeatureFilter;

                        IFeatureCursor hydlCursor = hydlFC.Search(sf, true);
                        IFeature hydlFe = null;
                        while ((hydlFe = hydlCursor.NextFeature()) != null)
                        {
                            oid2ErrInfo.Add(hydlFe.OID, "河流面中的非水系结构线（包含）");
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(hydlCursor);

                        sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses; //2-相交
                        if (hydlFC.HasCollabField())
                            sf.WhereClause += " and " + cmdUpdateRecord.CurFeatureFilter;
                        hydlCursor = hydlFC.Search(sf, true);
                        hydlFe = null;
                        while ((hydlFe = hydlCursor.NextFeature()) != null)
                        {
                            if(!oid2ErrInfo.ContainsKey(hydlFe.OID))
                                oid2ErrInfo.Add(hydlFe.OID, "河流面中的非水系结构线（交叉）");
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(hydlCursor);
                    }
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(hydaCursor);
                   
                }
                #endregion

                #region 检查河流面外的非水系结构线
                if (bStruct || bIgnoreSmall)
                {
                    

                    IQueryFilter qf = new QueryFilterClass();
                    qf.WhereClause = "GB = 210400";
                    if (hydlFC.HasCollabField())
                        qf.WhereClause += " and " + cmdUpdateRecord.CurFeatureFilter;
                    IFeatureCursor feCursor = hydlFC.Search(qf, true);
                    IFeature f;
                    while ((f = feCursor.NextFeature()) != null)
                    {
                        if (wo != null)
                            wo.SetText(string.Format("正在检查水系结构线【{0}】......", f.OID));

                        ISpatialFilter sf = new SpatialFilter();
                        sf.Geometry = f.Shape;
                        sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses;

                        //if (hydaFC.HasCollabField())
                        //    sf.WhereClause = cmdUpdateRecord.CurFeatureFilter;

                        int n = hydaFC2.FeatureCount(sf);
                        if (n>1)//该水系结构线横跨某一水系面
                        {
                            if (bStruct)
                            {                                
                                string info = string.Format("水系结构线横跨水系面-{0}", n);
                                oid2ErrInfo.Add(f.OID, info);
                            }
                        }
                        else if (n == 1)
                        {
                            if (bStruct)
                            {
                                IFeatureCursor cursor2 = hydaFC2.Search(sf, true);
                                IFeature fe2 = null;
                                while ((fe2 = cursor2.NextFeature()) != null)
                                {
                                    IPolyline pl = f.Shape as IPolyline;
                                    IPolygon pg = fe2.Shape as IPolygon;
                                    ITopologicalOperator tTopo = pl as ITopologicalOperator;
                                    IGeometry pl_clip = tTopo.Intersect(pg,esriGeometryDimension.esriGeometry1Dimension);
                                    IPolyline pl2 = tTopo.Difference(pl_clip) as IPolyline;
                                    if (pl2.Length >= maxDistance)
                                    {
                                        string info = string.Format("水系结构线横跨水系面-1,距离{0:F2}", pl2.Length);
                                        oid2ErrInfo.Add(f.OID, info);
                                    }
                                }
                                Marshal.ReleaseComObject(cursor2);
                            } 
                        }
                        else //n==0
                        {
                            if (bIgnoreSmall)
                            {
                                sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelWithin;
                                if (hydaFC2.FeatureCount(sf) == 0)//完全落在水系面外：水系结构线不横跨某一水系面，且不包含在任一水系面内
                                {
                                    oid2ErrInfo.Add(f.OID, "水系结构线不包含在任一水系面内");
                                    continue;
                                }
                            }
                        }
                    }
                    Marshal.ReleaseComObject(feCursor);
                }
                #endregion


                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                #region 输出结果
                //新建结果文件
                ShapeFileWriter resultFile = new ShapeFileWriter();
                Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                fieldName2Len.Add("图层名", 20);
                fieldName2Len.Add("编号", 20);
                fieldName2Len.Add("说明", 40);
                fieldName2Len.Add("检查项", 20);
                resultFile.createErrorResutSHPFile(resultSHPFileName, (hydlFC as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPolyline, fieldName2Len);

                //输出内容
                if (oid2ErrInfo.Count > 0)
                {
                    foreach (var item in oid2ErrInfo)
                    {
                        IFeature fe = hydlFC.GetFeature(item.Key);

                        Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                        fieldName2FieldValue.Add("图层名", hydlFC.AliasName);
                        fieldName2FieldValue.Add("编号", item.Key.ToString());
                        fieldName2FieldValue.Add("说明", item.Value.ToString());
                        fieldName2FieldValue.Add("检查项", "水系结构线检查");

                        resultFile.addErrorGeometry(fe.Shape, fieldName2FieldValue);
                    }
                }

                //保存结果文件
                resultFile.saveErrorResutSHPFile();
                #endregion
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.Source);
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                err = ex.Message;
            }
            */
            return err;
        }

        public static IList<IGeometry> GetGeoParts(IGeometry geo,double dist=0.0)
        {            
            IList<IGeometry> geoList = new List<IGeometry>();
            IGeometryCollection geoColl = geo as IGeometryCollection;

            
            if (geoColl != null)
            {
                if (geoColl.GeometryCount == 1)
                {
                    IPolyline pl = geo as IPolyline;
                    if (pl.Length > dist)
                    {
                        geoList.Add(geo);
                    }
                }
                else
                {
                    for (int i = 0; i < geoColl.GeometryCount; i++)
                    {
                        IPointCollection ptColl = geoColl.get_Geometry(i) as IPointCollection;
                        IGeometryCollection geoColl2 = new PolylineClass();
                        geoColl2.AddGeometryCollection(geoColl2);
                        IPolyline pl = geoColl2 as IPolyline;

                        if (pl != null && pl.Length > dist)
                            geoList.Add(pl as IGeometry);
                    }
                }
            }
            return geoList;
        }
    }
}
