# Saturn UI（灵星 UI）

> 一个基于 Avalonia 与 .NET 的本地优先 AI 对话客户端：轻量、原生、可扩展，支持本地后端与 OpenAI 兼容服务。

Saturn UI 是一款跨平台桌面 AI 对话前端。它不内置模型或推理服务，而是作为统一的聊天客户端连接到本地后端、gRPC 服务或 OpenAI 兼容 API。项目采用 MVVM 架构、LiteDB 本地存储和 Material 3 风格主题系统，适合作为本地 LLM、Agent 或在线模型服务的桌面入口。

## 当前状态

项目当前已经具备可运行的桌面端基础能力：

- ✅ Avalonia 桌面应用与 MVVM 架构
- ✅ 会话列表、消息流式输出、停止生成
- ✅ LiteDB 本地会话与消息持久化
- ✅ 本地 HTTP / gRPC 后端接入
- ✅ OpenAI Chat Completions 兼容接口接入
- ✅ Markdown 消息渲染与代码块样式
- ✅ 图片/文件拖拽与剪贴板粘贴的附件预览
- ✅ 多主题切换与低饱和度主题色混合配色

> 说明：附件目前主要用于界面展示、预览与本地记录；当前聊天请求主体仍以文本消息为主，尚未把附件内容统一上传给模型服务。

## 主要功能

### AI 对话

- 用户/助手消息气泡展示
- 流式响应展示（SSE 风格 token 增量输出）
- 可中断生成
- 错误消息独立展示
- 自动根据首条用户消息生成会话标题
- Markdown 渲染，适合展示列表、代码块、引用等内容

### 多后端接入

Saturn UI 支持两类提供商：

| 提供商 | 用途 | 当前实现 |
| --- | --- | --- |
| 本地 | 连接本地 LLM / Agent 服务 | HTTP、gRPC |
| 在线 | 连接 OpenAI 兼容服务 | `/chat/completions` |

#### 本地 HTTP 接口

默认地址：`http://127.0.0.1:8000`

请求路径：

```http
POST /chat
Content-Type: application/json
```

请求体：

```json
{
  "message": "你好",
  "conversation_id": "optional-session-id",
  "stream": true
}
```

非流式响应示例：

```json
{
  "content": "你好，有什么可以帮你？",
  "done": true
}
```

流式响应使用 SSE 风格，每行以 `data:` 开头：

```text
data: {"token":"你好"}
data: {"token":"，"}
data: {"token":"有什么可以帮你？"}
data: {"done":true}
```

错误响应可返回：

```json
{
  "error": "错误信息"
}
```

#### 本地 gRPC 接口

默认地址：`http://127.0.0.1:50051`

协议文件位于：`Saturn UI/Protos/chat.proto`

```proto
service ChatService {
  rpc Chat (ChatRequest) returns (stream ChatReply);
}
```

#### OpenAI 兼容接口

在线提供商使用 OpenAI Chat Completions 兼容格式：

- 默认 Base URL：`https://api.openai.com/v1`
- 请求路径：`/chat/completions`
- 鉴权：`Authorization: Bearer <API_KEY>`
- 支持配置：API Key、模型名、Base URL、Temperature、Max Tokens

因此除了 OpenAI 官方服务，也可以接入兼容 OpenAI API 的服务，例如本地网关、vLLM、LM Studio、LiteLLM 或其它兼容实现。

## 主题与视觉设计

Saturn UI 使用自定义 Material 3 风格主题令牌，并基于 Avalonia FluentTheme 扩展控件样式。

当前内置主题：

- `DeepSpace`：深空黑（默认）
- `Daylight`：极昼白
- `StarryBlue`：星夜蓝
- `AuroraPurple`：极光紫
- `GlacierCyan`：冰川青
- `NebulaPink`：星云粉
- `MidnightIndigo`：午夜靛蓝

配色策略：

- 大面积背景以黑/白为可读性基础。
- 在背景、卡片、AI 气泡、代码块等区域低比例混入当前主题色。
- 按钮、用户气泡、选中态等交互元素保留更明确的主题强调色。
- 亮色/暗色主题会分别使用 Avalonia 的 `ThemeVariant.Light` / `ThemeVariant.Dark`。

这样可以避免高饱和主题把整个界面染得过于明艳，同时保留主题个性。

## 技术栈

| 分类 | 技术 |
| --- | --- |
| UI 框架 | Avalonia UI 11.3.18 |
| 运行时 | .NET 10 |
| 架构 | MVVM |
| MVVM 工具 | CommunityToolkit.Mvvm |
| 主题 | Avalonia FluentTheme + 自定义 Material 3 令牌 |
| 图标 | Material.Icons.Avalonia |
| Markdown | Markdown.Avalonia |
| 本地存储 | LiteDB |
| 网络通信 | HttpClient、SSE 风格流式响应、gRPC |
| 依赖注入 | Microsoft.Extensions.DependencyInjection |
| 配置 | System.Text.Json / 本地 settings.json |

## 项目结构

```text
Saturn-UI/
├─ Saturn UI.sln
├─ README.md
├─ LICENSE
├─ Plan.md
├─ Implementation.md
└─ Saturn UI/
   ├─ App.axaml / App.axaml.cs          # 应用入口、全局资源、依赖注入
   ├─ Program.cs                        # 桌面启动入口
   ├─ Saturn UI.csproj                  # 项目文件与 NuGet 依赖
   ├─ Controls/                         # RippleButton、SelectableItem 等自定义控件
   ├─ Converters/                       # Avalonia 数据绑定转换器
   ├─ Models/                           # 会话、消息、请求/响应模型
   ├─ Protos/                           # gRPC 协议定义
   ├─ Services/                         # 主题、设置、存储、聊天服务
   │  └─ Chat/                          # HTTP / gRPC / OpenAI 提供商实现
   ├─ Themes/                           # 主题色、控件样式、图标资源
   ├─ ViewModels/                       # Main / Chat / SessionList / Settings VM
   └─ Views/                            # Avalonia 视图与窗口
```

## 运行要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows / macOS / Linux 桌面环境
- 可选：本地 LLM / Agent 后端，或 OpenAI 兼容 API 服务

## 构建与运行

在仓库根目录执行：

```bash
# 还原依赖
dotnet restore "Saturn UI.sln"

# 构建
dotnet build "Saturn UI.sln"

# 运行桌面应用
dotnet run --project "Saturn UI/Saturn UI.csproj"
```

也可以使用 Visual Studio / Rider 打开 `Saturn UI.sln` 后直接运行。

## 快速配置

首次启动后进入设置页：

1. 选择提供商：
   - `本地`：使用本地 HTTP 或 gRPC 服务。
   - `在线`：使用 OpenAI 兼容服务。
2. 本地模式：
   - 设置 HTTP 地址，例如 `http://127.0.0.1:8000`。
   - 或设置 gRPC 地址，例如 `http://127.0.0.1:50051`。
3. 在线模式：
   - 设置 API Key。
   - 设置模型名，例如你的服务商或本地兼容服务支持的模型名。
   - 设置 Base URL，例如 `https://api.openai.com/v1` 或本地兼容服务地址。
4. 保存设置后开始新会话。

## 本地数据与配置

Saturn UI 会在用户本地应用数据目录下保存配置与会话数据。

Windows 默认路径示例：

```text
%LOCALAPPDATA%/SaturnUI/
├─ settings.json      # 应用设置
├─ SaturnUI.db        # LiteDB 会话与消息数据
└─ Images/            # 粘贴图片缓存
```

配置项包括：

- 本地 HTTP 地址
- gRPC 地址
- 当前提供商
- OpenAI 兼容 API 配置
- 主题
- 字号
- 性能模式开关

> 注意：API Key 当前保存在本地 `settings.json` 中。请不要把该文件提交到公共仓库或分享给他人。

## 开发说明

### 添加新的聊天提供商

1. 在 `Saturn UI/Services/Chat/` 中实现 `IChatProvider`。
2. 在 `ChatService` 构造函数中注册新的 provider。
3. 在设置界面中增加相应配置项。

### 添加新主题

1. 在 `Saturn UI/Themes/ThemeColors.axaml` 中新增主题 ResourceDictionary。
2. 在 `ThemeDefinitions.ThemeKeys` 中加入主题 key。
3. 如果是亮色主题，把 key 加入 `ThemeDefinitions.LightThemeKeys`。
4. 根据需要补充显示名称与图标适配。

### UI 样式入口

- 全局主题色：`Saturn UI/Themes/ThemeColors.axaml`
- 控件样式：`Saturn UI/Themes/ThemeStyles.axaml`
- 运行时主题应用：`Saturn UI/Services/ThemeService.cs`

## 已知限制

- 当前项目是 AI 对话前端，不包含模型推理服务。
- 附件尚未作为多模态输入统一发送给后端模型。
- 本地 HTTP 流式响应采用 SSE 风格文本流；后端需要按约定输出 `data:` 行。
- UI 中部分未来协议入口可能仍处于规划/占位阶段，当前实际稳定实现以 HTTP、gRPC 与 OpenAI 兼容接口为主。
- 项目目前以桌面端为主，移动端适配不在当前工程内。

## 许可证

本项目使用 [Apache License 2.0](LICENSE)。
