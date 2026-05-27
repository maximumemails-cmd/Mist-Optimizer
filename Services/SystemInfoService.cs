using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using PCOptimizer.Models;
using System.Management;

namespace PCOptimizer.Services;

public sealed class SystemInfoService
{
    private CpuSnapshot? _previousCpuSnapshot;

    public string GetSummary()
    {
        var drives = DriveInfo.GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => $"{drive.Name} {drive.AvailableFreeSpace / 1024d / 1024d / 1024d:N1} GB free")
            .Take(4);

        var memory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var memoryText = memory > 0 ? $"{memory / 1024d / 1024d / 1024d:N1} GB available to runtime" : "Unknown RAM";

        return string.Join(Environment.NewLine,
            $"OS: {RuntimeInformation.OSDescription}",
            $"Architecture: {RuntimeInformation.OSArchitecture}",
            $"CPU cores: {Environment.ProcessorCount}",
            $"RAM: {memoryText}",
            $"User: {Environment.UserName}",
            $"Admin/root: {IsAdministrator()}",
            $"Storage: {string.Join("; ", drives)}");
    }

    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public HardwareSummary GetHardwareSummary()
    {
        return new HardwareSummary
        {
            Cpu = GetCpuName(),
            CpuUsage = GetCpuUsageDisplay(),
            Gpu = GetGpuName(),
            Ram = GetRamDescription(),
            Storage = GetStorageDescription(),
            Processes = GetProcessCountDisplay()
        };
    }

    public MemoryStats GetMemoryStats()
    {
        if (OperatingSystem.IsWindows())
        {
            return GetWindowsMemoryStats();
        }

        if (OperatingSystem.IsLinux())
        {
            return GetLinuxMemoryStats();
        }

        if (OperatingSystem.IsMacOS())
        {
            return GetMacMemoryStats();
        }

        return new MemoryStats();
    }

    public string GetProcessCountDisplay()
    {
        try
        {
            return $"Processes: {Process.GetProcesses().Length} running";
        }
        catch
        {
            return "Processes: Unknown";
        }
    }

    public string GetCpuUsageDisplay()
    {
        try
        {
            var snapshot = GetCpuSnapshot();

            if (snapshot is null)
            {
                return "CPU: Unknown";
            }

            if (_previousCpuSnapshot is null)
            {
                _previousCpuSnapshot = snapshot;
                return "CPU: measuring";
            }

            var previous = _previousCpuSnapshot.Value;
            _previousCpuSnapshot = snapshot;

            var totalDelta = snapshot.Value.Total - previous.Total;
            var idleDelta = snapshot.Value.Idle - previous.Idle;

            if (totalDelta <= 0)
            {
                return "CPU: Unknown";
            }

            var usage = Math.Clamp(1 - idleDelta / (double)totalDelta, 0, 1);
            return $"CPU: {usage:P0}";
        }
        catch
        {
            return "CPU: Unknown";
        }
    }

    public bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Equals(Environment.UserName, "root", StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string GetCpuName()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var registryValue = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "ProcessorNameString",
                    null);

                if (registryValue is string cpu && !string.IsNullOrWhiteSpace(cpu))
                {
                    return cpu.Trim();
                }
            }

            if (OperatingSystem.IsMacOS())
            {
                return SysctlString("machdep.cpu.brand_string");
            }

            if (OperatingSystem.IsLinux())
            {
                var modelLine = File.ReadLines("/proc/cpuinfo")
                    .FirstOrDefault(line => line.StartsWith("model name", StringComparison.OrdinalIgnoreCase));

                if (modelLine is not null)
                {
                    var parts = modelLine.Split(':', 2);
                    return parts.Length == 2 ? parts[1].Trim() : "Unknown";
                }
            }
        }
        catch
        {
        }

        return "Unknown";
    }

    private static CpuSnapshot? GetCpuSnapshot()
    {
        if (OperatingSystem.IsWindows())
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
            {
                return null;
            }

            var idleTicks = ToUInt64(idle);
            var kernelTicks = ToUInt64(kernel);
            var userTicks = ToUInt64(user);

            return new CpuSnapshot(idleTicks, kernelTicks + userTicks);
        }

        if (OperatingSystem.IsLinux())
        {
            var line = File.ReadLines("/proc/stat").FirstOrDefault();

            if (line is null || !line.StartsWith("cpu ", StringComparison.Ordinal))
            {
                return null;
            }

            var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Select(value => ulong.TryParse(value, out var parsed) ? parsed : 0)
                .ToArray();

            if (values.Length < 5)
            {
                return null;
            }

            var idle = values[3] + values[4];
            var total = values.Aggregate(0UL, (current, value) => current + value);
            return new CpuSnapshot(idle, total);
        }

        if (OperatingSystem.IsMacOS())
        {
            var host = mach_host_self();

            if (host == IntPtr.Zero ||
                host_processor_info(host, 2, out var processorCount, out var processorInfo, out var processorInfoCount) != 0 ||
                processorInfo == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                ulong idle = 0;
                ulong total = 0;

                for (var processor = 0; processor < processorCount; processor++)
                {
                    var offset = processor * 4 * sizeof(int);
                    var user = (uint)Marshal.ReadInt32(processorInfo, offset);
                    var system = (uint)Marshal.ReadInt32(processorInfo, offset + sizeof(int));
                    var idleTicks = (uint)Marshal.ReadInt32(processorInfo, offset + sizeof(int) * 2);
                    var nice = (uint)Marshal.ReadInt32(processorInfo, offset + sizeof(int) * 3);

                    idle += idleTicks;
                    total += user + system + idleTicks + nice;
                }

                return new CpuSnapshot(idle, total);
            }
            finally
            {
                vm_deallocate(mach_task_self(), processorInfo, processorInfoCount * sizeof(int));
            }
        }

        return null;
    }

    private static string GetGpuName()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "Unknown";
        }

        return GetWindowsGpuName();
    }

    [SupportedOSPlatform("windows")]
    private static string GetWindowsGpuName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            var names = searcher.Get()
                .Cast<ManagementObject>()
                .Select(item => item["Name"]?.ToString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Take(2)
                .ToList();

            return names.Count == 0 ? "Unknown" : string.Join(" / ", names);
        }
        catch
        {
        }

        return "Unknown";
    }

    private static string GetRamDescription()
    {
        var memoryStats = OperatingSystem.IsWindows()
            ? GetWindowsMemoryStats()
            : OperatingSystem.IsLinux()
                ? GetLinuxMemoryStats()
                : OperatingSystem.IsMacOS()
                    ? GetMacMemoryStats()
                    : new MemoryStats();

        var total = memoryStats.TotalDisplay;

        if (total == "Unknown")
        {
            return "Unknown";
        }

        var type = OperatingSystem.IsWindows() ? GetWindowsRamType() : "Unknown";
        return type == "Unknown" ? total : $"{total} {type}";
    }

    [SupportedOSPlatform("windows")]
    private static string GetWindowsRamType()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var searcher = new ManagementObjectSearcher("SELECT SMBIOSMemoryType, MemoryType FROM Win32_PhysicalMemory");
                var typeCode = searcher.Get()
                    .Cast<ManagementObject>()
                    .Select(item => Convert.ToInt32(item["SMBIOSMemoryType"] ?? item["MemoryType"] ?? 0))
                    .FirstOrDefault(code => code > 0);

                return typeCode switch
                {
                    20 => "DDR",
                    21 => "DDR2",
                    24 => "DDR3",
                    26 => "DDR4",
                    34 => "DDR5",
                    _ => "Unknown"
                };
            }
        }
        catch
        {
        }

        return "Unknown";
    }

    private static string GetStorageDescription()
    {
        try
        {
            var totalBytes = DriveInfo.GetDrives()
                .Where(drive => drive.IsReady && drive.DriveType != DriveType.CDRom)
                .Sum(drive => drive.TotalSize);

            return MemoryStats.FormatBytes(totalBytes);
        }
        catch
        {
            return "Unknown";
        }
    }

    private static MemoryStats GetWindowsMemoryStats()
    {
        var status = new MemoryStatusEx();

        if (!GlobalMemoryStatusEx(status))
        {
            return new MemoryStats();
        }

        var total = (long)status.TotalPhys;
        var available = (long)status.AvailPhys;

        return new MemoryStats
        {
            TotalBytes = total,
            AvailableBytes = available,
            UsedBytes = Math.Max(0, total - available)
        };
    }

    private static MemoryStats GetLinuxMemoryStats()
    {
        try
        {
            var values = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var number = new string(parts[1].Where(char.IsDigit).ToArray());

                if (long.TryParse(number, out var kilobytes))
                {
                    values[parts[0]] = kilobytes * 1024;
                }
            }

            var total = values.GetValueOrDefault("MemTotal");
            var available = values.GetValueOrDefault("MemAvailable");

            return new MemoryStats
            {
                TotalBytes = total,
                AvailableBytes = available,
                UsedBytes = Math.Max(0, total - available)
            };
        }
        catch
        {
            return new MemoryStats();
        }
    }

    private static MemoryStats GetMacMemoryStats()
    {
        try
        {
            var total = SysctlUInt64("hw.memsize");
            var host = mach_host_self();

            if (host == IntPtr.Zero || host_page_size(host, out var pageSize) != 0)
            {
                return new MemoryStats { TotalBytes = (long)total };
            }

            var info = new VmStatistics64();
            var count = (uint)(Marshal.SizeOf<VmStatistics64>() / sizeof(int));

            if (host_statistics64(host, 4, ref info, ref count) != 0)
            {
                return new MemoryStats { TotalBytes = (long)total };
            }

            var availablePages = info.free_count + info.inactive_count + info.speculative_count;
            var available = (long)(availablePages * pageSize);

            return new MemoryStats
            {
                TotalBytes = (long)total,
                AvailableBytes = available,
                UsedBytes = Math.Max(0, (long)total - available)
            };
        }
        catch
        {
            return new MemoryStats();
        }
    }

    private static string SysctlString(string name)
    {
        try
        {
            var length = IntPtr.Zero;
            sysctlbyname(name, null, ref length, IntPtr.Zero, 0);

            if (length == IntPtr.Zero)
            {
                return "Unknown";
            }

            var buffer = new byte[length.ToInt32()];
            sysctlbyname(name, buffer, ref length, IntPtr.Zero, 0);
            return Encoding.UTF8.GetString(buffer).TrimEnd('\0').Trim();
        }
        catch
        {
            return "Unknown";
        }
    }

    private static ulong SysctlUInt64(string name)
    {
        var length = new IntPtr(sizeof(ulong));
        var buffer = new byte[sizeof(ulong)];
        sysctlbyname(name, buffer, ref length, IntPtr.Zero, 0);
        return BitConverter.ToUInt64(buffer, 0);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("libSystem.dylib")]
    private static extern int sysctlbyname(string name, byte[]? oldp, ref IntPtr oldlenp, IntPtr newp, uint newlen);

    [DllImport("libSystem.dylib")]
    private static extern IntPtr mach_host_self();

    [DllImport("libSystem.dylib")]
    private static extern IntPtr mach_task_self();

    [DllImport("libSystem.dylib")]
    private static extern int host_page_size(IntPtr host, out uint pageSize);

    [DllImport("libSystem.dylib")]
    private static extern int host_statistics64(IntPtr host, int flavor, ref VmStatistics64 hostInfo, ref uint hostInfoCount);

    [DllImport("libSystem.dylib")]
    private static extern int host_processor_info(
        IntPtr host,
        int flavor,
        out uint outProcessorCount,
        out IntPtr outProcessorInfo,
        out uint outProcessorInfoCount);

    [DllImport("libSystem.dylib")]
    private static extern int vm_deallocate(IntPtr targetTask, IntPtr address, ulong size);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VmStatistics64
    {
        public uint free_count;
        public uint active_count;
        public uint inactive_count;
        public uint wire_count;
        public ulong zero_fill_count;
        public ulong reactivations;
        public ulong pageins;
        public ulong pageouts;
        public ulong faults;
        public ulong cow_faults;
        public ulong lookups;
        public ulong hits;
        public ulong purges;
        public uint purgeable_count;
        public uint speculative_count;
        public ulong decompressions;
        public ulong compressions;
        public ulong swapins;
        public ulong swapouts;
        public uint compressor_page_count;
        public uint throttled_count;
        public uint external_page_count;
        public uint internal_page_count;
        public ulong total_uncompressed_pages_in_compressor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    private readonly record struct CpuSnapshot(ulong Idle, ulong Total);

    private static ulong ToUInt64(FileTime fileTime)
    {
        return ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;
    }
}
