/*
 * 名称：web应用容器 - 外部程序/后端启动器
 * 功能：根据配置中的 Run 字段启动外部可执行程序，并等待其对外暴露 HTTP 端口（健康检查）。
 *       所有启动的进程会被登记，便于退出时统一清理，避免孤儿进程。
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WebAppLauncher.Services
{
    public class AppRunnerService
    {
        private readonly string _appBasePath;
        private readonly List<Process> _runningProcesses = new List<Process>();
        private readonly HashSet<int> _usedPorts = new HashSet<int>();
        private readonly object _procLock = new object();

        // 进程级 Job Object：主进程退出（含崩溃）时，Job 内的所有子进程
        // （包括脱离父进程树、自行 fork 的后代）都会被操作系统自动终止，
        // 彻底避免孤儿进程。
        private static readonly Lazy<JobObject?> _job = new Lazy<JobObject?>(() => JobObject.Create());

        public AppRunnerService(string appBasePath)
        {
            _appBasePath = appBasePath;
        }

        /// <summary>
        /// 启动 Run 配置中的可执行程序，并返回它监听的 URL（用于 WebView 跳转）。
        /// 若 waitForPort 为 true，会等待本地端口可连通或超时后返回。
        /// </summary>
        public async Task<string> RunProgramAsync(string runCmd, string url, bool waitForPort = true, int timeoutMs = 15000)
        {
            // 若 Run 整体是一个 http/https URL，说明后端由外部服务托管（如独立的
            // ASP.NET Core 应用），容器不负责启动进程，仅由 MainForm 将 WebView
            // 导航到该地址。直接返回，避免把它误当作本地程序去解析而失败。
            if (Uri.TryCreate(runCmd, UriKind.Absolute, out var runUri)
                && (runUri.Scheme == Uri.UriSchemeHttp || runUri.Scheme == Uri.UriSchemeHttps))
            {
                Logger.Info($"Run 为外部 URL，跳过程序启动（由外部服务托管）: {runCmd}");
                return url;
            }

            string program = runCmd;
            string args = string.Empty;

            var spaceIdx = runCmd.IndexOf(' ');
            if (spaceIdx >= 0)
            {
                program = runCmd.Substring(0, spaceIdx);
                args = runCmd.Substring(spaceIdx + 1).Trim();
            }

            var resolved = ResolveProgramPath(program);
            if (resolved == null)
            {
                var msg = $"无法找到可执行程序: {program}（已搜索 apps 目录与系统 PATH）。Run 配置将不会生效。";
                Logger.Warning(msg);
                throw new FileNotFoundException(msg, program);
            }

            // 端口冲突检测：同一实例内多个后端应用若使用相同端口会互相干扰
            var ep = TryParseLocalhostUrl(url);
            if (ep != null)
            {
                lock (_procLock)
                {
                    if (_usedPorts.Contains(ep.Value.port))
                    {
                        var msg = $"端口冲突：{ep.Value.port} 已被本容器中另一个应用占用，无法启动 {program}。请为不同应用配置不同端口。";
                        Logger.Error(msg);
                        throw new InvalidOperationException(msg);
                    }
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = resolved,
                Arguments = args,
                WorkingDirectory = _appBasePath,
                UseShellExecute = false,        // 必须关闭，才能捕获输出与退出
                CreateNoWindow = true,         // 隐藏后端/子进程的控制台窗口
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process;
            try
            {
                process = Process.Start(startInfo) ?? throw new InvalidOperationException("进程创建失败（返回 null）。");
            }
            catch (Exception ex)
            {
                Logger.Error($"启动程序失败: {resolved} {args}", ex);
                throw;
            }

            lock (_procLock)
            {
                _runningProcesses.Add(process);
                if (ep != null)
                    _usedPorts.Add(ep.Value.port);
            }

            // 将进程加入 Job Object，确保主进程退出时其（含脱离树的子进程）被自动清理
            try
            {
                _job.Value?.AddProcess(process);
            }
            catch (Exception ex)
            {
                // Job 绑定失败不应阻止启动，仅降级为 Kill(true) 清理
                Logger.Warning($"将进程加入 Job Object 失败（已降级为普通清理）: {ex.Message}");
            }

            // 异步捕获输出，避免子进程因输出缓冲写满而阻塞
            var procId = process.Id;
            _ = Task.Run(() =>
            {
                try
                {
                    while (!process.StandardOutput.EndOfStream)
                        Logger.Info($"[{Path.GetFileName(resolved)}:{procId}] {process.StandardOutput.ReadLine()}");
                }
                catch { }
            });
            _ = Task.Run(() =>
            {
                try
                {
                    while (!process.StandardError.EndOfStream)
                        Logger.Warning($"[{Path.GetFileName(resolved)}:{procId}] {process.StandardError.ReadLine()}");
                }
                catch { }
            });

            // 是否需要等待该进程对外暴露端口（仅当配置了本地后端 url 时）
            var expectsPort = waitForPort && TryParseLocalhostUrl(url) != null;

            // 仅当“期望进程常驻并提供端口”时，进程立即退出才视为启动失败。
            // 否则（如 explorer.exe、cmd /k 等一次性/复用实例的程序）启动即视为成功。
            if (expectsPort && process.HasExited)
            {
                var code = process.ExitCode;
                Logger.Error($"程序 {resolved} 启动后立即退出，退出码: {code}（该应用期望它常驻并提供端口）");
                RemoveProcess(process);
                throw new InvalidOperationException($"程序 {resolved} 启动后立即退出，退出码: {code}");
            }

            Logger.Info($"已启动程序: {resolved} (PID={procId})" + (expectsPort ? $"，等待端口就绪: {url}" : ""));

            if (expectsPort)
            {
                var ok = await WaitForPortAsync(url, timeoutMs);
                if (!ok)
                {
                    Logger.Warning($"端口未在 {timeoutMs}ms 内就绪: {url}。将继续尝试加载（可能页面稍后可用）。");
                }
                else
                {
                    Logger.Info($"端口已就绪: {url}");
                }
            }

            return url;
        }

        /// <summary>
        /// 通过 TCP 连接重试判断本地端口是否可连通（替代不可靠的 TcpListener 探测）。
        /// </summary>
        public static async Task<bool> WaitForPortAsync(string url, int timeoutMs = 15000, int intervalMs = 250)
        {
            var ep = TryParseLocalhostUrl(url);
            if (ep == null)
                return false;

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(ep.Value.host, ep.Value.port);
                    if (await Task.WhenAny(connectTask, Task.Delay(intervalMs)) == connectTask && client.Connected)
                    {
                        return true;
                    }
                }
                catch
                {
                    // 连接失败，继续重试
                }

                // 子进程退出则不再等待
                if (ep.Value.host == "127.0.0.1" || ep.Value.host == "localhost")
                {
                    // 无法在此关联具体进程，仅按超时返回
                }

                await Task.Delay(intervalMs);
            }
            return false;
        }

        private static (string host, int port)? TryParseLocalhostUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var host = uri.Host;
                if (host != "localhost" && host != "127.0.0.1" && host != "[::1]")
                    return null;
                return (host == "[::1]" ? "127.0.0.1" : host, uri.Port);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从 launch 命令中解析 --port 参数（如 "AspNetCoreServer.exe --port 63000 ..."），
        /// 用于当 url 未配置时，自动推导后端地址 http://localhost:{port}/index.html。
        /// </summary>
        public static int? TryParsePortFromLaunch(string launch)
        {
            if (string.IsNullOrWhiteSpace(launch))
                return null;
            var args = launch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--port" && int.TryParse(args[i + 1], out var port))
                    return port;
            }
            return null;
        }

        /// <summary>
        /// 推导后端地址：优先用显式 url；否则若 launch 含 --port，自动生成
        /// http://localhost:{port}/index.html（避免端口在 url 与 launch 中重复书写）。
        /// </summary>
        public static string? ResolveBackendUrl(string? url, string? launch)
        {
            if (!string.IsNullOrWhiteSpace(url))
                return url;
            var port = TryParsePortFromLaunch(launch ?? string.Empty);
            return port.HasValue ? $"http://localhost:{port}/index.html" : null;
        }

        /// <summary>
        /// 补全 --staticDir：当 launch 未显式写 --staticDir 时，用 source 的值作为静态目录。
        /// source 若带文件名（如 "apps/ai/index.html"）会自动截取目录部分（"apps/ai"）。
        /// </summary>
        public static string ApplyStaticDirDefault(string launch, string? source)
        {
            if (string.IsNullOrWhiteSpace(launch))
                return launch;
            // 已显式指定 --staticDir，则不再补全
            if (launch.Contains("--staticDir", StringComparison.OrdinalIgnoreCase))
                return launch;
            if (string.IsNullOrWhiteSpace(source))
                return launch;

            // 取 source 的目录部分（去掉末尾的文件名），得到静态根目录
            var dir = source.Trim().Trim('"');
            if (dir.IndexOf('.') > 0 && !dir.EndsWith("/") && !dir.EndsWith("\\"))
            {
                // 形如 apps/ai/index.html → 取目录
                try
                {
                    var fullOrRelative = Path.IsPathRooted(dir)
                        ? dir
                        : Path.Combine(Directory.GetCurrentDirectory(), dir);
                    var dirPart = Path.GetDirectoryName(fullOrRelative);
                    if (!string.IsNullOrEmpty(dirPart))
                    {
                        // 还原成相对当前目录的写法
                        dir = dirPart.StartsWith(Directory.GetCurrentDirectory(), StringComparison.OrdinalIgnoreCase)
                            ? dirPart.Substring(Directory.GetCurrentDirectory().Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            : dirPart;
                    }
                }
                catch { }
            }
            dir = dir.TrimEnd('/', '\\');

            return $"{launch.TrimEnd()} --staticDir {dir}";
        }

        /// <summary>
        /// 尝试解析 Run 配置中的程序路径，用于配置验证（不会真正启动进程）。
        /// </summary>
        public string? TryResolveProgram(string program)
        {
            if (string.IsNullOrWhiteSpace(program))
                return null;
            return ResolveProgramPath(program.Trim());
        }

        private string? ResolveProgramPath(string program)
        {
            if (Path.IsPathRooted(program) && File.Exists(program))
                return program;

            // 相对 apps 目录解析
            var candidate = Path.Combine(_appBasePath, program);
            if (File.Exists(candidate))
                return candidate;

            // 在 apps 子目录中递归查找同名可执行文件
            try
            {
                var matches = Directory.GetFiles(_appBasePath, program, SearchOption.AllDirectories);
                if (matches.Length > 0)
                    return matches[0];
            }
            catch { }

            // 回退到系统 PATH（含常见 .exe/.bat/.cmd）
            if (File.Exists(program))
                return program;

            foreach (var ext in new[] { "", ".exe", ".bat", ".cmd" })
            {
                var p = program + ext;
                var fromPath = GetFromPath(p);
                if (fromPath != null)
                    return fromPath;
            }

            return null;
        }

        private static string? GetFromPath(string fileName)
        {
            var values = Environment.GetEnvironmentVariable("PATH");
            if (values == null)
                return null;
            foreach (var dir in values.Split(Path.PathSeparator))
            {
                try
                {
                    var full = Path.Combine(dir, fileName);
                    if (File.Exists(full))
                        return full;
                }
                catch { }
            }
            return null;
        }

        private void RemoveProcess(Process process)
        {
            lock (_procLock)
            {
                _runningProcesses.Remove(process);
            }
            try { process.Dispose(); } catch { }
        }

        /// <summary>
        /// 终止并清理所有由本服务启动的子进程，避免退出时留下孤儿进程。
        /// </summary>
        public void KillAll()
        {
            List<Process> snapshot;
            lock (_procLock)
            {
                snapshot = new List<Process>(_runningProcesses);
                _runningProcesses.Clear();
                _usedPorts.Clear();
            }

            foreach (var p in snapshot)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        Logger.Info($"正在终止子进程 PID={p.Id} ({p.ProcessName})");
                        p.Kill(true); // 含子进程树
                        if (!p.WaitForExit(5000))
                            Logger.Warning($"子进程 PID={p.Id} 未能在 5s 内退出。");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"终止子进程 PID={p.Id} 失败: {ex.Message}");
                }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }

            // 释放 Job Object（会触发其中所有残留进程被终止）
            try
            {
                if (_job.IsValueCreated)
                    _job.Value?.Dispose();
            }
            catch { }
        }

        /// <summary>
        /// Windows Job Object 封装：将子进程加入 Job，主进程退出时由操作系统
        /// 自动终止 Job 内的全部进程（含脱离父进程树的后代）。
        /// </summary>
        private sealed class JobObject : IDisposable
        {
            private readonly IntPtr _handle;
            private bool _disposed;

            private JobObject(IntPtr handle) => _handle = handle;

            public static JobObject? Create()
            {
                try
                {
                    var hJob = CreateJobObject(IntPtr.Zero, null);
                    if (hJob == IntPtr.Zero)
                    {
                        Logger.Warning($"创建 Job Object 失败 (LastError={Marshal.GetLastWin32Error()})，将降级为 Kill(true) 清理。");
                        return null;
                    }

                    var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
                    {
                        BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                        {
                            LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                        }
                    };

                    var length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
                    if (!SetInformationJobObject(hJob, JobObjectInfoClass.ExtendedLimitInformation,
                            ref info, length))
                    {
                        Logger.Warning($"设置 Job Object 限制失败 (LastError={Marshal.GetLastWin32Error()})。");
                        CloseHandle(hJob);
                        return null;
                    }

                    return new JobObject(hJob);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"创建 Job Object 异常: {ex.Message}");
                    return null;
                }
            }

            public void AddProcess(Process process)
            {
                if (_disposed)
                    return;
                // 必须使用进程句柄，而非 PID（PID 可能复用）
                if (!AssignProcessToJobObject(_handle, process.Handle))
                {
                    throw new InvalidOperationException(
                        $"AssignProcessToJobObject 失败 (LastError={Marshal.GetLastWin32Error()})");
                }
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                try { if (_handle != IntPtr.Zero) CloseHandle(_handle); } catch { }
            }

            // ---- P/Invoke ----
            private const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetInformationJobObject(IntPtr hJob,
                JobObjectInfoClass jobObjectInfoClass,
                ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
                int cbJobObjectInfoLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool CloseHandle(IntPtr hObject);

            private enum JobObjectInfoClass
            {
                ExtendedLimitInformation = 9
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                public long PerProcessUserTimeLimit;
                public long PerJobUserTimeLimit;
                public int LimitFlags;
                public UIntPtr MinimumWorkingSetSize;
                public UIntPtr MaximumWorkingSetSize;
                public int ActiveProcessLimit;
                public IntPtr Affinity;
                public int PriorityClass;
                public int SchedulingClass;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct IO_COUNTERS
            {
                public ulong ReadOperationCount;
                public ulong WriteOperationCount;
                public ulong OtherOperationCount;
                public ulong ReadTransferCount;
                public ulong WriteTransferCount;
                public ulong OtherTransferCount;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
                public IO_COUNTERS IoInfo;
                public UIntPtr ProcessMemoryLimit;
                public UIntPtr JobMemoryLimit;
                public UIntPtr PeakProcessMemoryUsed;
                public UIntPtr PeakJobMemoryUsed;
            }
        }
    }
}
