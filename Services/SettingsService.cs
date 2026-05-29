using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        "Mist");

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
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            Normalize(settings);
            Save(settings);
            return settings;
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
        Normalize(settings);
        settings.LastOpenedUtc = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static void Normalize(AppSettings settings)
    {
        var defaults = new AppSettings();

        foreach (var processName in defaults.ProtectedProcesses)
        {
            if (!settings.ProtectedProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                settings.ProtectedProcesses.Add(processName);
            }
        }

        settings.RamCleanerProcessSelections = settings.RamCleanerProcessSelections is null
            ? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, bool>(settings.RamCleanerProcessSelections, StringComparer.OrdinalIgnoreCase);

        settings.SwuabValues = settings.SwuabValues is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(settings.SwuabValues, StringComparer.OrdinalIgnoreCase);
    }
}
