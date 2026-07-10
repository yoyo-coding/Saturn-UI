namespace SaturnUI;

/// <summary>
/// ??????????????????????? UI / Service ?????????
/// </summary>
public static class AppConstants
{
    public const string AppFolderName = "SaturnUI";

    public const string ProviderLocal = "本地";
    public const string ProviderOnline = "在线";

    public const string ProtocolHttp = "HTTP";
    public const string ProtocolGrpc = "gRPC";

    public const string DefaultTheme = "DeepSpace";
    public const string DefaultSessionTitle = "新会话";

    public const string DefaultHttpBaseUrl = "http://127.0.0.1:8000";
    public const string DefaultGrpcAddress = "http://127.0.0.1:50051";
    public const string DefaultOpenAiBaseUrl = "https://api.openai.com/v1";
    public const string DefaultOpenAiModel = "gpt-3.5-turbo";

    public const double DefaultFontSize = 14;
    public const double DefaultTemperature = 0.7;
    public const int DefaultMaxTokens = 2048;
}
