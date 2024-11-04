using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RsCms.WinApp
{
	public class AppConfig
	{
        public string WinName { get; set; }
        public string App { get; set; }

        public string SiteUrl { get; set; }

        /// <summary>
        /// 如果子程序是控制台应用，不显示窗体
        /// </summary>
        public bool NoWindow { get; set; } = true;
    }
}
