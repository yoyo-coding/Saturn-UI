# Saturn UI

Saturn UI 是一个基于 **Avalonia UI 11 + .NET 10** 的跨平台 AI 聊天桌面客户端。本次重构将应用界面直接重写为 Material 3 风格，同时保留现有会话、消息、附件、Markdown、主题、设置、本地后端与 OpenAI 兼容服务等功能。

## 功能特性

- **Material 3 全新 UI**：重写主窗口 Shell、会话侧栏、聊天画布、设置抽屉、提供商配置弹窗与启动页。
- **会话管理**：新建、选择、搜索、删除会话，并按更新时间维护列表。
- **聊天体验**：流式回复、停止生成、Enter 发送、Ctrl+Enter 换行。
- **Markdown 渲染**：AI 消息支持 Markdown、代码块与引用样式。
- **附件能力**：支持文件附件、图片附件、拖放文件与 Ctrl+V 粘贴图片。
- **多提供商**：支持本地 HTTP/SSE/gRPC 后端与 OpenAI 兼容 Chat Completions API。
- **主题系统**：Material 3 色彩令牌、Light/Dark 变体、动态主题切换与 Ripple 交互。
- **本地持久化**：LiteDB 存储会话与消息，JSON 存储用户设置。
- **测试覆盖**：包含消息流式缓冲、设置服务与本地存储测试。

## 技术栈

| 模块 | 技术 |
| --- | --- |
| UI | Avalonia UI 11.3.18 |
| 运行时 | .NET 10 |
| 架构 | MVVM |
| MVVM 工具 | CommunityToolkit.Mvvm |
| 图标 | Material.Icons.Avalonia |
| Markdown | Markdown.Avalonia |
| 存储 | LiteDB + System.Text.Json |
| 通信 | HttpClient / SSE / gRPC / OpenAI 兼容 API |
| 测试 | xUnit + Microsoft.NET.Test.Sdk |

## 项目结构

~~~text
Saturn-UI/
├─ Saturn UI.sln
├─ README.md
├─ REFACTOR_REPORT.md
├─ Saturn UI/
│  ├─ App.axaml / App.axaml.cs
│  ├─ AppConstants.cs
│  ├─ Controls/                 # RippleButton / RippleSelectableItem
│  ├─ Converters/               # UI 绑定转换器
│  ├─ Models/                   # Session / Message / API DTO
│  ├─ Protos/                   # gRPC proto
│  ├─ Services/                 # 设置、主题、存储、聊天 provider
│  ├─ Themes/                   # Material 3 色彩与控件样式
│  ├─ ViewModels/               # Main / Chat / SessionList / Settings
│  └─ Views/                    # 重写后的 Avalonia UI
└─ SaturnUI.Tests/              # 单元测试
~~~

## 界面重写范围

本次 UI 重写覆盖以下文件：

- Saturn UI/Views/MainWindow.axaml：Material 3 应用 Shell、顶部栏、侧栏开关、设置抽屉、启动遮罩。
- Saturn UI/Views/SessionListView.axaml：品牌区、FAB 新会话按钮、搜索栏、M3 会话卡片列表。
- Saturn UI/Views/ChatView.axaml：欢迎卡片、消息气泡、AI 标识、附件 chip、悬浮输入框。
- Saturn UI/Views/SettingsView.axaml：提供商、当前配置、主题、字体与性能模式的卡片化设置页。
- Saturn UI/Views/LocalProviderWindow.axaml：本地后端配置弹窗。
- Saturn UI/Views/OnlineProviderWindow.axaml：OpenAI 兼容服务配置弹窗。
- Saturn UI/Views/SplashWindow.axaml / MainView.axaml：统一启动与占位视觉。
- Saturn UI/Themes/ThemeStyles.axaml：Material 3 typography、surface、button、card、message 与 input 样式令牌。

## 运行与测试

~~~bash
# 还原依赖
dotnet restore "Saturn UI.sln"

# 构建
dotnet build "Saturn UI.sln" -v:minimal

# 运行测试
dotnet test "Saturn UI.sln" -v:minimal --no-build

# 启动应用
dotnet run --project "Saturn UI/Saturn UI.csproj"
~~~

当前验证结果：

~~~text
dotnet build "Saturn UI.sln" -v:minimal
# 0 warning, 0 error

dotnet test "Saturn UI.sln" -v:minimal --no-build
# Passed: 7 / Failed: 0 / Skipped: 0
~~~

## 提供商配置

### 本地后端

默认地址：http://127.0.0.1:8000

HTTP 请求示例：

~~~http
POST /chat
Content-Type: application/json
~~~

~~~json
{
  "message": "你好",
  "conversation_id": "optional-session-id",
  "stream": true
}
~~~

SSE 响应示例：

~~~text
data: {"token":"你"}
data: {"token":"好"}
data: {"done":true}
~~~

### gRPC

默认地址：http://127.0.0.1:50051，接口定义见 Saturn UI/Protos/chat.proto。

### OpenAI 兼容服务

默认 Base URL：https://api.openai.com/v1，请求路径：/chat/completions。也可连接 vLLM、LM Studio、LiteLLM 等兼容服务。

## 本地数据

Windows 默认数据目录：

~~~text
%LOCALAPPDATA%/SaturnUI/
├─ settings.json
├─ SaturnUI.db
└─ Images/
~~~

## 主题

主题由 ThemeColors.axaml、ThemeStyles.axaml 与 ThemeService.cs 共同驱动。当前包含：DeepSpace、Daylight、StarryBlue、AuroraPurple、GlacierCyan、NebulaPink、MidnightIndigo。

## 开发提示

- 新增 provider：在 Saturn UI/Services/Chat/ 实现 IChatProvider，并在 ChatService 中路由。
- 新增主题：在 ThemeColors.axaml 添加 ResourceDictionary，并同步 ThemeDefinitions.ThemeKeys。
- UI 改动需保留 ViewModel 绑定契约，尤其是消息、会话、设置和弹窗保存/取消命令。

## 许可证

见 Apache License 2.0：LICENSE。
