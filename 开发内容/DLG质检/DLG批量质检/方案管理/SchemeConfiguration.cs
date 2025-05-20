using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace SMGI.Plugin.CartographicGeneralization
{
    public partial class SchemeConfiguration : Form
    {
        private static string category1 = "点要素质检";
        private static string category2 = "面要素质检";
        private static string category3 = "线要素质检";
        private static string category4 = "多种要素质检";

        private static string operator1 = "点拓扑检查";
        private static string operator2 = "面要素宽度指标检查";
        private static string operator3 = "线线套合拓扑检查";
        private static string operator4 = "微短要素检查";

        private static Dictionary<string, List<string>> operatorCategoryMap = new Dictionary<string, List<string>>()
        {
            { category1, new List<string> { operator1 } },
            { category2, new List<string> { operator2 } },
            { category3, new List<string> { operator3 } },
            { category4, new List<string> { operator4 } }
        };

        private string realschemesPath
        {
            get;
            set;
        }

        private string configurationPath
        {
            get;
            set;
        }

        public SchemeConfiguration(string schemesPath)
        {
            realschemesPath = schemesPath;
            InitializeComponent();
        }

        private void Configuration_Load(object sender, EventArgs e)
        {
            #region 初始化方案列表

            if (Directory.Exists(realschemesPath))
            {
                string[] directories = Directory.GetDirectories(realschemesPath);

                SchemesComboBox.Items.Clear(); // 清空旧项

                foreach (string dir in directories)
                {
                    string folderName = Path.GetFileName(dir); // 只取文件夹名称，不含路径
                    SchemesComboBox.Items.Add(folderName);
                }

                if (SchemesComboBox.Items.Count > 0)
                {
                    SchemesComboBox.SelectedIndex = 0; // 默认选中第一项
                }
            }
            else
            {
                MessageBox.Show("方案路径不存在：" + realschemesPath);
            }

            #endregion

            #region 初始化算子列表

            OperatorsTreeView.Nodes.Clear();

            foreach (var category in operatorCategoryMap)
            {
                TreeNode categoryNode = new TreeNode(category.Key);
                foreach (string op in category.Value)
                {
                    categoryNode.Nodes.Add(new TreeNode(op));
                }
                OperatorsTreeView.Nodes.Add(categoryNode);
            }

            OperatorsTreeView.ExpandAll();

            #endregion
        }

        private void AddOperatorButton_Click(object sender, EventArgs e)
        {
            // 选择的必须是 OperatorsTreeView 的叶子节点
            TreeNode selectedNode = OperatorsTreeView.SelectedNode;

            if (selectedNode == null || selectedNode.Nodes.Count > 0)
            {
                MessageBox.Show("请选择一个具体的算子进行添加！");
                return;
            }

            string selectedOperator = selectedNode.Text;
            string category = selectedNode.Parent.Text;

            // 检查 RulesTreeView 中是否已存在该算子（避免重复添加）
            foreach (TreeNode categoryNode in RulesTreeView.Nodes)
            {
                foreach (TreeNode opNode in categoryNode.Nodes)
                {
                    if (opNode.Text == selectedOperator)
                    {
                        MessageBox.Show("无法重复添加相同算子！");
                        return;
                    }
                }
            }

            #region 更新界面：添加到 RulesTreeView

            TreeNode targetCategoryNode = RulesTreeView.Nodes.Cast<TreeNode>()
                .FirstOrDefault(n => n.Text == category);

            if (targetCategoryNode == null)
            {
                targetCategoryNode = new TreeNode(category);
                RulesTreeView.Nodes.Add(targetCategoryNode);
            }

            targetCategoryNode.Nodes.Add(new TreeNode(selectedOperator));
            RulesTreeView.ExpandAll();

            #endregion

            #region 添加配置文件：保存到子目录中

            string categoryDir = Path.Combine(configurationPath, category);
            if (!Directory.Exists(categoryDir))
            {
                Directory.CreateDirectory(categoryDir);
            }

            string filePath = Path.Combine(categoryDir, selectedOperator + ".xlsx");

            if (!File.Exists(filePath))
            {
                Excel.Application excelApp = new Excel.Application();
                Excel.Workbook workbook = excelApp.Workbooks.Add();
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Sheets[1];

                if (string.Equals(selectedOperator, operator1))
                {
                    worksheet.Name = "Check_TopoPointRelationHNcmd";
                }
                else if (string.Equals(selectedOperator, operator2))
                {
                    worksheet.Name = "Check_PolygonWidthCmdJS";
                }else if (string.Equals(selectedOperator, operator3))
                {
                    worksheet.Name = "Check_LineOverLineCmd";
                }
                else if (string.Equals(selectedOperator, operator3))
                {
                    worksheet.Name = "Check_MicroGeo";
                }

                worksheet.Cells[1, 1] = "ID";
                worksheet.Cells[1, 2] = "FCName1";
                worksheet.Cells[1, 3] = "FilterString1";
                worksheet.Cells[1, 4] = "Addin1";
                worksheet.Cells[1, 5] = "FCName2";
                worksheet.Cells[1, 6] = "FilterString2";
                worksheet.Cells[1, 7] = "Addin2";
                worksheet.Cells[1, 8] = "Relationship";
                worksheet.Cells[1, 9] = "Notes";
                worksheet.Cells[1, 10] = "MinWidth";

                workbook.SaveAs(filePath);
                workbook.Close(false);
                excelApp.Quit();

                System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            }

            #endregion
        }

        // 判断某个节点是否为叶子节点（即没有子节点）
        private bool IsLeafNode(TreeNode node)
        {
            return node != null && node.Nodes.Count == 0;
        }

        private void RemoveOperatorButton_Click(object sender, EventArgs e)
        {
            if (RulesTreeView.SelectedNode == null || !IsLeafNode(RulesTreeView.SelectedNode))
            {
                MessageBox.Show("请在规则树中选择一个要删除的算子（叶子节点）！");
                return;
            }

            TreeNode selectedNode = RulesTreeView.SelectedNode;
            TreeNode parentNode = selectedNode.Parent;

            if (parentNode == null)
            {
                MessageBox.Show("节点结构异常，无法删除！");
                return;
            }

            string operatorName = selectedNode.Text;
            string category = parentNode.Text;

            // 弹窗确认是否删除
            DialogResult result = MessageBox.Show(
                string.Format("确定要删除算子 \"{0}\" 吗？该操作无法撤销。", operatorName),
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return; // 用户取消操作
            }

            #region 删除配置文件（从类别子目录下删除）

            string categoryDir = Path.Combine(configurationPath, category);
            string filePath = Path.Combine(categoryDir, operatorName + ".xlsx");

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("删除文件失败!");
                }
            }

            #endregion

            #region 更新界面：从 RulesTreeView 中移除该算子

            parentNode.Nodes.Remove(selectedNode); // 从父节点中移除

            if (parentNode.Nodes.Count == 0)
            {
                RulesTreeView.Nodes.Remove(parentNode); // 如果该类别下没有算子了，则移除整个类别节点
            }

            #endregion
        }

        private void EditOperatorButton_Click(object sender, EventArgs e)
        {
            if (RulesTreeView.SelectedNode == null || !IsLeafNode(RulesTreeView.SelectedNode))
            {
                MessageBox.Show("请在规则树中选择一个要编辑的算子（叶子节点）！");
                return;
            }

            TreeNode selectedNode = RulesTreeView.SelectedNode;
            TreeNode parentNode = selectedNode.Parent;

            if (parentNode == null)
            {
                MessageBox.Show("节点结构异常，无法编辑！");
                return;
            }

            string operatorName = selectedNode.Text;
            string category = parentNode.Text;

            string categoryDir = Path.Combine(configurationPath, category);
            string filePath = Path.Combine(categoryDir, operatorName + ".xlsx");

            if (!File.Exists(filePath))
            {
                MessageBox.Show("配置文件不存在，无法编辑！");
                return;
            }

            try
            {
                OperatorConfiguration form = new OperatorConfiguration(filePath);
                form.ShowDialog();

                return;

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // 使用默认应用程序打开
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开文件失败!");
            }
        }

        private void SchemesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            configurationPath = Path.Combine(realschemesPath, SchemesComboBox.SelectedItem.ToString());

            RulesTreeView.Nodes.Clear();

            if (Directory.Exists(configurationPath))
            {
                // 遍历每个类别子目录
                string[] categoryDirs = Directory.GetDirectories(configurationPath);

                foreach (string categoryDir in categoryDirs)
                {
                    string[] xlsxFiles = Directory.GetFiles(categoryDir, "*.xlsx");

                    // 只有存在算子配置文件时才处理这个类别
                    if (xlsxFiles.Length > 0)
                    {
                        string category = new DirectoryInfo(categoryDir).Name;

                        TreeNode categoryNode = new TreeNode(category);

                        // 添加该类别下的所有算子配置文件
                        foreach (string file in xlsxFiles)
                        {
                            string opName = Path.GetFileNameWithoutExtension(file);
                            categoryNode.Nodes.Add(new TreeNode(opName));
                        }

                        RulesTreeView.Nodes.Add(categoryNode);
                    }
                }
            }

            RulesTreeView.ExpandAll(); // 展开全部节点
        }

        private void DeleteSchemeButton_Click(object sender, EventArgs e)
        {
            if (SchemesComboBox.SelectedItem != null)
            {
                string selectedScheme = SchemesComboBox.SelectedItem.ToString();

                if (!string.IsNullOrWhiteSpace(selectedScheme))
                {
                    DialogResult result = MessageBox.Show(
                        "确定要删除方案 \"" + selectedScheme + "\" 吗？该操作无法撤销！",
                        "确认删除",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result != DialogResult.Yes)
                        return;

                    #region 更新界面

                    int currentIndex = SchemesComboBox.SelectedIndex;
                    SchemesComboBox.Items.Remove(selectedScheme);

                    foreach (TreeNode categoryNode in RulesTreeView.Nodes)
                    {
                        categoryNode.Nodes.Clear();
                    }

                    int itemsCount = SchemesComboBox.Items.Count;

                    if (itemsCount > 0)
                    {
                        if (currentIndex >= itemsCount)
                        {
                            currentIndex = itemsCount - 1;
                        }
                        SchemesComboBox.SelectedIndex = currentIndex;
                    }
                    else
                    {
                        SchemesComboBox.SelectedIndex = -1;
                        SchemesComboBox.Text = string.Empty;
                    }

                    #endregion

                    #region 删除配置文件

                    string folderPath = Path.Combine(realschemesPath, selectedScheme);

                    if (Directory.Exists(folderPath))
                    {
                        try
                        {
                            Directory.Delete(folderPath, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("删除文件夹时发生错误: " + ex.Message);
                        }
                    }

                    #endregion
                }
            }
            else
            {
                MessageBox.Show("请先选择一个方案进行删除！");
            }
        }

        private void AddSchemeButton_Click(object sender, EventArgs e)
        {
            string newSchemeName = AddSchemeTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(newSchemeName))
            {
                string newFolderPath = Path.Combine(realschemesPath, newSchemeName);

                if (!Directory.Exists(newFolderPath))
                {
                    try
                    {
                        #region 创建配置文件

                        // 创建文件夹
                        Directory.CreateDirectory(newFolderPath);

                        #endregion

                        #region 更新界面

                        // 添加到 ComboBox
                        SchemesComboBox.Items.Add(newSchemeName);

                        // 可选：自动选中新添加的项
                        SchemesComboBox.SelectedItem = newSchemeName;

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("创建文件夹时发生错误: " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("该方案名称已存在，请使用其他名称。");
                }
            }
            else
            {
                MessageBox.Show("请输入方案名称！");
            }
        }

        private void YesButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("方案已保存!");
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
