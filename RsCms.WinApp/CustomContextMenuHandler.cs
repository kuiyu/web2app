using CefSharp;

namespace RsCms.WinApp
{
	internal class CustomContextMenuHandler : IContextMenuHandler
	{
		public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
		{
			// 禁用所有菜单
			model.Clear();
		}

		public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
		{
			// 返回false将阻止右键菜单命令的执行
			return false;
		}

		public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
		{
			
		}

		public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
		{
			// 返回false将阻止右键菜单的显示
			return false;
		}
	}
}
