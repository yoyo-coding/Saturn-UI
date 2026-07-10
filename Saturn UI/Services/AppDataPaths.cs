using System;
using System.IO;

namespace SaturnUI.Services;

/// <summary>
/// ???? Saturn UI ?????????????????????
/// </summary>
public static class AppDataPaths
{
    public static string ResolveDataDirectory(string? overrideDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            Directory.CreateDirectory(overrideDirectory);
            return overrideDirectory;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, AppConstants.AppFolderName);
        Directory.CreateDirectory(dir);
        return dir;
    }
}
