using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace WebAppLauncher
{
    /// <summary>
    /// 简单的图标生成器
    /// 注意：这只是一个示例，实际项目中建议使用专业的图标编辑工具
    /// </summary>
    public static class IconGenerator
    {
        /// <summary>
        /// 生成一个简单的应用程序图标
        /// </summary>
        public static void GenerateAppIcon()
        {
            try
            {
                Console.WriteLine("开始生成应用程序图标...");
                
                // 创建不同尺寸的位图
                var sizes = new[] { 16, 32, 48, 64, 128, 256 };
                var bitmaps = new Bitmap[sizes.Length];
                
                for (int i = 0; i < sizes.Length; i++)
                {
                    int size = sizes[i];
                    bitmaps[i] = CreateIconBitmap(size);
                    Console.WriteLine($"创建 {size}x{size} 图标位图");
                }
                
                // 在实际项目中，这里应该将多个位图合并为ICO文件
                // 但由于System.Drawing在.NET Core中的限制，我们只保存一个PNG作为示例
                
                // 保存主图标（256x256）
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.png");
                bitmaps[^1].Save(iconPath, ImageFormat.Png);
                Console.WriteLine($"图标已保存到: {iconPath}");
                
                // 清理资源
                foreach (var bitmap in bitmaps)
                {
                    bitmap.Dispose();
                }
                
                Console.WriteLine("图标生成完成！");
                Console.WriteLine("注意：这是一个PNG格式的图标示例。");
                Console.WriteLine("在实际项目中，请使用专业的图标编辑工具创建.ico文件。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成图标时出错: {ex.Message}");
            }
        }
        
        private static Bitmap CreateIconBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                
                // 绘制背景（蓝色渐变）
                var backgroundBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, size, size),
                    Color.FromArgb(67, 97, 238),  // #4361ee
                    Color.FromArgb(76, 201, 240), // #4cc9f0
                    System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
                
                graphics.FillRectangle(backgroundBrush, 0, 0, size, size);
                
                // 绘制内框
                int padding = size / 8;
                var innerRect = new Rectangle(padding, padding, size - 2 * padding, size - 2 * padding);
                graphics.FillRectangle(Brushes.White, innerRect);
                
                // 绘制文字
                string text = GetIconText(size);
                var font = new Font("Arial", size / 4, FontStyle.Bold);
                var textSize = graphics.MeasureString(text, font);
                var textPoint = new PointF(
                    (size - textSize.Width) / 2,
                    (size - textSize.Height) / 2);
                
                graphics.DrawString(text, font, Brushes.Black, textPoint);
                
                // 清理资源
                backgroundBrush.Dispose();
                font.Dispose();
            }
            
            return bitmap;
        }
        
        private static string GetIconText(int size)
        {
            return size switch
            {
                <= 32 => "W",
                <= 64 => "WA",
                <= 128 => "WAL",
                _ => "Web"
            };
        }
        
        /// <summary>
        /// 创建ICO文件的替代方案描述
        /// </summary>
        public static void ShowIconCreationInstructions()
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("如何创建应用程序图标 (.ico 文件)");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("方法1：使用在线工具");
            Console.WriteLine("1. 访问 https://icoconvert.com/");
            Console.WriteLine("2. 上传PNG图片");
            Console.WriteLine("3. 选择多尺寸 (16x16, 32x32, 48x48, 64x64, 128x128, 256x256)");
            Console.WriteLine("4. 下载生成的ICO文件");
            Console.WriteLine("5. 重命名为 app.ico 并放在项目根目录");
            Console.WriteLine();
            Console.WriteLine("方法2：使用专业软件");
            Console.WriteLine("1. Adobe Photoshop");
            Console.WriteLine("2. GIMP (免费)");
            Console.WriteLine("3. IcoFX (专业图标工具)");
            Console.WriteLine("4. Axialis IconWorkshop");
            Console.WriteLine();
            Console.WriteLine("方法3：使用Visual Studio");
            Console.WriteLine("1. 在解决方案资源管理器中右键项目");
            Console.WriteLine("2. 选择 添加 -> 新建项");
            Console.WriteLine("3. 选择 资源文件 -> 图标文件(.ico)");
            Console.WriteLine("4. 使用内置编辑器创建图标");
            Console.WriteLine();
            Console.WriteLine("图标设计建议：");
            Console.WriteLine("- 主色: #4361ee (蓝色)");
            Console.WriteLine("- 辅色: #4cc9f0 (青色)");
            Console.WriteLine("- 简洁明了，体现Web应用容器的概念");
            Console.WriteLine("- 包含多个尺寸以适应不同显示需求");
            Console.WriteLine("========================================");
        }
    }
}