namespace WebAppLauncher.Models
{
    public class WebAppSettings
    {
        public string CurrentApp { get; set; } = "app1";
        public Dictionary<string, WebAppConfig> Apps { get; set; } = new();
    }

    public class WebAppConfig
    {
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