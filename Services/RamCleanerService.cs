using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class RamCleanerService
{
    private readonly AppLogger _logger;
    private readonly ProcessService _processService;
    private readonly SystemInfoService _systemInfoService;

    public RamCleanerService(AppLogger logger, ProcessService processService, SystemInfoService systemInfoService)
    {
        _logger = logger;
        _processService = processService;
        _systemInfoService = systemInfoService;
    }

    public MemoryStats GetMemoryStats() => _systemInfoService.GetMemoryStats();

    public string GetProcessCountDisplay() => _systemInfoService.GetProcessCountDisplay();

    public Task<IReadOnlyList<ProcessInfo>> ScanAsync(IReadOnlyCollection<string> protectedProcesses)
    {
        var processes = _processService.ListProcesses(protectedProcesses);
        _logger.Info($"Process scan found {processes.Count} visible process(es).");
        return Task.FromResult(processes);
    }

    public Task<IReadOnlyList<ProcessInfo>> PreviewCleanAsync(IEnumerable<ProcessInfo> processes)
    {
        var preview = _processService.BuildPreview(processes).ToList();
        _logger.Info($"RAM cleaner preview found {preview.Count} non-protected app candidate(s). No apps were closed.");
        return Task.FromResult<IReadOnlyList<ProcessInfo>>(preview);
    }

    public Task CleanSelectedAsync(IEnumerable<ProcessInfo> processes, bool allowClosingSelectedNonCriticalApps)
    {
        var selected = processes
            .Where(process => process.IsSelectedForCleaning && !process.IsProtected && !process.IsSystemProcess)
            .ToList();

        if (!allowClosingSelectedNonCriticalApps)
        {
            _logger.Warning("Clean RAM / Focus Mode ran in preview-only mode. No apps were closed.");
            return Task.CompletedTask;
        }

        if (selected.Count == 0)
        {
            _logger.Warning("No manually selected non-critical apps were chosen for cleaning.");
            return Task.CompletedTask;
        }

        foreach (var process in selected)
        {
            _logger.Warning($"{process.Name} ({process.ProcessId}) would be closed here after a confirmation dialog. Closing is placeholder-only for now.");
        }

        _logger.Info("RAM cleaner completed safely. No processes were killed.");
        return Task.CompletedTask;
    }

    public void StartFocusMode()
    {
        _logger.Info("Background focus mode started in suggestion-only mode. It will not close apps or change system settings.");
    }

    public void StopFocusMode()
    {
        _logger.Info("Background focus mode stopped.");
    }
}
