# Saturn UI 实施计划

## 当前状态
- Avalonia MVVM 项目已搭建完成，支持 HTTP/SSE/gRPC 通信与会话管理
- 构建通过，基础聊天 UI 可运行
- 第二阶段核心功能已实现

## 实施阶段

### Phase 1：核心原型 ✅ 已完成

**目标**：可运行的基础聊天客户端，支持 HTTP/SSE 通信和会话管理。

| 步骤 | 内容 | 文件/目录 | 状态 |
|------|------|-----------|------|
| 1.1 | 重构项目为 Avalonia MVVM 应用 | `Saturn UI.csproj`, `Program.cs` | ✅ |
| 1.2 | 创建应用入口与全局资源 | `App.axaml`, `App.axaml.cs` | ✅ |
| 1.3 | 创建数据模型 | `Models/Session.cs`, `Models/Message.cs` | ✅ |
| 1.4 | 创建核心服务 | `Services/ChatService.cs`, `Services/LocalStorageService.cs`, `Services/SettingsService.cs` | ✅ |
| 1.5 | 创建 ViewModels | `ViewModels/MainViewModel.cs`, `ViewModels/ChatViewModel.cs`, `ViewModels/SessionListViewModel.cs` | ✅ |
| 1.6 | 创建 Views | `Views/MainWindow.axaml`, `Views/MainView.axaml`, `Views/ChatView.axaml`, `Views/SessionListView.axaml` | ✅ |
| 1.7 | 实现 HTTP + SSE 通信 | `Services/ChatService.cs` | ✅ |
| 1.8 | 实现 LiteDB 本地存储 | `Services/LocalStorageService.cs` | ✅ |
| 1.9 | 实现基础设置 | `Services/SettingsService.cs`, `Views/SettingsView.axaml` | ✅ |
| 1.10 | 验证构建运行 | 测试聊天、会话切换、流式输出 | ✅ |

**补全记录**：
- 2026-05-31：补充 `LynError` 动态资源（`#EF4444`）至 `App.axaml` 与 `Themes/Colors.axaml`

### Phase 2：完整功能 ✅ 核心已完成

**目标**：增强聊天体验，支持富文本、文件交互与多协议通信。

| 步骤 | 内容 | 文件/目录 | 状态 |
|------|------|-----------|------|
| 2.1 | 主题切换与自定义样式 | `Themes/Theme.axaml`, `App.axaml.cs` | ✅ |
| 2.2 | Markdown 渲染 + 代码高亮 | `Views/ChatView.axaml` (Markdown.Avalonia) | ✅ |
| 2.3 | 文件拖拽与图片粘贴 | `Views/ChatView.axaml`, `Views/ChatView.axaml.cs` | ✅ |
| 2.4 | gRPC 支持 | `Services/ChatService.cs`, `Protos/chat.proto` | ✅ |
| 2.5 | Android/iOS 适配 | 新增 `SaturnUI.Android/`, `SaturnUI.iOS/` 项目 | ⏳ 后续迭代 |

**Phase 2 实现详情**：

#### 2.1 主题切换与自定义样式
- 完善 `Themes/Theme.axaml`，添加 Light/Dark 双主题字典
- 新增主题色资源：`LynCodeBackgroundBrush`、`LynCodeBorderBrush`、`LynInlineCodeBackgroundBrush`、`LynSelectionBrush`
- 主题切换逻辑已在 `App.axaml.cs` 的 `ApplyTheme` 方法中实现
- 设置页面支持 Dark/Light 主题选择

#### 2.2 Markdown 渲染 + 代码高亮
- 已集成 `Markdown.Avalonia` 包（v11.0.2）
- `ChatView.axaml` 中 Assistant 消息使用 `MarkdownScrollViewer` 渲染
- 用户消息保持纯文本显示
- 支持 Markdown 语法：标题、列表、代码块、粗体/斜体等

#### 2.3 文件拖拽与图片粘贴
- **文件拖拽**：`ChatView.axaml.cs` 实现 `OnDragOver` / `OnDrop` 处理文件拖拽
- **图片粘贴**：支持 Ctrl+V 粘贴剪贴板中的图片（自动保存到本地 `Images` 目录）
- **图片预览**：消息中图片附件直接显示 `Image` 控件
- **文件附件**：非图片文件显示文件名图标
- `Message` 模型支持 `AttachmentPath`、`AttachmentName`、`HasAttachment`、`IsImageAttachment`

#### 2.4 gRPC 支持
- 已配置 `Grpc.Net.Client`、`Grpc.Tools`、`Google.Protobuf`
- `Protos/chat.proto` 定义流式 Chat 服务
- `ChatService.cs` 同时支持 HTTP/SSE 和 gRPC 协议（通过设置切换）
- 修复了 `ChatService` 类名与 proto 生成客户端的命名冲突

### Phase 3：优化与扩展（长期）
- AOT 编译优化
- 插件系统
- GPU 动画
- 多窗口支持

## 技术决策
- **UI 框架**：Avalonia 11.x + Fluent Theme
- **MVVM 框架**：CommunityToolkit.Mvvm (Source Generators)
- **本地存储**：LiteDB（轻量、无服务器、嵌入式）
- **JSON**：System.Text.Json
- **配置**：Microsoft.Extensions.Configuration.Json
- **DI**：Microsoft.Extensions.DependencyInjection
- **HTTP/SSE**：HttpClient + 自定义 SseStreamReader
- **Markdown**：Markdown.Avalonia
- **代码高亮**：AvaloniaEdit（通过 Markdown.Avalonia 集成）
- **gRPC**：Grpc.Net.Client
