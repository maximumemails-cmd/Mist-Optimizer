using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class ProcessService
{
    private static readonly string[] SystemProcessNames =
    [
        "system",
        "idle",
        "registry",
        "smss",
        "csrss",
        "wininit",
        "winlogon",
        "services",
        "lsass",
        "svchost",
        "dwm",
        "audiodg",
        "fontdrvhost",
        "kernel_task",
        "launchd",
        "WindowServer",
        "loginwindow"
    ];

    public IReadOnlyList<ProcessInfo> ListProcesses(IReadOnlyCollection<string> protectedNames)
    {
        var currentProcessId = Environment.ProcessId;

        return Process.GetProcesses()
            .Select(process => ToProcessInfo(process, currentProcessId, protectedNames))
            .Where(process => process is not null)
            .Select(process => process!)
            .OrderByDescending(process => process.MemoryBytes)
            .Take(80)
            .ToList();
    }

    public IReadOnlyList<ProcessInfo> BuildPreview(IEnumerable<ProcessInfo> processes)
    {
        foreach (var process in processes)
        {
            if (process.IsSystemProcess)
            {
                process.Reason = "System process protected.";
                process.IsSelectedForCleaning = false;
            }
            else if (process.IsProtected)
            {
                process.Reason = "Protected by user or default rules.";
                process.IsSelectedForCleaning = false;
            }
            else
            {
                process.Reason = "Could be closed manually after review.";
            }
        }

        return processes
            .Where(process => !process.IsSystemProcess && !process.IsProtected)
            .OrderByDescending(process => process.MemoryBytes)
            .ToList();
    }

    private static ProcessInfo? ToProcessInfo(Process process, int currentProcessId, IReadOnlyCollection<string> protectedNames)
    {
        try
        {
            var name = process.ProcessName;
            var isCurrentApp = process.Id == currentProcessId;
            var isSystem = process.Id <= 4 || SystemProcessNames.Contains(name, StringComparer.OrdinalIgnoreCase);
            var isProtected = isCurrentApp || protectedNames.Contains(name, StringComparer.OrdinalIgnoreCase);

            return new ProcessInfo
            {
                Name = name,
                ProcessId = process.Id,
                MemoryBytes = TryGetMemory(process),
                IsProtected = isProtected,
                IsSystemProcess = isSystem,
                Reason = isProtected ? "Protected app." : string.Empty
            };
        }
        catch
        {
            return null;
        }
    }

    private static long TryGetMemory(Process process)
    {
        try
        {
            return process.WorkingSet64;
        }
        catch
        {
            return 0;
        }
    }
}
