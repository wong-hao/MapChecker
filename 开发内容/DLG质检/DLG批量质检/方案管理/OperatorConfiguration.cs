using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SMGI.Common;
using Excel = Microsoft.Office.Interop.Excel;

namespace SMGI.Plugin.CartographicGeneralization
{
    public partial class OperatorConfiguration : Form
    {
        private string excelPath
        {
            get;
            set;
        }

        private Excel.Application excelApp;
        private Excel.Workbook workbook;
        private Excel.Worksheet worksheet;

        public OperatorConfiguration(string filePath)
        {
            InitializeComponent();
            excelPath = filePath;
            LoadExcelToDataGridView();
        }

        private void LoadExcelToDataGridView()
        {
            Excel.Range usedRange = null;
            try
            {
                excelApp = new Excel.Application();
                workbook = excelApp.Workbooks.Open(excelPath);
                worksheet = workbook.Sheets[1];
                usedRange = worksheet.UsedRange;

                int rows = usedRange.Rows.Count;
                int cols = usedRange.Columns.Count;

                OperatorsDataGridView.ColumnCount = cols;

                // 设置列名
                for (int col = 1; col <= cols; col++)
                {
                    Excel.Range cell = worksheet.Cells[1, col] as Excel.Range;
                    string headerText = cell.Text.ToString();
                    OperatorsDataGridView.Columns[col - 1].Name = string.IsNullOrEmpty(headerText) ? "Column" + col : headerText;
                    Marshal.ReleaseComObject(cell);
                }

                // 添加数据行
                for (int row = 2; row <= rows; row++)
                {
                    string[] rowData = new string[cols];
                    for (int col = 1; col <= cols; col++)
                    {
                        Excel.Range cell = worksheet.Cells[row, col] as Excel.Range;
                        rowData[col - 1] = cell.Text.ToString();
                        Marshal.ReleaseComObject(cell);
                    }
                    OperatorsDataGridView.Rows.Add(rowData);
                }

                OperatorsDataGridView.AllowUserToAddRows = true;
                OperatorsDataGridView.AllowUserToDeleteRows = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel加载失败: " + ex.Message);
            }
            finally
            {
                if (usedRange != null) Marshal.ReleaseComObject(usedRange);
                usedRange = null;
            }
        }

        private void SaveDataToExcel()
        {
            Excel.Range dataRange = null;
            try
            {
                int lastRow = worksheet.Rows.Count;
                int colCount = OperatorsDataGridView.Columns.Count;

                dataRange = worksheet.Range["A2", worksheet.Cells[lastRow, colCount]];
                dataRange.ClearContents();
                Marshal.ReleaseComObject(dataRange);

                int rowIndex = 2;
                foreach (DataGridViewRow row in OperatorsDataGridView.Rows)
                {
                    if (row.IsNewRow) continue;

                    for (int col = 0; col < colCount; col++)
                    {
                        worksheet.Cells[rowIndex, col + 1] = (row.Cells[col].Value != null) ? row.Cells[col].Value.ToString() : "";
                    }
                    rowIndex++;
                }

                workbook.Save();
                MessageBox.Show("保存成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败: " + ex.Message);
            }
        }

        private void ReleaseExcelResources()
        {
            try
            {
                if (worksheet != null)
                {
                    Marshal.ReleaseComObject(worksheet);
                    worksheet = null;
                }

                if (workbook != null)
                {
                    workbook.Close(false); // 注意：先关闭再释放！
                    Marshal.ReleaseComObject(workbook);
                    workbook = null;
                }

                if (excelApp != null)
                {
                    excelApp.Quit(); // Quit 应该最后调用
                    Marshal.ReleaseComObject(excelApp);
                    excelApp = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("释放 Excel 资源失败：" + ex.Message);
            }
        }


        private void OperatorConfiguration_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ReleaseExcelResources();
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭Excel时出错：" + ex.Message);
            }
        }

        private void YesButton_Click(object sender, EventArgs e)
        {
            SaveDataToExcel();

            Close();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (OperatorsDataGridView.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择要删除的行！");
                    return;
                }

                // 获取选中行索引（假设只选中一行）
                int selectedIndex = OperatorsDataGridView.SelectedRows[0].Index;

                if (selectedIndex >= OperatorsDataGridView.Rows.Count - 1)  // 跳过空行
                {
                    MessageBox.Show("无法删除空行！");
                    return;
                }

                // 删除 DataGridView 中的行
                OperatorsDataGridView.Rows.RemoveAt(selectedIndex);

                // 删除 Excel 中对应的行（+2 是因为 Excel 从 1 开始，且跳过了表头）
                Excel.Range rowToDelete = (Excel.Range)worksheet.Rows[selectedIndex + 2];
                rowToDelete.Delete();

                workbook.Save();

                MessageBox.Show("删除成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除失败: " + ex.Message);
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取当前 DataGridView 中的最大 ID
                int maxId = 0;
                for (int i = 0; i < OperatorsDataGridView.Rows.Count; i++)
                {
                    if (OperatorsDataGridView.Rows[i].IsNewRow)
                        continue;

                    string idStr = Convert.ToString(OperatorsDataGridView.Rows[i].Cells[0].Value);
                    if (!string.IsNullOrEmpty(idStr))
                    {
                        int id = Convert.ToInt32(idStr);
                        if (id > maxId)
                            maxId = id;
                    }
                }

                int newId = maxId + 1;

                // 构建新行
                Dictionary<string, string> inputValues = new Dictionary<string, string>();

                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl.GetType() == typeof(TextBox))
                    {
                        string textBoxName = ctrl.Name;
                        if (textBoxName.EndsWith("TextBox"))
                        {
                            string columnName = textBoxName.Substring(0, textBoxName.Length - "TextBox".Length);
                            if (OperatorsDataGridView.Columns.Contains(columnName))
                            {
                                inputValues[columnName] = ctrl.Text.Trim();
                            }
                            else
                            {
                                Console.WriteLine("警告：DataGridView 不包含列 '" + columnName + "'，跳过赋值。");
                            }
                        }
                    }
                }

                // 创建 DataGridViewRow 并按列顺序添加
                DataGridViewRow newRow = new DataGridViewRow();
                newRow.CreateCells(OperatorsDataGridView);

                newRow.Cells[0].Value = newId.ToString(); // 设置 ID

                for (int col = 1; col < OperatorsDataGridView.Columns.Count; col++)
                {
                    string colName = OperatorsDataGridView.Columns[col].Name;
                    newRow.Cells[col].Value = inputValues.ContainsKey(colName) ? inputValues[colName] : "";
                }

                OperatorsDataGridView.Rows.Add(newRow);

                MessageBox.Show("添加成功！");

                // 清空所有匹配的 TextBox
                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl.GetType() == typeof(TextBox) && ctrl.Name.EndsWith("TextBox"))
                    {
                        ctrl.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("添加失败: " + ex.Message);
            }
        }

        private void OperatorsDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (OperatorsDataGridView.SelectedRows.Count == 0)
                return;

            DataGridViewRow selectedRow = OperatorsDataGridView.SelectedRows[0];
            if (selectedRow.IsNewRow)
                return;

            try
            {
                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl.GetType() == typeof(TextBox))
                    {
                        string textBoxName = ctrl.Name;
                        if (textBoxName.EndsWith("TextBox"))
                        {
                            string columnName = textBoxName.Substring(0, textBoxName.Length - "TextBox".Length);

                            if (OperatorsDataGridView.Columns.Contains(columnName))
                            {
                                object value = selectedRow.Cells[columnName].Value;
                                ctrl.Text = Convert.ToString(value);
                            }
                            else
                            {
                                ctrl.Text = string.Empty;
                                Console.WriteLine("警告：DataGridView 中不包含列 '" + columnName + "'，对应的文本框已清空。");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("自动匹配列到文本框时发生错误：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (OperatorsDataGridView.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择要修改的行！");
                    return;
                }

                DataGridViewRow selectedRow = OperatorsDataGridView.SelectedRows[0];
                if (selectedRow.IsNewRow)
                {
                    MessageBox.Show("不能修改空行！");
                    return;
                }

                // 遍历当前窗体的所有控件
                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl.GetType() == typeof(TextBox))
                    {
                        string textBoxName = ctrl.Name;
                        if (textBoxName.EndsWith("TextBox"))
                        {
                            string columnName = textBoxName.Substring(0, textBoxName.Length - "TextBox".Length);
                            if (OperatorsDataGridView.Columns.Contains(columnName))
                            {
                                selectedRow.Cells[columnName].Value = ctrl.Text.Trim();
                            }
                            else
                            {
                                Console.WriteLine("警告：DataGridView 不包含列 '" + columnName + "'，跳过赋值。");
                            }
                        }
                    }
                }

                MessageBox.Show("修改成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("修改失败: " + ex.Message);
            }
        }
    }
}
