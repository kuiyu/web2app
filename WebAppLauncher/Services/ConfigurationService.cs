/*
 * 名称：web应用容器
 * 功能：用程序打开本地网页，vue页面，网站
 * 作者微信：runsoft1024
 */
using Microsoft.Extensions.Configuration;
using WebAppLauncher.Models;

namespace WebAppLauncher.Services
{
    public class ConfigurationService
    {
        private  IConfiguration _configuration;
        private readonly string _configFilePath;

        public ConfigurationService()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        public AppSettings GetAppSettings()
        {
            var settings = new AppSettings();
            
            try
            {
                var webAppSection = _configuration.GetSection("WebAppSettings");
                var apps = _configuration.GetSection("WebAppSettings:Apps");
                settings.WebAppSettings.Apps = apps.Get<WebAppConfig[]>();
                settings.WebAppSettings.CurrentApp = webAppSection.GetValue<string>("CurrentApp");
                var windowSection = _configuration.GetSection("WindowSettings");
                if (windowSection.Exists())
                {
                    if (int.TryParse(windowSection["Width"], out int width))
                        settings.WindowSettings.Width = width;
                    
                    if (int.TryParse(windowSection["Height"], out int height))
                        settings.WindowSettings.Height = height;
                    
                    settings.WindowSettings.StartPosition = windowSection["StartPosition"] ?? "CenterScreen";
                    
                    if (bool.TryParse(windowSection["DisableContextMenu"], out bool disableMenu))
                        settings.WindowSettings.DisableContextMenu = disableMenu;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件时出错: {ex.Message}");
            }

            return settings;
        }

        public WebAppConfig? GetCurrentAppConfig()
        {
            var settings = GetAppSettings();
            var appConfig = settings.WebAppSettings.Apps.FirstOrDefault(x => x.AppId == settings.WebAppSettings.CurrentApp);
                return appConfig;
        }

        public void UpdateCurrentApp(string appId)
        {
            try
            {
                // 读取并解析现有配置
                var appSettings = GetAppSettings();
                
                // 验证要切换的应用是否存在
                if (!appSettings.WebAppSettings.Apps.Any(x=>x.Name==appId))
                {
                    throw new ArgumentException($"应用 '{appId}' 不存在于配置中");
                }
                
                // 更新当前应用
                appSettings.WebAppSettings.CurrentApp = appId;
                
                // 将更新后的配置写回文件
                SaveAppSettings(appSettings);
                
                Console.WriteLine($"当前应用已更新为: {appId}");
                
                // 重新加载配置
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                
                _configuration = builder.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新配置文件时出错: {ex.Message}");
                throw; // 重新抛出异常，让调用者知道更新失败
            }
        }
        
        private void SaveAppSettings(AppSettings appSettings)
        {
            try
            {
                var settings = new
                {
                    WebAppSettings = new
                    {
                        CurrentApp = appSettings.WebAppSettings.CurrentApp,
                        Apps = appSettings.WebAppSettings.Apps.ToDictionary(
                         
                            kvp => new
                            {
                                AppId=kvp.AppId,
                                Name = kvp.Name,
                                Path = kvp.Path,
                                Title = kvp.Title,
                                Run=kvp.Run
                            }
                        )
                    },
                    WindowSettings = new
                    {
                        Width = appSettings.WindowSettings.Width,
                        Height = appSettings.WindowSettings.Height,
                        StartPosition = appSettings.WindowSettings.StartPosition,
                        DisableContextMenu = appSettings.WindowSettings.DisableContextMenu
                    }
                };
                
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置文件时出错: {ex.Message}");
                throw;
            }
        }
    }
}