using System;

namespace PCOptimizer.Models;

public sealed class StorageDriveInfo
{
    public string Name { get; init; } = "Unknown";
    public string Label { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public long TotalBytes { get; init; }
    public long FreeBytes { get; init; }
    public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);
    public double UsagePercent => TotalBytes <= 0 ? 0 : UsedBytes / (double)TotalBytes;
    public string TotalDisplay => MemoryStats.FormatBytes(TotalBytes);
    public string FreeDisplay => MemoryStats.FormatBytes(FreeBytes);
    public string UsedDisplay => MemoryStats.FormatBytes(UsedBytes);
    public string UsagePercentDisplay => TotalBytes <= 0 ? "Unknown" : $"{UsagePercent:P0}";
    public string Title => string.IsNullOrWhiteSpace(Label) ? Name : $"{Name} {Label}";
}
