using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class AppLogger
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Info(string message) => Add("Info", message);

    public void Warning(string message) => Add("Warning", message);

    public void Error(string message) => Add("Error", message);

    public void Clear() => Entries.Clear();

    public string Copy()
    {
        return string.Join(Environment.NewLine, Entries.Select(entry => entry.DisplayText));
    }

    public string Save()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PCOptimizer",
            "Logs");

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"pcoptimizer-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(path, Copy());
        Info($"Log saved to {path}");

        return path;
    }

    private void Add(string level, string message)
    {
        var entry = new LogEntry(DateTime.Now, level, message);
        Entries.Add(entry);
        Console.WriteLine(entry.DisplayText);
    }
}
