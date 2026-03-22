# WebAppLauncher 运行说明

## 快速开始

### 方法1：使用构建脚本（推荐）
1. 打开 `f:\work\webapp\` 文件夹
2. 双击运行 `build_and_run.bat`
3. 按照提示操作

### 方法2：使用Visual Studio
1. 使用 Visual Studio 2022+ 打开 `WebAppLauncher.sln`
2. 确保已安装 .NET 8.0 工作负载
3. 按 F5 运行

### 方法3：使用命令行
```bash
cd f:\work\webapp\WebAppLauncher
dotnet restore
dotnet build
dotnet run
```

## 项目验证

### 检查创建的文件
项目应包含以下关键文件：
```
f:\work\webapp\
├── WebAppLauncher.sln
├── WebAppLauncher/
│   ├── Program.cs
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   ├── WebAppLauncher.csproj
│   ├── appsettings.json
│   ├── Models/WebAppSettings.cs
│   ├── Services/ConfigurationService.cs
│   ├── Services/AppManagerService.cs
│   └── apps/
│       ├── app1/index.html  # 仪表板应用
│       └── app2/index.html  # 数据管理中心应用
├── README.md
├── build_and_run.bat
└── run_instructions.md
```

### 验证配置
打开 `appsettings.json` 检查配置：
```json
{
  "WebAppSettings": {
    "CurrentApp": "app1",  // 默认运行 app1
    "Apps": {
      "app1": { ... },
      "app2": { ... }
    }
  }
}
```

## 功能测试清单

### 基本功能
- [ ] 应用程序正常启动
- [ ] 显示主窗体窗口
- [ ] 窗口标题显示为 "我的Web应用1"
- [ ] WebView2控件成功加载app1的仪表板

### 右键菜单禁用
- [ ] 在Web应用区域右键，无菜单弹出
- [ ] 在窗体空白区域右键，无菜单弹出
- [ ] JavaScript右键禁用功能生效

### 菜单功能
- [ ] 文件菜单可用（刷新、打开目录、退出）
- [ ] 应用菜单显示两个应用选项
- [ ] 工具菜单可用（创建示例应用、检查状态）
- [ ] 帮助菜单可用（关于）

### 应用切换
- [ ] 通过应用菜单切换到app2
- [ ] app2的数据管理中心正常加载
- [ ] 窗口标题更新为"我的Web应用2"

### 配置管理
- [ ] 修改appsettings.json中的CurrentApp为app2
- [ ] 重启应用，自动加载app2
- [ ] 检查应用状态功能显示两个应用都可用

## 故障排除

### 1. WebView2初始化失败
**症状**：应用启动时卡住或报错
**解决**：
- 确保已安装WebView2运行时
- 第一次运行需要网络连接以下载运行时
- 或者手动安装：https://developer.microsoft.com/zh-cn/microsoft-edge/webview2/

### 2. 应用无法加载
**症状**：显示错误页面
**解决**：
- 检查appsettings.json中的路径配置
- 确认`apps/app1/index.html`文件存在
- 确保文件路径正确

### 3. 右键菜单未禁用
**症状**：仍然可以右键弹出菜单
**解决**：
- 检查WebView2设置`AreDefaultContextMenusEnabled = false`
- 确认JavaScript注入成功
- 检查窗体鼠标事件处理

### 4. 菜单功能异常
**症状**：菜单点击无反应
**解决**：
- 检查菜单事件绑定
- 确保相关服务类已正确初始化

## 开发环境要求

### 必需组件
1. **操作系统**：Windows 10/11
2. **.NET SDK**：8.0 或更高版本
3. **开发工具**：
   - Visual Studio 2022+（推荐）
   - 或 VS Code with C#扩展
   - 或 Rider

### 可选组件
1. **WebView2运行时**：自动下载或手动安装
2. **Git**：版本控制（可选）

## 性能优化建议

### 启动优化
- 启用WebView2预加载
- 异步初始化组件
- 延迟加载非必要资源

### 内存管理
- 及时释放WebView2资源
- 监控内存使用
- 实现应用缓存策略

### 用户体验
- 添加加载进度指示
- 实现错误恢复机制
- 优化切换动画效果

## 扩展开发指南

### 添加新Web应用
1. 在`apps`目录创建新文件夹
2. 添加Web文件（HTML/CSS/JS）
3. 更新`appsettings.json`配置
4. 重启应用或通过菜单切换

### 修改配置结构
1. 更新`Models/WebAppSettings.cs`
2. 修改`Services/ConfigurationService.cs`
3. 更新配置文件格式
4. 调整相关业务逻辑

### 集成新功能
1. 在`Services`目录添加新服务类
2. 在`MainForm`中集成新功能
3. 更新菜单或界面
4. 测试功能完整性

## 安全注意事项

### 生产环境部署
1. 禁用开发者工具
2. 启用HTTPS（如果加载远程内容）
3. 实现应用沙箱机制
4. 添加访问控制

### 配置安全
1. 加密敏感配置
2. 验证配置完整性
3. 实现配置备份
4. 监控配置变更

## 技术支持

### 获取帮助
1. 查看`README.md`详细文档
2. 检查错误日志和控制台输出
3. 验证环境配置
4. 参考示例代码

### 常见问题解答
Q: 如何更换默认应用？
A: 修改appsettings.json中的CurrentApp值

Q: 如何添加更多Web应用？
A: 在apps目录添加文件夹，并在配置中添加对应项

Q: 如何启用开发者工具？
A: 修改MainForm.cs中settings.AreDevToolsEnabled = true

Q: 如何修改窗口大小？
A: 修改appsettings.json中的WindowSettings

---

**项目状态**：已完成基础功能开发  
**测试状态**：待验证  
**部署准备**：可运行演示