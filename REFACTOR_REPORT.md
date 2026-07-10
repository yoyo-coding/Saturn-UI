# Saturn UI 项目重构报告

日期：2026-07-09  
工作区：F:\Saturn-UI

## 1. 重构目标

用户目标是“直接重写整个 UI”，并要求：

1. 保持 Material 3 设计风格。
2. 保留当前项目已有功能。
3. 优化整体性能。
4. 完成构建与测试。
5. 更新 README。
6. 生成项目重构报告。

## 2. UI 重写成果

本轮已直接重写 Avalonia UI 层，覆盖主要界面与弹窗：

| 文件 | 重写内容 |
| --- | --- |
| Saturn UI/Views/MainWindow.axaml | 新 Material 3 Shell、圆角会话栏、顶部 App Bar、设置抽屉、启动遮罩 |
| Saturn UI/Views/SessionListView.axaml | 品牌头部、FAB 新会话、搜索栏、M3 会话列表项 |
| Saturn UI/Views/ChatView.axaml | 欢迎卡片、左右消息气泡、AI 标识、Markdown 区、附件 chip、悬浮输入框 |
| Saturn UI/Views/SettingsView.axaml | 卡片化设置页：提供商、当前状态、主题、字体、性能模式 |
| Saturn UI/Views/LocalProviderWindow.axaml | 本地后端配置弹窗重写 |
| Saturn UI/Views/OnlineProviderWindow.axaml | OpenAI 兼容配置弹窗重写 |
| Saturn UI/Views/SplashWindow.axaml | 启动页视觉重写 |
| Saturn UI/Views/MainView.axaml | 单视图平台占位页重写 |
| Saturn UI/Themes/ThemeStyles.axaml | Material 3 typography、button、card、input、message、chip 样式体系 |

保留的绑定与交互包括：

- 会话搜索、新建、选择、删除。
- 聊天消息列表、用户/AI 消息区分、Markdown 渲染。
- 输入框双向绑定、Enter 发送、Ctrl+Enter 换行。
- 流式生成状态、停止生成按钮。
- 文件/图片附件显示、待发送附件清除。
- 设置页 provider、主题、字体大小、性能模式。
- 本地 provider 和 OpenAI provider 保存/取消按钮。
- 主窗口启动遮罩 SplashOverlay 与 SplashImage 代码契约。

## 3. Material 3 设计实现

- 采用圆角 Surface / Container 卡片组织页面信息。
- 顶部栏、设置抽屉、会话栏和输入框均使用 M3 surface hierarchy。
- 主操作使用 Filled / FilledTonal / FAB 按钮样式。
- 次级操作使用 Outlined / Icon 按钮样式。
- Typography 使用 Display / Headline / Title / Body / Label 分层。
- 消息气泡使用用户/AI 主题色令牌，并保留 Markdown 代码块样式。
- 启动页和主窗口遮罩统一品牌视觉。

## 4. 功能与性能重构

除 UI 重写外，项目中已完成以下功能性与性能改进：

- Message 流式内容使用缓冲刷新，降低高频 token 对 UI 属性变更的压力。
- SettingsService 支持设置规范化、测试数据目录与原子保存。
- LocalStorageService 拆分会话与消息持久化，降低大对象读写成本。
- ChatViewModel 修复发送命令 CanExecute、停止生成、流式完成 flush 与标题提升。
- MainViewModel 与 SessionListViewModel 在新会话创建后同步刷新侧栏。
- ChatView.axaml.cs 迁移到 Avalonia 11 DataTransfer / Clipboard extension API。
- OpenAiChatProvider 避免重复 [ERROR] 前缀。
- 修复可见中文常量和状态文案：本地/在线、新会话、就绪、生成中、完成、已取消。

## 5. 测试覆盖

新增测试项目：SaturnUI.Tests。

测试内容：

- MessageTests：流式缓冲、完成刷新、附件图片判断。
- SettingsServiceTests：默认值、规范化、保存加载。
- LocalStorageServiceTests：会话与消息持久化、附件字段保留。

## 6. 验证结果

已执行：

~~~text
dotnet build "Saturn UI.sln" -v:minimal
~~~

结果：

~~~text
0 warning, 0 error
~~~

已执行：

~~~text
dotnet test "Saturn UI.sln" -v:minimal --no-build
~~~

结果：

~~~text
Passed: 7
Failed: 0
Skipped: 0
Total: 7
~~~

## 7. 主要收益

- **视觉一致性**：主窗口、会话、聊天、设置与弹窗均统一到 Material 3 设计语言。
- **绑定兼容性**：保留原 ViewModel 和代码隐藏契约，避免功能断裂。
- **性能改善**：流式消息缓冲与 LiteDB 会话/消息拆分减少 UI 与存储压力。
- **可维护性**：主题样式、常量、存储路径、provider 路由和测试项目更清晰。
- **可测试性**：设置与存储服务可使用临时目录测试，减少对用户数据目录的依赖。

## 8. 后续建议

1. 为 Avalonia UI 增加截图级回归测试或自动化冒烟测试。
2. 为 HTTP / OpenAI provider 增加 mock HttpMessageHandler 单元测试。
3. 根据真实使用反馈继续细化空状态、长消息虚拟化与移动端单视图适配。
4. 将 provider 配置从硬编码弹窗进一步抽象为可扩展 provider settings schema。
