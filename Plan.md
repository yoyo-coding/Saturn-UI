# Saturn UI（灵星 UI）项目策划书

## 一、项目概述

### 项目名称

- **中文名**：灵星 UI
- **英文名**：Saturn UI

### 项目定位

Saturn UI 是一款基于 **Avalonia UI** 开发的轻量化 AI 对话前端系统，专注于：

- 高性能 AI 对话展示
- 跨平台统一体验
- 多协议后端兼容
- 极简、现代化 UI
- 本地 AI Agent 接入

本项目不承担 AI 推理任务，仅作为 AI Agent 的前端交互层，负责：

- 接收后端返回数据
- 管理会话与消息流
- 展示 AI 响应内容
- 提供用户交互能力

## 二、项目目标

### 核心目标

打造一个：

- **轻量**
- **高扩展**
- **跨平台**
- **本地优先**
- **现代化设计**
- **易于二次开发**

的 AI 对话 UI 框架。

## 三、项目特性

### 1. 轻量化设计

**目标：**

- 安装包体积小
- 内存占用低
- 启动速度快
- 低资源消耗

**方案：**

- Avalonia Fluent Design + 自定义控件
- 避免大型冗余依赖
- 按需加载模块
- 图片/动画资源最小化
- 支持 AOT 编译与裁剪（Trimming）进一步缩减体积

### 2. 跨平台支持

基于 Avalonia 实现：

| 平台支持情况 | 状态 |
|------------|------|
| Windows    | ✅ 官方支持 |
| Linux      | ✅ 官方支持 |
| macOS      | ✅ 官方支持 |
| Android    | ✅ 11.x 起支持 |
| iOS        | ✅ 11.x 起支持 |
| Web        | ❌ 不支持（非浏览器套壳） |

> **说明**：Avalonia 使用 Skia 自绘引擎，在各平台保持像素级一致的 UI 表现，不依赖原生控件映射，也不使用 WebView。

### 3. 本地 AI Agent 接入

Saturn UI 默认连接本地 AI Agent 服务。

**默认后端地址：**

- HTTP：`http://127.0.0.1:8000`
- gRPC：`127.0.0.1:50051`

**要求：**

- 后端仅监听本地回环地址（localhost / 127.0.0.1）
- 禁止默认暴露公网
- 提升隐私与安全性

### 4. 多协议兼容

| 支持协议 | 协议用途 |
|---------|---------|
| HTTP    | 普通请求 |
| SSE     | 流式输出 |
| WebSocket | 实时会话 |
| gRPC    | 高性能通信 |

### 5. 流式 AI 输出

**支持：**

- Token Streaming
- 打字机效果
- 中途停止生成
- 实时状态显示

**效果：**

```
AI 正在思考中...
▌
```

## 四、技术架构

### 总体架构

```
┌──────────────────────┐
│      Saturn UI          │
│  Avalonia Frontend   │
│  C# / .NET 9         │
└─────────┬────────────┘
          │
 ┌────────┴────────┐
 │                 │
 HTTP API        gRPC API
 │                 │
 └────────┬────────┘
          │
┌──────────────────────┐
│     AI Agent Core    │
│  (Python/C++/Rust)   │
└──────────────────────┘
```

### 渲染架构

Avalonia 采用 **Skia 自绘引擎**，不依赖平台原生控件，也不使用 WebView：

- **Windows**：Direct2D / Skia 渲染
- **macOS**：Metal / Skia 渲染
- **Linux**：X11 / Wayland / Skia 渲染
- **Android / iOS**：Skia 通过平台 GPU API 直接绘制

这保证了所有平台拥有一致的视觉表现和流畅的动画性能。

## 五、前端模块设计

### 1. 会话模块

**功能：**

- 新建会话
- 删除会话
- 历史记录
- 多会话切换
- 会话搜索

### 2. 消息模块

**支持：**

- Markdown
- 代码高亮
- LaTeX 数学公式
- 图片消息
- 文件消息
- 流式消息

### 3. 输入模块

**功能：**

- 多行输入
- 快捷发送
- Shift+Enter 换行
- 文件拖拽
- 图片粘贴

### 4. 设置模块

**支持：**

- 后端地址配置
- 协议切换
- 主题切换（Fluent Light / Fluent Dark / 自定义）
- 字体大小
- 性能模式

### 5. 插件系统（后期）

**支持：**

- UI 插件
- Tool 面板
- 第三方扩展
- 自定义主题

## 六、UI 设计方向

### 设计理念

**关键词：**

- 星系感
- 极简
- 半透明
- 科技感
- 呼吸感动画

### 色彩风格

**主色：**

- 深空黑
- 星蓝
- 冷白

**辅助色：**

- 极光紫
- 冰川青

### UI 风格参考

可参考：

- Discord
- Telegram Desktop
- Linear
- Notion
- ChatGPT

### 主题实现

Avalonia 内置 **Fluent Design System** 主题，支持：

- 默认 Light / Dark 模式
- 自定义 Acrylic / Mica 背景效果（Windows）
- 圆角、阴影、Reveal 聚焦动画
- 高对比度主题支持

如需 Material Design 风格，可通过社区包 `Material.Avalonia` 扩展。

## 七、通信协议设计

### HTTP API 示例

**请求：**

```http
POST /chat
Content-Type: application/json

{
  "message": "你好",
  "conversation_id": "abc123"
}
```

**返回：**

```json
{
  "content": "你好，请问有什么可以帮助你？"
}
```

### SSE 流式示例

```
data: {"token":"你"}

data: {"token":"好"}

data: {"done":true}
```

### gRPC 服务示例

```protobuf
service ChatService {
  rpc Chat(ChatRequest) returns (stream ChatReply);
}
```

## 八、性能目标

### 指标目标

| 指标 | 目标 |
|------|------|
| 冷启动   | < 1.5 秒（桌面） / < 2 秒（移动端） |
| 内存占用 | < 200MB（桌面） / < 150MB（移动端） |
| 流式延迟 | < 100ms  |
| UI 帧率  | 60FPS（支持 VSync） |
| 包体积   | < 60MB（桌面单文件 AOT） |

> **说明**：Avalonia 支持 .NET AOT（Ahead-of-Time）编译与 IL 裁剪，可显著降低启动时间和包体积。

## 九、安全设计

### 本地优先原则

**默认：**

- 仅允许 localhost
- 不上传用户数据
- 不依赖云端

### 安全措施

- API 地址白名单
- 本地 Token 鉴权
- 会话隔离
- 沙箱文件访问

## 十、项目目录结构建议

```
SaturnUI/
├── SaturnUI/                          ← 主项目（共享逻辑与 UI）
│   ├── App.axaml                   ← 应用入口与全局主题
│   ├── App.axaml.cs
│   ├── Views/                      ← 页面视图（.axaml）
│   │   ├── MainWindow.axaml
│   │   ├── MainView.axaml
│   │   ├── ChatView.axaml
│   │   ├── SettingsView.axaml
│   │   └── SessionListView.axaml
│   ├── ViewModels/                 ← MVVM 视图模型
│   │   ├── MainViewModel.cs
│   │   ├── ChatViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Models/                     ← 数据模型
│   │   ├── Message.cs
│   │   ├── Session.cs
│   │   └── ChatRequest.cs
│   ├── Services/                   ← 业务服务层
│   │   ├── ChatService.cs
│   │   ├── HttpClientService.cs
│   │   ├── GrpcClientService.cs
│   │   └── LocalStorageService.cs
│   ├── Converters/                 ← 数据绑定转换器
│   ├── Themes/                     ← 自定义主题与样式
│   │   ├── LynTheme.axaml
│   │   └── Colors.axaml
│   └── Assets/                     ← 图片、字体、图标
│
├── SaturnUI.Desktop/                  ← 桌面端入口（Windows/macOS/Linux）
│   ├── Program.cs
│   └── SaturnUI.Desktop.csproj
│
├── SaturnUI.Android/                  ← Android 入口
│   ├── MainActivity.cs
│   └── SaturnUI.Android.csproj
│
├── SaturnUI.iOS/                      ← iOS 入口
│   ├── AppDelegate.cs
│   └── SaturnUI.iOS.csproj
│
└── SaturnUI.sln
```

## 十一、推荐技术栈

### .NET / Avalonia 依赖建议

| 功能 | 推荐技术 | 包名 |
|------|---------|------|
| UI 框架 | Avalonia UI | `Avalonia` |
| 主题 | Fluent Design（官方） | `Avalonia.Themes.Fluent` |
| 控件扩展 | Avalonia.Controls | 内置 |
| 状态管理 | MVVM + CommunityToolkit.Mvvm | `CommunityToolkit.Mvvm` |
| 网络请求 | HttpClient + System.Net.Http | 内置 |
| gRPC | gRPC for .NET | `Grpc.Net.Client` |
| SSE | 自定义 / `System.Net.Http` | 内置 |
| Markdown | Markdown.Avalonia | `Markdown.Avalonia` |
| 代码高亮 | AvaloniaEdit | `AvaloniaEdit` |
| 动画 | Avalonia.Animation | 内置 |
| 本地存储 | LiteDB / SQLite | `LiteDB` / `Microsoft.Data.Sqlite` |
| 配置管理 | `Microsoft.Extensions.Configuration` | 内置 |
| 依赖注入 | `Microsoft.Extensions.DependencyInjection` | 内置 |
| JSON 序列化 | `System.Text.Json` | 内置 |
| 日志 | `Microsoft.Extensions.Logging` | 内置 |

### 可选增强包

| 功能 | 包名 |
|------|------|
| Material Design 主题 | `Material.Avalonia` |
| 图标库 | `Material.Icons.Avalonia` |
| 消息弹窗 | `Avalonia.Controls.Notifications` |
| 文件对话框 | `Avalonia.Dialogs` |
| 托盘图标 | `Avalonia.TrayIcon` |

## 十二、开发阶段规划

### Phase 1：核心原型

**预计：** 2~3 周

**内容：**

- Avalonia 项目搭建与跨平台配置
- 基础聊天 UI（Fluent Design）
- HTTP 通信（HttpClient）
- SSE 流式输出
- 会话系统与本地存储（LiteDB）

### Phase 2：完整功能

**预计：** 3~5 周

**内容：**

- gRPC 支持（Grpc.Net.Client）
- Markdown 渲染（Markdown.Avalonia）
- 代码高亮（AvaloniaEdit）
- 文件消息与拖拽
- 设置页面与主题切换
- Android / iOS 适配

### Phase 3：优化与扩展

**预计：** 长期维护

**内容：**

- AOT 编译与包体积优化
- 插件系统（Assembly 动态加载）
- 多主题与自定义 Acrylic 效果
- GPU 加速动画
- 多窗口支持（桌面端）

## 十三、未来扩展方向

### 可扩展能力

未来可接入：

- 本地 LLM
- MCP Tool
- 多 Agent
- 语音对话
- OCR
- 本地知识库
- RAG 系统

## 十四、项目优势

### 相比传统 AI 客户端

Saturn UI 的特点：

- 更轻量（Avalonia + .NET 裁剪）
- 更开放（开源 .NET 生态）
- 更适合本地 AI（C# 高性能网络层）
- 更适合作为 Agent 前端（跨平台原生渲染）
- 更易于二次开发（MVVM 架构，Visual Studio / Rider 生态）
- 更低资源占用（AOT 编译后无 JIT 开销）
- 桌面端体验更优（Skia 自绘，非 WebView 套壳）

### 相比 Flutter 版本的优势

| 维度 | Avalonia 版本 | Flutter 版本 |
|------|-------------|-------------|
| 桌面启动速度 | ✅ 更快（AOT） | ⚠️ 需 Dart VM 预热 |
| 桌面内存占用 | ✅ 更低 | ⚠️ 较高 |
| 原生系统集成 | ✅ 更深（.NET 互操作） | ⚠️ 需 Platform Channel |
| 桌面端 UI 一致性 | ✅ 像素级一致 | ✅ 一致 |
| 移动端成熟度 | ⚠️ 较新（11.x） | ✅ 成熟 |
| Linux 支持 | ✅ 官方一等公民 | ✅ 支持 |
| 开发工具链 | ✅ Rider / VS 顶级支持 | ⚠️ 依赖 Android Studio |

## 十五、项目愿景

Saturn UI 希望成为：

> "一个真正轻量、优雅、开放的本地 AI 交互前端。"

让开发者可以：

- 快速接入任意 AI Agent
- 自定义自己的 AI 工作流
- 在本地安全运行 AI 系统
- 获得接近原生应用的交互体验
- 在桌面端获得顶级的性能与集成体验

## 十六、项目口号（可选）

**中文：**

- 灵感如星，交互如流
- 让 AI 对话回归纯粹
- 轻若流星，快若思维

**英文：**

- Lightweight AI Interface
- Local First. Fast Always.
- Elegant UI for Intelligent Agents.
- Native Feel. Cross Platform. Local Only.
