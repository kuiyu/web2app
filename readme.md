# 将网站转换windows应用

## 运行环境

该项目需要.net8环境支持，需要提示[下载安装.net8](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/sdk-8.0.306-windows-x64-installer)



## 如何使用

打开app.json 配置
```json
{
  "WinName": "软商WinApp", //windown应用名称
  "App": "RsCms.WebApi.exe", //要执行的子应用名称（可不填)
  "SiteUrl": "https://localhost:6688", //要打开的网站名称
  "NoWindow": true

}

```

配置完成后，就可以将SiteUrl节点的网站变成windows应用

