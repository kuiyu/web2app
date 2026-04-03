/*
 * 名称：web应用容器
 * 功能：用程序打开本地网页，vue页面，网站
 * 作者微信：runsoft1024
 */
using WebAppLauncher.Models;

namespace WebAppLauncher.Services
{
    public class AppManagerService
    {
        private readonly ConfigurationService _configService;
        private readonly string _appsBasePath;

        public AppManagerService()
        {
            _configService = new ConfigurationService();
            _appsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps");
        }

        public List<WebAppInfo> GetAvailableApps()
        {
            var apps = new List<WebAppInfo>();
            var settings = _configService.GetAppSettings();

            foreach (var appConfig in settings.WebAppSettings.Apps)
            {
                var isUrl = PathHelper.IsUrl(appConfig.Path);
                var isAvailable = PathHelper.IsPathAvailable(_appsBasePath, appConfig.Path);
                var displayPath = isUrl ? appConfig.Path : Path.Combine(_appsBasePath, appConfig.Path);
                
                apps.Add(new WebAppInfo
                {
                    Id = appConfig.AppId,
                    Name = appConfig.Name,
                    Path = appConfig.Path,
                    Title = appConfig.Title,
                    IsAvailable = isAvailable,
                    FullPath = displayPath,
                    IsUrl = isUrl
                });
            }

            return apps;
        }

        public bool SwitchToApp(string appId)
        {
            try
            {
                var settings = _configService.GetAppSettings();
                
                if (!settings.WebAppSettings.Apps.Any(x=>x.AppId==appId))
                {
                    return false;
                }

                // 更新配置文件中的当前应用
                _configService.UpdateCurrentApp(appId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"切换应用时出错: {ex.Message}");
                return false;
            }
        }

        public string? GetAppPath(string appId)
        {
            var settings = _configService.GetAppSettings();
            var appConfig= settings.WebAppSettings.Apps.FirstOrDefault(x => x.AppId == appId);
            if (appConfig!=null)
            {
                // 如果是网址，直接返回网址
                if (PathHelper.IsUrl(appConfig.Path))
                {
                    return appConfig.Path;
                }
                
                // 否则返回本地文件路径
                return Path.Combine(_appsBasePath, appConfig.Path);
            }

            return null;
        }

        public bool ValidateAppPath(string appId)
        {
            var settings = _configService.GetAppSettings();
            
            var appConfig= settings.WebAppSettings.Apps.FirstOrDefault(x=>x.AppId == appId);
            if (appConfig!=null)
            {
                return PathHelper.IsPathAvailable(_appsBasePath, appConfig.Path);
            }

            return false;
        }

        public void OpenAppDirectory()
        {
            try
            {
                if (Directory.Exists(_appsBasePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _appsBasePath);
                }
                else
                {
                    Directory.CreateDirectory(_appsBasePath);
                    System.Diagnostics.Process.Start("explorer.exe", _appsBasePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开应用目录时出错: {ex.Message}");
            }
        }

        public void CreateSampleApp(string appId, string appName)
        {
            try
            {
                var appDir = Path.Combine(_appsBasePath, appId);
                Directory.CreateDirectory(appDir);

                var indexPath = Path.Combine(appDir, "index.html");
                
                var htmlContent = $@"
<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{appName}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Microsoft YaHei', sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            color: white;
        }}

        .container {{
            background: rgba(255, 255, 255, 0.1);
            backdrop-filter: blur(10px);
            border-radius: 20px;
            padding: 40px;
            text-align: center;
            max-width: 600px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
            border: 1px solid rgba(255, 255, 255, 0.2);
        }}

        h1 {{
            font-size: 2.5rem;
            margin-bottom: 20px;
            text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.3);
        }}

        p {{
            font-size: 1.1rem;
            line-height: 1.6;
            margin-bottom: 30px;
            opacity: 0.9;
        }}

        .features {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 20px;
            margin: 30px 0;
        }}

        .feature {{
            background: rgba(255, 255, 255, 0.15);
            padding: 20px;
            border-radius: 10px;
            transition: transform 0.3s ease;
        }}

        .feature:hover {{
            transform: translateY(-5px);
            background: rgba(255, 255, 255, 0.25);
        }}

        .feature-icon {{
            font-size: 2rem;
            margin-bottom: 10px;
        }}

        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid rgba(255, 255, 255, 0.2);
            font-size: 0.9rem;
            opacity: 0.7;
        }}

        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}

        .container {{
            animation: fadeIn 0.8s ease-out;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🎉 {appName}</h1>
        <p>这是一个示例Web应用，运行在WebAppLauncher中。您可以在此处构建自己的Web应用程序。</p>
        
        <div class='features'>
            <div class='feature'>
                <div class='feature-icon'>🚀</div>
                <h3>快速开发</h3>
                <p>使用现代Web技术</p>
            </div>
            <div class='feature'>
                <div class='feature-icon'>💡</div>
                <h3>易于集成</h3>
                <p>与WinForms无缝集成</p>
            </div>
            <div class='feature'>
                <div class='feature-icon'>🛡️</div>
                <h3>安全可靠</h3>
                <p>右键菜单已禁用</p>
            </div>
        </div>

        <div class='footer'>
            <p>应用ID: {appId} | 创建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            <p>© 2025 WebAppLauncher - 专业的Web应用容器</p>
        </div>
    </div>

    <script>
        // 简单的交互效果
        document.querySelectorAll('.feature').forEach(feature => {{
            feature.addEventListener('click', () => {{
                alert('您点击了功能项！');
            }});
        }});

        // 显示当前时间
        function updateTime() {{
            const now = new Date();
            console.log(`当前时间: ${{now.toLocaleTimeString('zh-CN')}}`);
        }}
        
        setInterval(updateTime, 1000);
        updateTime();
    </script>
</body>
</html>";

                File.WriteAllText(indexPath, htmlContent);
                Console.WriteLine($"示例应用已创建: {appDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建示例应用时出错: {ex.Message}");
            }
        }
    }

    public class WebAppInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public bool IsUrl { get; set; }
    }
}