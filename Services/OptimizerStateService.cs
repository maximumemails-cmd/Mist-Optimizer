using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class OptimizerStateService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string StatePath { get; }

    public OptimizerStateService()
    {
        var folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Mist");

        Directory.CreateDirectory(folder);
        StatePath = Path.Combine(folder, "optimizer-state.json");
    }

    public IReadOnlyList<OptimizerStateEntry> Load()
    {
        if (!File.Exists(StatePath))
        {
            return Array.Empty<OptimizerStateEntry>();
        }

        try
        {
            var json = File.ReadAllText(StatePath);
            return JsonSerializer.Deserialize<List<OptimizerStateEntry>>(json, _jsonOptions) ?? [];
        }
        catch
        {
            return Array.Empty<OptimizerStateEntry>();
        }
    }

    public void SaveApplied(OptimizerStateEntry entry)
    {
        var entries = Load().Where(item => item.OptimizationId != entry.OptimizationId).ToList();
        entries.Add(entry);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(entries, _jsonOptions));
    }

    public OptimizerStateEntry? Find(string optimizationId)
    {
        return Load().FirstOrDefault(item => item.OptimizationId == optimizationId);
    }
}
