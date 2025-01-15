using FocusTimer.Models;
using FocusTimer.Services;
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;

namespace FocusTimer.Forms
{
    public partial class MainForm : Form
    {
        private readonly TaskService _taskService;
        private System.Windows.Forms.Timer timer;
        private int remainingSeconds;
        private Models.FocusTask currentTask;

        // 左側面板控件 - 工作輸入區
        private TextBox txtTitle;
        private ComboBox cboType;
        private TextBox txtNotes;

        // 左側面板控件 - 計時器
        private NumericUpDown numTimerDuration;
        private Label lblTimer;
        private Button btnStart;
        private Button btnStop;

        public MainForm()
        {
            InitializeComponent();
            _taskService = new TaskService();
            InitializeTimer();
            InitializeTaskTypes();
        }

        private void InitializeComponent()
        {
            // 設置窗體基本屬性
            this.Size = new Size(800, 600);
            this.Text = "專注力追蹤器";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Font = new Font("Microsoft JhengHei UI", 9F);

            // 主面板
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(20)
            };

            // 工作輸入區域
            var taskInputGroup = CreateGroupBox("目前工作", 20);
            taskInputGroup.Size = new Size(720, 200);

            var lblTitle = CreateLabel("標題:", 30);
            txtTitle = CreateTextBox(30);
            txtTitle.Width = 600;

            var lblType = CreateLabel("類型:", 70);
            cboType = new ComboBox
            {
                Location = new Point(70, 67),
                Width = 600,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft JhengHei UI", 10F)
            };

            var lblNotes = CreateLabel("備註:", 110);
            txtNotes = new TextBox
            {
                Location = new Point(70, 107),
                Width = 600,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Microsoft JhengHei UI", 10F)
            };

            taskInputGroup.Controls.AddRange(new Control[] { lblTitle, txtTitle, lblType, cboType, lblNotes, txtNotes });

            // 計時器區域
            var timerGroup = CreateGroupBox("計時器", 240);
            timerGroup.Size = new Size(720, 250);

            var lblTimerDuration = CreateLabel("時間設定:", 30);
            numTimerDuration = new NumericUpDown
            {
                Location = new Point(100, 28),
                Width = 80,
                Minimum = 1,
                Maximum = 120,
                Value = 25,
                Font = new Font("Microsoft JhengHei UI", 10F)
            };
            var lblMinutes = new Label
            {
                Text = "分鐘",
                Location = new Point(190, 30),
                AutoSize = true
            };

            lblTimer = new Label
            {
                Text = "25:00",
                Font = new Font("Segoe UI", 48F, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, 70),
                Size = new Size(340, 100),
                AutoSize = false
            };

            btnStart = CreateButton("開始", Color.FromArgb(92, 184, 92));
            btnStop = CreateButton("停止", Color.FromArgb(217, 83, 79));
            btnStop.Enabled = false;

            btnStart.Location = new Point(70, 200);
            btnStop.Location = new Point(190, 200);

            var btnOpenTodoList = CreateButton("待辦事項", Color.FromArgb(51, 122, 183));
            btnOpenTodoList.Location = new Point(550, 200);
            btnOpenTodoList.Click += (s, e) =>
            {
                var todoForm = new ToDoListForm(this);
                todoForm.Show();
            };

            var btnStatistics = CreateButton("統計圖表", Color.FromArgb(70, 130, 180));
            btnStatistics.Location = new Point(550, 150);
            btnStatistics.Click += (s, e) =>
            {
                var statisticsForm = new StatisticsForm();
                statisticsForm.Show();
            };

            timerGroup.Controls.AddRange(new Control[] 
            { 
                lblTimerDuration, numTimerDuration, lblMinutes,
                lblTimer, btnStart, btnStop,
                btnOpenTodoList, btnStatistics
            });

            mainPanel.Controls.AddRange(new Control[] { taskInputGroup, timerGroup });
            this.Controls.Add(mainPanel);

            // 註冊事件
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            numTimerDuration.ValueChanged += NumTimerDuration_ValueChanged;
        }

        private GroupBox CreateGroupBox(string text, int y)
        {
            return new GroupBox
            {
                Text = text,
                Location = new Point(0, y),
                Size = new Size(360, 200),
                Font = new Font("Microsoft JhengHei UI", 9F, FontStyle.Bold)
            };
        }

        private Label CreateLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(10, y + 3),
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 9F)
            };
        }

        private TextBox CreateTextBox(int y)
        {
            return new TextBox
            {
                Location = new Point(70, y),
                Width = 280,
                Font = new Font("Microsoft JhengHei UI", 10F)
            };
        }

        private Button CreateButton(string text, Color backColor)
        {
            return new Button
            {
                Text = text,
                Width = 100,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void NumTimerDuration_ValueChanged(object sender, EventArgs e)
        {
            if (!timer.Enabled)
            {
                remainingSeconds = (int)numTimerDuration.Value * 60;
                UpdateTimerDisplay();
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("請輸入任務標題", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentTask = new Models.FocusTask
            {
                Title = txtTitle.Text,
                Type = cboType.Text,
                Notes = txtNotes.Text,
                StartTime = DateTime.Now,
                IsCompleted = false
            };

            remainingSeconds = (int)numTimerDuration.Value * 60;
            UpdateTimerDisplay();
            timer.Start();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            DisableInputs();
        }

        private void InitializeTaskTypes()
        {
            string[] types = { "日常", "開會", "issue", "oncall", "學習", "幫忙" };
            cboType.Items.AddRange(types);
            cboType.SelectedIndex = 0;
        }

        private void InitializeTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            remainingSeconds = 25 * 60; // 25分鐘
            UpdateTimerDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (remainingSeconds > 0)
            {
                remainingSeconds--;
                UpdateTimerDisplay();
            }
            else
            {
                timer.Stop();
                StopTask();
                MessageBox.Show("時間到！", "提醒", MessageBoxButtons.OK);
            }
        }

        private async Task StopTask()
        {
            timer.Stop();
            if (currentTask != null)
            {
                currentTask.EndTime = DateTime.Now;
                currentTask.Duration = 25 * 60 - remainingSeconds;
                currentTask.IsCompleted = true;
                await _taskService.SaveTaskRecord(currentTask);
            }

            ResetTimer();
            EnableInputs();
            ClearInputs();
        }

        private void UpdateTimerDisplay()
        {
            TimeSpan time = TimeSpan.FromSeconds(remainingSeconds);
            lblTimer.Text = $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void ResetTimer()
        {
            remainingSeconds = (int)numTimerDuration.Value * 60;
            UpdateTimerDisplay();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void DisableInputs()
        {
            txtTitle.Enabled = false;
            cboType.Enabled = false;
            txtNotes.Enabled = false;
            numTimerDuration.Enabled = false;
        }

        private void EnableInputs()
        {
            txtTitle.Enabled = true;
            cboType.Enabled = true;
            txtNotes.Enabled = true;
            numTimerDuration.Enabled = true;
        }

        private void ClearInputs()
        {
            txtTitle.Clear();
            txtNotes.Clear();
            cboType.SelectedIndex = 0;
        }

        private async void BtnStop_Click(object sender, EventArgs e)
        {
            await StopTask();
        }

        public void SetTaskFromTodo(Models.FocusTask task)
        {
            txtTitle.Text = task.Title;
            cboType.Text = task.Type;
            txtNotes.Text = task.Notes;
            numTimerDuration.Value = task.Duration / 60;  // 轉換為分鐘
        }
    }
} 