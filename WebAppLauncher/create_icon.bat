@echo off
echo ========================================
echo WebAppLauncher 图标生成脚本
echo ========================================
echo.

REM 检查是否安装了ImageMagick
where magick >nul 2>nul
if %errorlevel% neq 0 (
    echo 错误: 未检测到ImageMagick
    echo 请从 https://imagemagick.org/script/download.php 下载并安装
    echo 或者手动创建 app.ico 文件
    pause
    exit /b 1
)

echo 检测到 ImageMagick，开始生成图标...

REM 创建临时目录
set TEMP_DIR=%TEMP%\webapplauncher_icon
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"
mkdir "%TEMP_DIR%"

echo.
echo 1. 创建图标设计...
echo.

REM 创建不同尺寸的PNG图标
REM 16x16
magick -size 16x16 xc:none -fill "#4361ee" -draw "rectangle 0,0 15,15" -fill "#4cc9f0" -draw "rectangle 2,2 13,13" -fill white -pointsize 8 -font Arial -gravity center -annotate 0 "W" "%TEMP_DIR%\icon_16.png"

REM 32x32
magick -size 32x32 xc:none -fill "#4361ee" -draw "rectangle 0,0 31,31" -fill "#4cc9f0" -draw "rectangle 4,4 27,27" -fill white -pointsize 16 -font Arial -gravity center -annotate 0 "WA" "%TEMP_DIR%\icon_32.png"

REM 48x48
magick -size 48x48 xc:none -fill "#4361ee" -draw "rectangle 0,0 47,47" -fill "#4cc9f0" -draw "rectangle 6,6 41,41" -fill white -pointsize 22 -font Arial -gravity center -annotate 0 "WAL" "%TEMP_DIR%\icon_48.png"

REM 64x64
magick -size 64x64 xc:none -fill "#4361ee" -draw "rectangle 0,0 63,63" -fill "#4cc9f0" -draw "rectangle 8,8 55,55" -fill white -pointsize 28 -font Arial -gravity center -annotate 0 "Web" "%TEMP_DIR%\icon_64.png"

REM 128x128
magick -size 128x128 xc:none -fill "#4361ee" -draw "rectangle 0,0 127,127" -fill "#4cc9f0" -draw "rectangle 16,16 111,111" -fill white -pointsize 48 -font Arial -gravity center -annotate 0 "WAL" "%TEMP_DIR%\icon_128.png"

REM 256x256
magick -size 256x256 gradient:"#4361ee"-"#4cc9f0" -fill white -pointsize 80 -font Arial -gravity center -annotate 0 "WA" "%TEMP_DIR%\icon_256.png"

echo.
echo 2. 合并为ICO文件...
echo.

REM 将PNG图标合并为ICO文件
magick "%TEMP_DIR%\icon_16.png" "%TEMP_DIR%\icon_32.png" "%TEMP_DIR%\icon_48.png" "%TEMP_DIR%\icon_64.png" "%TEMP_DIR%\icon_128.png" "%TEMP_DIR%\icon_256.png" app.ico

echo.
echo 3. 清理临时文件...
echo.

REM 清理临时文件
rmdir /s /q "%TEMP_DIR%"

echo.
echo ========================================
echo 图标生成完成！
echo 已创建: app.ico
echo ========================================
echo.
echo 图标说明:
echo - 主色: #4361ee (蓝色)
echo - 辅色: #4cc9f0 (青色)
echo - 文字: "W"/"WA"/"Web"/"WAL" (WebAppLauncher缩写)
echo - 尺寸: 16x16, 32x32, 48x48, 64x64, 128x128, 256x256
echo.
echo 如果没有ImageMagick，您需要:
echo 1. 使用在线工具生成ICO文件
echo 2. 或使用Visual Studio的资源编辑器
echo 3. 或使用其他图标编辑工具
echo.
pause