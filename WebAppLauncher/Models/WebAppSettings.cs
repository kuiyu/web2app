/*
 * 名称：web应用容器 - 配置模型
 * 功能：定义 appsettings.json 的配置结构。
 *
 * 配置结构设计原则（让人一看就懂）：
 *  - 根对象平铺，不再嵌套 WebAppSettings 这一层；
 *  - 单个应用的字段全部自解释，避免 Path/Run 这种歧义命名。
 *
 * 单个应用 (AppConfig) 字段说明：
 *  - id     : 应用唯一标识（如 "ai-agent"），用于切换/默认启动，不可重复
 *  - name   : 在应用列表中显示的名称
 *  - title  : 桌面窗口标题栏文字
 *  - source : 要显示的 Web 内容地址，支持三种写法：
 *             · 本地相对路径  -> "apps/ai/index.html"（相对于程序目录）
 *             · 本地绝对路径  -> "file:///C:/xxx/index.html"
 *             · 远程网址      -> "https://www.deepseek.com"
 *  - launch : 【可选】启动本应用前要运行的本机程序命令，例如：
 *             · "node server.js"        （启动 Node 后端）
 *             · "cmd.exe /k echo hi"
 *             · "explorer.exe"
 *             若该命令会启动一个本地 HTTP 服务，请用 url 字段告诉容器它的地址。
 *  - url    : 【可选】最终要打开的网址（通常是本机后端，如 "http://localhost:63000"）。
 *             若填写，桌面将打开该网址而不是 source；
 *             source 仍可作为占位/兜底内容。
 *
 * 显示规则：最终打开的页面 = url 非空 ? url : source
 */
using System.Text.Json.Serialization;

namespace WebAppLauncher.Models
{
    /// <summary>配置文件根对象</summary>
    public class AppSettings
    {
        /// <summary>启动时默认打开的应用（填 apps 中某个 id）</summary>
        [JsonPropertyName("currentApp")]
        public string CurrentApp { get; set; } = string.Empty;

        /// <summary>应用列表</summary>
        [JsonPropertyName("apps")]
        public AppConfig[] Apps { get; set; } = new AppConfig[] { };

        /// <summary>窗口外观设置</summary>
        [JsonPropertyName("window")]
        public WindowConfig Window { get; set; } = new();
    }

    /// <summary>单个应用的定义</summary>
    public class AppConfig
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;
        [JsonPropertyName("launch")] public string Launch { get; set; } = string.Empty;
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    }

    /// <summary>窗口外观配置</summary>
    public class WindowConfig
    {
        [JsonPropertyName("width")] public int Width { get; set; } = 1200;
        [JsonPropertyName("height")] public int Height { get; set; } = 800;
        /// <summary>启动位置：CenterScreen / CenterParent / Manual 等</summary>
        [JsonPropertyName("startPosition")] public string StartPosition { get; set; } = "CenterScreen";
        [JsonPropertyName("disableContextMenu")] public bool DisableContextMenu { get; set; } = true;
    }
}
