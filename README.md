# Saturn UI（灵星 UI）

> 轻量、跨平台的 AI 对话前端界面

Saturn UI 是一款基于 [Avalonia UI](https://avaloniaui.net/) 开发的本地 AI 对话客户端，采用深色科技风格设计，支持流式消息展示与多会话管理。

## 技术栈

| 类别    | 技术                                       |
| ----- | ---------------------------------------- |
| UI 框架 | Avalonia UI 11.2                         |
| 运行时   | .NET 10                                  |
| 架构模式  | MVVM (CommunityToolkit.Mvvm)             |
| 主题    | Fluent Design (Dark)                     |
| 本地存储  | LiteDB                                   |
| 依赖注入  | Microsoft.Extensions.DependencyInjection |

## 项目结构

```
Saturn UI/
├── Saturn UI/                 # 主项目
│   ├── App.axaml           # 应用入口与全局主题
│   ├── Views/              # 视图 (.axaml)
│   ├── ViewModels/         # 视图模型 (MVVM)
│   ├── Models/             # 数据模型
│   ├── Services/           # 业务服务
│   ├── Converters/         # 数据绑定转换器
│   ├── Themes/             # 自定义样式与色彩
│   └── Assets/             # 图片、图标等资源
├── Saturn UI.sln              # 解决方案文件
├── Plan.md                 # 项目策划书
└── Implementation.md       # 实现文档
```

## 运行要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- 支持平台：Windows / macOS / Linux

## 构建与运行

```bash
# 还原依赖
dotnet restore "Saturn UI.sln"

# 构建
dotnet build "Saturn UI.sln"

# 运行
dotnet run --project "Saturn UI/Saturn UI.csproj"
```

或使用 Visual Studio / Rider 打开 `Saturn UI.sln` 直接运行。

## 设计特点

- **深色科技风格**：深空黑底色 + 星蓝/极光紫强调色
- **Fluent Design**：基于 Avalonia 原生 Fluent 主题扩展
- **Skia 自绘**：跨平台一致的渲染表现，非 WebView 套壳

## 许可证

[Apache License 2.0](LICENSE)
