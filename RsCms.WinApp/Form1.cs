using CefSharp;
using CefSharp.WinForms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RsCms.WinApp
{
	public partial class Form1 : Form
	{
		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		const int SW_HIDE = 0;

		public Form1()
		{
			InitializeComponent();
		}

		Process process; ChromiumWebBrowser browser;AppConfig config;
		private void RunApp()
		{
			if(!string.IsNullOrWhiteSpace(config.App))
			{
				process = new Process();
				process.StartInfo.FileName = System.IO.Path.Combine(AppContext.BaseDirectory, config.App);
				process.StartInfo.CreateNoWindow = config.NoWindow;
				process.StartInfo.UseShellExecute = false;
				process.Start();
			}
			
			// 如果上面的设置没有成功隐藏窗口，可以尝试下面的方法
			//var handle = process.MainWindowHandle;
			//if (handle!= IntPtr.Zero)
			//{
			//    ShowWindow(handle, SW_HIDE);
			//}
		}

		void CloseRsCmsWebapi()
		{
			process?.Close();
		}

		void InitCef()
		{
			var cefSettings = new CefSettings();
			cefSettings.LogSeverity = LogSeverity.Verbose;
			CefSharp.Cef.Initialize(cefSettings);
			browser = new ChromiumWebBrowser(config.SiteUrl);
			browser.MenuHandler = new CustomContextMenuHandler();
			this.Controls.Add(browser);
			browser.Dock = DockStyle.Fill;
		}

		AppConfig GetAppConfig()
		{
			var appConfig = new AppConfig();
			var jsonFile = Path.Combine(AppContext.BaseDirectory, "app.json");
			var json = File.ReadAllText(jsonFile);
			return JsonSerializer.Deserialize<AppConfig>(json);
		}
		private void Form1_Load(object sender, EventArgs e)
		{
			config = GetAppConfig();
			this.Text=config.WinName;

			if(string.IsNullOrWhiteSpace(config.SiteUrl))
			{
				MessageBox.Show("app.json未配置SiteUrl");
				return;
			}

			RunApp();
			
			InitCef();
			
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			CloseRsCmsWebapi();
		}
	}

	
}
