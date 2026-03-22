@echo off
echo ========================================
echo WebAppLauncher 构建和运行脚本
echo ========================================
echo.

REM 检查是否安装了.NET 8.0 SDK
echo 检查.NET 8.0 SDK...
dotnet --version | findstr "8.0" >nul
if %errorlevel% neq 0 (
    echo 错误: 未检测到.NET 8.0 SDK
    echo 请从 https://dotnet.microsoft.com/download/dotnet/8.0 下载并安装
    pause
    exit /b 1
)

echo .NET 8.0 SDK 已安装

REM 切换到项目目录
cd /d "%~dp0WebAppLauncher"

echo.
echo ========================================
echo 1. 恢复NuGet包...
echo ========================================
dotnet restore
if %errorlevel% neq 0 (
    echo 错误: NuGet包恢复失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo 2. 构建项目...
echo ========================================
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo 错误: 项目构建失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo 3. 发布项目...
echo ========================================
dotnet publish --configuration Release --output "..\publish" --self-contained false
if %errorlevel% neq 0 (
    echo 错误: 项目发布失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo 4. 运行应用程序...
echo ========================================
echo 正在启动 WebAppLauncher...
echo.
echo 应用功能说明:
echo - 通过 appsettings.json 配置当前运行的应用
echo - apps 目录包含两个示例Web应用
echo - 右键菜单已被禁用
echo - 可通过菜单切换应用
echo.
echo 按任意键启动应用程序...
pause >nul

REM 运行发布的应用
cd /d "%~dp0publish"
start WebAppLauncher.exe

echo.
echo 应用程序已启动！
echo 按任意键退出脚本...
pause >nul