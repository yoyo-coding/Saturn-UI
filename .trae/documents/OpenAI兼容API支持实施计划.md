# OpenAI 兼容 API 支持实施计划

## 一、摘要

为 Saturn UI 聊天应用添加 OpenAI 兼容 API 调用支持，使用户可以连接 OpenAI 官方 API、Azure OpenAI、Ollama、vLLM、LM Studio 等任何兼容 OpenAI 格式的服务。

## 二、当前状态分析

### 现有架构
- **通信协议**: gRPC（protobuf）和 HTTP（自定义 JSON 格式）
- **设置项**: `HttpBaseUrl`、`GrpcAddress`、`Protocol`（HTTP/SSE/WebSocket/gRPC）
- **ChatService**: 根据 `Protocol` 选择 gRPC 或 HTTP 客户端
- **请求格式**: 自定义 `ChatRequest`（message, conversation_id, stream）
- **响应格式**: 自定义 `ChatResponse`（content, token, done, error）

### 需要改动
1. **AppSettings**: 新增 OpenAI 相关配置字段
2. **ChatService**: 新增 OpenAI API 调用逻辑
3. **SettingsView**: 新增 OpenAI 配置 UI
4. **SettingsViewModel**: 新增 OpenAI 配置属性

## 三、实施方案

### 步骤 1: 扩展 AppSettings 配置

**文件**: `Saturn UI/Services/SettingsService.cs`

新增字段：
```csharp
public string OpenAiApiKey { get; set; } = "";
public string OpenAiModel { get; set; } = "gpt-3.5-turbo";
public string OpenAiBaseUrl { get; set; } = "https://api.openai.com/v1";
public double OpenAiTemperature { get; set; } = 0.7;
public int OpenAiMaxTokens { get; set; } = 2048;
```

### 步骤 2: 创建 OpenAI API 模型

**新建文件**: `Saturn UI/Models/OpenAiModels.cs`

定义 OpenAI API 的请求/响应结构：

```csharp
// 请求模型
public class OpenAiChatRequest
{
    public string model { get; set; }
    public List<OpenAiMessage> messages { get; set; }
    public double temperature { get; set; }
    public int max_tokens { get; set; }
    public bool stream { get; set; }
}

public class OpenAiMessage
{
    public string role { get; set; }  // "system", "user", "assistant"
    public string content { get; set; }
}

// 响应模型（非流式）
public class OpenAiChatResponse
{
    public string id { get; set; }
    public OpenAiChoice[] choices { get; set; }
    public OpenAiUsage usage { get; set; }
}

public class OpenAiChoice
{
    public int index { get; set; }
    public OpenAiMessage message { get; set; }
    public string finish_reason { get; set; }
}

// 流式响应（SSE）
public class OpenAiStreamChunk
{
    public string id { get; set; }
    public OpenAiStreamChoice[] choices { get; set; }
}

public class OpenAiStreamChoice
{
    public int index { get; set; }
    public OpenAiDelta delta { get; set; }
    public string finish_reason { get; set; }
}

public class OpenAiDelta
{
    public string role { get; set; }
    public string content { get; set; }
}
```

### 步骤 3: 扩展 ChatService 添加 OpenAI 支持

**文件**: `Saturn UI/Services/ChatService.cs`

新增方法：
1. `SendMessageOpenAiAsync()` - 非流式调用
2. `SendMessageOpenAiStreamAsync()` - SSE 流式调用

关键逻辑：
- 将 `Message` 列表转换为 OpenAI `messages` 格式
- 添加 `Authorization: Bearer {ApiKey}` 头
- 解析 SSE 流式响应（`data: {...}` 格式）
- 处理 `[DONE]` 结束标记
- 错误处理（401 认证失败、429 限流、500 服务错误）

在现有 `SendMessageAsync` 和 `SendMessageStreamAsync` 中添加 `Protocol == "OpenAI"` 分支。

### 步骤 4: 更新 SettingsViewModel

**文件**: `Saturn UI/ViewModels/SettingsViewModel.cs`

新增属性：
```csharp
[ObservableProperty] private string _openAiApiKey = "";
[ObservableProperty] private string _openAiModel = "gpt-3.5-turbo";
[ObservableProperty] private string _openAiBaseUrl = "https://api.openai.com/v1";
[ObservableProperty] private double _openAiTemperature = 0.7;
[ObservableProperty] private int _openAiMaxTokens = 2048;
```

更新 `Load()`、`SaveSettings()`、`ResetSettings()` 方法。

### 步骤 5: 更新 SettingsView UI

**文件**: `Saturn UI/Views/SettingsView.axaml`

在"后端配置"区域新增 OpenAI 配置项：
1. OpenAI API Key（密码框）
2. OpenAI 模型（下拉框 + 自定义输入）
3. OpenAI Base URL（文本框）
4. Temperature（滑块 0-2）
5. Max Tokens（数字输入框）

在"通信协议"下拉框新增 "OpenAI" 选项。

### 步骤 6: 错误处理与验证

在 ChatService 中添加：
- API Key 为空时提示用户
- HTTP 401 → "认证失败，请检查 API Key"
- HTTP 429 → "请求过于频繁，请稍后重试"
- HTTP 500 → "服务端错误"
- 网络超时 → "连接超时，请检查网络"

## 四、OpenAI API 兼容性说明

### 支持的端点
- **Chat Completions**: `POST /v1/chat/completions`

### 支持的参数
| 参数 | 类型 | 说明 |
|------|------|------|
| model | string | 模型名称 |
| messages | array | 对话历史 |
| temperature | number | 温度 0-2 |
| max_tokens | integer | 最大 token 数 |
| stream | boolean | 是否流式输出 |

### 兼容的服务
- OpenAI 官方 API (api.openai.com)
- Azure OpenAI Service
- Ollama (localhost:11434)
- vLLM
- LM Studio
- 任何 OpenAI 兼容 API

## 五、验证步骤

1. **编译验证**: `dotnet build` 无错误
2. **UI 验证**: 设置页面显示 OpenAI 配置项
3. **功能验证**:
   - 配置 OpenAI API Key 和模型
   - 选择协议为 "OpenAI"
   - 发送消息并接收响应
   - 测试流式输出
4. **错误处理验证**:
   - 无效 API Key → 显示认证错误
   - 网络断开 → 显示连接错误
   - 模型不存在 → 显示模型错误

## 六、文件改动清单

| 文件 | 改动类型 |
|------|---------|
| `Services/SettingsService.cs` | 修改 - 新增 OpenAI 配置字段 |
| `Models/OpenAiModels.cs` | 新建 - OpenAI API 数据模型 |
| `Services/ChatService.cs` | 修改 - 新增 OpenAI 调用逻辑 |
| `ViewModels/SettingsViewModel.cs` | 修改 - 新增 OpenAI 属性 |
| `Views/SettingsView.axaml` | 修改 - 新增 OpenAI 配置 UI |

## 七、实施顺序

1. ✅ 创建 OpenAiModels.cs
2. ✅ 扩展 AppSettings
3. ✅ 更新 SettingsViewModel
4. ✅ 更新 SettingsView UI
5. ✅ 扩展 ChatService
6. ✅ 测试验证

---

**预计工作量**: 2-3 小时
**风险等级**: 低（纯客户端改动，不影响现有功能）
