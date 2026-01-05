using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AppStartupLogger
{
    public partial class Form1 : Form
    {
        class AppState
        {
            public bool WasRunning;
            public DateTime? StartTime;
        }

        Dictionary<string, AppState> apps = new Dictionary<string, AppState>();
        AppConfig config;
        string logFilePath;

        System.Windows.Forms.Timer timer;
        private bool isExiting = false;


        public Form1()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.ShowIcon = true;

            config = AppConfig.Load();
            logFilePath = AppConfig.GetLogFilePath();

            foreach (var appName in config.Apps)
            {
                apps[appName] = new AppState();
            }

            if (!File.Exists(logFilePath))
            {
                File.AppendAllText(logFilePath,
                    "App,Start,End,Duration\n");
            }

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (var app in apps)
            {
                string name = app.Key;
                AppState state = app.Value;

                bool isRunning = IsProcessRunning(name);

                if (isRunning && !state.WasRunning)
                {
                    state.StartTime = DateTime.Now;
                }

                if (!isRunning && state.WasRunning && state.StartTime != null)
                {
                    SaveLog(name, state.StartTime.Value, DateTime.Now);
                    state.StartTime = null;
                }

                state.WasRunning = isRunning;
            }

            bool IsProcessRunning(string processName)
            {
                return Process.GetProcessesByName(processName).Length > 0;
            }

            void SaveLog(string appName, DateTime start, DateTime end)
            {
                TimeSpan duration = end - start;

                string line =
                    $"{appName},{start},{end},{duration.TotalSeconds}";

                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            UpdateChart();
        }

        void UpdateChart()
        {
            if (!File.Exists(logFilePath)) return;

            var appUsage = new Dictionary<string, double>();

            var lines = File.ReadAllLines(logFilePath);
            for (int i = 1; i < lines.Length; i++)
            {
                var p = lines[i].Split(',');
                if (p.Length >= 4)
                {
                    string appName = p[0];
                    if (double.TryParse(p[3], out double duration))
                    {
                        if (!appUsage.ContainsKey(appName))
                        {
                            appUsage[appName] = 0;
                        }
                        appUsage[appName] += duration;
                    }
                }
            }

            var series = new List<ISeries>();
            var labels = new List<string>();
            var sortedApps = appUsage.OrderByDescending(x => x.Value).ToList();

            int colorIndex = 0;
            var colors = new[]
            {
                SKColor.Parse("#4CAF50"),
                SKColor.Parse("#2196F3"),
                SKColor.Parse("#FF9800"),
                SKColor.Parse("#E91E63"),
                SKColor.Parse("#9C27B0"),
                SKColor.Parse("#00BCD4"),
                SKColor.Parse("#FFEB3B"),
                SKColor.Parse("#F44336")
            };

            foreach (var app in sortedApps)
            {
                double hours = Math.Round(app.Value / 3600, 2);
                labels.Add(app.Key);

                series.Add(new ColumnSeries<double>
                {
                    Name = app.Key,
                    Values = new[] { hours },
                    Fill = new SolidColorPaint(colors[colorIndex % colors.Length]),
                    Stroke = null,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 12,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                    DataLabelsFormatter = (point) => $"{point.Coordinate.PrimaryValue:F2}h"
                });

                colorIndex++;
            }

            cartesianChart1.Series = series;

            var typeface = SKTypeface.FromFamilyName(
                "Yu Gothic UI",
                SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);

            cartesianChart1.XAxes = new[]
            {
                new Axis
                {
                    Labels = labels.ToArray(),
                    TextSize = 14,
                    LabelsPaint = new SolidColorPaint(SKColors.White)
                    {
                        SKTypeface = typeface
                    },
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#404040"))
                    {
                        StrokeThickness = 1
                    }
                }
            };

            cartesianChart1.YAxes = new[]
            {
                new Axis
                {
                    Name = "使用時間（時間）",
                    MinLimit = 0,
                    TextSize = 14,
                    NameTextSize = 16,
                    LabelsPaint = new SolidColorPaint(SKColors.White)
                    {
                        SKTypeface = typeface
                    },
                    NamePaint = new SolidColorPaint(SKColors.White)
                    {
                        SKTypeface = typeface
                    },
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#404040"))
                    {
                        StrokeThickness = 1
                    }
                }
            };

            cartesianChart1.DrawMargin = new LiveChartsCore.Measure.Margin(80, 20, 80, 80);
            cartesianChart1.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom;
            cartesianChart1.LegendTextPaint = new SolidColorPaint(SKColors.White)
            {
                SKTypeface = typeface
            };
            cartesianChart1.LegendTextSize = 14;
            cartesianChart1.LegendBackgroundPaint = new SolidColorPaint(SKColor.Parse("#2D2D2D"));
        }

        private void 設定ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(config);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                apps.Clear();
                foreach (var appName in config.Apps)
                {
                    apps[appName] = new AppState();
                }

                MessageBox.Show("設定を保存しました。\n新しい設定は即座に反映されます。",
                    "設定完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void 終了ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isExiting = true;
            timer.Stop();
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isExiting)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            UpdateChart();
        }
    }
}
