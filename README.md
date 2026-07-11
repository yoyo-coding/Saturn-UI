# Saturn UI

Saturn UI 是一个基于 **Avalonia UI 11 + .NET 10** 构建的 Material 3 风格桌面 AI 助手。项目保留聊天、会话管理、设置、主题、Markdown 渲染与 OpenAI 兼容接口等原有能力，并新增独立的“编程模式”，用于轻量级代码阅读、编辑和 AI 辅助编程。

## 功能特性

- **聊天模式**：保留原有 AI 对话、会话列表、Markdown 消息渲染、流式响应与设置能力。
- **编程模式**：通过顶部“聊天 / 编程”切换进入代码工作区，不污染普通聊天历史。
- **文件与文件夹打开**：支持打开单个文本文件或整个文件夹；文件夹扫描会过滤 `.git`、`.vs`、`.idea`、`bin`、`obj`、`node_modules`、`.cache` 等目录。
- **代码编辑器**：基于 AvaloniaEdit，支持行号、等宽字体、当前行高亮、缩进设置、编辑、保存、另存为与未保存状态提示。
- **基础语法高亮**：初步支持 Python、C、C++、Java、Go、HTML，并为其他文本文件提供纯文本编辑能力。
- **AI 编程助手**：复用现有 ChatService 与应用设置中的本地 / OpenAI 兼容配置，可基于当前文件、选区和文件夹摘要回答问题。
- **虚影代码补全**：停止输入约 600ms 后自动请求补全，也可使用 `Ctrl+Space` 手动触发；`Tab` 接受建议，`Esc` 取消建议。
- **Material 3 视觉风格**：界面遵循 Material 3 的色彩、圆角和卡片化布局规范，保持聊天模式与编程模式的统一体验。

## 编程模式使用说明

1. 启动应用后，在顶部模式切换中选择 **编程**。
2. 点击文件按钮打开单个代码文件，或点击文件夹按钮打开一个项目目录。
3. 在左侧文件列表中选择文件，中间编辑器会显示并高亮代码。
4. 编辑后可点击保存按钮写回原文件，或使用另存为保存到新路径。
5. 在右侧 AI 编程助手中输入问题，例如“解释当前函数”“优化这段代码”“找出潜在 bug”。AI 会自动携带当前语言、文件路径、选区、文件内容摘要和文件夹树摘要。
6. 在支持的代码文件中暂停输入约 600ms 会请求虚影补全；也可以按 `Ctrl+Space` 主动请求，按 `Tab` 接受，按 `Esc` 取消。

## 当前限制

- 这是轻量级编辑器第一版，不包含完整 IDE 能力，例如终端、调试器、LSP、Git 面板、全局搜索和多标签编辑。
- 单文件默认限制约 2 MB，过大的文件不会直接加载。
- 二进制文件不会在编程模式中打开。
- AI 侧边对话目前为编程模式内存态，不写入普通聊天会话数据库。
- 语法高亮为基础规则高亮，不等同于完整语言解析器。
- 虚影补全当前以编辑器区域内的轻量浮层展示，后续可继续接入 AvaloniaEdit 自定义渲染层实现更接近 IDE 的光标后内联效果。

## 技术栈

| 模块 | 技术 |
| --- | --- |
| UI 框架 | Avalonia UI 11 |
| 运行时 | .NET 10 |
| 架构 | MVVM |
| MVVM 工具 | CommunityToolkit.Mvvm |
| 图标 | Material.Icons.Avalonia |
| 代码编辑器 | AvaloniaEdit |
| Markdown | Markdown.Avalonia |
| 本地数据 | LiteDB + System.Text.Json |
| AI 通信 | HttpClient / SSE / gRPC / OpenAI 兼容 API |
| 测试 | xUnit + Microsoft.NET.Test.Sdk |

## 项目结构

```text
Saturn-UI/
├─ Saturn UI.sln
├─ README.md
├─ REFACTOR_REPORT.md
├─ Saturn UI/
│  ├─ App.axaml / App.axaml.cs
│  ├─ Controls/
│  ├─ Converters/
│  ├─ Models/                  # 会话、消息、编程模式模型
│  ├─ Services/                # 聊天、设置、主题、存储等服务
│  │  └─ Coding/               # 编程模式相关服务
│  ├─ Themes/
│  ├─ ViewModels/              # Main / Chat / Coding / SessionList / Settings
│  └─ Views/                   # Avalonia 视图
└─ SaturnUI.Tests/
```

## 构建与运行

```bash
# 还原依赖
dotnet restore "Saturn UI.sln"

# 构建
dotnet build "Saturn UI.sln" -v:minimal

# 运行测试
dotnet test "Saturn UI.sln" -v:minimal --no-build

# 启动应用
dotnet run --project "Saturn UI/Saturn UI.csproj"
```

## 最近验证结果

```text
dotnet build "Saturn UI.sln" -v:minimal
# 0 warning, 0 error

dotnet test "Saturn UI.sln" -v:minimal --no-build
# Passed: 30 / Failed: 0 / Skipped: 0
```

## 重构报告

本次 UI 与编程模式相关的详细说明见：

- `REFACTOR_REPORT.md`
