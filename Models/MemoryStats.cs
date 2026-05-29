using System;

namespace PCOptimizer.Models;

public sealed class MemoryStats
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long AvailableBytes { get; init; }
    public long CachedBytes { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public double UsagePercent => TotalBytes <= 0 ? 0 : UsedBytes / (double)TotalBytes;
    public string TotalDisplay => FormatBytes(TotalBytes);
    public string UsedDisplay => FormatBytes(UsedBytes);
    public string AvailableDisplay => FormatBytes(AvailableBytes);
    public string CachedDisplay => FormatBytes(CachedBytes);
    public string UsagePercentDisplay => TotalBytes <= 0 ? "Unknown" : $"{UsagePercent:P0}";
    public string TimestampDisplay => Timestamp.ToString("HH:mm:ss");

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            return "Unknown";
        }

        if (bytes == 0)
        {
            return "0 MB";
        }

        var gigabytes = bytes / 1024d / 1024d / 1024d;

        if (gigabytes >= 1)
        {
            return $"{gigabytes:N1} GB";
        }

        return $"{bytes / 1024d / 1024d:N0} MB";
    }
}
