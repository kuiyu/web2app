using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// 解析命令行参数
string port = "5000";
string staticDir = Directory.GetCurrentDirectory();

// 反向代理映射：前缀 -> 上游目标地址。例如 /api=http://localhost:8080
var proxyRoutes = new List<(string Prefix, string Target)>();

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
    else if (args[i] == "--proxy" && i + 1 < args.Length)
    {
        // 支持多次 --proxy，或单次用分号分隔
        var parts = args[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var eq = p.IndexOf('=');
            if (eq > 0)
            {
                var prefix = p.Substring(0, eq).Trim();
                var target = p.Substring(eq + 1).Trim();
                if (!prefix.StartsWith("/")) prefix = "/" + prefix;
                if (!prefix.EndsWith("/")) prefix += "/";
                if (!target.EndsWith("/")) target += "/";
                proxyRoutes.Add((prefix, target));
            }
        }
    }
}

// 移除引号（如果有）
staticDir = staticDir.Trim('"');

// 确保静态文件目录是绝对路径
if (!Path.IsPathRooted(staticDir))
{
    staticDir = Path.Combine(Directory.GetCurrentDirectory(), staticDir);
}

Console.WriteLine($"启动ASP.NET Core服务器（静态托管 + 反向代理）");
Console.WriteLine($"端口: {port}");
Console.WriteLine($"静态文件目录: {staticDir}");
if (proxyRoutes.Count > 0)
{
    Console.WriteLine("反向代理路由:");
    foreach (var r in proxyRoutes)
        Console.WriteLine($"  {r.Prefix}* -> {r.Target}");
}

// 配置Kestrel监听端口
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// 共享的 HttpClient（复用连接，避免端口/套接字耗尽）
var sharedHandler = new HttpClientHandler
{
    AllowAutoRedirect = false,
    UseCookies = false
};
var httpClient = new HttpClient(sharedHandler)
{
    Timeout = TimeSpan.FromSeconds(120)
};

var app = builder.Build();

// 启用详细错误页面
app.UseDeveloperExceptionPage();

// ---- 反向代理中间件（必须放在静态文件之前）----
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";

    // 选择匹配长度最长的代理前缀
    (string Prefix, string Target)? matched = null;
    foreach (var r in proxyRoutes)
    {
        if (path.StartsWith(r.Prefix, StringComparison.OrdinalIgnoreCase))
        {
            if (matched == null || r.Prefix.Length > matched.Value.Prefix.Length)
                matched = r;
        }
    }

    if (matched == null)
    {
        await next();
        return;
    }

    var (prefix, target) = matched.Value;
    // 将前缀之后的剩余路径拼接到上游 target
    var remaining = path[prefix.Length..];
    if (!remaining.StartsWith("/")) remaining = "/" + remaining;
    var query = context.Request.QueryString.Value ?? "";
    var upstreamUrl = target.TrimEnd('/') + remaining + query;

    var method = context.Request.Method;
    var requestMsg = new HttpRequestMessage(new HttpMethod(method), upstreamUrl);

    // 复制请求头（跳过 host 等 hop-by-hop）
    foreach (var hdr in context.Request.Headers)
    {
        if (hdr.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
        if (hdr.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
        if (hdr.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
        try
        {
            requestMsg.Headers.TryAddWithoutValidation(hdr.Key, hdr.Value.ToArray());
        }
        catch { }
    }

    // 复制请求体
    if (context.Request.ContentLength > 0 || context.Request.Body.CanRead)
    {
        try
        {
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            if (ms.Length > 0)
            {
                ms.Position = 0;
                requestMsg.Content = new StreamContent(ms);
                foreach (var hdr in context.Request.Headers)
                {
                    if (hdr.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                        hdr.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        try { requestMsg.Content.Headers.TryAddWithoutValidation(hdr.Key, hdr.Value.ToArray()); } catch { }
                    }
                }
            }
        }
        catch { }
    }

    try
    {
        using var responseMsg = await httpClient.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead);

        context.Response.StatusCode = (int)responseMsg.StatusCode;

        // 复制响应头（跳过 chunked/transfer-encoding 等由框架处理的头）
        foreach (var hdr in responseMsg.Headers)
        {
            if (hdr.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
            if (hdr.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
            try { context.Response.Headers[hdr.Key] = hdr.Value.ToArray(); } catch { }
        }
        foreach (var hdr in responseMsg.Content.Headers)
        {
            if (hdr.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
            try { context.Response.Headers[hdr.Key] = hdr.Value.ToArray(); } catch { }
        }

        // CORS：允许跨域（便于 SPA 在本地端口访问代理后端的 API）
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS, PATCH";
        context.Response.Headers["Access-Control-Allow-Headers"] = "*";

        if (method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 204;
            return;
        }

        using var respStream = await responseMsg.Content.ReadAsStreamAsync();
        await respStream.CopyToAsync(context.Response.Body);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 502;
        await context.Response.WriteAsync($"代理上游错误: {ex.Message}");
    }
});

// 启用静态文件服务
if (Directory.Exists(staticDir))
{
    Console.WriteLine($"静态文件目录存在，启用静态文件服务");

    var staticFileOptions = new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(staticDir),
        RequestPath = "",
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream"
    };

    app.UseStaticFiles(staticFileOptions);

    app.UseDirectoryBrowser(new DirectoryBrowserOptions
    {
        FileProvider = new PhysicalFileProvider(staticDir),
        RequestPath = ""
    });

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
