/*
 * 名称：web应用容器
 * 功能：用程序打开本地网页，vue页面，网站
 * 作者微信：runsoft1024
 */
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace WebAppLauncher.Services
{
    public class EmbeddedWebServer
    {
        private IHost? _host;
        private readonly string _staticFilesPath;
        private readonly int _port;

        public EmbeddedWebServer(string staticFilesPath, int port = 5000)
        {
            _staticFilesPath = staticFilesPath;
            _port = port;
        }

        public async Task StartAsync()
        {
            var builder = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseKestrel()
                             .UseUrls($"http://localhost:{_port}")
                             .ConfigureServices(services =>
                             {
                                 services.AddDirectoryBrowser();
                             })
                             .Configure(app =>
                             {
                                 // 启用静态文件访问
                                 app.UseStaticFiles(new StaticFileOptions
                                 {
                                     FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(_staticFilesPath),
                                     RequestPath = ""  // 根路径访问
                                 });

                                 // 可选：启用目录浏览
                                 //app.UseDirectoryBrowser(new DirectoryBrowserOptions
                                 //{
                                 //    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(_staticFilesPath),
                                 //    RequestPath = ""
                                 //});
                             });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });

            _host = builder.Build();
            await _host.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
    }
}
