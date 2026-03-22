# WebAppLauncher - .NET 8.0 WinForms Web应用容器

## 项目概述

这是一个基于.NET 8.0 WinForms的Web应用容器项目，允许在Windows桌面应用中运行和管理多个Web应用程序。项目使用WebView2控件作为浏览器引擎，支持通过配置文件动态切换不同的Web应用。

## 功能特性

### 核心功能
- ✅ **多Web应用管理**：在`apps`目录下可存放多个独立的Web应用
- ✅ **配置驱动**：通过`appsettings.json`配置文件指定当前运行的Web应用
- ✅ **WebView2集成**：使用Microsoft Edge WebView2控件提供现代化的Web渲染
- ✅ **右键菜单禁用**：彻底禁用WebView2和窗体的右键菜单功能
- ✅ **应用切换**：运行时可通过菜单切换不同的Web应用

### 管理功能
- ✅ **应用状态监控**：检查各个Web应用的可用性
- ✅ **示例应用创建**：快速创建带有现代化UI的示例Web应用
- ✅ **配置热加载**：支持修改配置文件后重新加载应用
- ✅ **应用目录管理**：一键打开应用目录进行文件管理

### 安全特性
- ✅ **上下文菜单禁用**：WebView2和窗体右键菜单完全禁用
- ✅ **新窗口阻止**：阻止Web应用弹出新窗口
- ✅ **开发工具禁用**：生产环境禁用开发者工具
- ✅ **JavaScript控制**：通过JavaScript注入增强安全性

## 项目结构

```
f:\work\webapp\
├── WebAppLauncher.sln                    # 解决方案文件
├── WebAppLauncher/                       # 主项目目录
│   ├── Program.cs                       # 应用程序入口点
│   ├── MainForm.cs                      # 主窗体代码
│   ├── MainForm.Designer.cs             # 主窗体设计器
│   ├── WebAppLauncher.csproj            # 项目文件
│   ├── appsettings.json                 # 配置文件
│   ├── Models/                          # 数据模型
│   │   └── WebAppSettings.cs            # 应用设置模型
│   ├── Services/                        # 服务层
│   │   ├── ConfigurationService.cs      # 配置管理服务
│   │   └── AppManagerService.cs         # 应用管理服务
│   └── apps/                            # Web应用目录
│       ├── app1/                        # 示例应用1：仪表板
│       │   └── index.html               # 仪表板主界面
│       └── app2/                        # 示例应用2：数据管理中心
│           └── index.html               # 数据管理主界面
└── README.md                            # 项目说明文档
```

## 配置文件说明

### appsettings.json
```json
{
  "WebAppSettings": {
    "CurrentApp": "app1",                // 当前运行的应用ID
    "Apps": {                           // 应用配置
      "app1": {
        "Name": "示例应用1",             // 应用显示名称
        "Path": "apps/app1/index.html", // 应用文件路径
        "Title": "我的Web应用1"         // 窗口标题
      },
      "app2": {
        "Name": "示例应用2",
        "Path": "apps/app2/index.html",
        "Title": "我的Web应用2"
      }
    }
  },
  "WindowSettings": {
    "Width": 1200,                      // 窗口宽度
    "Height": 800,                      // 窗口高度
    "StartPosition": "CenterScreen",    // 启动位置
    "DisableContextMenu": true          // 是否禁用右键菜单
  }
}
```

## 使用方法

### 1. 运行应用
1. 使用Visual Studio 2022或更高版本打开解决方案
2. 确保已安装.NET 8.0 SDK
3. 编译并运行项目

### 2. 添加新的Web应用
1. 在`WebAppLauncher/apps/`目录下创建新的文件夹（如`myapp`）
2. 在文件夹中创建`index.html`或其他Web文件
3. 在`appsettings.json`的`Apps`部分添加新应用配置：
```json
"myapp": {
  "Name": "我的应用",
  "Path": "apps/myapp/index.html",
  "Title": "我的自定义应用"
}
```

### 3. 切换当前应用
1. 修改`appsettings.json`中的`CurrentApp`值为目标应用ID
2. 重启应用程序
3. 或者通过应用菜单动态切换（如果应用已配置）

### 4. 创建示例应用
1. 运行应用程序
2. 点击"工具" -> "创建示例应用"
3. 输入应用ID和名称
4. 新应用将自动创建在`apps`目录下

## 技术实现

### 核心技术栈
- **.NET 8.0**：使用最新的.NET框架
- **WinForms**：传统的Windows桌面应用框架
- **WebView2**：基于Microsoft Edge的现代Web渲染引擎
- **JSON配置**：使用System.Text.Json处理配置文件

### 关键实现
1. **WebView2集成**：异步初始化，支持本地文件加载
2. **右键禁用**：多层级禁用（WebView2设置、JavaScript注入、窗体事件）
3. **配置管理**：支持动态加载和更新应用配置
4. **应用管理**：提供应用状态检查、目录管理等功能

### 安全措施
1. 禁用WebView2默认上下文菜单
2. 注入JavaScript防止右键菜单
3. 禁用开发者工具
4. 阻止新窗口弹出
5. 禁用窗体本身的右键菜单

## 示例应用说明

### 应用1：仪表板控制台
- **功能**：系统监控、性能指标展示、活动日志
- **特点**：现代化UI设计、实时数据更新、交互式图表
- **技术**：纯HTML/CSS/JavaScript实现

### 应用2：数据管理中心
- **功能**：数据监控、数据库状态、系统日志
- **特点**：深色主题、专业数据展示、实时图表
- **技术**：Canvas绘图、动态数据模拟

## 开发注意事项

### 1. WebView2运行时
- 应用程序需要WebView2运行时环境
- 第一次运行会自动下载和安装（如果未安装）

### 2. 文件路径
- Web应用路径相对于应用程序执行目录
- 使用`Path.Combine`确保跨平台兼容性

### 3. 配置更新
- 修改`appsettings.json`后需要重启应用
- 或者通过菜单刷新当前应用

### 4. 安全性
- 生产环境建议禁用开发者工具
- 考虑添加额外的安全检查
- 限制Web应用的文件访问权限

## 扩展功能建议

### 未来增强
1. **插件系统**：支持动态加载Web应用插件
2. **权限管理**：为不同应用设置不同权限
3. **离线缓存**：支持Web应用的离线使用
4. **自动更新**：Web应用的自动更新机制
5. **多语言支持**：国际化界面
6. **主题切换**：支持浅色/深色主题

### 企业级功能
1. **用户认证**：集成企业身份验证
2. **日志审计**：完整的操作日志记录
3. **性能监控**：应用性能指标收集
4. **自动备份**：配置和数据的自动备份

## 故障排除

### 常见问题
1. **WebView2初始化失败**
   - 确保已安装WebView2运行时
   - 检查网络连接（第一次运行需要下载）

2. **Web应用无法加载**
   - 检查`appsettings.json`中的路径配置
   - 确认文件确实存在于指定路径

3. **右键菜单未完全禁用**
   - 检查WebView2设置是否正确
   - 确认JavaScript注入成功

4. **应用程序崩溃**
   - 检查.NET 8.0运行时是否安装
   - 查看Windows事件查看器日志

### 调试建议
1. 启用开发者工具（临时修改代码）
2. 查看控制台输出
3. 检查应用程序日志文件

## 许可证

本项目仅供学习和参考使用，可根据需要进行修改和扩展。

## 联系与支持

如有问题或建议，请通过项目维护者联系。

---

**版本**: 1.0.0  
**最后更新**: 2025-03-22  
**兼容性**: Windows 10/11, .NET 8.0