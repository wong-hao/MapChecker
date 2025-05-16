using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SMGI.Common;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.DataManagementTools;

namespace SMGI.Plugin.CartographicGeneralization
{
    public class CheckPolygonGap
    {
        public CheckPolygonGap()
        {
        }


        public string DoCheck(string resultSHPFileName, IFeatureClass fc, WaitOperation wo = null)
        {
            string err = "";

            try
            {
                IFeatureWorkspace fws = (fc as IDataset).Workspace as IFeatureWorkspace;

                //创建临时面要素类（去除面要素类中的内环）
                if (wo != null)
                    wo.SetText(string.Format("【{0}】正在创建临时类...", fc.AliasName));
                IFeatureClass temp_FC = DCDHelper.CreateFeatureClassStructToWorkspace(fws, fc, fc.AliasName + "_temp");
                string filter = "";
                if (fc.HasCollabField())
                    filter = cmdUpdateRecord.CurFeatureFilter;
                ExportPolygonFeature(fc, new QueryFilterClass { WhereClause = filter }, temp_FC, true);

                //融合图层所有面要素
                if (wo != null)
                    wo.SetText(string.Format("【{0}】正在合并面要素...", fc.AliasName));
                Geoprocessor gp = new Geoprocessor();
                gp.OverwriteOutput = true;

                Dissolve diss = new Dissolve();
                diss.in_features = temp_FC;
                diss.out_feature_class = (fc as IDataset).Workspace.PathName + "\\面缝隙合并_temp";
                //diss.multi_part = "SINGLE_PART";
                SMGI.Common.Helper.ExecuteGPTool(gp, diss, null);

                //提取面缝隙
                if (wo != null)
                    wo.SetText(string.Format("【{0}】正在进行面缝隙分析...", fc.AliasName));
                
                //建立结果文件
                ShapeFileWriter resultFile = new ShapeFileWriter();
                Dictionary<string, int> fieldName2Len = new Dictionary<string, int>();
                fieldName2Len.Add("图层名", 20);
                fieldName2Len.Add("检查项", 40);
                resultFile.createErrorResutSHPFile(resultSHPFileName, (fc as IGeoDataset).SpatialReference, esriGeometryType.esriGeometryPolygon, fieldName2Len);

                IFeatureClass mergeRecultFC = fws.OpenFeatureClass("面缝隙合并_temp");
                IFeatureCursor pCursor = mergeRecultFC.Search(null, false);
                IFeature fe = pCursor.NextFeature();
                while (fe != null)
                {
                    IPolygon plg = fe.Shape as IPolygon;

                    //找出内环，即为查找的面缝隙
                    var gc = plg as IGeometryCollection;
                    if (gc.GeometryCount > 1)
                    {
                        int index = gc.GeometryCount - 1;
                        while (index >= 0)
                        {
                            IRing r = gc.get_Geometry(index) as IRing;
                            if (!r.IsExterior)//内环
                            {
                                Dictionary<string, string> fieldName2FieldValue = new Dictionary<string, string>();
                                fieldName2FieldValue.Add("图层名", string.Format("{0}", fc.AliasName));
                                fieldName2FieldValue.Add("检查项", "面缝隙");

                                IGeometryCollection shape = new PolygonClass();
                                shape.AddGeometry(r);
                                resultFile.addErrorGeometry(shape as IGeometry, fieldName2FieldValue);
                            }

                            --index;
                        }
                    }
                    
                    fe = pCursor.NextFeature();
                }
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pCursor);

                //删除临时要素类
                (temp_FC as IDataset).Delete();
                (mergeRecultFC as IDataset).Delete();

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
        /// 以要素类org_fc为模板，创建一个空要素类
        /// </summary>
        /// <param name="fWS"></param>
        /// <param name="org_fc"></param>
        /// <param name="FeatureClassName"></param>
        /// <returns></returns>
        public static IFeatureClass CreateFeatureClassStructToWorkspace(IFeatureWorkspace fWS, IFeatureClass org_fc, string FeatureClassName)
        {
            FeatureClassName = FeatureClassName.Split('.').Last();

            if ((fWS as IWorkspace2).get_NameExists(esriDatasetType.esriDTFeatureClass, FeatureClassName))
            {
                if (MessageBox.Show(string.Format("要素类【{0}】已经存在，是否确定直接覆盖,或退出？", FeatureClassName), "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    IFeatureClass fc = fWS.OpenFeatureClass(FeatureClassName);
                    (fc as IDataset).Delete();
                }
                else
                {
                    return null;
                }
            }

            IFields org_fields;
            org_fields = (org_fc.Fields as IClone).Clone() as IFields;
            for (int i = 0; i < org_fields.FieldCount; i++)
            {
                IField field = org_fields.get_Field(i);
                IFieldEdit fieldEdit = field as IFieldEdit;
                fieldEdit.Name_2 = field.Name.ToUpper();
            }


            IObjectClassDescription featureDescription = new FeatureClassDescriptionClass();
            IFieldsEdit target_fields = featureDescription.RequiredFields as IFieldsEdit;
            for (int i = 0; i < org_fields.FieldCount; i++)
            {
                IField field = org_fields.get_Field(i);

                if (!(field as IFieldEdit).Editable)
                {
                    continue;
                }

                if (field.Type == esriFieldType.esriFieldTypeGeometry)
                {
                    (target_fields as IFieldsEdit).set_Field(target_fields.FindFieldByAliasName((featureDescription as IFeatureClassDescription).ShapeFieldName),
                        (field as ESRI.ArcGIS.esriSystem.IClone).Clone() as IField);

                    continue;
                }

                if (target_fields.FindField(field.Name) != -1)
                {
                    continue;
                }

                IField field_new = (field as ESRI.ArcGIS.esriSystem.IClone).Clone() as IField;
                (target_fields as IFieldsEdit).AddField(field_new);
            }
            return fWS.CreateFeatureClass(FeatureClassName, target_fields, featureDescription.InstanceCLSID,
                featureDescription.ClassExtensionCLSID, esriFeatureType.esriFTSimple,
                (featureDescription as IFeatureClassDescription).ShapeFieldName, string.Empty);
        }

        /// <summary>
        /// 将面要素类inFeatureClss中指定的要素复制到目标要素类中targetFeatureClss,同时删除每个面的内环
        /// </summary>
        /// <param name="inFeatureClss"></param>
        /// <param name="qf"></param>
        /// <param name="targetFeatureClss"></param>
        /// <param name="bRemoveInteriorRings"></param>
        public static void ExportPolygonFeature(IFeatureClass inFeatureClss, IQueryFilter qf, IFeatureClass targetFeatureClss, bool bRemoveInteriorRings)
        {
            if (inFeatureClss.ShapeType != esriGeometryType.esriGeometryPolygon && targetFeatureClss.ShapeType != esriGeometryType.esriGeometryPolygon)
            {
                return;
            }

            bool bProjection = false;
            ISpatialReference in_sr = (inFeatureClss as IGeoDataset).SpatialReference;
            ISpatialReference target_sr = (targetFeatureClss as IGeoDataset).SpatialReference;
            if (in_sr.Name != target_sr.Name)
            {
                bProjection = true;
            }

            IFeatureCursor pTargetFeatureCursor = targetFeatureClss.Insert(true);
            IFeatureBuffer pFeatureBuffer = targetFeatureClss.CreateFeatureBuffer();

            IFeatureCursor pInFeatureCursor = inFeatureClss.Search(qf, true);
            IFeature pInFeature = null;

            try
            {
                while ((pInFeature = pInFeatureCursor.NextFeature()) != null)
                {
                    IPolygon plg = pInFeature.ShapeCopy as IPolygon;
                    if (bProjection)
                        plg.Project(target_sr);//投影变换

                    //去除内环
                    if (bRemoveInteriorRings)
                    {
                        var gc = plg as IGeometryCollection;
                        if (gc.GeometryCount > 1)
                        {
                            bool hasInteriorRings = false;
                            int index = gc.GeometryCount - 1;
                            while(index >= 0 )
                            {
                                IRing r = gc.get_Geometry(index) as IRing;
                                if (!r.IsExterior)//内环
                                {
                                    gc.RemoveGeometries(index, 1);

                                    hasInteriorRings = true;
                                }

                                --index;
                            }

                            if(hasInteriorRings)                            
                                (plg as ITopologicalOperator).Simplify();
                        }
                    }

                    pFeatureBuffer.Shape = plg;
                    for (int i = 0; i < pFeatureBuffer.Fields.FieldCount; i++)
                    {
                        IField pfield = pFeatureBuffer.Fields.get_Field(i);
                        if (pfield.Type == esriFieldType.esriFieldTypeGeometry || pfield.Type == esriFieldType.esriFieldTypeOID)
                            continue;

                        if (pfield.Name.ToUpper() == "SHAPE_LENGTH" || pfield.Name.ToUpper() == "SHAPE_AREA")
                            continue;

                        //复制属性值
                        int index = pInFeature.Fields.FindField(pfield.Name);
                        if (index != -1 && pfield.Editable)
                        {
                            pFeatureBuffer.set_Value(i, pInFeature.get_Value(index));
                        }

                    }
                    pTargetFeatureCursor.InsertFeature(pFeatureBuffer);

                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.Source);
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                MessageBox.Show(ex.Message);
            }
            pTargetFeatureCursor.Flush();

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pTargetFeatureCursor);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pInFeatureCursor);
        }
    }
}
