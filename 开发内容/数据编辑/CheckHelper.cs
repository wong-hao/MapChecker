using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using SMGI.Common;
using System.Data;


namespace SMGI.Plugin.CartographicGeneralization
{
    class CheckHelper
    {
        public static IFeatureClass WaterFacilityPointFC = null;
        public static IFeatureClass WaterFacilityLineFC = null;
        public static IFeatureClass WaterFacilityAreaFC = null;

        public static IFeatureClass RoadFacilityPointFC = null;
        public static IFeatureClass RoadFacilityLineFC = null;
        public static IFeatureClass RoadFacilityAreaFC = null;

        public static bool IsValidGeoDataBase(string DbPath)
        {
            var wsf = new FileGDBWorkspaceFactoryClass();
            if (!(Directory.Exists(DbPath) && wsf.IsWorkspace(DbPath)))
            {
                //MessageBox.Show("数据源设置错误!");
                Marshal.ReleaseComObject(wsf);
                return false;
            }
            return true;
        }

        public static void CreateTopology(IFeatureDataset featureDataset, IFeatureClass topoFC, string topoName, esriTopologyRuleType ruleType, string ruleName, double clusterTolerance = -1)
        {
            // Attempt to acquire an exclusive schema lock on the feature dataset.
            ISchemaLock schemaLock = (ISchemaLock)featureDataset;
            
            //
            schemaLock.ChangeSchemaLock(esriSchemaLock.esriExclusiveSchemaLock);

            // Create the topology.
            ITopologyContainer2 topologyContainer = (ITopologyContainer2)featureDataset;
            if (clusterTolerance == -1)
                clusterTolerance = topologyContainer.DefaultClusterTolerance;
            ITopology topology = topologyContainer.CreateTopology(topoName, clusterTolerance, -1, "");

            // Add feature classes and rules to the topology.
            topology.AddClass(topoFC, 5, 1, 1, false);
            AddRuleToTopology(topology, ruleType, ruleName, topoFC);

            // Get an envelope with the topology's extents and validate the topology.
            IGeoDataset geoDataset = (IGeoDataset)topology;
            IEnvelope envelope = geoDataset.Extent;
            ValidateTopology(topology, envelope);

            //    DisplayErrorFeature(topology, ruleType, envelope);
            ITopologyRule topologyRule = new TopologyRuleClass();
            topologyRule.TopologyRuleType = ruleType;
        }
        
        public static void CreateTopology(IFeatureDataset featureDataset, List<IFeatureClass> topoFCList, string topoName, esriTopologyRuleType ruleType, string ruleName, double clusterTolerance = -1)
        {
            ITopology topology = null;

            try
            {
                // Attempt to acquire an exclusive schema lock on the feature dataset.
                ISchemaLock schemaLock = (ISchemaLock)featureDataset;

                //
                schemaLock.ChangeSchemaLock(esriSchemaLock.esriExclusiveSchemaLock);

                // Create the topology.
                ITopologyContainer2 topologyContainer = (ITopologyContainer2)featureDataset;
                if (clusterTolerance == -1)
                    clusterTolerance = topologyContainer.DefaultClusterTolerance;
                if ((featureDataset.Workspace as IWorkspace2).get_NameExists(esriDatasetType.esriDTTopology, topoName))
                {
                    (topologyContainer.get_TopologyByName(topoName) as IDataset).Delete();
                }
                topology = topologyContainer.CreateTopology(topoName, clusterTolerance, -1, "");

                // Add feature classes and rules to the topology.
                if (topoFCList.Count == 1)
                {
                    topology.AddClass(topoFCList[0], 5, 1, 1, false);
                    AddRuleToTopology(topology, ruleType, ruleName, topoFCList[0]);
                }
                else if (topoFCList.Count == 2)
                {
                    topology.AddClass(topoFCList[0], 5, 1, 1, false);
                    topology.AddClass(topoFCList[1], 5, 1, 1, false);
                    AddRuleToTopology(topology, ruleType, ruleName, topoFCList[0], topoFCList[1]);
                }
                else
                {
                    throw new ArgumentException("无法识别拓扑规则！");
                }

                // Get an envelope with the topology's extents and validate the topology.
                IGeoDataset geoDataset = (IGeoDataset)topology;
                IEnvelope envelope = geoDataset.Extent;
                ValidateTopology(topology, envelope);

                //    DisplayErrorFeature(topology, ruleType, envelope);
                ITopologyRule topologyRule = new TopologyRuleClass();
                topologyRule.TopologyRuleType = ruleType;
            }
            catch (Exception ex)
            {
                //删除临时拓扑
                if (topology != null)
                {
                    foreach (var topoFC in topoFCList)
                    {
                        topology.RemoveClass(topoFC);
                    }
                    (topology as IDataset).Delete();
                }

                throw ex;
            }
        }

        public static void ValidateTopology(ITopology topology, IEnvelope envelope)
        {
            // Get the dirty area within the provided envelope.
            IPolygon locationPolygon = new PolygonClass();
            ISegmentCollection segmentCollection = (ISegmentCollection)locationPolygon;
            segmentCollection.SetRectangle(envelope);
            IPolygon polygon = topology.get_DirtyArea(locationPolygon);

            // If a dirty area exists, validate the topology.    
            if (!polygon.IsEmpty)
            {
                // Define the area to validate and validate the topology.
                IEnvelope areaToValidate = polygon.Envelope;
                IEnvelope areaValidated = topology.ValidateTopology(areaToValidate);
            }
        }

        public static void AddRuleToTopology(ITopology topology, esriTopologyRuleType ruleType, String ruleName, IFeatureClass featureClass)
        {
            // Create a topology rule.
            ITopologyRule topologyRule = new TopologyRuleClass();
            topologyRule.TopologyRuleType = ruleType;
            topologyRule.Name = ruleName;
            topologyRule.OriginClassID = featureClass.FeatureClassID;
            topologyRule.AllOriginSubtypes = true;
            // Cast the topology to the ITopologyRuleContainer interface and add the rule.
            ITopologyRuleContainer topologyRuleContainer = (ITopologyRuleContainer)topology;
            if (topologyRuleContainer.get_CanAddRule(topologyRule))
            {
                topologyRuleContainer.AddRule(topologyRule);
            }
            else
            {
                throw new ArgumentException("无法识别拓扑规则！");
            }
        }

        public static void AddRuleToTopology(ITopology topology, esriTopologyRuleType ruleType, String ruleName, IFeatureClass featureClass, IFeatureClass ofeatureClass)
        {
            // Create a topology rule.
            ITopologyRule topologyRule = new TopologyRuleClass();
            topologyRule.TopologyRuleType = ruleType;
            topologyRule.Name = ruleName;
            topologyRule.OriginClassID = featureClass.FeatureClassID;
            topologyRule.OriginSubtype = 1;
            topologyRule.AllOriginSubtypes = true;
            topologyRule.DestinationClassID = ofeatureClass.FeatureClassID;
            topologyRule.DestinationSubtype = 1;
            topologyRule.AllDestinationSubtypes = true;
            // Cast the topology to the ITopologyRuleContainer interface and add the rule.
            ITopologyRuleContainer topologyRuleContainer = (ITopologyRuleContainer)topology;
            if (topologyRuleContainer.get_CanAddRule(topologyRule))
            {
                topologyRuleContainer.AddRule(topologyRule);
            }
            else
            {
                throw new ArgumentException("无法识别拓扑规则！");
            }
        }


        /// <summary>
        /// 创建临时数据集
        /// </summary>
        /// <param name="ws"></param>
        /// <param name="name"></param>
        /// <param name="sr"></param>
        /// <returns></returns>
        public static IFeatureDataset CreateTempFeatureDataset(IFeatureWorkspace ws, string fdtname, ISpatialReference sr, bool bOverride = true)
        {
            if ((ws as IWorkspace2).get_NameExists(esriDatasetType.esriDTFeatureDataset, fdtname))
            {
                if (!bOverride)
                {
                    if (MessageBox.Show(string.Format("要素数据集【{0}】已经存在，是否确定直接覆盖？", fdtname), "提示", MessageBoxButtons.YesNo) == DialogResult.No)
                        return null;
                }

                IFeatureDataset dt = ws.OpenFeatureDataset(fdtname);
                (dt as IDataset).Delete();
            }
            return ws.CreateFeatureDataset(fdtname, sr);
        }

        /// <summary>
        /// 向FeatureDataset中添加要素类,并复制数据
        /// </summary>
        /// <param name="fdt"></param>
        /// <param name="org_fc"></param>
        /// <param name="newFeatureClassName"></param>
        /// <param name="qf"></param>
        /// <param name="bSaveOldOID">是否增加一个字段，记录原要素类中的OID</param>
        /// <param name="addFieldName"></param>
        /// <returns></returns>
        public static IFeatureClass AddFeatureclassToFeatureDataset(IFeatureDataset fdt, IFeatureClass org_fc, string newFeatureClassName, IQueryFilter qf = null, bool bSaveOldOID = true, string addFieldName = "org_oid")
        {
            var idx = newFeatureClassName.LastIndexOf('.');
            if (idx != -1)
            {
                newFeatureClassName = newFeatureClassName.Substring(idx + 1);
            }
            else
            {
                newFeatureClassName = newFeatureClassName.Replace('.', '_');
            }

            IFields expFields;
            expFields = (org_fc.Fields as IClone).Clone() as IFields;
            for (int i = 0; i < expFields.FieldCount; i++)
            {
                IField field = expFields.get_Field(i);
                IFieldEdit fieldEdit = field as IFieldEdit;
                fieldEdit.Name_2 = field.Name.ToUpper();
            }
            if (bSaveOldOID)
            {
                IField pField = new FieldClass();
                IFieldEdit pFieldEdit = (IFieldEdit)pField;
                pFieldEdit.Name_2 = addFieldName;
                pFieldEdit.Type_2 = esriFieldType.esriFieldTypeInteger;
                (expFields as IFieldsEdit).AddField(pField);
            }
            IFeatureClass newFC = CreateFeatureClassWithFeatureDataset(fdt, newFeatureClassName, expFields);
            IFeatureClassLoad fcLoad = newFC as IFeatureClassLoad;
            if (fcLoad != null)
                fcLoad.LoadOnlyMode = true;

            try
            {
                int verIndex = org_fc.FindField(cmdUpdateRecord.CollabVERSION);

                //复制要素
                IFeatureCursor newFeCursor = newFC.Insert(true);

                if (org_fc.HasCollabField())//已删除的要素不参与
                {
                    if (qf.WhereClause != "")
                        qf.WhereClause += " and ";

                    qf.WhereClause += cmdUpdateRecord.CurFeatureFilter;
                }

                IFeatureCursor inFeCursor = org_fc.Search(qf, true);
                IFeature inFe = null;
                while ((inFe = inFeCursor.NextFeature()) != null)
                {
                    IFeatureBuffer featureBuf = newFC.CreateFeatureBuffer();

                    //追加要素
                    featureBuf.Shape = inFe.Shape;
                    for (int j = 0; j < featureBuf.Fields.FieldCount; j++)
                    {
                        IField pfield = featureBuf.Fields.get_Field(j);
                        if (pfield.Type == esriFieldType.esriFieldTypeGeometry || pfield.Type == esriFieldType.esriFieldTypeOID)
                        {
                            continue;
                        }

                        if (pfield.Name == "SHAPE_Length" || pfield.Name == "SHAPE_Area")
                        {
                            continue;
                        }

                        int index = inFe.Fields.FindField(pfield.Name);
                        if (pfield.Name == addFieldName)//增加字段
                        {
                            index = inFe.Fields.FindField("OBJECTID");//记录原要素类的OID
                        }

                        if (index != -1 && pfield.Editable)
                        {
                            featureBuf.set_Value(j, inFe.get_Value(index));
                        }

                    }

                    newFeCursor.InsertFeature(featureBuf);
                }
                System.Runtime.InteropServices.Marshal.ReleaseComObject(inFeCursor);

                newFeCursor.Flush();

                System.Runtime.InteropServices.Marshal.ReleaseComObject(newFeCursor);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.Source);
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (fcLoad != null)
                {
                    fcLoad.LoadOnlyMode = false;
                }
            }

            return newFC;
        }
        

        /// <summary>
        /// 在FeatureDataset下创建要素类
        /// </summary>
        /// <param name="fdt">要素数据集变量</param>
        /// <param name="name">要素类名称</param>
        /// <param name="org_fields">字段集合</param>
        /// <returns>新的要素类</returns>
        public static IFeatureClass CreateFeatureClassWithFeatureDataset(IFeatureDataset fdt, string name, IFields org_fields)
        {
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
                if (target_fields.FindField(field.Name) >= 0)
                {
                    continue;
                }
                IField field_new = (field as ESRI.ArcGIS.esriSystem.IClone).Clone() as IField;
                (target_fields as IFieldsEdit).AddField(field_new);
            }

            System.String strShapeField = string.Empty;

            return fdt.CreateFeatureClass(name, target_fields,
                  featureDescription.InstanceCLSID, featureDescription.ClassExtensionCLSID,
                  esriFeatureType.esriFTSimple,
                  (featureDescription as IFeatureClassDescription).ShapeFieldName,
                  string.Empty);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="featureWorkspace"></param>
        /// <param name="fdtname"></param>
        /// <param name="topologyName"></param>
        /// <returns></returns>
        public static ITopology OpenTopology(IFeatureWorkspace featureWorkspace, string fdtname, string topologyName)
        {
            IFeatureDataset featureDataset = featureWorkspace.OpenFeatureDataset(fdtname);
            ITopologyContainer topologyContainer = (ITopologyContainer)featureDataset;
            ITopology topology = topologyContainer.get_TopologyByName(topologyName);
            return topology;
        }

        /// <summary>
        /// 获取所有要素类
        /// </summary>
        /// <param name="featureDataset">要素集</param>
        /// <returns>要素类列表</returns>
        public static List<IFeatureClass> GetAllFeatureClassFromDataset(IFeatureDataset featureDataset, esriGeometryType geometry)
        {
            IFeatureClassContainer featureClassContainer = (IFeatureClassContainer)featureDataset;
            IEnumFeatureClass enumFeatureClass = featureClassContainer.Classes;
            IFeatureClass featureClass = enumFeatureClass.Next();
            List<IFeatureClass> featureClassList = new List<IFeatureClass>();
            while (featureClass != null)
            {
                if (geometry != esriGeometryType.esriGeometryAny)
                {
                    if (featureClass.ShapeType == geometry)
                    {
                        featureClassList.Add(featureClass);
                    }
                }
                else
                {
                    featureClassList.Add(featureClass);
                }
                featureClass = enumFeatureClass.Next();
            }
            return featureClassList;
        }

        /// <summary>
        /// 获取所有要素集
        /// </summary>
        /// <param name="workspace">工作空间对象</param>
        /// <returns>要素集列表</returns>
        public static List<IFeatureDataset> GetAllFeatureDataset(IWorkspace workspace)
        {
            IEnumDataset dataset = workspace.get_Datasets(esriDatasetType.esriDTFeatureDataset);
            IFeatureDataset featureDataset = dataset.Next() as IFeatureDataset;

            List<IFeatureDataset> featureDatasetList = new List<IFeatureDataset>();
            while (featureDataset != null)
            {
                featureDatasetList.Add(featureDataset);
                featureDataset = dataset.Next() as IFeatureDataset;
            }
            return featureDatasetList;
        }

        /// <summary>
        /// 获取所有要素类
        /// </summary>
        /// <param name="featureDataset">要素集</param>
        /// <returns>要素类列表</returns>
        public static List<IFeatureClass> GetAllFeatureClass(IWorkspace workspace, esriGeometryType geometry)
        {
            IEnumDataset dataset = workspace.get_Datasets(esriDatasetType.esriDTFeatureClass);
            IFeatureClass featureClass = dataset.Next() as IFeatureClass;

            List<IFeatureClass> featureClassList = new List<IFeatureClass>();
            while (featureClass != null)
            {
                if (geometry != esriGeometryType.esriGeometryAny)
                {
                    if (featureClass.ShapeType == geometry)
                    {
                        featureClassList.Add(featureClass);
                    }
                }
                else
                {
                    featureClassList.Add(featureClass);
                }
                featureClass = dataset.Next() as IFeatureClass;
            }
            return featureClassList;
        }

        public static Dictionary<string, IFeatureClass> GetAllFeatureClass(IFeatureWorkspace ws)
        {
            Dictionary<string, IFeatureClass> result = new Dictionary<string, IFeatureClass>();

            if (null == ws)
                return result;

            IEnumDataset enumDataset = (ws as IWorkspace).get_Datasets(esriDatasetType.esriDTAny);
            enumDataset.Reset();
            IDataset dataset = null;
            while ((dataset = enumDataset.Next()) != null)
            {
                if (dataset is IFeatureDataset)//要素数据集
                {
                    IFeatureDataset feDataset = dataset as IFeatureDataset;
                    IEnumDataset subEnumDataset = feDataset.Subsets;
                    subEnumDataset.Reset();
                    IDataset subDataset = null;
                    while ((subDataset = subEnumDataset.Next()) != null)
                    {
                        if (subDataset is IFeatureClass)//要素类
                        {
                            IFeatureClass fc = subDataset as IFeatureClass;
                            if (fc != null)
                                result.Add(subDataset.Name, fc);
                        }
                    }
                    Marshal.ReleaseComObject(subEnumDataset);
                }
                else if (dataset is IFeatureClass)//要素类
                {
                    IFeatureClass fc = dataset as IFeatureClass;
                    if (fc != null)
                        result.Add(dataset.Name, fc);
                }
                else
                {

                }

            }
            Marshal.ReleaseComObject(enumDataset);


            return result;
        }

        /// <summary>
        /// 打开shp文件
        /// </summary>
        /// <param name="shpFileName"></param>
        /// <returns></returns>
        public static IFeatureClass OpenSHPFile(string shpFileName)
        {
            IWorkspaceFactory wsFactory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace featureWS = wsFactory.OpenFromFile(System.IO.Path.GetDirectoryName(shpFileName), 0) as IFeatureWorkspace;

            IFeatureClass fc = featureWS.OpenFeatureClass(System.IO.Path.GetFileNameWithoutExtension(shpFileName));
            Marshal.ReleaseComObject(featureWS);
            Marshal.ReleaseComObject(wsFactory);
            return fc;
        }
        
        /// <summary>
        /// 加载临时图层至地图
        /// </summary>
        /// <param name="map"></param>
        /// <param name="fc"></param>
        public static void AddTempLayerToMap(IMap map, IFeatureClass fc)
        {
            IFeatureLayer fLayer = new FeatureLayerClass();
            fLayer.FeatureClass = fc;
            fLayer.Name = fc.AliasName + "_Temp";


            //加载临时文件
            map.AddLayer(fLayer);
            map.MoveLayer(fLayer, 0);
        }

        /// <summary>
        /// 加载临时图层至地图
        /// </summary>
        /// <param name="map"></param>
        /// <param name="fc"></param>
        public static void AddTempLayerToMapWithDefinitionExpression(IMap map, IFeatureClass fc, string definitionExpression)
        {
            IFeatureLayer fLayer = new FeatureLayerClass();
            fLayer.FeatureClass = fc;
            fLayer.Name = fc.AliasName + "_Temp";
            IFeatureLayerDefinition flDef = fLayer as IFeatureLayerDefinition;
            flDef.DefinitionExpression = definitionExpression;

            //加载临时文件
            map.AddLayer(fLayer);
            map.MoveLayer(fLayer, 0);
        }

        /// <summary>
        /// 以临时文件的形式加载shapefile文件
        /// </summary>
        /// <param name="map"></param>
        /// <param name="shpFileName"></param>
        public static void AddTempLayerFromSHPFile(IMap map, string shpFileName)
        {
            //打开shp文件
            IWorkspaceFactory wsFactory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace featureWS = wsFactory.OpenFromFile(System.IO.Path.GetDirectoryName(shpFileName), 0) as IFeatureWorkspace;

            IFeatureClass fc = featureWS.OpenFeatureClass(System.IO.Path.GetFileNameWithoutExtension(shpFileName));

            IFeatureLayer fLayer = new FeatureLayerClass();
            fLayer.FeatureClass = fc;
            fLayer.Name = fc.AliasName + "_Temp";

            Marshal.ReleaseComObject(featureWS);
            Marshal.ReleaseComObject(wsFactory);

            //加载临时文件
            map.AddLayer(fLayer);
            map.MoveLayer(fLayer, 0);
            
        }

        /// <summary>
        /// 获取配置文件中所有工具的名称
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public static Dictionary<string, string> getAllTools(XmlDocument xmlDoc)
        {
            XmlNodeList nodes = xmlDoc.SelectNodes("/CheckTools/Tool");

            Dictionary<string, string> tools = new Dictionary<string, string>();
            foreach (XmlNode xmlnode in nodes)
            {
                if (xmlnode.NodeType != XmlNodeType.Element)
                    continue;
                string name = ((xmlnode as XmlElement).GetAttribute("name"));
                string fn = (xmlnode as XmlElement).GetAttribute("fn");

                tools.Add(name, fn);
            }

            return tools;
        }


        /// <summary>
        /// 根据工具名获取工具参数信息
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="toolName"></param>
        /// <returns></returns>
        public static XmlNode SearchNodeByToolName(XmlDocument xmlDoc, string toolName)
        {
            XmlNodeList nodes = xmlDoc.SelectNodes("/CheckTools/Tool");

            XmlNode searchnode = null;
            foreach (XmlNode xmlnode in nodes)
            {
                if (xmlnode.NodeType != XmlNodeType.Element)
                    continue;
                string attribute = ((xmlnode as XmlElement).GetAttribute("name"));
                if (attribute == toolName)
                {
                    searchnode = xmlnode;

                    break;
                }
            }

            return searchnode;
        }

        /// <summary>
        /// 根据工具FN获取工具参数信息
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static XmlNode SearchNodeByFN(XmlDocument xmlDoc, string fn)
        {
            XmlNodeList nodes = xmlDoc.SelectNodes("/CheckTools/Tool");

            XmlNode searchnode = null;
            foreach (XmlNode xmlnode in nodes)
            {
                if (xmlnode.NodeType != XmlNodeType.Element)
                    continue;
                string attribute = ((xmlnode as XmlElement).GetAttribute("fn"));
                if (attribute == fn)
                {
                    searchnode = xmlnode;

                    break;
                }
            }

            return searchnode;
        }

        /// <summary>
        /// 返回指定工具的所有参数几何（名称-值列表）
        /// </summary>
        /// <param name="toolNode"></param>
        /// <returns></returns>
        public static Dictionary<string, string> getToolParams(XmlNode toolNode)
        {
            Dictionary<string, string> paramName2Value = new Dictionary<string, string>();
            foreach (XmlNode xmlnode in toolNode.ChildNodes)
            {
                if(xmlnode.NodeType!=XmlNodeType.Element)
                    continue;
                string name = (xmlnode as XmlElement).GetAttribute("name");
                string value = (xmlnode as XmlElement).GetAttribute("value");


                paramName2Value.Add(name, value);
            }

            return paramName2Value;
        }


        /// <summary>
        /// 返回线要素类lineFC中所有不被参考线要素类覆盖的要素OID集合
        /// </summary>
        /// <param name="lineFC"></param>
        /// <param name="lineQF"></param>
        /// <param name="referFC"></param>
        /// <param name="referQF"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static List<int> LineNotCoveredByLineClass(IFeatureClass lineFC, IQueryFilter lineQF, IFeatureClass referFC, IQueryFilter referQF, WaitOperation wo = null)
        {
            List<int> result = new List<int>();

            if (lineFC == null || referFC == null)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "线套合检查", (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_lineFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC, string.Format("{0}_coveredby", lineFC.AliasName), lineQF);
                int orgOIDIndex = temp_lineFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_lineFC.AliasName));
                }
                temp_topoFCList.Add(temp_lineFC);


                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, referFC, string.Format("{0}_covered", referFC.AliasName), referQF, false);
                temp_topoFCList.Add(temp_referFC);

                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "线套合检查结果", esriTopologyRuleType.esriTRTLineCoveredByLineClass, "Must Be Covered By Feature Class Of");

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "线套合检查", "线套合检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineCoveredByLineClass, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_lineFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    result.Add(org_oid);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;

        }

        /// <summary>
        /// 返回线要素类lineFC中所有与参考线要素类中要素有叠置的要素OID集合
        /// </summary>
        /// <param name="lineFC"></param>
        /// <param name="lineQF"></param>
        /// <param name="referFC"></param>
        /// <param name="referQF"></param>
        /// <param name="wo"></param>
        /// <returns>Dictionary<lineFC中重叠要素的OID, referFC中与lineFC中OID要素重叠的要素集合></returns>
        public static Dictionary<int, List<int>> LineOverlapLine(IFeatureClass lineFC, IQueryFilter lineQF, IFeatureClass referFC, IQueryFilter referQF, WaitOperation wo = null)
        {
            Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();

            if (lineFC == null || referFC == null)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "线重叠检查", (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_lineFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC, string.Format("{0}_OverlapBy", lineFC.AliasName), lineQF);
                int orgOIDIndex = temp_lineFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_lineFC.AliasName));
                }
                temp_topoFCList.Add(temp_lineFC);


                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, referFC, string.Format("{0}_Overlap", referFC.AliasName), referQF);
                int orgReferOIDIndex = temp_referFC.FindField("org_oid");
                if (orgReferOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_referFC.AliasName));
                }
                temp_topoFCList.Add(temp_referFC);

                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "线重叠检查结果", esriTopologyRuleType.esriTRTLineNoOverlapLine, "Must Not Overlap With");

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "线重叠检查", "线重叠检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineNoOverlapLine, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_lineFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    IFeature referFe = temp_referFC.GetFeature(topologyErrorFeature.DestinationOID);
                    int org_referOID = int.Parse(referFe.get_Value(orgReferOIDIndex).ToString());

                    if (!result.ContainsKey(org_oid))
                        result.Add(org_oid, new List<int>());

                    if (!result[org_oid].Contains(org_referOID))
                        result[org_oid].Add(org_referOID);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回点要素类ptFC中所有不被线要素类要素覆盖的点OID集合
        /// </summary>
        /// <param name="ptFC"></param>
        /// <param name="ptQF"></param>
        /// <param name="lineFC"></param>
        /// <param name="lineQF"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static List<int> PointNotCoveredByLine(IFeatureClass ptFC, IQueryFilter ptQF, IFeatureClass lineFC, IQueryFilter lineQF, WaitOperation wo = null)
        {
            List<int> result = new List<int>();

            if (ptFC == null || lineFC == null)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "点在线上检查", (ptFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_ptFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, ptFC, string.Format("{0}_coveredby", ptFC.AliasName), ptQF);
                int orgOIDIndex = temp_ptFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_ptFC.AliasName));
                }
                temp_topoFCList.Add(temp_ptFC);


                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC, string.Format("{0}_covered", lineFC.AliasName), lineQF, false);
                temp_topoFCList.Add(temp_referFC);

                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "点在线上检查结果", esriTopologyRuleType.esriTRTPointCoveredByLine, "Point Must Be Covered By Line");

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "点在线上检查", "点在线上检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTPointCoveredByLine, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_ptFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    result.Add(org_oid);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回线要素类lineFC中所有不完全被面要素类要素包含的线OID集合
        /// </summary>
        /// <param name="lineFC"></param>
        /// <param name="lineQF"></param>
        /// <param name="plgFC"></param>
        /// <param name="plgQF"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static List<int> LineNotBeContainedByPolygon(IFeatureClass lineFC, IQueryFilter lineQF, IFeatureClass plgFC, IQueryFilter plgQF, WaitOperation wo = null)
        {
            List<int> result = new List<int>();

            if (lineFC == null || plgFC == null)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "线包含于面内检查", (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_lineFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC, string.Format("{0}_MustBeInsideBy", lineFC.AliasName), lineQF);
                int orgOIDIndex = temp_lineFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_lineFC.AliasName));
                }
                temp_topoFCList.Add(temp_lineFC);


                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, plgFC, string.Format("{0}_MustBeInside", plgFC.AliasName), plgQF, false);
                temp_topoFCList.Add(temp_referFC);

                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "线包含于面内检查结果", esriTopologyRuleType.esriTRTLineInsideArea, "Line Must Be Contained By Polygon");

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "线包含于面内检查", "线包含于面内检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineInsideArea, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_lineFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    if (CheckHelper.ExistsWaterFacilities(fe.ShapeCopy, (lineFC as IDataset).Workspace as IFeatureWorkspace)) continue;

                    result.Add(org_oid);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }


        /// <summary>
        /// 返回线要素类lineFC中所有不被面要素类要素边界覆盖的<线OID->不覆盖几何>集合
        /// </summary>
        /// <param name="lineFC"></param>
        /// <param name="lineQF"></param>
        /// <param name="plgFC"></param>
        /// <param name="plgQF"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<int, List<IPolyline>> LineNotBeCoveredByPolygonBoundaries(IFeatureClass lineFC, IQueryFilter lineQF, IFeatureClass plgFC, IQueryFilter plgQF, WaitOperation wo = null)
        {
            Dictionary<int, List<IPolyline>> result = new Dictionary<int, List<IPolyline>>();

            if (lineFC == null || plgFC == null)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "线被面边界覆盖检查", (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_lineFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC, string.Format("{0}_MustBeCoveredBy", lineFC.AliasName), lineQF);
                int orgOIDIndex = temp_lineFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_lineFC.AliasName));
                }
                temp_topoFCList.Add(temp_lineFC);


                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, plgFC, string.Format("{0}_MustBeCovered", plgFC.AliasName), plgQF, false);
                temp_topoFCList.Add(temp_referFC);

                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "线被面边界覆盖检查结果", esriTopologyRuleType.esriTRTLineCoveredByAreaBoundary, "Line Must Be Covered By Area Boundary");

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "线被面边界覆盖检查", "线被面边界覆盖检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineCoveredByAreaBoundary, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_lineFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    IPolyline pl = (topologyErrorFeature as IFeature).Shape as IPolyline;
                    if (pl == null)
                        continue;

                    if (result.ContainsKey(org_oid))
                    {
                        result[org_oid].Add(pl);
                    }
                    else
                    {
                        List<IPolyline> geoList = new List<IPolyline>();
                        geoList.Add(pl);

                        result.Add(org_oid, geoList);
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回目标面要素类plgFC中所有不被参考面要素类要素边界覆盖的<目标面要素OID->不覆盖几何>集合
        /// </summary>
        /// <param name="plgFC"></param>
        /// <param name="plgQF"></param>
        /// <param name="plgReferFC"></param>
        /// <param name="plgReferQF"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<int, List<IPolyline>> PolygonBoundariesNotBeCoveredByPolygonBoundaries(IFeatureClass plgFC, IQueryFilter plgQF, IFeatureClass plgReferFC, IQueryFilter plgReferQF, WaitOperation wo = null)
        {
            Dictionary<int, List<IPolyline>> result = new Dictionary<int, List<IPolyline>>();

            if (plgFC == null || plgReferFC == null)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "面边界被其它要素边界覆盖检查", (plgFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_plgFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, plgFC, string.Format("{0}_MustBeCoveredBy", plgFC.AliasName), plgQF);
                int orgOIDIndex = temp_plgFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_plgFC.AliasName));
                }
                temp_topoFCList.Add(temp_plgFC);


                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, plgReferFC, string.Format("{0}_MustBeCovered", plgReferFC.AliasName), plgReferQF, false);
                temp_topoFCList.Add(temp_referFC);

                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "面边界被其它要素边界覆盖检查结果", esriTopologyRuleType.esriTRTAreaBoundaryCoveredByAreaBoundary, "Area Boundary Must Be Covered By Area Boundary");

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "面边界被其它要素边界覆盖检查", "面边界被其它要素边界覆盖检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTAreaBoundaryCoveredByAreaBoundary, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_plgFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    IPolyline pl = (topologyErrorFeature as IFeature).Shape as IPolyline;
                    if (pl == null)
                        continue;

                    if (result.ContainsKey(org_oid))
                    {
                        result[org_oid].Add(pl);
                    }
                    else
                    {
                        List<IPolyline> geoList = new List<IPolyline>();
                        geoList.Add(pl);

                        result.Add(org_oid, geoList);
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }
        
        /// <summary>
        /// 返回线要素类中的伪节点信息
        /// </summary>
        /// <param name="lineFC"></param>
        /// <param name="qf"></param>
        /// <param name="clusterTolerance"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<IPoint, KeyValuePair<int,int>> NotHavePseudonodes(IFeatureClass lineFC, IQueryFilter qf, double clusterTolerance = -1, WaitOperation wo = null)
        {
            Dictionary<IPoint, KeyValuePair<int, int>> result = new Dictionary<IPoint, KeyValuePair<int, int>>();

            if (lineFC == null || lineFC.ShapeType != esriGeometryType.esriGeometryPolyline)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "伪节点检查", (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_FC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC, string.Format("{0}_pseudo", lineFC.AliasName), qf);
                int orgOIDIndex = temp_FC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_FC.AliasName));
                }
                temp_topoFCList.Add(temp_FC);


                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "伪节点检查结果", esriTopologyRuleType.esriTRTLineNoPseudos, "Must not have pseudonodes", clusterTolerance);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "伪节点检查", "伪节点检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineNoPseudos, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_FC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid1 = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    fe = temp_FC.GetFeature(topologyErrorFeature.DestinationOID);
                    int org_oid2 = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    IPoint p = (topologyErrorFeature as IFeature).Shape as IPoint;

                    if (CheckHelper.ExistsWaterFacilities(p, (lineFC as IDataset).Workspace as IFeatureWorkspace)) continue;

                    if(p != null)
                        result.Add(p, new KeyValuePair<int, int>(org_oid1, org_oid2));
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }
        
        /// <summary>
        /// 返回线要素类中的悬挂点信息
        /// </summary>
        /// <param name="lineFC"></param>
        /// <param name="qf"></param>
        /// <param name="clusterTolerance"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<int, List<IPoint>> NotHaveDangles(IFeatureClass lineFC, IQueryFilter qf, double clusterTolerance = -1, WaitOperation wo = null)
        {
            Dictionary<int, List<IPoint>> result = new Dictionary<int, List<IPoint>>();

            if (lineFC == null || lineFC.ShapeType != esriGeometryType.esriGeometryPolyline)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "悬挂点检查", (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_FC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC, string.Format("{0}_dangles", lineFC.AliasName), qf);
                int orgOIDIndex = temp_FC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_FC.AliasName));
                }
                temp_topoFCList.Add(temp_FC);


                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "悬挂点检查结果", esriTopologyRuleType.esriTRTLineNoDangles, "Must not have dangles", clusterTolerance);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "悬挂点检查", "悬挂点检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineNoDangles, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_FC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());

                    IPoint p = (topologyErrorFeature as IFeature).Shape as IPoint;

                    if (result.ContainsKey(org_oid))
                    {
                        result[org_oid].Add(p);
                    }
                    else
                    {
                        List<IPoint> ptList = new List<IPoint>();
                        ptList.Add(p);
                        result.Add(org_oid, ptList);
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回面重叠错误
        /// </summary>
        /// <param name="plgFC"></param>
        /// <param name="qf"></param>
        /// <param name="clusterTolerance"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<IPolygon, KeyValuePair<int,int>> AreaNoOverlap(IFeatureClass plgFC, IQueryFilter qf, double clusterTolerance = -1, WaitOperation wo = null)
        {
            Dictionary<IPolygon, KeyValuePair<int, int>> result = new Dictionary<IPolygon, KeyValuePair<int, int>>();

            if (plgFC == null || plgFC.ShapeType != esriGeometryType.esriGeometryPolygon)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "面重叠检查", (plgFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_FC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, plgFC, string.Format("{0}_AreaNoOverlap", plgFC.AliasName), qf);
                int orgOIDIndex = temp_FC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_FC.AliasName));
                }
                temp_topoFCList.Add(temp_FC);


                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "面重叠检查结果", esriTopologyRuleType.esriTRTAreaNoOverlap, "Area Must not have Overlap", clusterTolerance);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "面重叠检查", "面重叠检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTAreaNoOverlap, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe1 = temp_FC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid1 = int.Parse(fe1.get_Value(orgOIDIndex).ToString());

                    IFeature fe2 = temp_FC.GetFeature(topologyErrorFeature.DestinationOID);
                    int org_oid2 = int.Parse(fe2.get_Value(orgOIDIndex).ToString());

                    IPolygon errShape = (topologyErrorFeature as IFeature).Shape as IPolygon;

                    result.Add(errShape, new KeyValuePair<int,int>(org_oid1, org_oid2));
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回点重叠错误
        /// </summary>
        /// <param name="fc"></param>
        /// <param name="qf"></param>
        /// <param name="clusterTolerance"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static List<KeyValuePair<int, int>> PointNoOverlap(IFeatureClass fc, IQueryFilter qf, double clusterTolerance = -1, WaitOperation wo = null)
        {
            List<KeyValuePair<int, int>> result = new List<KeyValuePair<int, int>>();

            if (fc == null || fc.ShapeType != esriGeometryType.esriGeometryPoint)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "点重叠检查", (fc as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_FC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, fc, string.Format("{0}_PointNoOverlap", fc.AliasName), qf);
                int orgOIDIndex = temp_FC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_FC.AliasName));
                }
                temp_topoFCList.Add(temp_FC);


                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "点重叠检查结果", esriTopologyRuleType.esriTRTPointDisjoint, "Point Must Disjoint", clusterTolerance);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "点重叠检查", "点重叠检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTPointDisjoint, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe1 = temp_FC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid1 = int.Parse(fe1.get_Value(orgOIDIndex).ToString());

                    IFeature fe2 = temp_FC.GetFeature(topologyErrorFeature.DestinationOID);
                    int org_oid2 = int.Parse(fe2.get_Value(orgOIDIndex).ToString());

                    result.Add(new KeyValuePair<int,int>(org_oid1,org_oid2));
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回线重叠错误
        /// </summary>
        /// <param name="fc"></param>
        /// <param name="qf"></param>
        /// <param name="clusterTolerance"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<IPolyline, KeyValuePair<int, int>> LineNoOverlap(IFeatureClass fc, IQueryFilter qf, double clusterTolerance = -1, WaitOperation wo = null)
        {
            Dictionary<IPolyline, KeyValuePair<int, int>> result = new Dictionary<IPolyline, KeyValuePair<int, int>>();

            if (fc == null || fc.ShapeType != esriGeometryType.esriGeometryPolyline)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "线重叠检查", (fc as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_FC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, fc, string.Format("{0}_LineNoOverlap", fc.AliasName), qf);
                int orgOIDIndex = temp_FC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_FC.AliasName));
                }
                temp_topoFCList.Add(temp_FC);


                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "线重叠检查结果", esriTopologyRuleType.esriTRTLineNoOverlap, "Line Must not have Overlap", clusterTolerance);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "线重叠检查", "线重叠检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineNoOverlap, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe1 = temp_FC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid1 = int.Parse(fe1.get_Value(orgOIDIndex).ToString());

                    IFeature fe2 = temp_FC.GetFeature(topologyErrorFeature.DestinationOID);
                    int org_oid2 = int.Parse(fe2.get_Value(orgOIDIndex).ToString());

                    IPolyline errShape = (topologyErrorFeature as IFeature).Shape as IPolyline;

                    result.Add(errShape, new KeyValuePair<int, int>(org_oid1, org_oid2));
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回线交叉错误
        /// </summary>
        /// <param name="fc"></param>
        /// <param name="qf"></param>
        /// <param name="clusterTolerance"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<IPoint, KeyValuePair<int, int>> LineNoCross(IFeatureClass fc, IQueryFilter qf, double clusterTolerance = -1, WaitOperation wo = null)
        {
            Dictionary<IPoint, KeyValuePair<int, int>> result = new Dictionary<IPoint, KeyValuePair<int, int>>();
            
            if (fc == null || fc.ShapeType != esriGeometryType.esriGeometryPolyline)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "线交叉检查", (fc as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_FC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, fc, string.Format("{0}_LineNoCross", fc.AliasName), qf);
                int orgOIDIndex = temp_FC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_FC.AliasName));
                }
                temp_topoFCList.Add(temp_FC);


                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "线交叉检查结果", esriTopologyRuleType.esriTRTLineNoIntersectOrInteriorTouch, "Line Must not intersect", clusterTolerance);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "线交叉检查", "线交叉检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                //IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineNoIntersectOrInteriorTouchLine, geoDatasetTopo.Extent, true, false);
                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineNoIntersectOrInteriorTouch, geoDatasetTopo.Extent, true, false);
                
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe1 = temp_FC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid1 = int.Parse(fe1.get_Value(orgOIDIndex).ToString());

                    IFeature fe2 = temp_FC.GetFeature(topologyErrorFeature.DestinationOID);
                    int org_oid2 = int.Parse(fe2.get_Value(orgOIDIndex).ToString());

                    IPoint errShape = (topologyErrorFeature as IFeature).Shape as IPoint;
                    if (errShape == null) //自重叠的类型此处不查（线段导不出点）
                        continue;

                    // ==== 空间过滤：排除错误在附属设施中 ====
                    if (CheckHelper.ExistsWaterFacilities(errShape, (fc as IDataset).Workspace as IFeatureWorkspace)) continue;

                    result.Add(errShape, new KeyValuePair<int, int>(org_oid1, org_oid2));
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回线要素类lineFC中所有与plgFC相交的线的原始OID对与对应图层名称
        /// </summary>
        public static List<Tuple<IPoint, int, string, int, string>> CrossLayerLineNoCross(
            IFeatureClass lineFC,
            IQueryFilter lineQF,
            IFeatureClass plgFC,
            IQueryFilter plgQF,
            WaitOperation wo = null)
        {
            List<Tuple<IPoint, int, string, int, string>> result = new List<Tuple<IPoint, int, string, int, string>>();

            if (lineFC == null || plgFC == null)
                return result;

            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "跨图层线交叉",
                    (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                    throw new Exception("创建临时拓扑数据集失败！");

                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_lineFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC,
                    string.Format("{0}_LineIntersect", lineFC.AliasName), lineQF);
                int orgOIDIndex = temp_lineFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_lineFC.AliasName));
                temp_topoFCList.Add(temp_lineFC);

                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, plgFC,
                    string.Format("{0}_LineIntersectBy", plgFC.AliasName), plgQF, false);
                temp_topoFCList.Add(temp_referFC);

                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "跨图层线交叉检查结果",
                    esriTopologyRuleType.esriTRTLineNoIntersectLine, "Line Must not intersect", -1);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                temp_topology = CheckHelper.OpenTopology(fws, "跨图层线交叉", "跨图层线交叉检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(
                    SpatialReference,
                    esriTopologyRuleType.esriTRTLineNoIntersectLine,
                    geoDatasetTopo.Extent,
                    true,
                    false);

                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_lineFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());
                    string org_fc_name = lineFC.AliasName;
                    int dst_oid = topologyErrorFeature.DestinationOID;
                    string dst_fc_name = plgFC.AliasName;

                    IPoint errShape = (topologyErrorFeature as IFeature).Shape as IPoint;
                    if (errShape == null) //自重叠的类型此处不查（线段导不出点）
                        continue;

                    // ==== 空间过滤：排除错误在附属设施中 ====
                    if (CheckHelper.ExistsWaterFacilities(errShape, (lineFC as IDataset).Workspace as IFeatureWorkspace)) continue;

                    result.Add(Tuple.Create(errShape, org_oid, org_fc_name, dst_oid, dst_fc_name));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回线要素类lineFC中所有与plgFC未重叠的线的原始OID对与对应图层名称
        /// </summary>
        public static List<Tuple<int, string, int, string>> CrossLayerLineOverlap(
            IFeatureClass lineFC,
            IQueryFilter lineQF,
            IFeatureClass plgFC,
            IQueryFilter plgQF,
            WaitOperation wo = null)
        {
            List<Tuple<int, string, int, string>> result = new List<Tuple<int, string, int, string>>();

            if (lineFC == null || plgFC == null)
                return result;

            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "跨图层线重叠",
                    (lineFC as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                    throw new Exception("创建临时拓扑数据集失败！");

                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_lineFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, lineFC,
                    string.Format("{0}_LineNoCovered", lineFC.AliasName), lineQF);
                int orgOIDIndex = temp_lineFC.FindField("org_oid");
                if (orgOIDIndex == -1)
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_lineFC.AliasName));
                temp_topoFCList.Add(temp_lineFC);

                var temp_referFC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, plgFC,
                    string.Format("{0}_LineNoCoveredBy", plgFC.AliasName), plgQF, false);
                temp_topoFCList.Add(temp_referFC);

                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "跨图层线重叠检查结果",
                    esriTopologyRuleType.esriTRTLineCoveredByLineClass, "Line Must be Covered", -1);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                temp_topology = CheckHelper.OpenTopology(fws, "跨图层线重叠", "跨图层线重叠检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(
                    SpatialReference,
                    esriTopologyRuleType.esriTRTLineCoveredByLineClass,
                    geoDatasetTopo.Extent,
                    true,
                    false);

                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe = temp_lineFC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid = int.Parse(fe.get_Value(orgOIDIndex).ToString());
                    string org_fc_name = lineFC.AliasName;
                    int dst_oid = topologyErrorFeature.DestinationOID;
                    string dst_fc_name = plgFC.AliasName;

                    result.Add(Tuple.Create(org_oid, org_fc_name, dst_oid, dst_fc_name));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回自相交的错误
        /// </summary>
        /// <param name="fc"></param>
        /// <param name="qf"></param>
        /// <param name="clusterTolerance"></param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public static Dictionary<IGeometry, int> LineNoSelfIntersect(IFeatureClass fc, IQueryFilter qf, double clusterTolerance = -1, WaitOperation wo = null)
        {
            Dictionary<IGeometry, int> result = new Dictionary<IGeometry, int>();

            if (fc == null || fc.ShapeType != esriGeometryType.esriGeometryPolyline)
                return result;

            //临时数据库
            string fullPath = DCDHelper.GetAppDataPath() + "\\MyWorkspace.gdb";
            IWorkspace ws = DCDHelper.createTempWorkspace(fullPath);
            IFeatureWorkspace fws = ws as IFeatureWorkspace;

            IFeatureDataset temp_fdt = null;
            ITopology temp_topology = null;
            List<IFeatureClass> temp_topoFCList = new List<IFeatureClass>();

            try
            {
                //创建临时数据集及要素类
                if (wo != null)
                    wo.SetText("正在创建临时拓扑数据集......");

                temp_fdt = CheckHelper.CreateTempFeatureDataset(fws, "自相交检查", (fc as IGeoDataset).SpatialReference);
                if (temp_fdt == null)
                {
                    throw new Exception("创建临时拓扑数据集失败！");
                }

                //添加参与拓扑分析的要素类
                if (wo != null)
                    wo.SetText("正在提取要素......");
                var temp_FC = CheckHelper.AddFeatureclassToFeatureDataset(temp_fdt, fc, string.Format("{0}_LineNoSelfIntersect", fc.AliasName), qf);
                int orgOIDIndex = temp_FC.FindField("org_oid");
                if (orgOIDIndex == -1)
                {
                    throw new Exception(string.Format("创建要素类【{0}】失败！", temp_FC.AliasName));
                }
                temp_topoFCList.Add(temp_FC);


                //构建拓扑
                if (wo != null)
                    wo.SetText("正在构建拓扑......");
                CheckHelper.CreateTopology(temp_fdt, temp_topoFCList, "自相交检查结果", esriTopologyRuleType.esriTRTLineNoSelfIntersect, "Line Must not Self Intersect", clusterTolerance);

                if (wo != null)
                    wo.SetText("正在输出检查结果......");

                //获取拓扑结果
                temp_topology = CheckHelper.OpenTopology(fws, "自相交检查", "自相交检查结果");
                IErrorFeatureContainer errorFeatureContainer = (IErrorFeatureContainer)temp_topology;
                IGeoDataset geoDatasetTopo = (IGeoDataset)temp_topology;
                ISpatialReference SpatialReference = geoDatasetTopo.SpatialReference;

                IEnumTopologyErrorFeature enumTopologyErrorFeature = errorFeatureContainer.get_ErrorFeaturesByRuleType(SpatialReference, esriTopologyRuleType.esriTRTLineNoSelfIntersect, geoDatasetTopo.Extent, true, false);
                ITopologyErrorFeature topologyErrorFeature = null;
                while ((topologyErrorFeature = enumTopologyErrorFeature.Next()) != null)
                {
                    IFeature fe1 = temp_FC.GetFeature(topologyErrorFeature.OriginOID);
                    int org_oid1 = int.Parse(fe1.get_Value(orgOIDIndex).ToString());

                    result.Add((topologyErrorFeature as IFeature).Shape, org_oid1);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时拓扑
                if (temp_topology != null)
                {
                    foreach (var topoFC in temp_topoFCList)
                    {
                        temp_topology.RemoveClass(topoFC);
                    }
                    (temp_topology as IDataset).Delete();
                    temp_topology = null;
                }

                //删除临时要素类
                foreach (var topoFC in temp_topoFCList)
                {
                    (topoFC as IDataset).Delete();
                }
                temp_topoFCList = null;

                //删除临时数据集
                if (temp_fdt != null)
                {
                    (temp_fdt as IDataset).Delete();
                    temp_fdt = null;
                }
            }

            return result;
        }         

        public static DataTable getConfigTable(string mdbPath, string tabaleName, int scale)
        {
            DataTable dt = null;
            if (scale == 1000000)
            {
                tabaleName += "100W";
            }
            else if (scale == 500000)
            {
                tabaleName += "50W";
            }
            else if (scale == 250000)
            {
                tabaleName += "25W";
            }
            else if (scale == 100000)
            {
                tabaleName += "10W";
            }
            else if (scale == 50000)
            {
                tabaleName += "5W";
            }
            else if (scale == 25000)
            {
                tabaleName += "2W5"; 
            }
            else if (scale == 10000)
            {
                tabaleName += "1W";
            }
            else
            {
                MessageBox.Show(string.Format("未找到当前设置的参考比例尺【{0}】对应的检查规则表!", scale));
                return null;
            }

            dt = CommonMethods.ReadToDataTable(mdbPath, tabaleName);
            if (dt == null)
            {
                return null;
            }

            return dt;
        }

        public static DataTable ReadToDataTable(string mdbFilePath, string tableName)
        {
            if (!System.IO.File.Exists(mdbFilePath))
                return null;

            IWorkspaceFactory wsFactory = new AccessWorkspaceFactoryClass();
            IWorkspace ws = wsFactory.OpenFromFile(mdbFilePath, 0);
            IEnumDataset enumDataset = ws.get_Datasets(esriDatasetType.esriDTAny);
            enumDataset.Reset();
            IDataset dataset = enumDataset.Next();
            ITable table = null;
            while (dataset != null)
            {
                if (dataset.Name == tableName)
                {
                    table = dataset as ITable;
                    break;
                }
                dataset = enumDataset.Next();
            }
            Marshal.ReleaseComObject(enumDataset);

            if (table == null)
                return null;

            DataTable dt = new DataTable();
            ICursor cursor = table.Search(null, false);
            IRow row = null;
            //添加表的字段信息
            for (int i = 0; i < table.Fields.FieldCount; i++)
            {
                dt.Columns.Add(table.Fields.Field[i].Name);
            }
            //添加数据
            while ((row = cursor.NextRow()) != null)
            {
                DataRow dr = dt.NewRow();
                for (int i = 0; i < row.Fields.FieldCount; i++)
                {
                    object obValue = row.get_Value(i);
                    if (obValue != null && !Convert.IsDBNull(obValue))
                    {
                        dr[i] = row.get_Value(i);
                    }
                    else
                    {
                        dr[i] = "";
                    }
                }
                dt.Rows.Add(dr);
            }
            Marshal.ReleaseComObject(cursor);


            Marshal.ReleaseComObject(ws);
            Marshal.ReleaseComObject(wsFactory);

            return dt;
        }

        public static void ExportFeatureClassToShapefile(IFeatureClass fc, string shpFullPath)
        {
            string filePath = null;
            string fileName = null;
            filePath = System.IO.Path.GetDirectoryName(shpFullPath);
            fileName = System.IO.Path.GetFileNameWithoutExtension(shpFullPath);
            IWorkspaceFactory wsf = new ShapefileWorkspaceFactoryClass();
            IWorkspace outWorkspace = wsf.OpenFromFile(filePath, 0);

            IDataset inDataSet = fc as IDataset;
            IFeatureClassName inFCName = inDataSet.FullName as IFeatureClassName;
            IWorkspace inWorkspace = inDataSet.Workspace;


            IDataset outDataSet = outWorkspace as IDataset;
            IWorkspaceName outWorkspaceName = outDataSet.FullName as IWorkspaceName;
            IFeatureClassName outFCName = new FeatureClassNameClass();
            IDatasetName outDataSetName = outFCName as IDatasetName;
            outDataSetName.WorkspaceName = outWorkspaceName;
            outDataSetName.Name = fileName;

            IFieldChecker fieldChecker = new FieldCheckerClass();
            fieldChecker.InputWorkspace = inWorkspace;
            fieldChecker.ValidateWorkspace = outWorkspace;
            IFields fields = fc.Fields;
            IFields outFields = null;
            IEnumFieldError enumFieldError = null;
            fieldChecker.Validate(fields, out enumFieldError, out outFields);

            IFeatureDataConverter featureDataConverter = new FeatureDataConverterClass();
            featureDataConverter.ConvertFeatureClass(inFCName, null, null, outFCName, null, outFields, "", 100, 0);
        }

        public static bool DeleteShapeFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactory();//文件夹
                    IFeatureWorkspace featureWorkspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(fileName), 0) as IFeatureWorkspace;
                    IFeatureClass fc = featureWorkspace.OpenFeatureClass(System.IO.Path.GetFileName(fileName));
                    (fc as IDataset).Delete();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    System.Diagnostics.Trace.WriteLine(ex.Source);
                    System.Diagnostics.Trace.WriteLine(ex.StackTrace);

                    MessageBox.Show(string.Format("删除文件失败【{0}】!", fileName));

                    throw ex;
                }
            }

            return true;
        }

        public static bool ExistsWaterFacilities(IGeometry geometry, IFeatureWorkspace featureWorkspace)
        {
            bool flag = false;

            if (featureWorkspace == null || geometry == null)
            {
                return flag;
            }

            ReadWaterFacilitiesConfig();

            #region 逐项检查
            foreach (var tt in waterFacilitiesTts)
            {
                string ptName = tt.Item1;
                string ptSQL = tt.Item2;
                string beizhu = tt.Item3;
                IFeatureClass ptFC = null;

                try
                {
                    ptFC = featureWorkspace.OpenFeatureClass(ptName);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (ptFC == null)
                {
                    continue;
                }

                try
                {
                    // 判断图层根据要素类型设定空间关系
                    esriSpatialRelEnum sr;
                    if (ptFC.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        sr = esriSpatialRelEnum.esriSpatialRelIntersects;
                    }
                    else
                    {
                        sr = esriSpatialRelEnum.esriSpatialRelWithin;
                    }

                    ISpatialFilter filter = new SpatialFilterClass();
                    filter.Geometry = geometry;
                    filter.SpatialRel = sr;
                    filter.GeometryField = ptFC.ShapeFieldName;

                    if (!string.IsNullOrEmpty(ptSQL))
                    {
                        filter.WhereClause = ptSQL;
                    }

                    IFeatureCursor cursor = ptFC.Search(filter, false);
                    IFeature feature = cursor.NextFeature();

                    // 释放游标资源（建议加上）
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(cursor);

                    if (feature != null)
                    {
                        flag = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    System.Diagnostics.Trace.WriteLine(ex.Source);
                    System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                    continue;
                }
            }
            #endregion

            return flag;
        }

        private static List<Tuple<string, string, string>> waterFacilitiesTts = new List<Tuple<string, string, string>>(); //配置信息表（单行）

        //读取质检内容配置表
        private static void ReadWaterFacilitiesConfig()
        {
            waterFacilitiesTts.Clear();
            string dbPath = GApplication.Application.Template.Root + @"\质检\质检内容配置.xlsx";
            string tableName = "水系附属设施";
            DataTable ruleDataTable = CommonMethods.ReadToDataTable(dbPath, tableName);
            if (ruleDataTable == null)
            {
                return;
            }
            for (int i = 0; i < ruleDataTable.Rows.Count; i++)
            {
                string ptName = (ruleDataTable.Rows[i]["FCName"]).ToString();
                string ptSQL = (ruleDataTable.Rows[i]["FilterString"]).ToString();
                string beizhu = (ruleDataTable.Rows[i]["Beizhu"]).ToString();
                Tuple<string, string, string > tt = new Tuple<string, string, string>(ptName, ptSQL, beizhu);
                waterFacilitiesTts.Add(tt);
            }
        }
    }
}
