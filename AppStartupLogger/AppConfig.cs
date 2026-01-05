using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppStartupLogger
{
    public class AppConfig
    {
        [JsonPropertyName("apps")]
        public List<string> Apps { get; set; } = new List<string>();

        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AppStartupLogger");

        private static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "config.json");

        public static string GetAppDataFolder() => AppDataFolder;
        public static string GetLogFilePath() => Path.Combine(AppDataFolder, "app_log.csv");

        public static AppConfig Load()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                if (!File.Exists(ConfigFilePath))
                {
                    var defaultConfig = CreateDefaultConfig();
                    defaultConfig.Save();
                    return defaultConfig;
                }

                string json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config ?? CreateDefaultConfig();
            }
            catch
            {
                return CreateDefaultConfig();
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定ファイルの保存に失敗しました: {ex.Message}",
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                Apps = new List<string>
                {
                    "Discord",
                }
            };
        }
    }
}
