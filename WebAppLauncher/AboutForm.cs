using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace WebAppLauncher
{
    /// <summary>
    /// “关于”窗体：显示程序信息 + 二维码图片。
    /// 二维码由外部提供，约定放在程序根目录 qrcode.png
    /// （将你的二维码图片命名为 qrcode.png 放到 WebAppLauncher.exe 同级目录即可）。
    /// </summary>
    public partial class AboutForm : Form
    {
        private PictureBox _picQr = null!;
        private Label _lblQrHint = null!;

        public AboutForm()
        {
            InitializeComponent();
            LoadImage();
        }

        private void InitializeComponent()
        {
            var version = "0.1.0";

            this.Text = "关于 WebAppLauncher";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(420, 470);
            this.BackColor = Color.White;

            var lblTitle = new Label
            {
                Text = "WebAppLauncher",
                Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 18)
            };

            // 固定宽度、自动换行，避免长文本被窗体边缘裁掉
            var lblInfo = new Label
            {
                Text = $"版本：{version}\n" +
                       "一个将 Web 应用打包为桌面程序的容器。\n" +
                       "可在无 Node/运行环境的电脑上，通过内嵌 ASP.NET Core 托管并代理 Web 应用。\n\n" +
                       "作者微信：runsoft1024",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false,
                Width = this.ClientSize.Width - 40,
                Height = 120,
                Location = new Point(20, 60)
            };

            _picQr = new PictureBox
            {
                Size = new Size(200, 200),
                Location = new Point((this.ClientSize.Width - 200) / 2, 195),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblQrHint = new Label
            {
                Text = "未找到图片资源（me）",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Visible = false,
                Location = new Point(20, 440)
            };

            // 关闭按钮已移除：通过标题栏“×”或 Alt+F4 关闭窗体
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblInfo);
            this.Controls.Add(_picQr);
            this.Controls.Add(_lblQrHint);
        }

        private void LoadImage()
        {
            try
            {
                // 从嵌入资源 Resource1.resx 的 "me" 读取图片（me.jpg，资源类型即 Bitmap）
                if (Resource1.me == null)
                {
                    _lblQrHint.Visible = true;
                    return;
                }
                _picQr.Image = Resource1.me;
                _lblQrHint.Visible = false;
            }
            catch (Exception ex)
            {
                _lblQrHint.Text = $"图片加载失败：{ex.Message}";
                _lblQrHint.Visible = true;
            }
        }
    }
}
