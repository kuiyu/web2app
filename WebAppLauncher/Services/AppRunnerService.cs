using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebAppLauncher.Models;

namespace WebAppLauncher.Services
{
    public class AppRunnerService
    {
        private readonly ConfigurationService _configService;
        private readonly string _appBasePath;

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
                    if (await RunProgramAsync(programPath))
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
            if (settings.WebAppSettings.Apps.TryGetValue(appId, out var appConfig))
            {
                return appConfig;
            }
            return null;
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
        private async Task<bool> RunProgramAsync(string programPath)
        {
            try
            {
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

            return results.ToString();
        }
    }
}