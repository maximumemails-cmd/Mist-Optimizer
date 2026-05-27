using System;

namespace PCOptimizer.Models;

public sealed class LogEntry
{
    public LogEntry(DateTime timestamp, string level, string message)
    {
        Timestamp = timestamp;
        Level = level;
        Message = message;
    }

    public DateTime Timestamp { get; }
    public string Level { get; }
    public string Message { get; }

    public string DisplayText => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level}: {Message}";
}
