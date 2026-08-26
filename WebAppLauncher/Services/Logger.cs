/*
 * 名称：web应用容器 - 日志服务
 * 功能：将日志同时输出到控制台和本地文件（logs/webapplauncher-yyyy-MM-dd.log）
 *       桌面程序默认无控制台，落盘日志便于排查问题。
 */
using System;
using System.IO;
using System.Text;

namespace WebAppLauncher.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string? _logDir;
        private static string? _logFile;
        private static bool _initialized;

        public static void Initialize(string? baseDirectory = null)
        {
            try
            {
                var baseDir = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
                _logDir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(_logDir);

                var fileName = $"webapplauncher-{DateTime.Now:yyyy-MM-dd}.log";
                _logFile = Path.Combine(_logDir, fileName);
                _initialized = true;
            }
            catch
            {
                // 初始化失败不应影响主程序
                _initialized = false;
            }
        }

        public static void Log(LogLevel level, string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            // 控制台（测试模式或调试时仍有用）
            try { Console.WriteLine(line); } catch { }

            if (!_initialized || string.IsNullOrEmpty(_logFile))
                return;

            try
            {
                lock (_lock)
                {
                    // 跨天自动切换日志文件
                    var expected = Path.Combine(_logDir!, $"webapplauncher-{DateTime.Now:yyyy-MM-dd}.log");
                    if (!string.Equals(expected, _logFile, StringComparison.OrdinalIgnoreCase))
                        _logFile = expected;

                    File.AppendAllText(_logFile, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // 日志写入失败绝不应影响业务
            }
        }

        public static void Debug(string message) => Log(LogLevel.Debug, message);
        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Error(string message) => Log(LogLevel.Error, message);
        public static void Error(string message, Exception ex) =>
            Log(LogLevel.Error, $"{message}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
    }
}
