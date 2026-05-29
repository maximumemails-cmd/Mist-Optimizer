using System;
using System.Collections.Generic;

namespace PCOptimizer.Models;

public sealed class AppSettings
{
    public bool EnableAnimations { get; set; } = true;
    public bool ShowAdvancedTweaks { get; set; }
    public List<string> ProtectedProcesses { get; set; } =
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
        "wininit",
        "winlogon",
        "services",
        "lsass",
        "svchost",
        "System",
        "Idle",
        "swuab",
        "NVIDIA",
        "nvidia",
        "Razer Cortex",
        "razer cortex",
        "Razer Central",
        "razer central",
        "OBS",
        "obs",
        "obs64",
        "Hone",
        "hone",
        "Medal",
        "medal",
        "Outplayed",
        "outplayed",
        "Ghast",
        "ghast",
        "ExitLag",
        "exitlag",
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
    public bool RamCleanerFocusSuggestionsEnabled { get; set; } = true;
    public bool RamCleanerTrimSelectedApps { get; set; } = true;
    public bool RamCleanerCloseSelectedApps { get; set; }
    public bool RamCleanerStandbyCleanupEnabled { get; set; }
    public Dictionary<string, bool> RamCleanerProcessSelections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> SwuabValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime LastOpenedUtc { get; set; } = DateTime.UtcNow;
}
