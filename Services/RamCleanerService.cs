using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
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

    public bool IsStandbyCleanupSupported => OperatingSystem.IsWindows() && Environment.OSVersion.Version.Major >= 6;
    public bool IsStandbyCleanupAvailable => IsStandbyCleanupSupported && _systemInfoService.IsAdministrator();
    public string StandbyCleanupStatus => !OperatingSystem.IsWindows()
        ? "Standby cleanup is Windows-only."
        : !IsStandbyCleanupSupported
            ? "Standby cleanup is not supported on this Windows version."
            : IsStandbyCleanupAvailable
                ? "Standby cleanup available with current privileges."
                : "Standby cleanup requires running Mist as administrator.";

    public Task<IReadOnlyList<ProcessInfo>> ScanAsync(IReadOnlyCollection<string> protectedProcesses, bool focusSuggestions)
    {
        var processes = _processService.ListProcesses(protectedProcesses, focusSuggestions);
        _logger.Info($"Process scan found {processes.Count} visible process(es).");
        return Task.FromResult(processes);
    }

    public Task<IReadOnlyList<ProcessInfo>> PreviewCleanAsync(IEnumerable<ProcessInfo> processes)
    {
        var preview = _processService.BuildPreview(processes).ToList();
        _logger.Info($"RAM cleaner preview found {preview.Count} non-protected app candidate(s). No apps were closed.");
        return Task.FromResult<IReadOnlyList<ProcessInfo>>(preview);
    }

    public Task<RamCleanResult> CleanSelectedAsync(IEnumerable<ProcessInfo> processes, RamCleanOptions options)
    {
        return Task.Run(() =>
        {
            var beforeStats = GetMemoryStats();
            var selected = processes
                .Where(process => process.IsSelectedForCleaning && process.CanSelect)
                .ToList();
            var messages = new List<string>();
            var failures = new List<string>();
            var measuredProcessReclaimed = 0L;
            var trimmedCount = 0;
            var closeRequestedCount = 0;
            var closedCount = 0;

            var mistReclaimed = CleanMistMemory();
            messages.Add($"Mist self cleanup reclaimed {MemoryStats.FormatBytes(mistReclaimed)} from this process.");

            if (selected.Count == 0)
            {
                messages.Add("No selected app processes were changed.");
                _logger.Warning("RAM cleaner had no selected non-protected process candidates.");
            }

            foreach (var process in selected)
            {
                if (process.RecommendedAction == ProcessOptimizationAction.CloseApp && options.CloseSelectedApps)
                {
                    closeRequestedCount++;
                    var closeResult = TryCloseProcess(process);
                    if (closeResult.Succeeded)
                    {
                        closedCount++;
                        measuredProcessReclaimed += closeResult.ReclaimedBytes;
                        messages.Add($"{process.Name} ({process.ProcessId}) closed gracefully.");
                    }
                    else
                    {
                        failures.Add(closeResult.Message);
                    }

                    continue;
                }

                if (!options.TrimSelectedApps)
                {
                    continue;
                }

                var trimResult = TryTrimProcess(process);
                if (trimResult.Succeeded)
                {
                    trimmedCount++;
                    measuredProcessReclaimed += trimResult.ReclaimedBytes;
                    messages.Add($"{process.Name} ({process.ProcessId}) working set trimmed.");
                }
                else
                {
                    failures.Add(trimResult.Message);
                }
            }

            var standbyAttempted = false;
            var standbySucceeded = false;
            var standbyMessage = StandbyCleanupStatus;

            if (options.CleanStandbyList)
            {
                standbyAttempted = true;
                var standbyResult = TryPurgeStandbyList();
                standbySucceeded = standbyResult.Succeeded;
                standbyMessage = standbyResult.Message;

                if (standbyResult.Succeeded)
                {
                    messages.Add(standbyResult.Message);
                }
                else
                {
                    failures.Add(standbyResult.Message);
                }
            }

            var afterStats = GetMemoryStats();
            var observedFreed = Math.Max(0, beforeStats.UsedBytes - afterStats.UsedBytes);

            _logger.Info($"RAM cleaner completed. Selected: {selected.Count}; trimmed: {trimmedCount}; close requests: {closeRequestedCount}; closed: {closedCount}; observed system delta: {MemoryStats.FormatBytes(observedFreed)}.");

            foreach (var failure in failures)
            {
                _logger.Warning(failure);
            }

            return new RamCleanResult
            {
                ObservedSystemFreedBytes = observedFreed,
                MeasuredProcessReclaimedBytes = measuredProcessReclaimed,
                MistReclaimedBytes = mistReclaimed,
                SelectedCount = selected.Count,
                TrimmedCount = trimmedCount,
                CloseRequestedCount = closeRequestedCount,
                ClosedCount = closedCount,
                StandbyAttempted = standbyAttempted,
                StandbySucceeded = standbySucceeded,
                StandbyMessage = standbyMessage,
                Messages = messages,
                Failures = failures
            };
        });
    }

    public void StartFocusMode()
    {
        _logger.Info("Background focus mode started in suggestion-only mode. It will not close apps or change system settings.");
    }

    public void StopFocusMode()
    {
        _logger.Info("Background focus mode stopped.");
    }

    private long CleanMistMemory()
    {
        var current = Process.GetCurrentProcess();
        var before = TryGetWorkingSet(current);

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        _logger.Info("RAM cleaner ran GC collection and one-time LOH compaction for Mist.");

        _ = TryTrimWorkingSet(current, "Mist", out _);
        current.Refresh();
        return Math.Max(0, before - TryGetWorkingSet(current));
    }

    private ProcessCleanResult TryTrimProcess(ProcessInfo processInfo)
    {
        try
        {
            using var target = Process.GetProcessById(processInfo.ProcessId);
            var before = TryGetWorkingSet(target);
            var succeeded = TryTrimWorkingSet(target, $"{processInfo.Name} ({processInfo.ProcessId})", out var message);
            target.Refresh();
            var reclaimed = Math.Max(0, before - TryGetWorkingSet(target));

            return new ProcessCleanResult(succeeded, reclaimed, message);
        }
        catch (Exception ex)
        {
            return new ProcessCleanResult(false, 0, $"{processInfo.Name} ({processInfo.ProcessId}) could not be trimmed: {ex.Message}");
        }
    }

    private ProcessCleanResult TryCloseProcess(ProcessInfo processInfo)
    {
        try
        {
            using var target = Process.GetProcessById(processInfo.ProcessId);
            var before = TryGetWorkingSet(target);

            if (target.MainWindowHandle == IntPtr.Zero || !target.CloseMainWindow())
            {
                return new ProcessCleanResult(false, 0, $"{processInfo.Name} ({processInfo.ProcessId}) has no closeable main window. Mist did not force-kill it.");
            }

            if (!target.WaitForExit(2500))
            {
                return new ProcessCleanResult(false, 0, $"{processInfo.Name} ({processInfo.ProcessId}) did not exit after a graceful close request.");
            }

            return new ProcessCleanResult(true, before, $"{processInfo.Name} ({processInfo.ProcessId}) closed gracefully.");
        }
        catch (Exception ex)
        {
            return new ProcessCleanResult(false, 0, $"{processInfo.Name} ({processInfo.ProcessId}) could not be closed: {ex.Message}");
        }
    }

    private bool TryTrimWorkingSet(Process process, string displayName, out string message)
    {
        if (!OperatingSystem.IsWindows())
        {
            message = $"{displayName}: working-set trimming is Windows-only.";
            _logger.Warning(message);
            return false;
        }

        try
        {
            if (EmptyWorkingSet(process.Handle))
            {
                message = $"{displayName}: working set trimmed. Windows may reclaim or reassign memory immediately.";
                _logger.Info(message);
                return true;
            }

            message = $"{displayName}: Windows declined working-set trimming ({new Win32Exception(Marshal.GetLastWin32Error()).Message}).";
            _logger.Warning(message);
            return false;
        }
        catch (Exception ex)
        {
            message = $"{displayName}: working-set trim failed: {ex.Message}";
            _logger.Warning(message);
            return false;
        }
    }

    private StandbyCleanResult TryPurgeStandbyList()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new StandbyCleanResult(false, StandbyCleanupStatus);
        }

        if (!IsStandbyCleanupSupported)
        {
            return new StandbyCleanResult(false, StandbyCleanupStatus);
        }

        if (!TryEnablePrivilege("SeProfileSingleProcessPrivilege", out var privilegeMessage))
        {
            return new StandbyCleanResult(false, $"{StandbyCleanupStatus} {privilegeMessage}");
        }

        var command = SystemMemoryListCommandMemoryPurgeStandbyList;
        var handle = GCHandle.Alloc(command, GCHandleType.Pinned);

        try
        {
            var status = NtSetSystemInformation(SystemMemoryListInformation, handle.AddrOfPinnedObject(), (uint)Marshal.SizeOf(command));
            if (status == 0)
            {
                _logger.Info("Windows standby list purge requested successfully.");
                return new StandbyCleanResult(true, "Windows standby list purge requested successfully.");
            }

            return new StandbyCleanResult(false, $"Windows standby list purge failed with NTSTATUS 0x{status:X8}.");
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryEnablePrivilege(string privilegeName, out string message)
    {
        message = string.Empty;

        try
        {
            using var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges);
            var privileges = new TokenPrivileges
            {
                Count = 1,
                Luid = 0,
                Attr = PrivilegeEnabled
            };

            if (!LookupPrivilegeValue(null, privilegeName, ref privileges.Luid))
            {
                message = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                return false;
            }

            if (!AdjustTokenPrivileges(identity.Token, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                message = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                return false;
            }

            var error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                message = new Win32Exception(error).Message;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
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

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr processHandle);

    [DllImport("ntdll.dll")]
    private static extern int NtSetSystemInformation(int systemInformationClass, IntPtr systemInformation, uint systemInformationLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TokenPrivileges newState, int bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string? systemName, string name, ref long luid);

    private const int PrivilegeEnabled = 2;
    private const int SystemMemoryListInformation = 80;
    private const int SystemMemoryListCommandMemoryPurgeStandbyList = 4;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TokenPrivileges
    {
        public int Count;
        public long Luid;
        public int Attr;
    }

    private sealed record ProcessCleanResult(bool Succeeded, long ReclaimedBytes, string Message);
    private sealed record StandbyCleanResult(bool Succeeded, string Message);
}
