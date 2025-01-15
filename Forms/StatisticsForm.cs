using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using FocusTimer.Services;

namespace FocusTimer.Forms
{
    public partial class StatisticsForm : Form
    {
        private readonly TaskService _taskService;
        private TabControl tabControl;
        private Chart chartMonth;
        private Chart chartWeek;
        private Chart chartDay;

        public StatisticsForm()
        {
            InitializeComponent();
            _taskService = new TaskService();
            LoadStatistics();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.Text = "工作統計";
            this.StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 創建三個頁籤
            var tabMonth = new TabPage("本月統計");
            var tabWeek = new TabPage("本週統計");
            var tabDay = new TabPage("今日統計");

            // 創建三個圓餅圖
            chartMonth = CreatePieChart();
            chartWeek = CreatePieChart();
            chartDay = CreatePieChart();

            tabMonth.Controls.Add(chartMonth);
            tabWeek.Controls.Add(chartWeek);
            tabDay.Controls.Add(chartDay);

            tabControl.TabPages.AddRange(new[] { tabMonth, tabWeek, tabDay });
            this.Controls.Add(tabControl);
        }

        private Chart CreatePieChart()
        {
            var chart = new Chart
            {
                Dock = DockStyle.Fill
            };

            var chartArea = new ChartArea();
            chartArea.Position = new ElementPosition(5, 5, 70, 90);  // 調整圖表區域位置，留出空間給圖例
            chart.ChartAreas.Add(chartArea);

            // 添加圖例
            var legend = new Legend
            {
                Docking = Docking.Right,  // 圖例靠右
                Alignment = StringAlignment.Center,  // 圖例垂直置中
                IsDockedInsideChartArea = false  // 圖例在圖表區域外
            };
            chart.Legends.Add(legend);

            var series = new Series
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                Legend = legend.Name,
                Font = new Font("Microsoft JhengHei UI", 9F)  // 設置字體
            };

            chart.Series.Add(series);
            return chart;
        }

        private async void LoadStatistics()
        {
            try
            {
                // 獲取時間範圍
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var weekStart = today.AddDays(-(int)today.DayOfWeek);

                // 獲取數據
                var monthTasks = await _taskService.GetTasksAsync(monthStart, today.AddDays(1));
                var weekTasks = monthTasks.Where(t => t.StartTime >= weekStart);
                var dayTasks = monthTasks.Where(t => t.StartTime.Date == today);

                // 更新圖表
                UpdateChart(chartMonth, monthTasks, "本月");
                UpdateChart(chartWeek, weekTasks, "本週");
                UpdateChart(chartDay, dayTasks, "今日");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入統計資料時發生錯誤: {ex.Message}", "錯誤", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateChart(Chart chart, IEnumerable<Models.FocusTask> tasks, string period)
        {
            if (!tasks.Any())
            {
                chart.Series[0].Points.Clear();
                chart.Titles.Clear();
                chart.Titles.Add(new Title($"{period}沒有任何工作記錄")
                {
                    Font = new Font("Microsoft JhengHei UI", 12F, FontStyle.Bold)
                });
                return;
            }

            var statistics = tasks
                .GroupBy(t => t.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Duration = g.Sum(t => t.Duration) / 60.0 // 轉換為分鐘
                })
                .OrderByDescending(x => x.Duration);

            chart.Series[0].Points.Clear();
            foreach (var stat in statistics)
            {
                var point = chart.Series[0].Points.Add(stat.Duration);
                
                // 設置圖例文字，包含時間資訊
                string timeText;
                if (stat.Duration >= 60)
                {
                    double hours = Math.Round(stat.Duration / 60, 1);
                    timeText = $"{hours:F1}小時";
                    point.Label = $"{timeText}\n({stat.Duration / statistics.Sum(x => x.Duration):P1})";
                    point.LegendText = $"{stat.Type} - {timeText}";  // 在圖例中顯示類型和時間
                }
                else
                {
                    timeText = $"{stat.Duration:F0}分鐘";
                    point.Label = $"{timeText}\n({stat.Duration / statistics.Sum(x => x.Duration):P1})";
                    point.LegendText = $"{stat.Type} - {timeText}";  // 在圖例中顯示類型和時間
                }

                // 為不同類型設置不同顏色
                switch (stat.Type)
                {
                    case "日常":
                        point.Color = Color.FromArgb(91, 155, 213);
                        break;
                    case "開會":
                        point.Color = Color.FromArgb(237, 125, 49);
                        break;
                    case "issue":
                        point.Color = Color.FromArgb(165, 165, 165);
                        break;
                    case "oncall":
                        point.Color = Color.FromArgb(255, 192, 0);
                        break;
                    case "學習":
                        point.Color = Color.FromArgb(112, 173, 71);
                        break;
                    case "幫忙":
                        point.Color = Color.FromArgb(165, 105, 189);
                        break;
                    default:
                        break;
                }
            }

            // 添加標題
            chart.Titles.Clear();
            chart.Titles.Add(new Title($"{period}工作統計")
            {
                Font = new Font("Microsoft JhengHei UI", 12F, FontStyle.Bold),
                Docking = Docking.Top
            });
        }
    }
} 