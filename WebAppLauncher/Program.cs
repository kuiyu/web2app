/*
 * 名称：web应用容器
 * 功能：用程序打开本地网页，vue页面，网站
 * 作者微信：runsoft1024
 */
using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using WebAppLauncher.Services;

namespace WebAppLauncher
{
    [SupportedOSPlatform("windows")]
    internal static class Program
    {
        // 单实例互斥体：防止多次启动导致端口冲突
        private const string MutexName = @"Global\WebAppLauncher_SingleInstance_9F3C2A1B";
        private static Mutex? _singleInstanceMutex;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void  Main()
        {
            // 初始化日志（尽早，便于捕获后续错误）
            Logger.Initialize();

            // 检查是否以测试模式运行
            var args = Environment.GetCommandLineArgs();
            bool testMode = args.Contains("--test") || args.Contains("-t");

            if (testMode)
            {
                RunTests();
                return;
            }

            // 单实例检查
            bool createdNew;
            try
            {
                _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);
            }
            catch (Exception ex)
            {
                Logger.Error("创建单实例互斥体失败", ex);
                createdNew = true; // 失败时退化为允许多开，避免无法启动
            }

            if (!createdNew)
            {
                Logger.Warning("已存在另一个实例，本次启动将被阻止。");
                MessageBox.Show("WebAppLauncher 已在运行，请勿重复启动。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _singleInstanceMutex?.Close();
                return;
            }

            // 设置应用程序域异常处理
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 启用Windows Forms视觉样式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                HandleFatalException(ex);
            }
            finally
            {
                // 释放单实例互斥
                try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
                try { _singleInstanceMutex?.Close(); } catch { }

                Logger.Info("应用程序已退出。");
            }
        }
        private static void RunTests()
        {
            Console.WriteLine("WebAppLauncher 测试模式");
            Console.WriteLine("========================");
            Console.WriteLine();
            
            try
            {
                // 首先运行网址支持测试
                Console.WriteLine("运行网址支持测试...");
                var urlTestType = Type.GetType("WebAppLauncher.Tests.TestUrlSupport, WebAppLauncher");
                if (urlTestType != null)
                {
                    var runUrlTestsMethod = urlTestType.GetMethod("RunTests");
                    if (runUrlTestsMethod != null)
                    {
                        runUrlTestsMethod.Invoke(null, null);
                        Console.WriteLine();
                    }
                }
                
                // 然后运行动态加载的测试类
                var testType = Type.GetType("WebAppLauncher.Tests.TestAppSwitching, WebAppLauncher");
                if (testType != null)
                {
                    var runMethod = testType.GetMethod("RunTests");
                    if (runMethod != null)
                    {
                        runMethod.Invoke(null, null);
                    }
                    else
                    {
                        Console.WriteLine("未找到 TestAppSwitching.RunTests 方法");
                    }
                }
                else
                {
                    Console.WriteLine("未找到测试类，直接测试配置服务...");
                    TestConfigurationDirectly();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
        
        private static void TestConfigurationDirectly()
        {
            Console.WriteLine("直接配置测试:");
            Console.WriteLine();
            
            try
            {
                // 测试配置读取
                var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                Console.WriteLine($"配置文件路径: {configFilePath}");
                Console.WriteLine($"文件存在: {File.Exists(configFilePath)}");
                
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    Console.WriteLine($"配置文件内容:");
                    Console.WriteLine(json);
                    Console.WriteLine();
                    
                    // 检查关键配置
                    if (json.Contains("\"CurrentApp\": \"app1\""))
                    {
                        Console.WriteLine("✓ 当前应用配置正确 (app1)");
                    }
                    else
                    {
                        Console.WriteLine("✗ 当前应用配置不正确");
                    }
                    
                    // 检查应用数量
                    int appCount = 0;
                    if (json.Contains("\"app1\"")) appCount++;
                    if (json.Contains("\"app2\"")) appCount++;
                    Console.WriteLine($"✓ 应用数量: {appCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"配置测试失败: {ex.Message}");
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void HandleException(Exception ex)
        {
            Logger.Error("发生未处理异常", ex);
            MessageBox.Show($"应用程序发生错误:\n{ex.Message}", 
                "应用程序错误", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }

        private static void HandleFatalException(Exception ex)
        {
            Logger.Error("发生致命异常，应用程序即将关闭", ex);
            MessageBox.Show($"应用程序发生致命错误:\n{ex.Message}\n\n应用程序将关闭。", 
                "致命错误", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }
}