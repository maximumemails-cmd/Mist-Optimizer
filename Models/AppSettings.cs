using System;
using System.Collections.Generic;

namespace PCOptimizer.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public bool EnableAnimations { get; set; } = true;
    public bool ShowAdvancedTweaks { get; set; }
    public List<string> ProtectedProcesses { get; set; } =
    [
        "PCOptimizer",
        "PCOptimizer.exe",
        "explorer",
        "explorer.exe",
        "Discord",
        "Discord.exe",
        "chrome",
        "chrome.exe",
        "msedge",
        "msedge.exe",
        "firefox",
        "firefox.exe",
        "safari"
    ];

    public List<string> SelectedOptimizationIds { get; set; } = [];
    public DateTime LastOpenedUtc { get; set; } = DateTime.UtcNow;
}
