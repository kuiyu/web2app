/*
 * 名称：web应用容器 - 嵌入式Web服务器
 * 功能：基于 Kestrel (Microsoft.AspNetCore.Hosting) 的轻量级静态文件服务器，
 *       用于托管本地Web应用。
 *
 * 安全说明（生产级）：
 * 1. 静态文件根目录通过 PhysicalFileProvider 严格限定在 webRoot 内，
 *    PhysicalFileProvider 会自动拒绝访问根目录之外的路径（如 "../" 越界请求
 *    返回 404），天然防止路径穿越。
 * 2. 未知 MIME 类型默认不提供服务（ServeUnknownFileTypes=false），
 *    避免源码/配置文件等非预期文件被下载。
 * 3. 启动时会自动在端口范围内重试，避免端口被占用导致启动失败。
 * 4. 本服务当前由 file:// 本地文件路由方案取代而未启用；
 *    保留供未来 Node.js 后端方案（需提供同源静态资源）时使用。
 */
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace WebAppLauncher.Services
{
    public class EmbeddedWebServer : IDisposable
    {
        private readonly string _webRoot;
        private IWebHost? _host;
        private bool _disposed;

        public EmbeddedWebServer(string webRoot)
        {
            _webRoot = webRoot ?? throw new ArgumentNullException(nameof(webRoot));
        }

        public int Port { get; private set; }

        /// <summary>
        /// 尝试在 [startPort, startPort+range) 范围内找到可用端口并启动服务器，
        /// 避免端口被占用导致启动失败（端口冲突自动重试）。
        /// </summary>
        public void Start(int startPort = 5000, int range = 20)
        {
            for (int p = startPort; p < startPort + range; p++)
            {
                try
                {
                    var builder = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls($"http://localhost:{p}")
                        .UseStartup(c => new Startup(_webRoot))
                        .ConfigureLogging(logging => logging.ClearProviders()); // 统一走我们的 Logger

                    _host = builder.Build();
                    _host.Start();
                    Port = p;
                    Logger.Info($"嵌入式Web服务器已启动: http://localhost:{p}/ 根目录: {_webRoot}");
                    return;
                }
                catch (Exception ex) when (IsPortInUse(ex))
                {
                    Logger.Warning($"端口 {p} 被占用，尝试下一个端口。");
                    try { _host?.Dispose(); } catch { }
                    _host = null;
                    continue;
                }
            }

            throw new InvalidOperationException(
                $"在端口范围 {startPort}-{startPort + range - 1} 内均未找到可用端口，无法启动嵌入式Web服务器。");
        }

        private static bool IsPortInUse(Exception ex)
        {
            // Kestrel 端口被占用通常抛 IOException / SocketException
            return ex is IOException
                || ex is System.Net.Sockets.SocketException
                || ex.InnerException is IOException
                || ex.InnerException is System.Net.Sockets.SocketException;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try { _host?.StopAsync().Wait(TimeSpan.FromSeconds(5)); } catch { }
            try { _host?.Dispose(); } catch { }
        }

        public class Startup
        {
            private readonly string _root;

            public Startup(string webRoot) => _root = webRoot;

            // 注意：静态文件根目录由 EmbeddedWebServer 构造时传入；
            // ASP.NET Core 的 PhysicalFileProvider 已对根目录之外的请求做边界限制。
            public void Configure(IApplicationBuilder app)
            {
                var fileProvider = new PhysicalFileProvider(_root);
                var contentTypeProvider = new FileExtensionContentTypeProvider();

                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = fileProvider,
                    DefaultFileNames = new[] { "index.html" }
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = fileProvider,
                    ContentTypeProvider = contentTypeProvider,
                    // 不服务未知类型，防止源码/配置等非预期文件被下载
                    ServeUnknownFileTypes = false,
                    DefaultContentType = "application/octet-stream"
                });
            }
        }
    }
}
