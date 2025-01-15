using FocusTimer.Models;
using FocusTimer.Services;
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;

namespace FocusTimer.Forms
{
    public partial class ToDoListForm : Form
    {
        private readonly TaskService _taskService;
        private readonly MainForm _mainForm;

        // 控件宣告
        private DataGridView dgvTodoList;
        private TextBox txtTitle;
        private ComboBox cboType;
        private TextBox txtNotes;
        private NumericUpDown numDuration;
        private Button btnAdd;

        public ToDoListForm(MainForm mainForm)
        {
            InitializeComponent();
            _taskService = new TaskService();  // 使用 TaskService 替代 DatabaseManager
            _mainForm = mainForm;
            InitializeTaskTypes();
            LoadUncompletedTasks();
        }

        private void InitializeComponent()
        {
            // 修改窗體大小和位置
            this.Size = new Size(600, 500);  // 縮小窗體
            this.Text = "待辦事項清單";
            this.StartPosition = FormStartPosition.Manual;  // 改為手動定位
            this.Location = new Point(50, 50);  // 設定在左上角
            this.BackColor = Color.White;
            this.Font = new Font("Microsoft JhengHei UI", 9F);

            // 建立主面板
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // 建立輸入區域
            var inputGroup = new GroupBox
            {
                Text = "新增待辦事項",
                Dock = DockStyle.Top,
                Height = 240,
                Padding = new Padding(10)
            };

            // 建立輸入控件
            var lblTitle = new Label { Text = "標題:", Location = new Point(20, 35) };
            txtTitle = new TextBox 
            { 
                Location = new Point(120, 32),
                Width = 400
            };

            var lblType = new Label { Text = "類型:", Location = new Point(20, 70) };
            cboType = new ComboBox
            {
                Location = new Point(120, 67),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblDuration = new Label { Text = "預計時間:", Location = new Point(20, 105) };
            numDuration = new NumericUpDown
            {
                Location = new Point(120, 102),
                Width = 80,
                Minimum = 1,
                Maximum = 480,
                Value = 25
            };
            var lblMinutes = new Label 
            { 
                Text = "分鐘",
                Location = new Point(210, 105),
                AutoSize = true
            };

            var lblNotes = new Label { Text = "備註:", Location = new Point(20, 140) };
            txtNotes = new TextBox
            {
                Location = new Point(120, 137),
                Width = 400,
                Height = 40,
                Multiline = true
            };

            btnAdd = new Button
            {
                Text = "新增待辦事項",
                Location = new Point(120, 190),
                Width = 120,
                Height = 30,
                BackColor = Color.FromArgb(92, 184, 92),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAdd.Click += BtnAdd_Click;

            // 添加轉移到專注力追蹤器的按鈕
            var btnMoveToTimer = new Button
            {
                Text = "移至專注計時",
                Location = new Point(250, 190),
                Width = 120,
                Height = 30,
                BackColor = Color.FromArgb(51, 122, 183),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnMoveToTimer.Click += BtnMoveToTimer_Click;

            inputGroup.Controls.AddRange(new Control[] 
            { 
                lblTitle, txtTitle,
                lblType, cboType,
                lblDuration, numDuration, lblMinutes,
                lblNotes, txtNotes,
                btnAdd, btnMoveToTimer
            });

            // 建立待辦事項列表
            dgvTodoList = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                MultiSelect = false
            };

            // 添加刪除按鈕列
            var deleteColumn = new DataGridViewButtonColumn
            {
                HeaderText = "操作",
                Text = "刪除",
                UseColumnTextForButtonValue = true,
                FillWeight = 30
            };

            dgvTodoList.Columns.AddRange(
            
                new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "標題", FillWeight = 100 },
                new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "類型", FillWeight = 50 },
                new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "預計時間(分鐘)", FillWeight = 50 },
                new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "備註", FillWeight = 100 },
                deleteColumn
            );

            dgvTodoList.CellClick += DgvTodoList_CellClick;

            // 添加控件到主面板
            mainPanel.Controls.Add(dgvTodoList);
            mainPanel.Controls.Add(inputGroup);

            // 添加主面板到窗體
            this.Controls.Add(mainPanel);
        }

        private void InitializeTaskTypes()
        {
            string[] types = { "日常", "開會", "issue", "oncall", "學習", "幫忙" };
            cboType.Items.AddRange(types);
            cboType.SelectedIndex = 0;
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("請輸入待辦事項標題", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 創建新的任務
                var task = new Models.FocusTask
                {
                    Title = txtTitle.Text,
                    Type = cboType.Text,
                    Notes = txtNotes.Text,
                    Duration = (int)numDuration.Value * 60  // 轉換為秒
                };

                // 保存到資料庫
                int taskId = await _taskService.SaveTodoTask(task);
                task.Id = taskId;

                // 新增到列表
                var row = dgvTodoList.Rows.Add(
                    task.Title,
                    task.Type,
                    numDuration.Value,
                    task.Notes
                );
                dgvTodoList.Rows[row].Tag = taskId;  // 儲存 ID

                // 清空輸入
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增待辦事項時發生錯誤: {ex.Message}", "錯誤", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearInputs()
        {
            txtTitle.Clear();
            txtNotes.Clear();
            cboType.SelectedIndex = 0;
            numDuration.Value = 25;
        }

        private async void LoadUncompletedTasks()
        {
            try
            {
                var tasks = await _taskService.GetTodoTasksAsync();

                dgvTodoList.Rows.Clear();
                foreach (var task in tasks)
                {
                    var row = dgvTodoList.Rows.Add(
                        task.Title,
                        task.Type,
                        task.Duration / 60,  // 轉換為分鐘
                        task.Notes
                    );
                    // 儲存 task.Id 到 Row 的 Tag 屬性中
                    dgvTodoList.Rows[row].Tag = task.Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入待辦事項時發生錯誤: {ex.Message}", "錯誤", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DgvTodoList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvTodoList.Columns.Count - 1 && e.RowIndex >= 0)  // 最後一列是刪除按鈕
            {
                if (MessageBox.Show("確定要刪除這個待辦事項嗎？", "確認", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        // 獲取要刪除的任務 ID
                        int taskId = (int)dgvTodoList.Rows[e.RowIndex].Tag;
                        
                        // 從資料庫中刪除
                        await _taskService.DeleteTodoTask(taskId);
                        
                        // 從列表中移除
                        dgvTodoList.Rows.RemoveAt(e.RowIndex);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"刪除待辦事項時發生錯誤: {ex.Message}", "錯誤", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // 添加轉移到專注力追蹤器的事件處理方法
        private async void BtnMoveToTimer_Click(object sender, EventArgs e)
        {
            if (dgvTodoList.SelectedRows.Count == 0)
            {
                MessageBox.Show("請先選擇一個待辦事項", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var selectedRow = dgvTodoList.SelectedRows[0];
                var taskId = (int)selectedRow.Tag;

                var timerTask = new Models.FocusTask
                {
                    Title = selectedRow.Cells["Title"].Value.ToString(),
                    Type = selectedRow.Cells["Type"].Value.ToString(),
                    Notes = selectedRow.Cells["Notes"].Value.ToString(),
                    Duration = Convert.ToInt32(selectedRow.Cells["Duration"].Value) * 60,
                    StartTime = DateTime.Now,
                    IsCompleted = false
                };

                await _taskService.DeleteTodoTask(taskId);
                dgvTodoList.Rows.Remove(selectedRow);

                _mainForm.SetTaskFromTodo(timerTask);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移動待辦事項時發生錯誤: {ex.Message}", "錯誤", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 