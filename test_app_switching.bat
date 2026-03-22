@echo off
echo ========================================
echo WebAppLauncher 应用切换功能测试
echo ========================================
echo.

REM 切换到项目目录
cd /d "%~dp0WebAppLauncher"

echo 1. 检查当前配置文件...
echo.
type appsettings.json
echo.

echo 2. 编译项目...
echo.
dotnet build --configuration Debug
if %errorlevel% neq 0 (
    echo 错误: 项目编译失败
    pause
    exit /b 1
)

echo.
echo 3. 运行配置测试...
echo.
dotnet run -- --test
if %errorlevel% neq 0 (
    echo 警告: 测试运行可能有问题
)

echo.
echo 4. 手动测试说明
echo ========================================
echo.
echo 要手动测试应用切换功能:
echo.
echo 步骤1: 运行应用程序
echo    dotnet run
echo    或者
echo    双击 build_and_run.bat
echo.
echo 步骤2: 测试菜单功能
echo    1. 点击"应用"菜单
echo    2. 选择"示例应用2 (app2)"
echo    3. 验证窗口标题是否变为"我的Web应用2"
echo    4. 验证Web内容是否切换到数据管理中心
echo    5. 切换回"示例应用1 (app1)"
echo.
echo 步骤3: 测试右键菜单禁用
echo    1. 在Web应用区域右键 - 应该无菜单
echo    2. 在窗口空白区域右键 - 应该无菜单
echo.
echo 步骤4: 测试其他功能
echo    1. 文件 -> 刷新应用
echo    2. 文件 -> 打开应用目录
echo    3. 工具 -> 检查应用状态
echo    4. 帮助 -> 关于
echo.
echo 步骤5: 验证配置文件更新
echo    1. 切换应用后，检查appsettings.json
echo    2. CurrentApp 应该已更新
echo.
echo ========================================
echo 测试完成！
echo.
echo 如果应用切换功能仍然不工作:
echo 1. 检查是否有错误消息
echo 2. 查看控制台输出
echo 3. 检查配置文件权限
echo 4. 确保Web应用文件存在
echo.
pause