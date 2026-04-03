using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// 解析命令行参数
string port = "5000";
string staticDir = Directory.GetCurrentDirectory();

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length)
    {
        port = args[i + 1];
    }
    else if (args[i] == "--staticDir" && i + 1 < args.Length)
    {
        staticDir = args[i + 1];
    }
}

// 移除引号（如果有）
staticDir = staticDir.Trim('"');

// 确保静态文件目录是绝对路径
if (!Path.IsPathRooted(staticDir))
{
    staticDir = Path.Combine(Directory.GetCurrentDirectory(), staticDir);
}

Console.WriteLine($"启动ASP.NET Core静态文件服务器");
Console.WriteLine($"端口: {port}");
Console.WriteLine($"静态文件目录: {staticDir}");

// 配置Kestrel监听端口
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

// 启用详细错误页面
app.UseDeveloperExceptionPage();

// 启用静态文件服务
if (Directory.Exists(staticDir))
{
    Console.WriteLine($"静态文件目录存在，启用静态文件服务");
    
    // 配置静态文件选项
    var staticFileOptions = new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(staticDir),
        RequestPath = "",
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream"
    };
    
    app.UseStaticFiles(staticFileOptions);
    
    // 启用目录浏览（可选）
    app.UseDirectoryBrowser(new DirectoryBrowserOptions
    {
        FileProvider = new PhysicalFileProvider(staticDir),
        RequestPath = ""
    });
    
    // 设置默认文件（index.html）
    app.UseFileServer(new FileServerOptions
    {
        FileProvider = new PhysicalFileProvider(staticDir),
        RequestPath = "",
        EnableDirectoryBrowsing = true,
        EnableDefaultFiles = true
    });
}
else
{
    Console.WriteLine($"静态文件目录不存在: {staticDir}");
    app.MapGet("/", () => $"静态文件目录不存在: {staticDir}");
}

Console.WriteLine($"服务器已启动，访问地址: http://localhost:{port}");

app.Run();
