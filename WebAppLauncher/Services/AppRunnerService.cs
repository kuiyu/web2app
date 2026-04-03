/*
 * 名称：web应用容器
 * 功能：用程序打开本地网页，vue页面，网站
 * 作者微信：runsoft1024
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using WebAppLauncher.Models;

namespace WebAppLauncher.Services
{
    public class AppRunnerService
    {
        private readonly ConfigurationService _configService;
        private readonly string _appBasePath;
        private readonly Dictionary<string, Process> _runningAspNetCoreProcesses = new Dictionary<string, Process>();

        public AppRunnerService()
        {
            _configService = new ConfigurationService();
            _appBasePath = AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// 运行指定应用的Run字段中的程序
        /// </summary>
        /// <param name="appId">应用ID</param>
        /// <returns>成功运行的程序数量</returns>
        public async Task<int> RunAppsForCurrentApp(string appId)
        {
            try
            {
                var appConfig = GetAppConfig(appId);
                if (appConfig == null || string.IsNullOrWhiteSpace(appConfig.Run))
                {
                    Console.WriteLine($"应用 '{appId}' 没有配置Run字段或Run字段为空");
                    return 0;
                }

                // 解析逗号分隔的程序路径
                var programPaths = ParseProgramPaths(appConfig.Run);
                if (programPaths.Length == 0)
                {
                    Console.WriteLine($"应用 '{appId}' 的Run字段没有有效的程序路径");
                    return 0;
                }

                Console.WriteLine($"为应用 '{appId}' 启动 {programPaths.Length} 个程序...");

                int successCount = 0;
                foreach (var programPath in programPaths)
                {
                    if (await RunProgramAsync(programPath, appId, appConfig.Path))
                    {
                        successCount++;
                    }
                }

                Console.WriteLine($"成功启动 {successCount}/{programPaths.Length} 个程序");
                return successCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"运行应用 '{appId}' 的Run程序时出错: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 运行当前选中应用的Run字段中的程序
        /// </summary>
        public async Task<int> RunAppsForCurrentApp()
        {
            var currentAppId = _configService.GetAppSettings().WebAppSettings.CurrentApp;
            return await RunAppsForCurrentApp(currentAppId);
        }

        /// <summary>
        /// 获取应用配置
        /// </summary>
        private WebAppConfig? GetAppConfig(string appId)
        {
            var settings = _configService.GetAppSettings();
            var appConfig = settings.WebAppSettings.Apps.FirstOrDefault(x => x.AppId == appId);
            return appConfig;
        }

        /// <summary>
        /// 解析逗号分隔的程序路径
        /// </summary>
        private string[] ParseProgramPaths(string runConfig)
        {
            if (string.IsNullOrWhiteSpace(runConfig))
                return Array.Empty<string>();

            // 分割逗号，去除空白字符，过滤空项
            return runConfig.Split(',')
                .Select(path => path.Trim())
                .Where(path => !string.IsNullOrEmpty(path))
                .ToArray();
        }

        /// <summary>
        /// 运行单个程序
        /// </summary>
        private async Task<bool> RunProgramAsync(string programPath, string appId, string appPath)
        {
            try
            {
                // 检查是否是ASP.NET Core程序（http://localhost开头）
                if (IsAspNetCoreLocalhostUrl(programPath))
                {
                     await RunAspNetCoreAppAsync(programPath, appId, appPath);

                    return true;
                }

                // 处理路径
                string fullPath = ResolveProgramPath(programPath);
                
                if (string.IsNullOrEmpty(fullPath))
                {
                    Console.WriteLine($"程序路径无效或文件不存在: {programPath}");
                    return false;
                }

                Console.WriteLine($"启动程序: {fullPath}");

                // 创建进程启动信息
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true, // 使用Shell执行，可以打开各种文件类型
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                // 启动进程
                var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    Console.WriteLine($"无法启动程序: {fullPath}");
                    return false;
                }

                // 等待一小段时间确保进程启动
                await Task.Delay(100);
                
                // 检查进程是否仍在运行
                if (process.HasExited)
                {
                    Console.WriteLine($"程序启动后立即退出: {fullPath}, 退出代码: {process.ExitCode}");
                    return process.ExitCode == 0;
                }

                Console.WriteLine($"程序启动成功: {fullPath}, 进程ID: {process.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动程序 '{programPath}' 时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解析程序路径
        /// </summary>
        private string ResolveProgramPath(string programPath)
        {
            if (string.IsNullOrWhiteSpace(programPath))
                return string.Empty;

            programPath = programPath.Trim();

            // 检查是否是绝对路径
            if (Path.IsPathRooted(programPath))
            {
                // 绝对路径，直接检查文件是否存在
                if (File.Exists(programPath) || Directory.Exists(programPath))
                    return programPath;
                    
                // 如果文件不存在，尝试添加.exe扩展名
                if (!programPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var exePath = programPath + ".exe";
                    if (File.Exists(exePath))
                        return exePath;
                }
            }
            else
            {
                // 相对路径，尝试在以下位置查找：
                // 1. 相对于应用程序基础目录
                // 2. 在系统PATH中查找
                // 3. 常见程序目录

                // 1. 相对于应用程序基础目录
                var fullPath = Path.Combine(_appBasePath, programPath);
                if (File.Exists(fullPath) || Directory.Exists(fullPath))
                    return fullPath;

                // 2. 如果相对路径不存在，尝试添加.exe扩展名
                if (!programPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var exePath = Path.Combine(_appBasePath, programPath + ".exe");
                    if (File.Exists(exePath))
                        return exePath;
                }

                // 3. 在系统PATH中查找
                try
                {
                    var pathEnv = Environment.GetEnvironmentVariable("PATH");
                    if (!string.IsNullOrEmpty(pathEnv))
                    {
                        var paths = pathEnv.Split(Path.PathSeparator);
                        foreach (var path in paths)
                        {
                            if (string.IsNullOrEmpty(path))
                                continue;

                            var testPath = Path.Combine(path, programPath);
                            if (File.Exists(testPath))
                                return testPath;

                            // 尝试添加.exe扩展名
                            if (!programPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                testPath = Path.Combine(path, programPath + ".exe");
                                if (File.Exists(testPath))
                                    return testPath;
                            }
                        }
                    }
                }
                catch
                {
                    // 忽略PATH查找错误
                }
            }

            // 如果以上方法都找不到，返回原始路径（让Process.Start处理）
            return programPath;
        }

        /// <summary>
        /// 检查是否是ASP.NET Core本地主机URL
        /// </summary>
        private bool IsAspNetCoreLocalhostUrl(string programPath)
        {
            return !string.IsNullOrWhiteSpace(programPath) && 
                   (programPath.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) || 
                    programPath.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 运行ASP.NET Core应用
        /// </summary>
        private async Task RunAspNetCoreAppAsync(string localhostUrl, string appId, string appPath)
        {
            try
            {
                Console.WriteLine($"启动ASP.NET Core应用: {localhostUrl}, 应用ID: {appId}, 静态文件路径: {appPath}");

                // 检查端口是否已经在使用
                if (IsPortInUse(localhostUrl))
                {
                    Console.WriteLine($"端口已在使用: {localhostUrl}");
                    return ; // 端口已在使用，认为启动成功
                }

                // 解析端口号
                var uri = new Uri(localhostUrl);
                var port = uri.Port;

                // 确定静态文件目录
                var wwwRootPath = ResolveStaticFileDirectory(appPath);
                if (string.IsNullOrEmpty(wwwRootPath) || !Directory.Exists(wwwRootPath))
                {
                    Console.WriteLine($"静态文件目录不存在: {wwwRootPath}");
                    return ;
                }

                Console.WriteLine($"静态文件目录: {wwwRootPath}");

                //启动web
               Program.WebServer =new EmbeddedWebServer(wwwRootPath, port);
                await Program.WebServer.StartAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动ASP.NET Core应用时出错: {ex.Message}");
                return ;
            }
        }

        /// <summary>
        /// 解析静态文件目录
        /// </summary>
        private string ResolveStaticFileDirectory(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath))
                return string.Empty;

            // 如果是URL，返回空
            if (PathHelper.IsUrl(appPath))
                return string.Empty;

            // 如果是相对路径，转换为绝对路径
            if (!Path.IsPathRooted(appPath))
            {
                string fullPath;
                
                // 检查appPath是否已经以"apps/"开头
                if (appPath.StartsWith("apps/", StringComparison.OrdinalIgnoreCase) || 
                    appPath.StartsWith("apps\\", StringComparison.OrdinalIgnoreCase))
                {
                    // 如果已经以"apps/"开头，直接使用_appBasePath作为基础路径
                    fullPath = Path.Combine(_appBasePath, appPath.Replace('/', '\\'));
                }
                else
                {
                    // 否则，添加"apps"目录
                    var appsBasePath = Path.Combine(_appBasePath, "apps");
                    fullPath = Path.Combine(appsBasePath, appPath.Replace('/', '\\'));
                }
                
                Console.WriteLine($"解析静态文件路径: {appPath} -> {fullPath}");
                
                // 如果路径是文件，返回文件所在目录
                if (File.Exists(fullPath))
                {
                    var dirPath = Path.GetDirectoryName(fullPath) ?? string.Empty;
                    Console.WriteLine($"路径是文件，返回目录: {dirPath}");
                    return dirPath;
                }
                
                // 如果路径是目录，直接返回
                if (Directory.Exists(fullPath))
                {
                    Console.WriteLine($"路径是目录，直接返回: {fullPath}");
                    return fullPath;
                }
                
                // 如果文件和目录都不存在，尝试查找最近的存在的目录
                var currentPath = fullPath;
                while (!Directory.Exists(currentPath) && !string.IsNullOrEmpty(currentPath))
                {
                    currentPath = Path.GetDirectoryName(currentPath);
                }
                
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    Console.WriteLine($"找到最近的存在目录: {currentPath}");
                    return currentPath;
                }
            }
            else
            {
                // 绝对路径
                if (File.Exists(appPath))
                {
                    return Path.GetDirectoryName(appPath) ?? string.Empty;
                }
                
                if (Directory.Exists(appPath))
                {
                    return appPath;
                }
            }

            Console.WriteLine($"无法解析静态文件目录: {appPath}");
            return string.Empty;
        }

        /// <summary>
        /// 检查端口是否在使用
        /// </summary>
        private bool IsPortInUse(string url)
        {
            try
            {
                var uri = new Uri(url);
                var port = uri.Port;

                // 尝试绑定端口，如果成功则端口未被使用，否则端口已被使用
                using (var listener = new TcpListener(IPAddress.Loopback, port))
                {
                    try
                    {
                        listener.Start();
                        // 端口未被使用
                        return false;
                    }
                    catch (SocketException)
                    {
                        // 端口已被使用
                        return true;
                    }
                    finally
                    {
                        try
                        {
                            listener.Stop();
                        }
                        catch
                        {
                            // 忽略关闭时的异常
                        }
                    }
                }
            }
            catch
            {
                // 发生异常，认为端口已被使用
                return true;
            }
        }
        private bool StartWeb(string url)
        {
            try
            {
                var uri = new Uri(url);
                var port = uri.Port;

                // 尝试绑定端口，如果成功则端口未被使用，否则端口已被使用
                using (var listener = new TcpListener(IPAddress.Loopback, port))
                {
                    try
                    {
                        listener.Start();
                        // 端口未被使用，说明ASP.NET Core应用没有成功启动
                        return false;
                    }
                    catch (SocketException)
                    {
                        // 端口已被使用，说明ASP.NET Core应用成功启动
                        return true;
                    }
                    finally
                    {
                        try
                        {
                            listener.Stop();
                        }
                        catch
                        {
                            // 忽略关闭时的异常
                        }
                    }
                }
            }
            catch
            {
                // 发生异常，认为ASP.NET Core应用成功启动
                return true;
            }
        }

        /// <summary>
        /// 读取进程输出
        /// </summary>
        private async Task ReadProcessOutput(Process process, string url)
        {
            try
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine($"[ASP.NET Core] {line}");
                    }
                }

                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine($"[ASP.NET Core Error] {line}");
                    }
                }

                // 进程退出
                process.WaitForExit();
                Console.WriteLine($"ASP.NET Core应用退出: {url}, 退出代码: {process.ExitCode}");
                _runningAspNetCoreProcesses.Remove(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取ASP.NET Core进程输出时出错: {ex.Message}");
                _runningAspNetCoreProcesses.Remove(url);
            }
        }

        /// <summary>
        /// 关闭所有ASP.NET Core进程
        /// </summary>
        public void StopAllAspNetCoreProcesses()
        {
            var processesToStop = _runningAspNetCoreProcesses.ToList();
            foreach (var (url, process) in processesToStop)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        Console.WriteLine($"关闭ASP.NET Core应用: {url}, 进程ID: {process.Id}");
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭ASP.NET Core应用时出错: {ex.Message}");
                }
                finally
                {
                    _runningAspNetCoreProcesses.Remove(url);
                }
            }
        }

        /// <summary>
        /// 验证Run字段配置
        /// </summary>
        public string ValidateRunConfig(string appId)
        {
            var appConfig = GetAppConfig(appId);
            if (appConfig == null)
                return $"应用 '{appId}' 不存在";

            if (string.IsNullOrWhiteSpace(appConfig.Run))
                return $"应用 '{appId}' 的Run字段为空";

            var programPaths = ParseProgramPaths(appConfig.Run);
            if (programPaths.Length == 0)
                return $"应用 '{appId}' 的Run字段没有有效的程序路径";

            var results = new System.Text.StringBuilder();
            results.AppendLine($"应用 '{appId}' 的Run字段验证结果:");
            results.AppendLine($"  共 {programPaths.Length} 个程序路径");

            for (int i = 0; i < programPaths.Length; i++)
            {
                var path = programPaths[i];
                
                // 检查是否是ASP.NET Core本地主机URL
                if (IsAspNetCoreLocalhostUrl(path))
                {
                    results.AppendLine($"  [{i + 1}] ✅ {path} (ASP.NET Core应用)");
                }
                else
                {
                    var resolvedPath = ResolveProgramPath(path);
                    
                    if (string.IsNullOrEmpty(resolvedPath) || 
                        (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath)))
                    {
                        results.AppendLine($"  [{i + 1}] ❌ {path} (未找到)");
                    }
                    else
                    {
                        var exists = File.Exists(resolvedPath) ? "文件" : "目录";
                        results.AppendLine($"  [{i + 1}] ✅ {path} -> {resolvedPath} ({exists})");
                    }
                }
            }

            return results.ToString();
        }
    }
}