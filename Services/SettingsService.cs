using System;
using System.IO;
using System.Text.Json;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class SettingsService
{
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PCOptimizer");

    public string SettingsPath => Path.Combine(SettingsDirectory, SettingsFileName);

    public AppSettings Load()
    {
        Directory.CreateDirectory(SettingsDirectory);

        if (!File.Exists(SettingsPath))
        {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        settings.LastOpenedUtc = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
