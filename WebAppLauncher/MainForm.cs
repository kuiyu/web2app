using Microsoft.Web.WebView2.WinForms;
using WebAppLauncher.Models;
using WebAppLauncher.Services;

namespace WebAppLauncher
{
    public partial class MainForm : Form
    {
        private readonly ConfigurationService _configService;
        private readonly AppManagerService _appManager;
        private readonly AppRunnerService _appRunner;
        private readonly WebView2 _webView;
        private WebAppConfig? _currentApp;
        private string _appBasePath;
        private MenuStrip _menuStrip;
        private ToolStripMenuItem _fileMenu;
        private ToolStripMenuItem _appMenu;
        private ToolStripMenuItem _toolsMenu;
        private ToolStripMenuItem _helpMenu;
        private System.Windows.Forms.Timer? _mouseCheckTimer;
        private bool _menuVisible;
        private const int MENU_TRIGGER_HEIGHT = 50; // 顶部50像素区域触发显示
        private const int MOUSE_CHECK_INTERVAL = 100; // 每100ms检查一次鼠标位置

        public MainForm()
        {
            InitializeComponent();
            
            _configService = new ConfigurationService();
            _appManager = new AppManagerService();
            _appRunner = new AppRunnerService();
            _appBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            
            // 不再需要隐藏菜单计时器（立即隐藏）
            
            // 初始化鼠标检查计时器
            _mouseCheckTimer = new System.Windows.Forms.Timer();
            _mouseCheckTimer.Interval = MOUSE_CHECK_INTERVAL;
            _mouseCheckTimer.Tick += MouseCheckTimer_Tick;
            _mouseCheckTimer.Start();
            
            // 先创建内容Panel作为WebView2的容器（占满整个窗体）
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            
            // 创建WebView2控件
            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                CreationProperties = null
            };
            
            // 将WebView2添加到内容Panel
            contentPanel.Controls.Add(_webView);
            
            // 添加内容Panel（占满整个窗体）
            Controls.Add(contentPanel);
            
            // 再创建并添加菜单栏（当显示时在最上层）
            CreateMenuStrip();
            _menuVisible = false;
            _menuStrip.Visible = false;
            
            // 确保菜单栏在Z-Order的最上层
            _menuStrip.BringToFront();
            
            // 初始化WebView2
            InitializeWebViewAsync();
            
            // 应用窗口设置
            ApplyWindowSettings();
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                // 初始化WebView2环境
                var environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                await _webView.EnsureCoreWebView2Async(environment);
                
                // 配置WebView2
                ConfigureWebView();
                
                // 加载当前应用
                LoadCurrentApp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化WebView2时出错: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureWebView()
        {
            var settings = _webView.CoreWebView2.Settings;
            
            // 禁用右键菜单
            settings.AreDefaultContextMenusEnabled = false;
            
            // 启用其他常用功能
            settings.IsScriptEnabled = true;
            settings.IsWebMessageEnabled = true;
            settings.AreDevToolsEnabled = false; // 生产环境建议禁用
            
            // 禁用缩放控制（用户手动缩放）
            settings.IsZoomControlEnabled = false;
            
            // 禁用文本选择（可选）
            settings.IsStatusBarEnabled = false;
            settings.IsBuiltInErrorPageEnabled = false;
            
            // 监听上下文菜单请求事件（彻底禁用右键）
            _webView.CoreWebView2.ContextMenuRequested += (sender, args) =>
            {
                args.Handled = true;
            };
            
            // 监听导航完成事件
            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            
            // 监听新窗口创建事件（阻止弹出新窗口）
            _webView.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                args.Handled = true;
                if (args.Uri != null)
                {
                    // 在主窗口中打开链接
                    _webView.CoreWebView2.Navigate(args.Uri);
                }
            };
            
            // 监听Web消息（用于与Web应用通信）
            _webView.CoreWebView2.WebMessageReceived += (sender, args) =>
            {
                try
                {
                    var message = args.TryGetWebMessageAsString();
                    Console.WriteLine($"收到Web消息: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理Web消息时出错: {ex.Message}");
                }
            };
            
            // 监听窗体大小变化事件，实现自动缩放
            this.Resize += OnFormResize;
            
            // 注入JavaScript禁用右键
            InjectDisableContextMenuScript();
        }
        
        private async void InjectDisableContextMenuScript()
        {
            try
            {
                // 等待WebView2准备就绪
                await Task.Delay(1000);
                
                // 注入JavaScript彻底禁用右键
                var disableScript = @"
                    // 禁用右键菜单
                    document.addEventListener('contextmenu', function(e) {
                        e.preventDefault();
                        return false;
                    }, false);
                    
                    // 禁用文本选择（可选）
                    document.addEventListener('selectstart', function(e) {
                        e.preventDefault();
                        return false;
                    }, false);
                    
                    // 禁用拖拽
                    document.addEventListener('dragstart', function(e) {
                        e.preventDefault();
                        return false;
                    }, false);
                    
                    // 发送消息到宿主应用
                    window.addEventListener('load', function() {
                        window.chrome.webview.postMessage('Web应用已加载，右键菜单已禁用');
                    });
                ";
                
                await _webView.CoreWebView2.ExecuteScriptAsync(disableScript);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注入禁用脚本时出错: {ex.Message}");
            }
        }

        private void ApplyWindowSettings()
        {
            var settings = _configService.GetAppSettings().WindowSettings;
            
            // 设置窗口大小
            this.Size = new Size(settings.Width, settings.Height);
            
            // 设置启动位置
            switch (settings.StartPosition.ToLower())
            {
                case "centerscreen":
                    this.StartPosition = FormStartPosition.CenterScreen;
                    break;
                case "windowsdefaultlocation":
                    this.StartPosition = FormStartPosition.WindowsDefaultLocation;
                    break;
                case "manual":
                    this.StartPosition = FormStartPosition.Manual;
                    break;
                default:
                    this.StartPosition = FormStartPosition.CenterScreen;
                    break;
            }
            
            // 禁用窗体本身的右键菜单
            this.ContextMenuStrip = null;
            
            // 监听窗体鼠标事件
            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    // 阻止右键菜单
                    //e.Handled = true;
                }
            };
        }

        private void LoadCurrentApp()
        {
            _currentApp = _configService.GetCurrentAppConfig();
            
            if (_currentApp != null)
            {
                LoadAppById(_configService.GetAppSettings().WebAppSettings.CurrentApp);
            }
            else
            {
                ShowErrorPage("未找到当前应用的配置");
            }
        }
        
        private async void LoadAppById(string appId)
        {
            var appConfig = _configService.GetAppSettings().WebAppSettings.Apps
                .FirstOrDefault(kvp => kvp.Key == appId).Value;
            
            if (appConfig != null)
            {
                _currentApp = appConfig;
                
                // 设置窗口标题
                this.Text = appConfig.Title;
                
                try
                {
                    // 使用PathHelper构建合适的Uri
                    var uri = PathHelper.BuildUri(_appBasePath, appConfig.Path);
                    _webView.Source = uri;
                    
                    // 异步运行Run字段中的程序（不等待完成，避免阻塞UI）
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var successCount = await _appRunner.RunAppsForCurrentApp(appId);
                            if (successCount > 0)
                            {
                                Console.WriteLine($"应用 '{appId}' 的Run字段程序启动完成");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"运行应用 '{appId}' 的Run程序时出错: {ex.Message}");
                        }
                    });
                }
                catch (FileNotFoundException ex)
                {
                    // 本地文件不存在
                    ShowErrorPage($"应用文件未找到: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // 其他错误（如无效的网址格式）
                    ShowErrorPage($"加载应用时出错: {ex.Message}");
                }
            }
            else
            {
                ShowErrorPage($"未找到应用 '{appId}' 的配置");
            }
        }

        private async void OnNavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                var errorText = $"导航失败: {e.WebErrorStatus}";
                ShowErrorPage(errorText);
                return;
            }
            
            try
            {
                await Task.Delay(100);
                Console.WriteLine("导航完成，恢复WebView原始滚动行为...");
                await RestoreWebViewNativeBehavior();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"恢复WebView原始行为时出错: {ex.Message}");
            }
        }

        private async Task ApplyNoScrollbarAndAutoZoom()
        {
            try
            {
                if (_webView.CoreWebView2 == null)
                    return;

                // 获取窗体当前大小（减去菜单栏高度）
                var menuHeight = _menuStrip?.Height ?? 0;
                var availableWidth = _webView.Width;
                var availableHeight = _webView.Height - menuHeight;
                
                if (availableWidth <= 0 || availableHeight <= 0)
                    return;

                // 构建JavaScript代码，实现无滚动条和自动缩放
                var jsCode = $@"
                    (function() {{
                        console.log('开始应用无滚动条和自动缩放...');
                        console.log('窗体可用尺寸: {availableWidth}x{availableHeight}');
                        
                        // 1. 隐藏所有滚动条
                        var style = document.createElement('style');
                        style.textContent = `
                            html, body {{
                                overflow: hidden !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                height: 100% !important;
                                width: 100% !important;
                            }}
                            
                            /* 隐藏所有可能的滚动条 */
                            * {{
                                scrollbar-width: none !important; /* Firefox */
                                -ms-overflow-style: none !important; /* IE and Edge */
                            }}
                            
                            *::-webkit-scrollbar {{
                                display: none !important; /* Chrome, Safari, Opera */
                            }}
                            
                            /* 确保内容容器不产生滚动 */
                            body > div, body > main, body > section {{
                                overflow: hidden !important;
                                max-height: 100vh !important;
                            }}
                        `;
                        document.head.appendChild(style);
                        
                        // 2. 获取页面内容实际尺寸
                        var body = document.body;
                        var html = document.documentElement;
                        
                        // 获取页面的实际宽度和高度
                        var pageWidth = Math.max(
                            body.scrollWidth,
                            body.offsetWidth,
                            html.clientWidth,
                            html.scrollWidth,
                            html.offsetWidth
                        );
                        
                        var pageHeight = Math.max(
                            body.scrollHeight,
                            body.offsetHeight,
                            html.clientHeight,
                            html.scrollHeight,
                            html.offsetHeight
                        );
                        
                        console.log('页面实际尺寸:', pageWidth, 'x', pageHeight);
                        
                        // 3. 计算合适的缩放比例
                        var availableWidth = {availableWidth};
                        var availableHeight = {availableHeight};
                        
                        // 计算宽度和高度的缩放比例
                        var scaleX = availableWidth / pageWidth;
                        var scaleY = availableHeight / pageHeight;
                        
                        // 取较小的缩放比例，确保内容完全显示且不变形
                        var scale = Math.min(scaleX, scaleY);
                        
                        // 限制缩放范围，避免过大或过小
                        scale = Math.min(Math.max(scale, 0.1), 2.0);
                        
                        console.log('计算得到的缩放比例:', scale);
                        
                        // 4. 先检查是否已经存在缩放容器
                        var existingContainer = document.getElementById('webapplauncher-zoom-container');
                        if (existingContainer) {{
                            // 更新现有容器的缩放比例
                            existingContainer.style.transform = 'scale(' + scale + ')';
                            existingContainer.style.transformOrigin = 'top left';
                        }} else {{
                            // 创建新的缩放容器
                            var container = document.createElement('div');
                            container.id = 'webapplauncher-zoom-container';
                            container.style.cssText = `
                                position: fixed;
                                top: 0;
                                left: 0;
                                width: ` + pageWidth + `px;
                                height: ` + pageHeight + `px;
                                transform-origin: top left;
                                transform: scale(` + scale + `);
                                overflow: hidden;
                                z-index: 999999;
                            `;
                            
                            // 将页面内容移动到容器中
                            var bodyChildren = Array.from(body.children);
                            bodyChildren.forEach(child => {{
                                if (child.id !== 'webapplauncher-zoom-container') {{
                                    container.appendChild(child.cloneNode(true));
                                }}
                            }});
                            
                            // 清空body并添加容器
                            body.innerHTML = '';
                            body.appendChild(container);
                        }}
                        
                        // 5. 调整body尺寸以适应缩放
                        body.style.cssText = `
                            width: 100% !important;
                            height: 100% !important;
                            margin: 0 !important;
                            padding: 0 !important;
                            overflow: hidden !important;
                            background-color: white;
                        `;
                        
                        // 6. 发送缩放信息回宿主应用
                        window.chrome.webview.postMessage('缩放应用完成，比例：' + scale.toFixed(2));
                        
                        console.log('无滚动条和自动缩放应用完成');
                        return scale;
                    }})();
                ";

                // 执行JavaScript代码
                var result = await _webView.CoreWebView2.ExecuteScriptAsync(jsCode);
                Console.WriteLine($"应用缩放完成，缩放比例: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用无滚动条和自动缩放时出错: {ex.Message}");
            }
        }

        private void OnFormResize(object? sender, EventArgs e)
        {
            // 窗体大小变化时，WebView会自动调整，不需要额外处理
            // 保持像浏览器一样的原生行为
        }

        private async Task ApplyResponsiveAutoZoom()
        {
            try
            {
                if (_webView.CoreWebView2 == null)
                    return;

                // 获取窗体当前大小（减去菜单栏高度）
                var menuHeight = _menuStrip?.Height ?? 0;
                var availableWidth = _webView.Width;
                var availableHeight = _webView.Height - menuHeight;
                
                if (availableWidth <= 0 || availableHeight <= 0)
                    return;

                Console.WriteLine($"开始应用响应式自动缩放，可用尺寸: {availableWidth}x{availableHeight}");

                // 使用更可靠的响应式方法：通过CSS viewport和媒体查询实现自适应
                var responsiveJs = $@"
                    (function() {{
                        console.log('应用响应式自动缩放...');
                        
                        // 1. 移除任何可能干扰的自定义缩放样式
                        var existingContainer = document.getElementById('webapplauncher-zoom-container');
                        if (existingContainer) {{
                            existingContainer.parentNode.removeChild(existingContainer);
                        }}
                        
                        // 2. 重置所有缩放相关的样式
                        document.body.style.transform = '';
                        document.body.style.transformOrigin = '';
                        document.body.style.width = '';
                        document.body.style.height = '';
                        document.body.style.position = '';
                        
                        // 3. 创建自适应CSS
                        var style = document.createElement('style');
                        style.id = 'webapplauncher-responsive-style';
                        style.textContent = `
                            /* 基础自适应设置 */
                            html, body {{
                                overflow: hidden !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                width: 100% !important;
                                height: 100% !important;
                                box-sizing: border-box !important;
                            }}
                            
                            /* 隐藏所有滚动条 */
                            * {{
                                scrollbar-width: none !important;
                                -ms-overflow-style: none !important;
                            }}
                            
                            *::-webkit-scrollbar {{
                                display: none !important;
                            }}
                            
                            /* 自适应容器 */
                            .webapplauncher-container {{
                                width: 100% !important;
                                height: 100% !important;
                                overflow: hidden !important;
                                position: relative !important;
                            }}
                            
                            /* 确保所有内容都包含在容器内 */
                            .webapplauncher-container > * {{
                                max-width: 100% !important;
                                max-height: 100% !important;
                                box-sizing: border-box !important;
                            }}
                            
                            /* 响应式媒体查询 */
                            @media (max-width: {availableWidth}px) {{
                                body > * {{
                                    transform-origin: top left !important;
                                }}
                            }}
                        `;
                        
                        // 移除旧的样式
                        var oldStyle = document.getElementById('webapplauncher-responsive-style');
                        if (oldStyle) {{
                            oldStyle.parentNode.removeChild(oldStyle);
                        }}
                        
                        document.head.appendChild(style);
                        
                        // 4. 创建自适应容器
                        var container = document.createElement('div');
                        container.className = 'webapplauncher-container';
                        container.id = 'webapplauncher-responsive-container';
                        
                        // 5. 将页面内容移动到容器中
                        var bodyChildren = Array.from(document.body.children);
                        bodyChildren.forEach(child => {{
                            if (child.id !== 'webapplauncher-responsive-container' && 
                                child.id !== 'webapplauncher-responsive-style') {{
                                container.appendChild(child.cloneNode(true));
                            }}
                        }});
                        
                        // 6. 清空body并添加容器
                        document.body.innerHTML = '';
                        document.body.appendChild(container);
                        
                        // 7. 发送响应式缩放完成信息
                        window.chrome.webview.postMessage('响应式缩放应用完成，窗体尺寸：' + {availableWidth} + 'x' + {availableHeight});
                        
                        console.log('响应式自动缩放完成');
                        return 'responsive_zoom_applied';
                    }})();
                ";

                var result = await _webView.CoreWebView2.ExecuteScriptAsync(responsiveJs);
                Console.WriteLine($"响应式自动缩放应用完成: {result}");
                
                // 额外应用一个简单的缩放以确保效果
                await ApplyDirectZoom(availableWidth, availableHeight);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用响应式自动缩放时出错: {ex.Message}");
            }
        }

        private async Task ApplyDirectZoom(int availableWidth, int availableHeight)
        {
            try
            {
                if (_webView.CoreWebView2 == null)
                    return;

                // 使用直接缩放方法
                var directZoomJs = $@"
                    (function() {{
                        console.log('应用直接缩放...');
                        
                        // 获取页面实际尺寸
                        var body = document.body;
                        var html = document.documentElement;
                        
                        var pageWidth = Math.max(
                            body.scrollWidth,
                            body.offsetWidth,
                            html.clientWidth,
                            html.scrollWidth,
                            html.offsetWidth
                        );
                        
                        var pageHeight = Math.max(
                            body.scrollHeight,
                            body.offsetHeight,
                            html.clientHeight,
                            html.scrollHeight,
                            html.offsetHeight
                        );
                        
                        console.log('页面尺寸:', pageWidth, 'x', pageHeight);
                        console.log('可用尺寸:', {availableWidth}, 'x', {availableHeight});
                        
                        // 计算缩放比例
                        if (pageWidth <= 0 || pageHeight <= 0) {{
                            pageWidth = Math.max(pageWidth, 1);
                            pageHeight = Math.max(pageHeight, 1);
                        }}
                        
                        var scaleX = {availableWidth} / pageWidth;
                        var scaleY = {availableHeight} / pageHeight;
                        var scale = Math.min(scaleX, scaleY);
                        
                        // 限制缩放范围
                        scale = Math.min(Math.max(scale, 0.1), 2.0);
                        
                        console.log('直接缩放比例:', scale);
                        
                        // 应用缩放
                        body.style.transform = 'scale(' + scale + ')';
                        body.style.transformOrigin = 'top left';
                        body.style.width = {availableWidth} + 'px';
                        body.style.height = {availableHeight} + 'px';
                        
                        // 确保html元素也正确设置
                        html.style.overflow = 'hidden';
                        html.style.width = '100%';
                        html.style.height = '100%';
                        
                        window.chrome.webview.postMessage('直接缩放完成，比例：' + scale.toFixed(2));
                        
                        return scale;
                    }})();
                ";

                var result = await _webView.CoreWebView2.ExecuteScriptAsync(directZoomJs);
                Console.WriteLine($"直接缩放应用完成，缩放比例: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用直接缩放出错: {ex.Message}");
            }
        }

        private async Task RestoreWebViewNativeBehavior()
        {
            try
            {
                if (_webView.CoreWebView2 == null)
                    return;

                var restoreJs = @"
                    (function() {
                        try {
                            // 移除所有自定义样式
                            var stylesToRemove = [
                                'webapplauncher-autozoom-style',
                                'webapplauncher-responsive-style',
                                'webapplauncher-zoom-container',
                                'webapplauncher-responsive-container'
                            ];
                            
                            stylesToRemove.forEach(function(id) {
                                var element = document.getElementById(id);
                                if (element) {
                                    element.parentNode.removeChild(element);
                                }
                            });
                            
                            // 重置html和body的样式
                            var html = document.documentElement;
                            var body = document.body;
                            
                            // 重置html样式
                            html.style.overflow = '';
                            html.style.width = '';
                            html.style.height = '';
                            html.style.margin = '';
                            html.style.padding = '';
                            
                            // 重置body样式
                            body.style.transform = '';
                            body.style.transformOrigin = '';
                            body.style.width = '';
                            body.style.height = '';
                            body.style.position = '';
                            body.style.overflow = '';
                            body.style.margin = '';
                            body.style.padding = '';
                            
                            // 恢复滚动条
                            var restoreScrollStyle = document.createElement('style');
                            restoreScrollStyle.textContent = `
                                html, body {
                                    overflow: auto !important;
                                }
                                * {
                                    scrollbar-width: auto !important;
                                    -ms-overflow-style: auto !important;
                                }
                                *::-webkit-scrollbar {
                                    display: block !important;
                                }
                            `;
                            document.head.appendChild(restoreScrollStyle);
                            
                            console.log('WebView原生行为已恢复');
                            return 'native_behavior_restored';
                        } catch(e) {
                            console.error('恢复WebView原生行为时出错:', e);
                            return 'error: ' + e.message;
                        }
                    })();
                ";

                var result = await _webView.CoreWebView2.ExecuteScriptAsync(restoreJs);
                Console.WriteLine($"WebView原生行为恢复完成: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"恢复WebView原生行为时出错: {ex.Message}");
            }
        }

        private async Task RestoreOriginalScrollbars()
        {
            try
            {
                if (_webView.CoreWebView2 == null)
                    return;

                Console.WriteLine("开始恢复原始滚动条...");

                // 构建JavaScript代码，恢复原始滚动条并移除缩放
                var restoreJs = @"
                    (function() {
                        console.log('恢复原始滚动条和移除缩放...');
                        
                        // 1. 移除所有自定义缩放容器
                        var zoomContainer = document.getElementById('webapplauncher-zoom-container');
                        if (zoomContainer) {
                            // 将内容移回body
                            var containerChildren = Array.from(zoomContainer.children);
                            containerChildren.forEach(child => {
                                document.body.appendChild(child);
                            });
                            zoomContainer.parentNode.removeChild(zoomContainer);
                        }
                        
                        // 2. 移除响应式容器
                        var responsiveContainer = document.getElementById('webapplauncher-responsive-container');
                        if (responsiveContainer) {
                            var responsiveChildren = Array.from(responsiveContainer.children);
                            responsiveChildren.forEach(child => {
                                document.body.appendChild(child);
                            });
                            responsiveContainer.parentNode.removeChild(responsiveContainer);
                        }
                        
                        // 3. 移除自定义样式
                        var customStyle = document.getElementById('webapplauncher-responsive-style');
                        if (customStyle) {
                            customStyle.parentNode.removeChild(customStyle);
                        }
                        
                        // 4. 重置所有缩放相关的样式
                        document.body.style.transform = '';
                        document.body.style.transformOrigin = '';
                        document.body.style.width = '';
                        document.body.style.height = '';
                        document.body.style.position = '';
                        document.body.style.overflow = '';
                        document.body.style.margin = '';
                        document.body.style.padding = '';
                        
                        // 5. 恢复html元素的原始样式
                        document.documentElement.style.overflow = '';
                        document.documentElement.style.width = '';
                        document.documentElement.style.height = '';
                        document.documentElement.style.margin = '';
                        document.documentElement.style.padding = '';
                        
                        // 6. 恢复滚动条显示
                        var restoreScrollbarsStyle = document.createElement('style');
                        restoreScrollbarsStyle.textContent = `
                            /* 恢复滚动条显示 */
                            html, body {
                                overflow: auto !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                width: auto !important;
                                height: auto !important;
                            }
                            
                            /* 恢复所有滚动条 */
                            * {
                                scrollbar-width: auto !important;
                                -ms-overflow-style: auto !important;
                            }
                            
                            *::-webkit-scrollbar {
                                display: block !important;
                            }
                            
                            /* 确保内容可以正常滚动 */
                            body > div, body > main, body > section {
                                overflow: auto !important;
                                max-height: none !important;
                            }
                        `;
                        document.head.appendChild(restoreScrollbarsStyle);
                        
                        // 7. 强制重新计算布局
                        document.body.style.display = 'none';
                        document.body.offsetHeight; // 强制重排
                        document.body.style.display = '';
                        
                        console.log('原始滚动条恢复完成');
                        window.chrome.webview.postMessage('滚动条已恢复，缩放已移除');
                        return 'scrollbars_restored';
                    })();
                ";

                var result = await _webView.CoreWebView2.ExecuteScriptAsync(restoreJs);
                Console.WriteLine($"恢复原始滚动条完成: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"恢复原始滚动条时出错: {ex.Message}");
            }
        }

        private async void ShowErrorPage(string errorMessage)
        {
            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>错误</title>
                    <style>
                        html, body {{
                            overflow: hidden !important;
                            margin: 0 !important;
                            padding: 0 !important;
                            width: 100% !important;
                            height: 100% !important;
                        }}
                        
                        body {{ 
                            font-family: 'Microsoft YaHei', sans-serif; 
                            display: flex; 
                            justify-content: center; 
                            align-items: center; 
                            height: 100vh; 
                            margin: 0; 
                            background-color: #f8f9fa;
                            color: #333;
                            overflow: hidden !important;
                        }}
                        .error-container {{ 
                            text-align: center; 
                            padding: 40px;
                            background: white;
                            border-radius: 10px;
                            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
                            max-width: 500px;
                            transform-origin: center center;
                        }}
                        .error-icon {{ 
                            font-size: 48px; 
                            color: #dc3545; 
                            margin-bottom: 20px;
                        }}
                        .error-message {{ 
                            font-size: 16px; 
                            margin-bottom: 20px; 
                            color: #666;
                        }}
                        .retry-btn {{ 
                            background-color: #007bff; 
                            color: white; 
                            border: none; 
                            padding: 10px 20px; 
                            border-radius: 5px; 
                            cursor: pointer;
                            font-size: 14px;
                        }}
                        .retry-btn:hover {{ 
                            background-color: #0056b3; 
                        }}
                    </style>
                </head>
                <body>
                    <div class='error-container'>
                        <div class='error-icon'>⚠️</div>
                        <h2>加载失败</h2>
                        <div class='error-message'>{errorMessage}</div>
                        <button class='retry-btn' onclick='location.reload()'>重新加载</button>
                    </div>
                </body>
                </html>";
            
            _webView.NavigateToString(html);
            
            // 等待页面加载完成后恢复滚动条
            try
            {
                await Task.Delay(800);
                await RestoreOriginalScrollbars();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误页面恢复滚动条时出错: {ex.Message}");
            }
        }

        private void CreateMenuStrip()
        {
            _menuStrip = new MenuStrip();
            
            // 不使用Dock，而是手动设置位置和大小
            _menuStrip.Location = new Point(0, 0);
            
            // 文件菜单
            _fileMenu = new ToolStripMenuItem("文件(&F)");
            _fileMenu.DropDownItems.Add("刷新应用(&R)", null, (s, e) => LoadCurrentApp());
            _fileMenu.DropDownItems.Add(new ToolStripSeparator());
            _fileMenu.DropDownItems.Add("打开应用目录(&O)", null, (s, e) => _appManager.OpenAppDirectory());
            _fileMenu.DropDownItems.Add(new ToolStripSeparator());
            _fileMenu.DropDownItems.Add("退出(&X)", null, (s, e) => Close());
            
            // 应用菜单
            _appMenu = new ToolStripMenuItem("应用(&A)");
            UpdateAppMenuItems();
            
            // 工具菜单
            _toolsMenu = new ToolStripMenuItem("工具(&T)");
            _toolsMenu.DropDownItems.Add("创建示例应用(&C)", null, (s, e) => CreateSampleAppDialog());
            _toolsMenu.DropDownItems.Add("检查应用状态(&S)", null, (s, e) => CheckAppStatus());
            _toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            _toolsMenu.DropDownItems.Add("验证Run字段配置(&R)", null, (s, e) => ValidateRunConfig());
            _toolsMenu.DropDownItems.Add("恢复原始滚动条(&Z)", null, (s, e) => RestoreScrollbarsFunction());
            
            // 帮助菜单
            _helpMenu = new ToolStripMenuItem("帮助(&H)");
            _helpMenu.DropDownItems.Add("关于(&A)...", null, (s, e) => ShowAboutDialog());
            
            _menuStrip.Items.AddRange(new ToolStripItem[] { _fileMenu, _appMenu, _toolsMenu, _helpMenu });
            
            // 监听菜单的鼠标移动事件，保持菜单显示
            _menuStrip.MouseMove += MenuStrip_MouseMove;
            
            // 添加菜单栏
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;
            
            // 确保菜单栏在Z-Order的最上层
            _menuStrip.BringToFront();
        }

        private void MenuStrip_MouseMove(object? sender, MouseEventArgs e)
        {
            // 鼠标在菜单上移动，保持菜单显示
            if (_menuVisible)
            {
                ResetHideTimer();
            }
        }

        private void MouseCheckTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 检查鼠标是否在窗体范围内
                if (this.Bounds.Contains(Cursor.Position))
                {
                    var mousePos = PointToClient(Cursor.Position);
                    
                    // 检查鼠标是否在顶部触发区域
                    bool mouseInTopArea = mousePos.Y <= MENU_TRIGGER_HEIGHT;
                    // 检查鼠标是否在菜单区域
                    bool mouseInMenuArea = _menuVisible && _menuStrip.Bounds.Contains(mousePos);
                    
                    if (mouseInTopArea || mouseInMenuArea)
                    {
                        // 鼠标在顶部或菜单区域，显示菜单
                        ShowMenu();
                    }
                    else if (_menuVisible)
                    {
                        // 鼠标不在任何相关区域，立即隐藏菜单
                        HideMenu();
                    }
                }
                else if (_menuVisible)
                {
                    // 鼠标不在窗体范围内，立即隐藏菜单
                    HideMenu();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查鼠标位置时出错: {ex.Message}");
            }
        }
        
        private void UpdateAppMenuItems()
        {
            _appMenu.DropDownItems.Clear();
            
            var apps = _appManager.GetAvailableApps();
            
            // 从配置文件获取当前应用，而不是从服务缓存
            var currentApp = GetCurrentAppFromConfig();
            
            foreach (var app in apps)
            {
                var menuItem = new ToolStripMenuItem
                {
                    Text = $"{app.Name} ({app.Id})",
                    Tag = app.Id,
                    Checked = app.Id == currentApp
                };
                
                menuItem.Click += (s, e) =>
                {
                    if (_appManager.SwitchToApp(app.Id))
                    {
                        // 直接加载指定的应用
                        LoadAppById(app.Id);
                        UpdateAppMenuItems();
                    }
                };
                
                _appMenu.DropDownItems.Add(menuItem);
            }
            
            if (apps.Count > 0)
            {
                _appMenu.DropDownItems.Add(new ToolStripSeparator());
            }
            
            _appMenu.DropDownItems.Add("重新加载应用列表(&R)", null, (s, e) => UpdateAppMenuItems());
        }
        
        private string GetCurrentAppFromConfig()
        {
            try
            {
                // 直接从配置文件读取当前应用，避免缓存问题
                var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(configFile))
                {
                    var json = File.ReadAllText(configFile);
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                    
                    if (jsonDoc.RootElement.TryGetProperty("WebAppSettings", out var webAppSettings) &&
                        webAppSettings.TryGetProperty("CurrentApp", out var currentApp))
                    {
                        return currentApp.GetString() ?? "app1";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取当前应用配置时出错: {ex.Message}");
            }
            
            return "app1";
        }
        
        private void CreateSampleAppDialog()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "创建示例应用";
                dialog.Size = new Size(400, 200);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                
                var lblId = new Label { Text = "应用ID:", Location = new Point(20, 20), Size = new Size(80, 25) };
                var txtId = new TextBox { Location = new Point(100, 20), Size = new Size(250, 25), Text = $"sample_{DateTime.Now:yyyyMMddHHmmss}" };
                
                var lblName = new Label { Text = "应用名称:", Location = new Point(20, 60), Size = new Size(80, 25) };
                var txtName = new TextBox { Location = new Point(100, 60), Size = new Size(250, 25), Text = "示例应用" };
                
                var btnCreate = new Button { Text = "创建", Location = new Point(100, 100), Size = new Size(100, 30) };
                var btnCancel = new Button { Text = "取消", Location = new Point(220, 100), Size = new Size(100, 30) };
                
                btnCreate.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(txtId.Text) && !string.IsNullOrEmpty(txtName.Text))
                    {
                        _appManager.CreateSampleApp(txtId.Text, txtName.Text);
                        dialog.DialogResult = DialogResult.OK;
                        dialog.Close();
                        
                        MessageBox.Show($"示例应用已创建成功！\n\n应用ID: {txtId.Text}\n应用名称: {txtName.Text}\n\n请手动更新appsettings.json配置文件以添加此应用到应用列表。", 
                            "创建成功", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                    }
                };
                
                btnCancel.Click += (s, e) =>
                {
                    dialog.DialogResult = DialogResult.Cancel;
                    dialog.Close();
                };
                
                dialog.Controls.AddRange(new Control[] { lblId, txtId, lblName, txtName, btnCreate, btnCancel });
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // 刷新应用列表
                    UpdateAppMenuItems();
                }
            }
        }
        
        private void CheckAppStatus()
        {
            var apps = _appManager.GetAvailableApps();
            var message = "应用状态检查:\n\n";
            
            foreach (var app in apps)
            {
                var status = app.IsAvailable ? "✅ 可用" : "❌ 不可用";
                var type = app.IsUrl ? "🌐 网址" : "📁 本地文件";
                message += $"{app.Name} ({app.Id}): {status} ({type})\n";
                
                if (app.IsUrl)
                {
                    message += $"  网址: {app.Path}\n";
                }
                else if (!app.IsAvailable)
                {
                    message += $"  路径: {app.FullPath}\n";
                }
            }
            
            MessageBox.Show(message, "应用状态", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void ValidateRunConfig()
        {
            try
            {
                var settings = _configService.GetAppSettings();
                var message = new System.Text.StringBuilder();
                message.AppendLine("Run字段配置验证:");
                message.AppendLine("==================");
                message.AppendLine();
                
                foreach (var app in settings.WebAppSettings.Apps)
                {
                    var validationResult = _appRunner.ValidateRunConfig(app.Key);
                    message.AppendLine(validationResult);
                    message.AppendLine();
                }
                
                MessageBox.Show(message.ToString(), "Run字段配置验证", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证Run字段配置时出错: {ex.Message}", 
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ShowAboutDialog()
        {
            var aboutText = $@"
WebAppLauncher v1.0
.NET 8.0 WinForms Web应用容器

功能特点：
• 支持多个Web应用管理
• 通过appsettings.json配置
• 集成WebView2浏览器
• 禁用右键菜单
• 应用切换功能

项目路径：{Application.StartupPath}
当前时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}

作者微信：runsoft1024
© 2025 WebAppLauncher - 专业的Web应用容器";
            
            MessageBox.Show(aboutText, "关于 WebAppLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void RestoreScrollbarsFunction()
        {
            try
            {
                if (_webView.CoreWebView2 == null)
                {
                    MessageBox.Show("WebView2未初始化", "调试信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 获取当前窗体信息
                var menuHeight = _menuStrip?.Height ?? 0;
                var webViewWidth = _webView.Width;
                var webViewHeight = _webView.Height;
                var availableWidth = webViewWidth;
                var availableHeight = webViewHeight - menuHeight;

                // 获取页面信息
                var pageInfoJs = @"
                    (function() {
                        var body = document.body;
                        var html = document.documentElement;
                        
                        var pageWidth = Math.max(
                            body.scrollWidth,
                            body.offsetWidth,
                            html.clientWidth,
                            html.scrollWidth,
                            html.offsetWidth
                        );
                        
                        var pageHeight = Math.max(
                            body.scrollHeight,
                            body.offsetHeight,
                            html.clientHeight,
                            html.scrollHeight,
                            html.offsetHeight
                        );
                        
                        var viewportWidth = window.innerWidth;
                        var viewportHeight = window.innerHeight;
                        
                        return {
                            pageWidth: pageWidth,
                            pageHeight: pageHeight,
                            viewportWidth: viewportWidth,
                            viewportHeight: viewportHeight,
                            hasZoomContainer: document.getElementById('webapplauncher-zoom-container') !== null
                        };
                    })();
                ";

                var pageInfoResult = await _webView.CoreWebView2.ExecuteScriptAsync(pageInfoJs);
                var pageInfoJson = pageInfoResult.Trim('"').Replace("\\\"", "\"");
                
                dynamic? pageInfo = null;
                try
                {
                    pageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(pageInfoJson);
                }
                catch
                {
                    pageInfo = new { 
                        pageWidth = 0, 
                        pageHeight = 0, 
                        viewportWidth = 0, 
                        viewportHeight = 0,
                        hasZoomContainer = false 
                    };
                }

                double pageWidth = pageInfo?.pageWidth ?? 0;
                double pageHeight = pageInfo?.pageHeight ?? 0;
                double viewportWidth = pageInfo?.viewportWidth ?? 0;
                double viewportHeight = pageInfo?.viewportHeight ?? 0;
                bool hasZoomContainer = pageInfo?.hasZoomContainer ?? false;

                // 计算缩放比例
                double scaleX = availableWidth / (pageWidth > 0 ? pageWidth : 1000);
                double scaleY = availableHeight / (pageHeight > 0 ? pageHeight : 800);
                double scale = Math.Min(scaleX, scaleY);
                scale = Math.Min(Math.Max(scale, 0.1), 2.0);

                // 显示调试信息
                var debugInfo = $@"调试信息 - 缩放功能
================================

窗体信息:
- 窗体宽度: {this.Width}px
- 窗体高度: {this.Height}px
- 菜单栏高度: {menuHeight}px
- WebView宽度: {webViewWidth}px
- WebView高度: {webViewHeight}px
- 可用宽度: {availableWidth}px
- 可用高度: {availableHeight}px

页面信息:
- 页面宽度: {pageWidth}px
- 页面高度: {pageHeight}px
- 视口宽度: {viewportWidth}px
- 视口高度: {viewportHeight}px
- 缩放容器: {(hasZoomContainer ? "已存在" : "不存在")}

缩放计算:
- 宽度比例: {scaleX:F4}
- 高度比例: {scaleY:F4}
- 最终比例: {scale:F4} ({scale * 100:F1}%)

操作选项:
1. 点击'确定'应用缩放
2. 点击'取消'仅查看信息";

                var result = MessageBox.Show(debugInfo, "缩放功能调试", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                
                if (result == DialogResult.OK)
                {
                    // 恢复原始滚动条
                    await RestoreOriginalScrollbars();
                    MessageBox.Show("原始滚动条已恢复，缩放已移除", "操作完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调试缩放功能时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_MouseMove(object? sender, MouseEventArgs e)
        {
            // 不再需要窗体级别的鼠标移动检测（由计时器处理）
        }

        private void ShowMenu()
        {
            if (!_menuVisible)
            {
                _menuVisible = true;
                _menuStrip.Visible = true;
                _menuStrip.BringToFront();
                Console.WriteLine("菜单已显示");
            }
        }

        private void ResetHideTimer()
        {
            // 不再需要重置隐藏计时器
        }

        private void HideMenuTimer_Tick(object? sender, EventArgs e)
        {
            // 不再使用隐藏计时器
        }

        private void HideMenu()
        {
            if (_menuVisible)
            {
                _menuVisible = false;
                _menuStrip.Visible = false;
                Console.WriteLine("菜单已隐藏");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // 清理鼠标检查计时器
            if (_mouseCheckTimer != null)
            {
                _mouseCheckTimer.Stop();
                _mouseCheckTimer.Dispose();
            }
            // 清理WebView2资源
            _webView?.Dispose();
        }
    }
}