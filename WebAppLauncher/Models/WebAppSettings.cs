/*
 * 名称：web应用容器
 * 功能：用程序打开本地网页，vue页面，网站
 * 作者微信：runsoft1024
 */
using System.Runtime.CompilerServices;

namespace WebAppLauncher.Models
{
    public class WebAppSettings
    {
        public string CurrentApp { get; set; } = "app1";
        public WebAppConfig[] Apps { get; set; } = new WebAppConfig[] { };

        
    }

    public class WebAppConfig
    {
        public string AppId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public string Run { get; set; } = string.Empty;
    }

    public class WindowSettings
    {
        public int Width { get; set; } = 1200;
        public int Height { get; set; } = 800;
        public string StartPosition { get; set; } = "CenterScreen";
        public bool DisableContextMenu { get; set; } = true;
    }

    public class AppSettings
    {
        public WebAppSettings WebAppSettings { get; set; } = new();
        public WindowSettings WindowSettings { get; set; } = new();

        
    }
}