using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class ProcessService
{
    private static readonly string[] FocusCloseCandidates =
    [
        "discord",
        "teams",
        "slack",
        "spotify",
        "steam",
        "epicgameslauncher",
        "battle.net",
        "onedrive",
        "dropbox",
        "googledrivefs",
        "adobe creative cloud",
        "creative cloud"
    ];

    private readonly ProtectedProcessHelper _protectedProcessHelper;

    public ProcessService(ProtectedProcessHelper protectedProcessHelper)
    {
        _protectedProcessHelper = protectedProcessHelper;
    }

    public IReadOnlyList<ProcessInfo> ListProcesses(IReadOnlyCollection<string> protectedNames, bool focusSuggestions)
    {
        return Process.GetProcesses()
            .Select(process => ToProcessInfo(process, protectedNames, focusSuggestions))
            .Where(process => process is not null)
            .Select(process => process!)
            .OrderByDescending(process => process.MemoryBytes)
            .Take(120)
            .ToList();
    }

    public IReadOnlyList<ProcessInfo> BuildPreview(IEnumerable<ProcessInfo> processes)
    {
        foreach (var process in processes)
        {
            if (!process.CanSelect)
            {
                process.IsSelectedForCleaning = false;
                process.Reason = process.IsSystemProcess
                    ? "System-critical process is protected."
                    : "Protected by Mist rules or user settings.";
            }
            else if (process.IsSelectedForCleaning)
            {
                process.Reason = process.RecommendedAction == ProcessOptimizationAction.CloseApp
                    ? "Selected for a graceful close request. Mist will not force-kill it."
                    : "Selected for Windows working-set trimming.";
            }
            else
            {
                process.Reason = "Available but not selected.";
            }
        }

        return processes
            .Where(process => process.IsSelectedForCleaning && process.CanSelect)
            .OrderByDescending(process => process.EstimatedReclaimableBytes)
            .ToList();
    }

    private ProcessInfo? ToProcessInfo(Process process, IReadOnlyCollection<string> protectedNames, bool focusSuggestions)
    {
        try
        {
            var name = process.ProcessName;
            var memory = TryGetWorkingSet(process);
            var privateMemory = TryGetPrivateMemory(process);
            var executableName = TryGetExecutableName(process);
            var description = TryGetDescription(process);
            var windowTitle = TryGetWindowTitle(process);
            var hasWindow = HasVisibleMainWindow(process);
            var isSystem = _protectedProcessHelper.IsCriticalSystemProcess(process);
            var isProtected = _protectedProcessHelper.IsProtected(process, protectedNames);
            var isMist = process.Id == Environment.ProcessId;
            var sensitiveMatch = _protectedProcessHelper.MatchSensitiveProcess(name, executableName, windowTitle, description);
            var neverAutoSelect = sensitiveMatch is not null;
            var category = isSystem
                ? "System"
                : isProtected
                    ? "Protected"
                    : sensitiveMatch?.Category ?? (hasWindow ? "App" : "Background");
            var action = GetRecommendedAction(name, memory, hasWindow, isSystem, isProtected, neverAutoSelect, focusSuggestions);
            var estimate = EstimateReclaimableBytes(memory, action);

            return new ProcessInfo
            {
                Name = name,
                ProcessId = process.Id,
                MemoryBytes = memory,
                PrivateMemoryBytes = privateMemory,
                EstimatedReclaimableBytes = estimate,
                IsProtected = isProtected,
                IsSystemProcess = isSystem,
                IsMistProcess = isMist,
                HasVisibleWindow = hasWindow,
                NeverAutoSelect = neverAutoSelect,
                WindowTitle = windowTitle,
                ExecutableName = executableName,
                Description = description,
                Category = category,
                RecommendedAction = action,
                IsSelectedForCleaning = !neverAutoSelect && (action == ProcessOptimizationAction.TrimWorkingSet || (focusSuggestions && action == ProcessOptimizationAction.CloseApp)),
                Reason = BuildReason(action, category, isProtected, isSystem, neverAutoSelect, sensitiveMatch?.Pattern, focusSuggestions)
            };
        }
        catch
        {
            return null;
        }
    }

    private static ProcessOptimizationAction GetRecommendedAction(
        string name,
        long memoryBytes,
        bool hasWindow,
        bool isSystem,
        bool isProtected,
        bool neverAutoSelect,
        bool focusSuggestions)
    {
        if (isSystem || isProtected || memoryBytes < 40L * 1024 * 1024)
        {
            return ProcessOptimizationAction.Ignore;
        }

        if (!neverAutoSelect && focusSuggestions && IsFocusCloseCandidate(name) && memoryBytes >= 90L * 1024 * 1024)
        {
            return ProcessOptimizationAction.CloseApp;
        }

        return hasWindow || memoryBytes >= 80L * 1024 * 1024
            ? ProcessOptimizationAction.TrimWorkingSet
            : ProcessOptimizationAction.Ignore;
    }

    private static bool IsFocusCloseCandidate(string name)
    {
        return FocusCloseCandidates.Any(candidate =>
            name.Contains(candidate, StringComparison.OrdinalIgnoreCase) ||
            candidate.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildReason(
        ProcessOptimizationAction action,
        string category,
        bool isProtected,
        bool isSystem,
        bool neverAutoSelect,
        string? sensitivePattern,
        bool focusSuggestions)
    {
        if (isSystem)
        {
            return "System-critical process. Mist will not close or trim it.";
        }

        if (isProtected)
        {
            return "Protected by Mist rules or user settings.";
        }

        if (neverAutoSelect)
        {
            return $"{category} software matched '{sensitivePattern}'. Mist will not auto-select it or recommend closing it.";
        }

        return action switch
        {
            ProcessOptimizationAction.CloseApp => focusSuggestions
                ? "Focus suggestion: likely background app. Mist can request a graceful close."
                : "Can be closed after review.",
            ProcessOptimizationAction.TrimWorkingSet => $"{category} candidate. Mist can ask Windows to trim unused working-set pages.",
            _ => "Low expected reclaim or not a safe optimization candidate."
        };
    }

    private static long EstimateReclaimableBytes(long memoryBytes, ProcessOptimizationAction action)
    {
        if (memoryBytes <= 0)
        {
            return 0;
        }

        return action switch
        {
            ProcessOptimizationAction.CloseApp => memoryBytes,
            ProcessOptimizationAction.TrimWorkingSet => Math.Max(0, Math.Min(memoryBytes / 2, memoryBytes - 32L * 1024 * 1024)),
            _ => 0
        };
    }

    private static bool HasVisibleMainWindow(Process process)
    {
        try
        {
            return process.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(process.MainWindowTitle);
        }
        catch
        {
            return false;
        }
    }

    private static string TryGetWindowTitle(Process process)
    {
        try
        {
            return process.MainWindowTitle ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TryGetExecutableName(Process process)
    {
        try
        {
            return Path.GetFileName(process.MainModule?.FileName) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TryGetDescription(Process process)
    {
        try
        {
            return process.MainModule?.FileVersionInfo.FileDescription ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static long TryGetWorkingSet(Process process)
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

    private static long TryGetPrivateMemory(Process process)
    {
        try
        {
            return process.PrivateMemorySize64;
        }
        catch
        {
            return 0;
        }
    }
}
