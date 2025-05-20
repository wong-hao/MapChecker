using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SMGI.Common;
using System.IO;
using System.Xml;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using SMGI.Plugin.CartographicGeneralization;
using Path = System.IO.Path;
using Excel = Microsoft.Office.Interop.Excel;

namespace SMGI.Plugin.CartographicGeneralization
{
    public partial class batchDataCheckForm : Form
    {
        private List<string> _checkReusltFileNameList;
        private XmlDocument _checkToolConfigXml;
        private Dictionary<string, MethodInfo> fn2MethodInfo;
        private IFeatureWorkspace _fws;
        private IWorkspace _ws;
        private StreamWriter _logSW;
        private GApplication _app;
        Dictionary<string, string> name2FN = new Dictionary<string, string>();
        private string schemeName = string.Empty;
        private string referenceScale = string.Empty;

        private string excelPath
        {
            get;
            set;
        }

        private string configurationPath
        {
            get;
            set;
        }

        public string ResultOutputFilePath
        {
            get
            {
                return tbOutFilePath.Text;
            }
        }

        public double ReferScale
        {
            get
            {
                return double.Parse(referenceScale);
            }
        }

        public batchDataCheckForm(GApplication app, string schemesPath)
        {
            InitializeComponent();

            _app = app;
            _checkReusltFileNameList = new List<string>();
            _checkToolConfigXml = null;
            fn2MethodInfo = new Dictionary<string, MethodInfo>();
            _fws = null;
            _ws = null;
            _logSW = null;

            excelPath = schemesPath;
        }
        private string _path;

        private void batchDataCheckForm_Load(object sender, EventArgs e)
        {
            #region 初始化方案列表

            if (Directory.Exists(excelPath))
            {
                string[] directories = Directory.GetDirectories(excelPath);

                SchemesComboBox.Items.Clear();

                foreach (string dir in directories)
                {
                    string folderName = Path.GetFileName(dir);
                    SchemesComboBox.Items.Add(folderName);
                }

                if (SchemesComboBox.Items.Count > 0)
                {
                    SchemesComboBox.SelectedIndex = 0; // 会触发 SelectedIndexChanged，加载规则树
                }
            }
            else
            {
                MessageBox.Show("方案路径不存在：" + excelPath);
            }

            #endregion

            #region 初始化目标gdb

            if (GApplication.Application != null && GApplication.Application.Workspace != null)
            {
                tbGDBFilePath.Text = GApplication.Application.Workspace.EsriWorkspace.PathName;
            }

            #endregion

            // 初始化质检工具项
            string xmlFile = _app.Template.Root + "\\质检\\CheckTool.xml";
            if (!File.Exists(xmlFile))
            {
                MessageBox.Show("无法找到质检工具配置文件【" + xmlFile + "】！");
                return;
            }

            _checkToolConfigXml = new XmlDocument();
            _checkToolConfigXml.Load(xmlFile);
            XmlNode checkToolsNode = _checkToolConfigXml.SelectSingleNode("/CheckTools");
            XmlNodeList toolsNode = _checkToolConfigXml.SelectNodes("/CheckTools/Tool");

            List<string> scaleList = (checkToolsNode as XmlElement).GetAttribute("scale").Split('/').ToList();
            foreach (var item in scaleList)
            {
                cbx_Scale.Items.Add(item);
            }

            foreach (TreeNode schemeCategoryNode in chkToolTreeView.Nodes)
            {
                foreach (TreeNode opNode in schemeCategoryNode.Nodes)
                {
                    string methodName = opNode.Tag as string;
                    if (!string.IsNullOrEmpty(methodName) && !fn2MethodInfo.ContainsKey(methodName))
                    {
                        MethodInfo mf = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mf != null)
                        {
                            fn2MethodInfo.Add(methodName, mf);
                        }
                    }
                }
            }

            #region 其它初始化

            cbx_Scale.SelectedIndex = 0;

            _path = OutputSetup.GetDir();
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path);
            }
            tbOutFilePath.Text = _path + "_批量质检";

            #endregion
        }

        private void btnGDB_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "选择GDB工程文件夹";
            fbd.ShowNewFolderButton = false;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!GApplication.GDBFactory.IsWorkspace(fbd.SelectedPath))
                {
                    MessageBox.Show("不是有效的GDB文件");
                    return;
                }

                tbGDBFilePath.Text = fbd.SelectedPath;
            }
        }

        private void btnOutputPath_Click(object sender, EventArgs e)
        {
            var fd = new FolderBrowserDialog();
            if (fd.ShowDialog() == DialogResult.OK && fd.SelectedPath.Length > 0)
            {
                string filepath = OutputSetup.GetDir();
                tbOutFilePath.Text = fd.SelectedPath + string.Format(@"\{0}_{1}", filepath.Substring(filepath.LastIndexOf(@"\") + 1), "批量质检");
                if (Directory.Exists(filepath))
                {
                    Directory.Delete(filepath);
                }
            }
        }

        private List<TreeNode> GetCheckedLeafNodes(TreeNodeCollection nodes)
        {
            var result = new List<TreeNode>();
            foreach (TreeNode node in nodes)
            {
                if (node.Checked)
                {
                    if (node.Nodes.Count == 0)
                    {
                        result.Add(node); // 是叶子节点
                    }
                }

                // 递归处理子节点
                if (node.Nodes.Count > 0)
                {
                    result.AddRange(GetCheckedLeafNodes(node.Nodes));
                }
            }
            return result;
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(tbOutFilePath.Text))
            {
                Directory.CreateDirectory(tbOutFilePath.Text);
            }

            if (tbGDBFilePath.Text == "")
            {
                MessageBox.Show("待检查数据库路径不能为空！");
                return;
            }

            var wsf = new FileGDBWorkspaceFactoryClass();
            if (!(Directory.Exists(tbGDBFilePath.Text) && wsf.IsWorkspace(tbGDBFilePath.Text)))
            {
                MessageBox.Show("输入的待检查数据库无效！");
                Marshal.ReleaseComObject(wsf);
                return;
            }

            double scale = 0;
            double.TryParse(cbx_Scale.Text, out scale);
            if (scale <= 0)
            {
                MessageBox.Show("输入的比例尺必须为一个正数！");
                return;
            }

            List<TreeNode> checkedLeafNodes = GetCheckedLeafNodes(chkToolTreeView.Nodes);
            if (checkedLeafNodes.Count == 0)
            {
                MessageBox.Show("请选择至少一个检查项！");
                return;
            }

            if (tbOutFilePath.Text == "")
            {
                MessageBox.Show("请指定检查结果输出路径！");
                return;
            }

            schemeName = SchemesComboBox.SelectedItem != null ? SchemesComboBox.SelectedItem.ToString() : null;

            if (string.IsNullOrEmpty(schemeName))
            {
                MessageBox.Show("当前未选择任何方案！");
                return;
            }

            referenceScale = cbx_Scale.Text;

            if (string.IsNullOrEmpty(referenceScale))
            {
                MessageBox.Show("参考比例尺不能为空!");
                return;
            }

            btOK.Enabled = false;

            string logFileName = Path.Combine(ResultOutputFilePath, "CheckLog.txt");
            var logFS = File.Open(logFileName, FileMode.Create);
            _logSW = new StreamWriter(logFS, Encoding.Default);

            using (var wo = _app.SetBusy())
            {
                wo.SetText("正在处理......");
                _ws = wsf.OpenFromFile(tbGDBFilePath.Text, 0);
                _fws = _ws as IFeatureWorkspace;

                int total = checkedLeafNodes.Count;
                int completed = 0;

                foreach (TreeNode node in checkedLeafNodes)
                {
                    string operatorName = node.Text;
                    string methodName = node.Tag.ToString();

                    wo.SetText("正在执行【" + operatorName + "】......");

                    MethodInfo mf = fn2MethodInfo[methodName];
                    mf.Invoke(this, null);

                    completed++;
                    int progressPercent = (int)((double)completed / total * 100);

                    CheckerProgressBar.Minimum = 0;
                    CheckerProgressBar.Maximum = 100;
                    CheckerProgressBar.Style = ProgressBarStyle.Continuous;
                    CheckerProgressBar.Value = progressPercent;
                    CheckerProgressBar.Refresh();

                    CheckerProgressLabel.Text = "进度：" + progressPercent + "%（" + completed + "/" + total + "）";
                    Application.DoEvents();
                }

                _logSW.Flush();
                logFS.Close();
            }

            btOK.Enabled = true;

            Marshal.ReleaseComObject(_fws);
            Marshal.ReleaseComObject(wsf);
            this.Close();

            if (DialogResult.Yes == MessageBox.Show("检查完毕，是否查看日志信息？", "完成", MessageBoxButtons.YesNo))
            {
                System.Diagnostics.Process.Start(logFileName);
            }
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(tbOutFilePath.Text))
            {
                Directory.Delete(tbOutFilePath.Text);
            }
            if (btOK.Enabled == false)
            {
                if (MessageBox.Show("正在执行检查，是否确定要中止？", "警告", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;
            }
        }

        #region 函数
        private Dictionary<string, string> readCheckToolConfigFile(XmlNodeList tools)
        {
            Dictionary<string, string> name2FN = new Dictionary<string, string>();
            foreach (XmlNode xmlnode in tools)
            {
                if (xmlnode.NodeType != XmlNodeType.Element)
                    continue;

                string name = ((xmlnode as XmlElement).GetAttribute("name"));
                string fn = (xmlnode as XmlElement).GetAttribute("fn");

                name2FN.Add(name, fn);
            }


            return name2FN;
        }

        private XmlNode SearchNodeByFN(XmlDocument xmlDoc, string fn)
        {
            XmlNode toolNode = null;

            XmlNodeList nodes = xmlDoc.SelectNodes("/CheckTools/Tool");
            foreach (XmlNode xmlnode in nodes)
            {
                if (xmlnode.NodeType != XmlNodeType.Element)
                    continue;

                string attribute = ((xmlnode as XmlElement).GetAttribute("fn"));
                if (attribute == fn)
                {
                    toolNode = xmlnode;
                    break;
                }
            }

            return toolNode;
        }
        #endregion

        #region 检查函数

        // 面要素宽度检查
        private void Check_PolygonWidthCmdJS()
        {
            string toolName = "Check_PolygonWidthCmdJS";
            string excelName = string.Empty;
            string sheetName = string.Empty;

            // 获取勾选的叶子节点
            List<TreeNode> checkedLeafNodes = GetCheckedLeafNodes(chkToolTreeView.Nodes);
            foreach (TreeNode node in checkedLeafNodes)
            {
                if (node.Tag != null && node.Tag.ToString() == toolName)
                {
                    excelName = node.Text.Trim();           // 节点名即为文件名（不带扩展名）
                    sheetName = node.Tag.ToString().Trim(); // Tag 是方法名
                    break;
                }
            }

            if (string.IsNullOrEmpty(excelName))
            {
                MessageBox.Show("未选中当前方法的配置节点！");
                return;
            }

            Dictionary<KeyValuePair<string, string>, double> _feType2Width = new Dictionary<KeyValuePair<string, string>, double>();

            try
            {
                string schemePath = Path.Combine(excelPath, schemeName);

                // 遍历所有类别子目录，查找 Excel 文件
                string[] categoryDirs = Directory.GetDirectories(schemePath);
                string targetExcelPath = null;

                foreach (string categoryDir in categoryDirs)
                {
                    string[] excelFiles = Directory.GetFiles(categoryDir, excelName + "*.xlsx");
                    if (excelFiles.Length > 0)
                    {
                        targetExcelPath = excelFiles[0]; // 取第一个匹配的 Excel
                        break;
                    }
                }

                if (string.IsNullOrEmpty(targetExcelPath))
                {
                    MessageBox.Show("未找到指定的配置文件！");
                    return;
                }

                // 读取 Excel 表为 DataTable
                DataTable ruleDataTable = CommonMethods.ReadToDataTableForChecker(targetExcelPath, sheetName);
                if (ruleDataTable == null || ruleDataTable.Rows.Count == 0)
                {
                    MessageBox.Show("配置文件表内容为空或读取失败！");
                    return;
                }

                // 校验列名
                bool hasFilter = ruleDataTable.Columns.Contains("FilterString1") || ruleDataTable.Columns.Contains("sqlFilter");
                bool hasMinWidth = ruleDataTable.Columns.Contains("MinWidth") || ruleDataTable.Columns.Contains("minWidth");

                if (!ruleDataTable.Columns.Contains("FCName1") || !hasFilter || !hasMinWidth)
                {
                    MessageBox.Show("表缺少必要列！");
                    return;
                }

                string filterColumn = ruleDataTable.Columns.Contains("FilterString1") ? "FilterString1" : "sqlFilter";
                string minWidthColumn = ruleDataTable.Columns.Contains("MinWidth") ? "MinWidth" : "minWidth";

                _feType2Width.Clear();

                for (int i = 0; i < ruleDataTable.Rows.Count; i++)
                {
                    DataRow row = ruleDataTable.Rows[i];

                    string fcName = row["FCName1"].ToString().Trim();
                    string filter = row[filterColumn].ToString().Trim();
                    string minWidthStr = row[minWidthColumn].ToString().Trim();

                    double minWidth;
                    if (double.TryParse(minWidthStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out minWidth))
                    {
                        if (minWidth <= 0)
                        {
                            MessageBox.Show(string.Format("第 {0} 行最小图面宽度不合法！", i + 1));
                            return;
                        }

                        KeyValuePair<string, string> kv = new KeyValuePair<string, string>(fcName, filter);
                        _feType2Width[kv] = minWidth;
                    }
                    else
                    {
                        MessageBox.Show(string.Format("第 {0} 行最小图面宽度格式错误！", i + 1));
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载规则表失败: " + ex.Message);
                return;
            }

            // 执行检查
            string outPutFileName = Path.Combine(ResultOutputFilePath, string.Format("{0}.shp", toolName));
            string err = CheckPolygonWidthCmdJS.DoCheck(outPutFileName, _feType2Width, ReferScale);

            // 日志记录
            if (_logSW != null)
            {
                if (string.IsNullOrEmpty(err))
                {
                    _logSW.WriteLine(string.Format("【{0}】{1} 完毕！", DateTime.Now.ToString("HH:mm:ss"), toolName));
                }
                else
                {
                    _logSW.WriteLine(string.Format("【{0}】{1} 异常: {2}", DateTime.Now.ToString("HH:mm:ss"), toolName, err));
                }
            }
        }

        //点拓扑检查
        private void Check_TopoPointRelationHNcmd()
        {
            string toolName = "Check_TopoPointRelationHNcmd";
            string excelName = string.Empty;
            string sheetName = string.Empty;

            // 遍历 TreeView 中勾选的叶子节点，查找匹配当前工具名的项
            List<TreeNode> checkedLeafNodes = GetCheckedLeafNodes(chkToolTreeView.Nodes);
            foreach (TreeNode node in checkedLeafNodes)
            {
                if (node.Tag != null && node.Tag.ToString() == toolName)
                {
                    excelName = node.Text.Trim();
                    sheetName = node.Tag.ToString().Trim();  // Tag 就是方法名
                    break;
                }
            }

            List<Tuple<string, string, string, string, string>> tts = new List<Tuple<string, string, string, string, string>>(); //配置信息表（单行）

            tts.Clear();

            string schemePath = Path.Combine(excelPath, schemeName);

            // 遍历所有类别子目录，查找 Excel 文件
            string[] categoryDirs = Directory.GetDirectories(schemePath);
            string targetExcelPath = null;

            foreach (string categoryDir in categoryDirs)
            {
                string[] excelFiles = Directory.GetFiles(categoryDir, excelName + "*.xlsx");
                if (excelFiles.Length > 0)
                {
                    targetExcelPath = excelFiles[0]; // 取第一个匹配的 Excel
                    break;
                }
            }

            if (string.IsNullOrEmpty(targetExcelPath))
            {
                MessageBox.Show("未找到指定的配置文件！");
                return;
            }

            // 读取 Excel 表为 DataTable
            DataTable ruleDataTable = CommonMethods.ReadToDataTableExcelForChcker(targetExcelPath, sheetName);
            if (ruleDataTable == null || ruleDataTable.Rows.Count == 0)
            {
                MessageBox.Show("配置文件表内容为空或读取失败！");
                return;
            }
            foreach (DataColumn col in ruleDataTable.Columns)
            {
                Console.WriteLine("列名：" + col.ColumnName + "，长度：" + col.ColumnName.Length);
            }
            for (int i = 0; i < ruleDataTable.Rows.Count; i++)
            {
                string ptName = (ruleDataTable.Rows[i]["FCName1"]).ToString();
                string ptSQL = (ruleDataTable.Rows[i]["FilterString1"]).ToString();
                string relName = (ruleDataTable.Rows[i]["FCName2"]).ToString();
                string relSQL = (ruleDataTable.Rows[i]["FilterString2"]).ToString();
                string beizhu = (ruleDataTable.Rows[i]["Notes"]).ToString();
                Tuple<string, string, string, string, string> tt = new Tuple<string, string, string, string, string>(ptName, ptSQL, relName, relSQL, beizhu);
                tts.Add(tt);
            }

            //检查
            IWorkspace workspace = GApplication.Application.Workspace.EsriWorkspace;
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            var resultMessage = TopoPointRelationHNCheck.Check(featureWorkspace, tts);

            //保存质检结果
            resultMessage = TopoPointRelationHNCheck.SaveResult(ResultOutputFilePath);
            _logSW.WriteLine(string.Format("【{0}】{1}完毕！", DateTime.Now.ToString("HH:mm:ss"), toolName));
        }

        //线线套合拓扑检查
        private void Check_LineOverLineCmd()
        {
            string toolName = "Check_LineOverLineCmd";
            string excelName = string.Empty;
            string sheetName = string.Empty;

            // 遍历 TreeView 中勾选的叶子节点，查找匹配当前工具名的项
            List<TreeNode> checkedLeafNodes = GetCheckedLeafNodes(chkToolTreeView.Nodes);
            foreach (TreeNode node in checkedLeafNodes)
            {
                if (node.Tag != null && node.Tag.ToString() == toolName)
                {
                    excelName = node.Text.Trim();
                    sheetName = node.Tag.ToString().Trim();  // Tag 就是方法名
                    break;
                }
            }

            List<Tuple<string, string, string, string, string>> tts = new List<Tuple<string, string, string, string, string>>(); //配置信息表（单行）

            tts.Clear();

            string schemePath = Path.Combine(excelPath, schemeName);

            // 遍历所有类别子目录，查找 Excel 文件
            string[] categoryDirs = Directory.GetDirectories(schemePath);
            string targetExcelPath = null;

            foreach (string categoryDir in categoryDirs)
            {
                string[] excelFiles = Directory.GetFiles(categoryDir, excelName + "*.xlsx");
                if (excelFiles.Length > 0)
                {
                    targetExcelPath = excelFiles[0]; // 取第一个匹配的 Excel
                    break;
                }
            }

            if (string.IsNullOrEmpty(targetExcelPath))
            {
                MessageBox.Show("未找到指定的配置文件！");
                return;
            }

            // 读取 Excel 表为 DataTable
            DataTable ruleDataTable = CommonMethods.ReadToDataTableExcelForChcker(targetExcelPath, sheetName);
            if (ruleDataTable == null || ruleDataTable.Rows.Count == 0)
            {
                MessageBox.Show("配置文件表内容为空或读取失败！");
                return;
            }
            foreach (DataColumn col in ruleDataTable.Columns)
            {
                Console.WriteLine("列名：" + col.ColumnName + "，长度：" + col.ColumnName.Length);
            }
            for (int i = 0; i < ruleDataTable.Rows.Count; i++)
            {
                string ptName = (ruleDataTable.Rows[i]["FCName1"]).ToString();
                string ptSQL = (ruleDataTable.Rows[i]["FilterString1"]).ToString();
                string relName = (ruleDataTable.Rows[i]["FCName2"]).ToString();
                string relSQL = (ruleDataTable.Rows[i]["FilterString2"]).ToString();
                string beizhu = (ruleDataTable.Rows[i]["Notes"]).ToString();

                Tuple<string, string, string, string, string> tt = new Tuple<string, string, string, string, string>(ptName, ptSQL, relName, relSQL, beizhu);
                tts.Add(tt);
            }

            //检查
            IWorkspace workspace = GApplication.Application.Workspace.EsriWorkspace;
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            var resultMessage = CheckLineOverLineCmd.Check(featureWorkspace, tts);

            //保存质检结果
            resultMessage = CheckLineOverLineCmd.SaveResult(ResultOutputFilePath);
            _logSW.WriteLine(string.Format("【{0}】{1}完毕！", DateTime.Now.ToString("HH:mm:ss"), toolName));
        }

        //微短要素检查
        private void Check_MicroGeo()
        {
            string toolName = "Check_MicroGeo";
            string excelName = string.Empty;
            string sheetName = string.Empty;

            // 遍历 TreeView 中勾选的叶子节点，查找匹配当前工具名的项
            List<TreeNode> checkedLeafNodes = GetCheckedLeafNodes(chkToolTreeView.Nodes);
            foreach (TreeNode node in checkedLeafNodes)
            {
                if (node.Tag != null && node.Tag.ToString() == toolName)
                {
                    excelName = node.Text.Trim();
                    sheetName = node.Tag.ToString().Trim();  // Tag 就是方法名
                    break;
                }
            }

            List<Tuple<string, string, string>> tts = new List<Tuple<string, string, string>>(); //配置信息表（单行）

            tts.Clear();

            string schemePath = Path.Combine(excelPath, schemeName);

            // 遍历所有类别子目录，查找 Excel 文件
            string[] categoryDirs = Directory.GetDirectories(schemePath);
            string targetExcelPath = null;

            foreach (string categoryDir in categoryDirs)
            {
                string[] excelFiles = Directory.GetFiles(categoryDir, excelName + "*.xlsx");
                if (excelFiles.Length > 0)
                {
                    targetExcelPath = excelFiles[0]; // 取第一个匹配的 Excel
                    break;
                }
            }

            if (string.IsNullOrEmpty(targetExcelPath))
            {
                MessageBox.Show("未找到指定的配置文件！");
                return;
            }

            // 读取 Excel 表为 DataTable
            DataTable ruleDataTable = CommonMethods.ReadToDataTableExcelForChcker(targetExcelPath, sheetName);
            if (ruleDataTable == null || ruleDataTable.Rows.Count == 0)
            {
                MessageBox.Show("配置文件表内容为空或读取失败！");
                return;
            }
            foreach (DataColumn col in ruleDataTable.Columns)
            {
                Console.WriteLine("列名：" + col.ColumnName + "，长度：" + col.ColumnName.Length);
            }
            for (int i = 0; i < ruleDataTable.Rows.Count; i++)
            {
                string ptName = (ruleDataTable.Rows[i]["FCName1"]).ToString();
                string lenThresholdinTable = (ruleDataTable.Rows[i]["Notes"]).ToString();
                string areaThresholdinTable = (ruleDataTable.Rows[i]["MinWidth"]).ToString();

                Tuple<string, string, string> tt = new Tuple<string, string, string>(ptName, lenThresholdinTable, areaThresholdinTable);
                tts.Add(tt);
            }

            List<IFeatureClass> fc2List = new List<IFeatureClass>();
            double lenThreshold = 0;
            double areaThreshold = 0;

            foreach (var tt in tts)
            {
                string fcName = tt.Item1;


                if (!(_fws as IWorkspace2).get_NameExists(esriDatasetType.esriDTFeatureClass, fcName))
                    continue;
                IFeatureClass fc = _fws.OpenFeatureClass(fcName);

                fc2List.Add(fc);

                double.TryParse(tt.Item2, out lenThreshold);
                double.TryParse(tt.Item3, out areaThreshold);
            }

            // 执行检查
            string outPutFileName_line = ResultOutputFilePath + string.Format("\\{0}_线.shp", toolName);
            string outPutFileName_area = ResultOutputFilePath + string.Format("\\{0}_面.shp", toolName);
            string err = CheckMicroGeoCmd.DoCheck(outPutFileName_line, outPutFileName_area, fc2List, lenThreshold, areaThreshold, ReferScale);
            if (_logSW != null)
            {
                if (err == "")
                {
                    _logSW.WriteLine(string.Format("【{0}】{1}完毕！", DateTime.Now.ToString("HH:mm:ss"), toolName));
                }
                else
                {
                    _logSW.WriteLine(string.Format("【{0}】{1}时发生异常:{2}", DateTime.Now.ToString("HH:mm:ss"), toolName, err));
                }
            }
        }

        #endregion

        //生成的文件夹为空则删除，不为空则退出
        private void batchDataCheckForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists(tbOutFilePath.Text))
            {
                string[] files = Directory.GetFiles(tbOutFilePath.Text);
                if (files == null)
                {
                    Directory.Delete(tbOutFilePath.Text);
                }
            }
        }

        private void SchemesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            configurationPath = Path.Combine(excelPath, SchemesComboBox.SelectedItem.ToString());
            chkToolTreeView.Nodes.Clear();

            #region 初始化规则列表

            name2FN = new Dictionary<string, string>();

            if (!Directory.Exists(configurationPath))
            {
                MessageBox.Show("配置路径不存在！");
                return;
            }

            // 遍历每个类别目录
            string[] categoryDirs = Directory.GetDirectories(configurationPath);
            Excel.Application excelApp = new Excel.Application();

            try
            {
                foreach (string categoryDir in categoryDirs)
                {
                    string category = new DirectoryInfo(categoryDir).Name;
                    TreeNode categoryNode = new TreeNode(category);
                    chkToolTreeView.Nodes.Add(categoryNode);

                    // 获取该目录下所有xlsx文件
                    string[] xlsxFiles = Directory.GetFiles(categoryDir, "*.xlsx");
                    foreach (string file in xlsxFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        Excel.Workbook workbook = excelApp.Workbooks.Open(file, ReadOnly: true);
                        Excel.Worksheet sheet = workbook.Sheets[1];
                        string sheetName = sheet.Name;

                        name2FN[fileName] = sheetName;

                        TreeNode leafNode = new TreeNode(fileName)
                        {
                            Tag = sheetName
                        };
                        categoryNode.Nodes.Add(leafNode);

                        workbook.Close(false);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(sheet);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    }
                }

                fn2MethodInfo.Clear();
                chkToolTreeView.ExpandAll(); // 展开全部节点

                foreach (var kv in name2FN)
                {
                    MethodInfo mf = GetType().GetMethod(kv.Value, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mf != null && !fn2MethodInfo.ContainsKey(kv.Value))
                    {
                        fn2MethodInfo.Add(kv.Value, mf);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取表名失败: " + ex.Message);
            }
            finally
            {
                excelApp.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            }

            #endregion
        }

        private void chkToolTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // 避免递归触发多次事件
            chkToolTreeView.AfterCheck -= chkToolTreeView_AfterCheck;

            try
            {
                // 递归设置子节点的勾选状态与当前节点一致
                SetChildNodesChecked(e.Node, e.Node.Checked);

                // 子节点全选/取消时反向影响父节点
                UpdateParentNodeChecked(e.Node);
            }
            finally
            {
                chkToolTreeView.AfterCheck += chkToolTreeView_AfterCheck;
            }
        }

        private void SetChildNodesChecked(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                if (child.Nodes.Count > 0)
                {
                    SetChildNodesChecked(child, isChecked);
                }
            }
        }

        private void UpdateParentNodeChecked(TreeNode node)
        {
            TreeNode parent = node.Parent;
            while (parent != null)
            {
                bool allChecked = true;
                foreach (TreeNode sibling in parent.Nodes)
                {
                    if (!sibling.Checked)
                    {
                        allChecked = false;
                        break;
                    }
                }

                parent.Checked = allChecked;
                parent = parent.Parent;
            }
        }
    }
}
