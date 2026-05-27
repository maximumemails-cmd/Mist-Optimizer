using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class OptimizationEngine
{
    private readonly AppLogger _logger;
    private readonly SystemInfoService _systemInfoService;
    private readonly WindowsCommandService _windowsCommandService;
    private readonly OptimizerStateService _optimizerStateService;

    public OptimizationEngine(
        AppLogger logger,
        SystemInfoService systemInfoService,
        WindowsCommandService windowsCommandService,
        OptimizerStateService optimizerStateService)
    {
        _logger = logger;
        _systemInfoService = systemInfoService;
        _windowsCommandService = windowsCommandService;
        _optimizerStateService = optimizerStateService;
    }

    public async Task<bool> ApplySelectedAsync(
        IEnumerable<OptimizationAction> actions,
        IProgress<double> progress,
        bool previewOnly)
    {
        var selectedActions = actions.Where(action => action.IsSelected).ToList();

        if (selectedActions.Count == 0)
        {
            _logger.Warning("No optimisations selected.");
            progress.Report(0);
            return false;
        }

        var restartNeeded = false;

        for (var index = 0; index < selectedActions.Count; index++)
        {
            var action = selectedActions[index];
            action.Status = OptimizationStatus.Running;
            _logger.Info($"{action.Name}: {(previewOnly ? "previewing" : "running")}.");

            try
            {
                await Task.Delay(180);

                if (previewOnly)
                {
                    action.Status = OptimizationStatus.Skipped;
                    LogPreview(action);
                    progress.Report((index + 1) / (double)selectedActions.Count);
                    continue;
                }

                if (action.RequiresSystemWarning)
                {
                    _logger.Warning($"{action.Name}: this would change registry, networking, services, boot settings, or another system-level area. User confirmation is required before any real implementation can run.");
                }

                if (action.RequiresAdmin && action.IsImplemented && !_systemInfoService.IsAdministrator())
                {
                    action.Status = OptimizationStatus.Skipped;
                    _logger.Warning($"{action.Name}: skipped because administrator permission is required.");
                    progress.Report((index + 1) / (double)selectedActions.Count);
                    continue;
                }

                if (!action.IsImplemented)
                {
                    action.Status = OptimizationStatus.NotImplemented;
                    _logger.Warning($"{action.Name}: not implemented yet. No system changes were made. Intended action: {action.ExactAction}");
                }
                else
                {
                    action.Status = await RunImplementedActionAsync(action);
                    _logger.Info($"{action.Name}: completed.");
                }

                restartNeeded |= action.RequiresRestart && action.Status != OptimizationStatus.Failed;
            }
            catch (Exception ex)
            {
                action.Status = OptimizationStatus.Failed;
                _logger.Error($"{action.Name}: failed. {ex.Message}");
            }

            progress.Report((index + 1) / (double)selectedActions.Count);
        }

        return restartNeeded;
    }

    public async Task RevertSelectedAsync(IEnumerable<OptimizationAction> actions, IProgress<double> progress)
    {
        var selectedActions = actions.Where(action => action.IsSelected).ToList();

        if (selectedActions.Count == 0)
        {
            _logger.Warning("No optimisations selected for revert.");
            progress.Report(0);
            return;
        }

        for (var index = 0; index < selectedActions.Count; index++)
        {
            var action = selectedActions[index];
            action.Status = OptimizationStatus.Running;

            try
            {
                await RevertActionAsync(action);
                action.Status = OptimizationStatus.Completed;
            }
            catch (Exception ex)
            {
                action.Status = OptimizationStatus.Failed;
                _logger.Error($"{action.Name}: revert failed. {ex.Message}");
            }

            progress.Report((index + 1) / (double)selectedActions.Count);
        }
    }

    private void LogPreview(OptimizationAction action)
    {
        _logger.Info($"{action.Name}: preview only. Source: {action.Source}");
        _logger.Info($"{action.Name}: current value/state: {GetCurrentStateSummary(action)}");
        _logger.Info($"{action.Name}: new value/state: {GetNewStateSummary(action)}");
        _logger.Info($"{action.Name}: admin required: {action.RequiresAdmin}; restart required: {action.RequiresRestart}; revert available: {action.Reversible}.");
        _logger.Info($"{action.Name}: exact action: {action.ExactAction}");
        _logger.Info($"{action.Name}: undo/revert path: {action.UndoAction}");
    }

    private async Task<OptimizationStatus> RunImplementedActionAsync(OptimizationAction action)
    {
        switch (action.Id)
        {
            case "network-flush-dns":
                return await FlushDnsAsync(action);
            case "hardware-detect":
                _logger.Info($"Detect Hardware Info result:{Environment.NewLine}{_systemInfoService.GetSummary()}");
                SaveState(action, "Read-only diagnostic not run", "Read-only diagnostic logged", false);
                return OptimizationStatus.Completed;
            case "storage-temp-scan":
                LogTempScan();
                SaveState(action, "Read-only scan not run", "Temp folder size estimate logged", false);
                return OptimizationStatus.Completed;
            default:
                _logger.Warning($"{action.Name}: implemented handler missing, skipped safely.");
                return OptimizationStatus.Skipped;
        }
    }

    private async Task<OptimizationStatus> FlushDnsAsync(OptimizationAction action)
    {
        if (!_systemInfoService.IsWindows)
        {
            _logger.Warning($"{action.Name}: skipped because ipconfig /flushdns is Windows-only.");
            return OptimizationStatus.Skipped;
        }

        var result = await _windowsCommandService.ExecuteAsync("ipconfig", "/flushdns", explicitlyAllowed: true);

        if (result.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error;
            throw new InvalidOperationException($"ipconfig /flushdns failed with exit code {result.ExitCode}. {detail}".Trim());
        }

        SaveState(action, "DNS resolver cache contained unknown entries", "DNS resolver cache flushed", false);
        _logger.Info($"{action.Name}: verification passed because ipconfig exited successfully.");
        return OptimizationStatus.Completed;
    }

    private Task RevertActionAsync(OptimizationAction action)
    {
        var state = _optimizerStateService.Find(action.Id);

        if (state is null)
        {
            _logger.Warning($"{action.Name}: no optimizer-state.json entry was found to revert.");
            return Task.CompletedTask;
        }

        if (!state.RevertAvailable)
        {
            _logger.Warning($"{action.Name}: revert is not available for this action.");
            return Task.CompletedTask;
        }

        switch (action.Id)
        {
            case "network-flush-dns":
            case "hardware-detect":
            case "storage-temp-scan":
                _logger.Info($"{action.Name}: revert verified. This action made no persistent system change.");
                return Task.CompletedTask;
            default:
                _logger.Warning($"{action.Name}: no revert handler exists, skipped safely.");
                return Task.CompletedTask;
        }
    }

    private void SaveState(OptimizationAction action, string originalValue, string newValue, bool restartNeeded)
    {
        _optimizerStateService.SaveApplied(new OptimizerStateEntry
        {
            OptimizationId = action.Id,
            SourceBatchFile = action.Source,
            OriginalValue = originalValue,
            NewValue = newValue,
            DateApplied = DateTime.UtcNow,
            RestartNeeded = restartNeeded,
            RevertAvailable = action.Reversible
        });

        _logger.Info($"{action.Name}: state saved to {_optimizerStateService.StatePath}.");
    }

    private static string GetCurrentStateSummary(OptimizationAction action)
    {
        return action.Id switch
        {
            "network-flush-dns" => "Windows DNS resolver cache may contain cached hostname lookups; individual entries are not read by this app.",
            "hardware-detect" => "Hardware summary has not been refreshed for this action yet.",
            "storage-temp-scan" => "Current temp folder size has not been scanned for this action yet.",
            _ => "Unknown"
        };
    }

    private static string GetNewStateSummary(OptimizationAction action)
    {
        return action.Id switch
        {
            "network-flush-dns" => "DNS resolver cache cleared if Windows command succeeds.",
            "hardware-detect" => "Hardware summary written to the log.",
            "storage-temp-scan" => "Temp folder count and approximate size written to the log.",
            _ => "Unknown"
        };
    }

    private void LogTempScan()
    {
        var tempPath = Path.GetTempPath();
        var files = Directory.EnumerateFiles(tempPath, "*", SearchOption.TopDirectoryOnly).ToList();
        var totalBytes = files.Sum(path =>
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        });

        _logger.Info($"Temp scan result: {files.Count} top-level files in {tempPath}, approximately {totalBytes / 1024d / 1024d:N1} MB.");
    }
}
