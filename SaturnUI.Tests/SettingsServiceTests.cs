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
            AccentColor = "not-a-color",
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
        Assert.Equal(AppConstants.DefaultAccentColor, service.Settings.AccentColor);
        Assert.Equal(AppConstants.DefaultUseLightTheme, service.Settings.UseLightTheme);
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
            settings.AccentColor = "#00aaff";
            settings.UseLightTheme = true;
            settings.Provider = AppConstants.ProviderOnline;
            settings.OpenAiModel = "custom-model";
        });

        var reloaded = new SettingsService(temp.Path);

        Assert.Equal("#00AAFF", reloaded.Settings.AccentColor);
        Assert.True(reloaded.Settings.UseLightTheme);
        Assert.Equal(AppConstants.ProviderOnline, reloaded.Settings.Provider);
        Assert.Equal("custom-model", reloaded.Settings.OpenAiModel);
    }
}

public class DynamicColorPaletteTests
{
    [Theory]
    [InlineData("#abc", "#AABBCC")]
    [InlineData("0x336699", "#336699")]
    [InlineData("80336699", "#336699")]
    [InlineData("nope", DynamicColorPalette.FallbackSeedColor)]
    public void NormalizeHexColorAcceptsCommonSeedFormats(string input, string expected)
    {
        Assert.Equal(expected, DynamicColorPalette.NormalizeHexColor(input));
    }

    [Fact]
    public void CreateDerivesDifferentLightAndDarkSurfaceColors()
    {
        var dark = DynamicColorPalette.Create("#6750A4", useLightTheme: false);
        var light = DynamicColorPalette.Create("#6750A4", useLightTheme: true);

        Assert.NotEqual(dark.Surface, light.Surface);
        Assert.NotEqual(dark.Primary, light.Primary);
        Assert.NotEqual(dark.PrimaryContainer, light.PrimaryContainer);
    }
}
