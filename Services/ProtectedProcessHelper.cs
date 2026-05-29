using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PCOptimizer.Services;

public sealed class ProtectedProcessHelper
{
    private static readonly SensitiveProcessPattern[] SensitiveProcessPatterns =
    [
        new("swuab", "Optimisation"),
        new("nvidia", "GPU"),
        new("razer", "Optimisation"),
        new("cortex", "Optimisation"),
        new("razer cortex", "Optimisation"),
        new("razercortex", "Optimisation"),
        new("razer central", "Optimisation"),
        new("razercentral", "Optimisation"),
        new("obs", "Recording"),
        new("obs64", "Recording"),
        new("hone", "Optimisation"),
        new("medal", "Recording"),
        new("outplayed", "Recording"),
        new("ghast", "Optimisation"),
        new("exitlag", "Optimisation")
    ];

    private static readonly string[] BuiltInProtectedNames =
    [
        "Mist",
        "Mist.exe",
        "PCOptimizer",
        "PCOptimizer.exe",
        "explorer",
        "explorer.exe",
        "dwm",
        "dwm.exe",
        "csrss",
        "csrss.exe",
        "wininit",
        "wininit.exe",
        "winlogon",
        "winlogon.exe",
        "services",
        "services.exe",
        "lsass",
        "lsass.exe",
        "svchost",
        "svchost.exe",
        "System",
        "Idle",
        "Registry",
        "smss",
        "smss.exe",
        "fontdrvhost",
        "fontdrvhost.exe"
    ];

    private static readonly string[] CriticalNames =
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
        "fontdrvhost",
        "memory compression",
        "secure system"
    ];

    public IReadOnlyCollection<string> BuiltInProtectedProcesses => BuiltInProtectedNames;
    public IReadOnlyCollection<string> NeverAutoSelectPatterns => SensitiveProcessPatterns.Select(pattern => pattern.Pattern).ToList();

    public bool IsProtected(Process process, IReadOnlyCollection<string> userProtectedNames)
    {
        try
        {
            return process.Id == Environment.ProcessId ||
                   IsProtectedName(process.ProcessName, userProtectedNames) ||
                   IsProtectedName(Path.GetFileName(process.MainModule?.FileName) ?? string.Empty, userProtectedNames);
        }
        catch
        {
            return process.Id == Environment.ProcessId || IsProtectedName(process.ProcessName, userProtectedNames);
        }
    }

    public bool IsProtectedName(string name, IReadOnlyCollection<string> userProtectedNames)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return BuiltInProtectedNames.Concat(userProtectedNames).Any(candidate => NameMatches(name, candidate));
    }

    public bool IsCriticalSystemProcess(Process process)
    {
        try
        {
            return process.Id <= 4 || CriticalNames.Any(name => NameMatches(process.ProcessName, name));
        }
        catch
        {
            return true;
        }
    }

    public SensitiveProcessMatch? MatchSensitiveProcess(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalizedValue = NormalizeForContains(value);

            foreach (var pattern in SensitiveProcessPatterns)
            {
                if (normalizedValue.Contains(NormalizeForContains(pattern.Pattern), StringComparison.OrdinalIgnoreCase))
                {
                    return new SensitiveProcessMatch(pattern.Pattern, pattern.Category);
                }
            }
        }

        return null;
    }

    private static bool NameMatches(string processName, string protectedName)
    {
        var left = Normalize(processName);
        var right = Normalize(protectedName);
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string name)
    {
        var trimmed = name.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }

    private static string NormalizeForContains(string value)
    {
        return Normalize(value)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SensitiveProcessPattern(string Pattern, string Category);
}

public sealed record SensitiveProcessMatch(string Pattern, string Category);
