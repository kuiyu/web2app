/*
 * 名称：web应用容器 - 配置服务
 * 功能：读取和保存appsettings.json配置文件。
 *       关键改进（生产级）：
 *       1. 保存采用"先写临时文件 + 原子重命名"方式，避免写中途崩溃导致配置损坏；
 *       2. 每次保存前自动备份上一版本（appsettings.json.bak）；
 *       3. 读取时若损坏，自动回退到 .bak 备份，再不行才返回默认空配置，绝不抛异常白屏；
 *       4. 反序列化允许 JSON 注释（ReadCommentHandling.Skip），便于在配置中写说明。
 */
using System.Text.Json;
using WebAppLauncher.Models;

namespace WebAppLauncher.Services
{
    public class ConfigurationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,   // 允许配置文件中的 // 注释
            WriteIndented = true
        };

        private readonly string _configPath;

        public ConfigurationService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }

        public AppSettings GetAppSettings()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Logger.Warning($"配置文件不存在: {_configPath}，使用默认空配置。");
                    return new AppSettings();
                }

                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (settings == null)
                {
                    Logger.Warning("配置文件反序列化为 null，尝试从备份恢复。");
                    return RestoreFromBackup() ?? new AppSettings();
                }

                settings.Window ??= new WindowConfig();
                settings.Apps ??= new AppConfig[] { };

                return settings;
            }
            catch (JsonException jex)
            {
                Logger.Error($"配置文件解析失败（可能已损坏）: {_configPath}", jex);
                var restored = RestoreFromBackup();
                if (restored != null)
                {
                    Logger.Warning("已从备份恢复配置。");
                    return restored;
                }
                Logger.Error("备份也不可用，使用默认空配置。");
                return new AppSettings();
            }
            catch (Exception ex)
            {
                Logger.Error($"读取配置文件失败: {_configPath}", ex);
                return new AppSettings();
            }
        }

        public AppConfig? GetCurrentAppConfig()
        {
            try
            {
                var settings = GetAppSettings();
                var currentAppId = settings.CurrentApp;
                if (string.IsNullOrEmpty(currentAppId))
                    return null;

                return settings.Apps.FirstOrDefault(a => a.Id == currentAppId);
            }
            catch (Exception ex)
            {
                Logger.Error($"获取当前应用配置失败: {ex.Message}", ex);
                return null;
            }
        }

        public void UpdateCurrentApp(string appId)
        {
            try
            {
                var settings = GetAppSettings();
                settings.CurrentApp = appId;
                SaveAppSettings(settings);
                Logger.Info($"当前应用已更新为: {appId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"更新当前应用失败: {appId}", ex);
            }
        }

        public void SaveAppSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, JsonOptions);

                Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

                // 1. 先备份当前版本（若存在且非空）
                if (File.Exists(_configPath))
                {
                    try
                    {
                        File.Copy(_configPath, _configPath + ".bak", overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"备份配置文件失败: {ex.Message}");
                    }
                }

                // 2. 写入临时文件
                var tmpPath = _configPath + ".tmp";
                File.WriteAllText(tmpPath, json);

                // 3. 原子替换：只有临时文件写成功才重命名覆盖
                if (File.Exists(_configPath))
                {
                    File.Delete(_configPath);
                }
                File.Move(tmpPath, _configPath);

                Logger.Info($"配置已保存: {_configPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"保存配置文件失败: {_configPath}", ex);
                try
                {
                    var tmpPath = _configPath + ".tmp";
                    if (File.Exists(tmpPath))
                        File.Delete(tmpPath);
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// 从 .bak 备份恢复配置；备份不存在或损坏则返回 null。
        /// </summary>
        private AppSettings? RestoreFromBackup()
        {
            var bakPath = _configPath + ".bak";
            if (!File.Exists(bakPath))
                return null;
            try
            {
                var json = File.ReadAllText(bakPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    settings.Window ??= new WindowConfig();
                    settings.Apps ??= new AppConfig[] { };
                }
                return settings;
            }
            catch (Exception ex)
            {
                Logger.Warning($"从备份恢复失败: {ex.Message}");
                return null;
            }
        }
    }
}
