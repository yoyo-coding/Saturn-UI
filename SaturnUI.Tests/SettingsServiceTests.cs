using System.Text.Json;
using SaturnUI;
using SaturnUI.Services;

namespace SaturnUI.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void LoadNormalizesInvalidSettings()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new AppSettings
        {
            HttpBaseUrl = "",
            GrpcAddress = "",
            Protocol = "BAD",
            Theme = "MissingTheme",
            FontSize = 99,
            Provider = "unknown",
            OpenAiModel = "",
            OpenAiBaseUrl = "",
            OpenAiTemperature = 99,
            OpenAiMaxTokens = -1
        }));

        var service = new SettingsService(temp.Path);

        Assert.Equal(AppConstants.DefaultHttpBaseUrl, service.Settings.HttpBaseUrl);
        Assert.Equal(AppConstants.DefaultGrpcAddress, service.Settings.GrpcAddress);
        Assert.Equal(AppConstants.ProtocolHttp, service.Settings.Protocol);
        Assert.Equal(AppConstants.DefaultTheme, service.Settings.Theme);
        Assert.Equal(AppConstants.DefaultFontSize, service.Settings.FontSize);
        Assert.Equal(AppConstants.ProviderLocal, service.Settings.Provider);
        Assert.Equal(AppConstants.DefaultOpenAiModel, service.Settings.OpenAiModel);
        Assert.Equal(AppConstants.DefaultOpenAiBaseUrl, service.Settings.OpenAiBaseUrl);
        Assert.Equal(2, service.Settings.OpenAiTemperature);
        Assert.Equal(1, service.Settings.OpenAiMaxTokens);
    }

    [Fact]
    public void SaveAndReloadPersistsSettingsAtomically()
    {
        using var temp = new TempDirectory();
        var service = new SettingsService(temp.Path);

        service.Update(settings =>
        {
            settings.Theme = "AuroraPurple";
            settings.Provider = AppConstants.ProviderOnline;
            settings.OpenAiModel = "custom-model";
        });

        var reloaded = new SettingsService(temp.Path);

        Assert.Equal("AuroraPurple", reloaded.Settings.Theme);
        Assert.Equal(AppConstants.ProviderOnline, reloaded.Settings.Provider);
        Assert.Equal("custom-model", reloaded.Settings.OpenAiModel);
    }
}
