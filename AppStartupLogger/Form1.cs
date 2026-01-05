using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Management;

namespace AppStartupLogger
{
    public partial class Form1 : Form
    {
        class AppState
        {
            public bool WasRunning;
            public DateTime? StartTime;
        }

        Dictionary<string, AppState> apps = new Dictionary<string, AppState>()
        {
            { "vivaldi", new AppState() },
            { "Discord", new AppState() },
            { "Code", new AppState() },
            { "GenshinImpact", new AppState() },
            { "StarRail", new AppState() },
            { "javaw", new AppState() }
        };

        string logFilePath = "app_log.csv";
        string pcStartupLogFilePath = "pc_startup_log.csv";
        DateTime? lastBootTime = null;

        System.Windows.Forms.Timer timer;


        public Form1()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.ShowIcon = true;

            if (!File.Exists(logFilePath))
            {
                File.AppendAllText(logFilePath,
                    "App,Start,End,Duration\n");
            }

            if (!File.Exists(pcStartupLogFilePath))
            {
                File.AppendAllText(pcStartupLogFilePath,
                    "BootTime,AppStartTime\n");
            }

            RecordPCStartup();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        void RecordPCStartup()
        {
            try
            {
                DateTime bootTime = GetSystemBootTime();
                DateTime appStartTime = DateTime.Now;

                var lines = File.Exists(pcStartupLogFilePath) 
                    ? File.ReadAllLines(pcStartupLogFilePath) 
                    : new string[0];

                bool alreadyRecorded = false;
                foreach (var line in lines)
                {
                    if (line.StartsWith(bootTime.ToString("yyyy-MM-dd HH:mm")))
                    {
                        alreadyRecorded = true;
                        break;
                    }
                }

                if (!alreadyRecorded)
                {
                    string logLine = $"{bootTime:yyyy-MM-dd HH:mm:ss},{appStartTime:yyyy-MM-dd HH:mm:ss}";
                    File.AppendAllText(pcStartupLogFilePath, logLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PC起動時間の記録エラー: {ex.Message}");
            }
        }

        DateTime GetSystemBootTime()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string lastBootUpTime = mo["LastBootUpTime"].ToString();
                        return ManagementDateTimeConverter.ToDateTime(lastBootUpTime);
                    }
                }
            }
            catch
            {
                return DateTime.Now.AddMilliseconds(-Environment.TickCount);
            }

            return DateTime.Now.AddMilliseconds(-Environment.TickCount);
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

                File.AppendAllText("app_log.csv", line + Environment.NewLine);
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

            if (File.Exists(pcStartupLogFilePath))
            {
                var pcLines = File.ReadAllLines(pcStartupLogFilePath);
                if (pcLines.Length > 1)
                {
                    double totalBootCount = (pcLines.Length - 1) * 0.5;
                    appUsage["PC起動回数"] = totalBootCount;
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

                if (app.Key == "PC起動回数")
                {
                    series.Add(new ColumnSeries<double>
                    {
                        Name = app.Key,
                        Values = new[] { app.Value },
                        Fill = new SolidColorPaint(SKColor.Parse("#FFD700")),
                        Stroke = null,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 12,
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                        DataLabelsFormatter = (point) => $"{point.Coordinate.PrimaryValue:F0}回"
                    });
                }
                else
                {
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
                }

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
                    Name = "使用時間（時間）/ 起動回数",
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            UpdateChart();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}
