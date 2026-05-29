using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class OptimizationCatalogService
{
    private const string NotImplementedReason = "Selectable for review. Mist will log that no safe apply handler exists yet.";
    private const string NeedsReviewReason = "Selectable for review. Admin, restart, and validation warnings do not make this unavailable.";
    private const string DangerousReason = "Disabled for safety because this can break Windows, networking, security, recovery, apps, or normal laptop use.";
    private const string ConflictReason = "Disabled because the audited batch files contain conflicting values for this setting.";
    private readonly AppLogger? _logger;

    public OptimizationCatalogService(AppLogger? logger = null)
    {
        _logger = logger;
    }

    public OptimizationCatalogReport LastReport { get; private set; } = new();

    public IReadOnlyList<OptimizationAction> GetAll(IReadOnlyCollection<string> selectedIds)
    {
        var optimizerstuffPath = FindOptimizerstuffPath();
        var batchFiles = optimizerstuffPath is null
            ? Array.Empty<string>()
            : Directory.GetFiles(optimizerstuffPath, "*.bat", SearchOption.AllDirectories);
        var loadingFailures = 0;
        var parsedCommandCount = batchFiles.Sum(path => CountCommandLikeLines(path, ref loadingFailures));

        var actions = BuiltInActions()
            .Concat(BatchAuditActions())
            .Concat(ParseOptimizerstuffActions(batchFiles, ref loadingFailures))
            .GroupBy(action => action.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => MergeDuplicateSources(group.ToList()))
            .ToList();

        foreach (var action in actions)
        {
            action.IsSelected = selectedIds.Contains(action.Id) && action.IsEnabled;
        }

        LastReport = BuildReport(actions, optimizerstuffPath, batchFiles.Length, parsedCommandCount, loadingFailures);
        return actions;
    }

    private static IReadOnlyList<OptimizationAction> BuiltInActions()
    {
        return new List<OptimizationAction>
        {
            Implemented("network-flush-dns", "Flush DNS Cache", "Clears Windows DNS resolver cache entries. Imported from the audited batch files and implemented as a fixed Windows-only command.", "Network", false, false, RiskLevel.Safe, "Run fixed command: ipconfig /flushdns. No user input is passed to the command.", "No persistent setting is changed. Revert logs that no restore is needed.", "Optimizerstuff/main.bat:150,267,285,313,372,683; Optimizerstuff/compiled.bat:53,73,130; Optimizerstuff/aio.bat:91,121,135; Optimizerstuff/aiomain.bat:155,184,209,502,545"),
            Implemented("hardware-detect", "Detect Hardware Info", "Reads available OS, CPU, memory, storage, and platform details without changing the system.", "Hardware", false, false, RiskLevel.Safe, "Read local system information using safe platform APIs already used by the app.", "No system changes are made, so no undo is required.", "Built-in catalog"),
            Implemented("storage-temp-scan", "Scan Temp Files", "Scans the current user's temp folder and reports file count and approximate size without deleting anything.", "Storage", false, false, RiskLevel.Safe, "Enumerate top-level files in the current user's temp folder and log count/size.", "No files are deleted, so no undo is required.", "Built-in catalog"),

            Candidate("network-reset-stack", "Reset Network Stack", "Would reset selected Windows networking components after backup and consent.", "Network", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Built-in catalog", "Unknown"),
            Candidate("network-throttling", "Disable Network Throttling", "Would review multimedia network throttling settings with restore support.", "Network", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Built-in catalog", "Unknown"),
            Candidate("network-tcp", "Optimise TCP Settings", "Would review TCP tuning settings and apply only reversible changes.", "Network", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("network-dns", "Set DNS", "Would let the user choose DNS servers before changing adapter settings.", "Network", false, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("network-arp", "Clear ARP Cache", "Would clear cached ARP entries after confirmation.", "Network", false, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "No"),

            Candidate("hardware-gpu-scheduling", "GPU Scheduling", "Would review hardware accelerated GPU scheduling support.", "Hardware", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("hardware-usb-suspend", "Disable USB Selective Suspend", "Would adjust USB power saving only with clear restore guidance.", "Hardware", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("hardware-storage", "Optimise Storage Settings", "Would review storage maintenance settings without touching data.", "Hardware", false, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),

            Candidate("gaming-game-mode", "Enable Game Mode", "Would enable Windows Game Mode if available.", "Gaming", false, true, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "Yes"),
            Candidate("gaming-captures", "Disable Xbox Game Bar Captures", "Would review capture settings and ask before changing them.", "Gaming", false, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("gaming-focus", "High Performance Game Focus", "Would prepare a reversible focus profile for games.", "Gaming", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("gaming-background-suggestions", "Reduce Background App Suggestions", "Would review background suggestion settings.", "Gaming", false, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),

            Candidate("startup-scan", "Scan Startup Apps", "Would scan startup locations and show a report without changing entries.", "Startup", false, false, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),
            Candidate("startup-disable-selected", "Disable Selected Startup Apps", "Would disable only user-selected startup entries with backup support.", "Startup", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("startup-impact-report", "Startup Impact Report", "Would summarize startup impact from safe data sources.", "Startup", false, false, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),

            Candidate("services-scan", "Scan Non-Critical Services", "Would scan service metadata and list candidates without modifying them.", "Services", false, true, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),
            Candidate("services-disable-selected", "Disable Selected Non-Critical Services", "Would disable only explicit user-selected non-critical services.", "Services", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("services-safety-report", "Service Safety Report", "Would classify services by safety level before any change is possible.", "Services", false, true, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),

            Candidate("privacy-telemetry", "Reduce Telemetry", "Would review telemetry-related settings with restore support.", "Privacy", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("privacy-ad-id", "Disable Advertising ID", "Would review advertising ID settings.", "Privacy", false, true, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "Yes"),
            Candidate("privacy-suggested-content", "Disable Suggested Content", "Would review suggested content settings.", "Privacy", false, true, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "Yes"),

            Candidate("storage-app-temp", "Clear App Temp Files", "Would clear selected app temp files only after preview and confirmation.", "Storage", false, false, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("storage-windows-temp", "Clear Windows Temp", "Would clear selected Windows temp files after admin consent.", "Storage", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("storage-recycle-bin", "Empty Recycle Bin", "Would empty recycle bin only after explicit confirmation.", "Storage", false, false, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "No"),

            Candidate("power-current-plan", "Show Current Power Plan", "Would read and display the active Windows power plan.", "Power", false, false, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),
            Candidate("power-high-performance", "Enable High Performance", "Would switch to a user-approved performance power plan.", "Power", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),
            Candidate("power-no-sleep-gaming", "Disable Sleep While Gaming", "Would temporarily adjust sleep settings for gaming sessions.", "Power", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),

            Candidate("visual-reduce-animations", "Reduce Animations", "Would review Windows animation settings.", "Visual Effects", false, true, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "Yes"),
            Candidate("visual-transparency", "Disable Transparency", "Would review transparency settings.", "Visual Effects", false, true, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "Yes"),
            Candidate("visual-performance-preset", "Performance Visual Preset", "Would apply a reversible visual performance preset.", "Visual Effects", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes"),

            Candidate("drivers-driver-info", "Check Driver Info", "Would show installed driver information without installing anything.", "Drivers / Updates", false, false, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),
            Candidate("drivers-windows-update", "Check Windows Update Status", "Would read Windows Update status without changing settings.", "Drivers / Updates", false, false, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),
            Candidate("drivers-no-auto-update", "No Automatic Driver Updating Yet", "Documents that automatic driver updates are intentionally not implemented.", "Drivers / Updates", false, false, RiskLevel.Safe, "Not implemented yet", NotImplementedReason, "Built-in catalog", "No"),

            Candidate("advanced-memory-compression", "Memory Compression", "Would review memory compression state. Hidden unless advanced tweaks are enabled.", "Advanced", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Built-in catalog", "Unknown"),
            Candidate("advanced-timer-resolution", "Timer Resolution", "Would review timer resolution options. Hidden unless advanced tweaks are enabled.", "Advanced", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Built-in catalog", "Unknown"),
            Candidate("advanced-cpu-priority", "CPU Priority Profiles", "Would prepare app-specific CPU priority profiles. Hidden unless advanced tweaks are enabled.", "Advanced", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Built-in catalog", "Yes")
        };
    }

    private static IReadOnlyList<OptimizationAction> BatchAuditActions()
    {
        return new List<OptimizationAction>
        {
            Batch("batch-system-restore-policy", "System Restore Policy Edits", "Alters restore-point policy/frequency registry values.", "Restore / Backups", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:42-45", "Unknown", "Restart unknown"),
            Batch("batch-create-restore-point", "Create Windows Restore Point", "Creates a Windows restore point before risky changes.", "Restore / Backups", false, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:51", "No", "No restart"),
            Batch("batch-tcp-autotuning", "TCP Auto-Tuning Profiles", "Changes TCP auto-tuning between normal, disabled, restricted, and highly restricted.", "Network", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-tcp-congestion-provider", "TCP Congestion Provider", "Changes CTCP/congestion provider settings.", "Network", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-ecn-disable", "Disable ECN", "Disables explicit congestion notification.", "Network", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-rss-profile", "Receive-Side Scaling Profile", "Changes RSS and related receive-side scaling values.", "Network", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-rsc-profile", "Receive Segment Coalescing Profile", "Changes RSC on/off through netsh and PowerShell.", "Network", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-dca-netdma", "DCA / NetDMA Tuning", "Changes direct cache access and NetDMA values.", "Hardware", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-task-offload", "Task Offload Profile", "Changes network task offload on/off.", "Network", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-adapter-mtu", "Persistent Adapter MTU", "Persists hardcoded MTU values against adapter names.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:180,217,421; Optimizerstuff/aiomain.bat:447", "Yes", "Restart unknown"),
            Batch("batch-tcp-timestamps", "TCP Timestamp Profile", "Enables/disables TCP timestamps.", "Network", true, true, RiskLevel.Moderate, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-retransmission-timers", "TCP Retransmission Timers", "Changes initial/min RTO and SYN retransmission settings.", "Network", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-port-connection-limits", "TCP Port / Connection Limits", "Changes dynamic port, max user port, and connection limit values.", "Network", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:198-200,235-237,343-344", "Yes", "Restart unknown"),
            Batch("batch-sack-profile", "SACK / RTT Resiliency Profile", "Changes SACK and non-SACK RTT resiliency settings.", "Network", true, true, RiskLevel.Advanced, "Conflict", ConflictReason, "Optimizerstuff/main.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-syn-protection-disable", "SYN Attack Protection Change", "Disables/enables SYN attack protection.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:205,242", "Yes", "Restart unknown"),
            Batch("batch-nonlocal-source", "Non-Local Source Address Change", "Changes non-local source-address behavior.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:208,245", "Yes", "Restart unknown"),
            Batch("batch-network-stack-reset", "Broad Network Stack Reset", "Runs Winsock/IP/IPv4/IPv6 network reset commands.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Unknown", "Restart required"),
            Batch("batch-ip-release-renew", "IP Release / Renew", "Drops and renews IP leases.", "Network", false, false, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "No restart"),
            Batch("batch-firewall-reset", "Windows Firewall Reset", "Resets Windows Firewall configuration.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:312", "Unknown", "Restart unknown"),
            Batch("batch-adapter-duplex", "Network Adapter Duplex Mode", "Changes active adapter duplex mode.", "Hardware", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:377; Optimizerstuff/compiled.bat:86; Optimizerstuff/aio.bat:140", "Unknown", "Restart unknown"),
            Batch("batch-java-priority", "Java Process Priority", "Sets javaw.exe process priority to high.", "Gaming", false, false, RiskLevel.Moderate, "Needs review", "Disabled because the app must not silently change user processes.", "Optimizerstuff/main.bat:159; Optimizerstuff/compiled.bat:59; Optimizerstuff/aio.bat:97", "Yes", "No restart"),
            Batch("batch-standby-memory-clear", "Standby Memory Clear", "Attempts to clear standby memory through a nonstandard PowerShell command.", "Storage", false, false, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:164; Optimizerstuff/compiled.bat:63; Optimizerstuff/aio.bat:101", "No", "No restart"),
            Batch("batch-high-performance-plan", "High Performance Power Plan", "Switches to the built-in high performance power plan.", "Power", false, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:168; Optimizerstuff/compiled.bat:65; Optimizerstuff/aio.bat:103", "Yes", "No restart"),
            Batch("batch-processor-idle-plan", "Processor Idle Power Setting", "Changes processor idle behavior in the current power plan.", "Power", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:305-306", "Yes", "Restart unknown"),
            Batch("batch-bcd-timers", "Boot Timer Settings", "Changes BCDEdit timer settings.", "Power", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:301-304", "Yes", "Restart required"),
            Batch("batch-nagle-adapter", "Adapter Nagle / ACK Registry Values", "Writes TcpNoDelay, TcpAckFrequency, TcpDelAckTicks, and InterfaceMetric to adapter registry paths.", "Network", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat; Optimizerstuff/aio.bat; Optimizerstuff/aiomain.bat", "Yes", "Restart unknown"),
            Batch("batch-global-tcp-registry", "Global TCP Registry Values", "Adds global TcpAckFrequency, TCPNoDelay, TCPDelAckTicks, and DisableTaskOffload values.", "Network", true, true, RiskLevel.Advanced, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat; Optimizerstuff/compiled.bat; Optimizerstuff/aio.bat", "Yes", "Restart unknown"),
            Batch("batch-msmq-tcp-nodelay", "MSMQ TCPNoDelay", "Changes MSMQ TCP behavior.", "Services", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat; Optimizerstuff/aio.bat", "Yes", "Restart unknown"),
            Batch("batch-psched-bandwidth", "QoS Reservable Bandwidth Policy", "Changes Psched NonBestEffortLimit.", "Network", true, true, RiskLevel.Moderate, "Needs review", "Disabled because this is a common low-value tweak with unclear modern benefit.", "Optimizerstuff/main.bat:297; Optimizerstuff/aio.bat:249-260", "Yes", "Restart unknown"),
            Batch("batch-minecraft-qos-policy", "Minecraft Java QoS Policy", "Adds DSCP/throttle policy registry values for javaw.exe.", "Gaming", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/aio.bat:249-260", "Yes", "Restart unknown"),
            Batch("batch-disable-ipv6", "Disable IPv6", "Disables IPv6 through netsh.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:590; Optimizerstuff/aio.bat:193", "Yes", "Restart unknown"),
            Batch("batch-dns-service-priority", "DNS Service Provider Priority", "Changes Local/Hosts/DNS/NetBT service-provider priority values.", "Network", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:605-608; Optimizerstuff/aio.bat:199-203; Optimizerstuff/aiomain.bat:330-333", "Yes", "Restart unknown"),
            Batch("batch-delivery-optimization", "Delivery Optimization Download Mode", "Changes Windows Delivery Optimization registry values.", "Drivers / Updates", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:645-647; Optimizerstuff/aio.bat:232-234", "Yes", "Restart unknown"),
            Batch("batch-broad-tcpip-registry", "Broad TCP/IP Registry Bundle", "Writes many TCP/IP parameter values, including routing and PMTU-related values.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:327-356; Optimizerstuff/aio.bat:262-276", "Unknown", "Restart unknown"),
            Batch("batch-ndis-rss-base-cpu", "NDIS RSS Base CPU", "Changes NDIS RSS base CPU registry value.", "Hardware", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:409; Optimizerstuff/aiomain.bat:441", "Yes", "Restart unknown"),
            Batch("batch-tcp-security-profiles", "TCP Security Profiles", "Disables TCP memory pressure protection and security profiles.", "Network", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:433,437; Optimizerstuff/aiomain.bat:453,455", "Unknown", "Restart unknown"),
            Batch("batch-neighbor-cache-limit", "Neighbor Cache Limit", "Increases neighbor/ARP cache limit.", "Network", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:441; Optimizerstuff/aiomain.bat:457", "Yes", "Restart unknown"),
            Batch("batch-isatap-teredo-disable", "Disable ISATAP / Teredo", "Disables Windows transition tunnel adapters.", "Network", true, true, RiskLevel.Moderate, "Needs review", NeedsReviewReason, "Optimizerstuff/main.bat:449,453; Optimizerstuff/aiomain.bat:461,463", "Yes", "Restart unknown"),
            Batch("batch-invalid-stopthelag", "Invalid StopTheLag Commands", "Contains invalid or unreliable PowerShell networking commands.", "Gaming", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/main.bat:493-567", "Unknown", "Restart unknown"),
            Batch("batch-minecraft-java-launch", "Minecraft Java Launch Profile", "Launches Minecraft using a hardcoded Java path and arguments.", "Gaming", false, false, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/compiled.bat:97-103; Optimizerstuff/aio.bat:150-156", "No", "No restart"),
            Batch("batch-placeholder-echo", "Placeholder Optimizer Branches", "Batch branches that only echo a hardcoded value and do not optimise anything.", "Advanced", false, false, RiskLevel.Safe, "Not implemented yet", "Disabled because the batch content does not perform an optimisation.", "Optimizerstuff/aiomain.bat:138,168,195", "Yes", "No restart"),
            Batch("batch-device-msi-priority", "Device MSI / Priority Registry Values", "Writes interrupt/MSI registry values under an undefined device path.", "Hardware", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/aiomain.bat:298-299", "Unknown", "Restart unknown"),
            Batch("batch-lanmanserver-profile", "LanmanServer / SMB Profile", "Changes SMB/server service behavior such as oplocks and sharing retries.", "Services", true, true, RiskLevel.Dangerous, "Disabled for safety", DangerousReason, "Optimizerstuff/aiomain.bat:514-527", "Yes", "Restart unknown"),
            Batch("batch-routes-missing", "Routes Optimizer Missing Implementation", "The menu references a routes optimizer, but no route commands were found.", "Network", false, false, RiskLevel.Safe, "Not implemented yet", "Disabled because no real optimisation command exists in the batch file.", "Optimizerstuff/aiomain.bat:57,73", "Unknown", "No restart")
        };
    }

    private static OptimizationAction Implemented(
        string id,
        string name,
        string description,
        string category,
        bool requiresRestart,
        bool requiresAdmin,
        RiskLevel riskLevel,
        string exactAction,
        string undoAction,
        string source)
    {
        return new OptimizationAction
        {
            Id = id,
            Name = name,
            Description = description,
            Category = category,
            ExactAction = exactAction,
            UndoAction = undoAction,
            Source = source,
            ImplementationStatus = "Implemented",
            RestartBadgeText = requiresRestart ? "Restart required" : "No restart",
            Reversibility = "Yes",
            RequiresRestart = requiresRestart,
            RequiresAdmin = requiresAdmin,
            RequiresSystemWarning = requiresAdmin || requiresRestart,
            RiskLevel = riskLevel,
            IsImplemented = true,
            IsEnabled = true,
            Reversible = true,
            Status = OptimizationStatus.Waiting
        };
    }

    private static OptimizationAction Candidate(
        string id,
        string name,
        string description,
        string category,
        bool requiresRestart,
        bool requiresAdmin,
        RiskLevel riskLevel,
        string implementationStatus,
        string disabledReason,
        string source,
        string reversibility)
    {
        return CatalogAction(id, name, description, category, requiresRestart, requiresAdmin, riskLevel, implementationStatus, disabledReason, source, reversibility, requiresRestart ? "Restart required" : "No restart");
    }

    private static OptimizationAction Batch(
        string id,
        string name,
        string description,
        string category,
        bool requiresRestart,
        bool requiresAdmin,
        RiskLevel riskLevel,
        string implementationStatus,
        string disabledReason,
        string source,
        string reversibility,
        string restartBadgeText)
    {
        return CatalogAction(id, name, description, category, requiresRestart, requiresAdmin, riskLevel, implementationStatus, disabledReason, source, reversibility, restartBadgeText);
    }

    private static OptimizationAction CatalogAction(
        string id,
        string name,
        string description,
        string category,
        bool requiresRestart,
        bool requiresAdmin,
        RiskLevel riskLevel,
        string implementationStatus,
        string disabledReason,
        string source,
        string reversibility,
        string restartBadgeText)
    {
        var isUnavailable = IsExplicitlyUnavailable(riskLevel, implementationStatus, disabledReason);

        return new OptimizationAction
        {
            Id = id,
            Name = name,
            Description = description,
            Category = category,
            ExactAction = isUnavailable ? "Unavailable: no safe command can run from this row." : "Selectable: Mist will preview/log this action unless a safe apply handler exists.",
            UndoAction = reversibility == "Yes" ? "A future implementation must save and restore the original value first." : "No safe revert path is currently available.",
            Source = source,
            ImplementationStatus = implementationStatus,
            DisabledReason = isUnavailable ? disabledReason : string.Empty,
            RestartBadgeText = restartBadgeText,
            Reversibility = reversibility,
            RequiresRestart = requiresRestart,
            RequiresAdmin = requiresAdmin,
            RequiresSystemWarning = requiresAdmin || requiresRestart || riskLevel != RiskLevel.Safe,
            RiskLevel = riskLevel,
            IsImplemented = false,
            IsEnabled = !isUnavailable,
            Reversible = reversibility == "Yes",
            Status = isUnavailable
                ? OptimizationStatus.Skipped
                : OptimizationStatus.NotImplemented
        };
    }

    private static OptimizationAction MergeDuplicateSources(IReadOnlyList<OptimizationAction> actions)
    {
        if (actions.Count == 1)
        {
            return actions[0];
        }

        var first = actions[0];
        var sources = string.Join("; ", actions.Select(action => action.Source).Distinct(StringComparer.OrdinalIgnoreCase));

        return new OptimizationAction
        {
            Id = first.Id,
            Name = first.Name,
            Description = first.Description,
            Category = first.Category,
            ExactAction = first.ExactAction,
            UndoAction = first.UndoAction,
            Source = sources,
            ImplementationStatus = first.ImplementationStatus,
            DisabledReason = first.DisabledReason,
            RestartBadgeText = first.RestartBadgeText,
            Reversibility = first.Reversibility,
            RequiresRestart = first.RequiresRestart,
            RequiresAdmin = first.RequiresAdmin,
            RequiresSystemWarning = first.RequiresSystemWarning,
            RiskLevel = first.RiskLevel,
            IsImplemented = first.IsImplemented,
            IsEnabled = first.IsEnabled,
            Reversible = first.Reversible,
            Status = first.Status
        };
    }

    private IEnumerable<OptimizationAction> ParseOptimizerstuffActions(IEnumerable<string> batchFiles, ref int loadingFailures)
    {
        var actions = new List<OptimizationAction>();

        foreach (var path in batchFiles)
        {
            IEnumerable<(int LineNumber, string Command)> commands;

            try
            {
                commands = File.ReadLines(path)
                    .Select((line, index) => (LineNumber: index + 1, Command: line.Trim()))
                    .Where(item => IsCommandLikeLine(item.Command))
                    .ToList();
            }
            catch (Exception ex)
            {
                loadingFailures++;
                _logger?.Error($"Failed to load Optimizerstuff file {path}: {ex.Message}");
                continue;
            }

            foreach (var command in commands)
            {
                var category = CategorizeCommand(command.Command);
                var fileName = Path.GetFileName(path);
                var id = $"optimizerstuff-{SanitizeId(fileName)}-{command.LineNumber}";

                actions.Add(new OptimizationAction
                {
                    Id = id,
                    Name = $"Optimizerstuff: {SummarizeCommand(command.Command)}",
                    Description = "Imported directly from Optimizerstuff so the definition remains visible for review.",
                    Category = category,
                    ExactAction = command.Command,
                    UndoAction = "No safe automatic revert is available until this imported command is reviewed.",
                    Source = $"{fileName}:{command.LineNumber}",
                    ImplementationStatus = "Imported for review",
                    DisabledReason = NeedsReviewReason,
                    RestartBadgeText = InferRestartBadge(command.Command),
                    Reversibility = "Unknown",
                    RequiresRestart = InferRequiresRestart(command.Command),
                    RequiresAdmin = true,
                    RequiresSystemWarning = true,
                    RiskLevel = RiskLevel.Advanced,
                    IsImplemented = false,
                    IsEnabled = IsSupportedImportedCommand(command.Command),
                    Reversible = false,
                    Status = IsSupportedImportedCommand(command.Command)
                        ? OptimizationStatus.NotImplemented
                        : OptimizationStatus.Skipped
                });
            }
        }

        return actions;
    }

    private static OptimizationCatalogReport BuildReport(IReadOnlyList<OptimizationAction> actions, string? optimizerstuffPath, int batchFileCount, int parsedCommandCount, int loadingFailures)
    {
        return new OptimizationCatalogReport
        {
            OptimizerstuffPath = optimizerstuffPath ?? "Not found",
            BatchFileCount = batchFileCount,
            ParsedCommandCount = parsedCommandCount,
            LoadingFailureCount = loadingFailures,
            SafeCount = actions.Count(action => action.RiskLevel == RiskLevel.Safe),
            CautionCount = actions.Count(action => action.RiskLevel is RiskLevel.Moderate or RiskLevel.Advanced),
            DangerousCount = actions.Count(action => action.RiskLevel == RiskLevel.Dangerous),
            ConflictCount = actions.Count(action => action.ImplementationStatus == "Conflict"),
            DuplicateCount = Math.Max(0, parsedCommandCount - actions.Count),
            RequiresRestartCount = actions.Count(action => action.RequiresRestart),
            NoRestartCount = actions.Count(action => !action.RequiresRestart),
            UnknownRestartCount = actions.Count(action => action.RestartBadge.Contains("unknown", StringComparison.OrdinalIgnoreCase)),
            VisibleRestartCount = actions.Count(action => action.RequiresRestart),
            VisibleNoRestartCount = actions.Count(action => !action.RequiresRestart),
            ApplyableCount = actions.Count(action => action.IsEnabled),
            DisabledCount = actions.Count(action => !action.IsEnabled)
        };
    }

    private static string? FindOptimizerstuffPath()
    {
        var candidates = new List<string>
        {
            Path.Combine(Environment.CurrentDirectory, "Optimizerstuff"),
            Path.Combine(AppContext.BaseDirectory, "Optimizerstuff")
        };

        AddAncestorCandidates(Environment.CurrentDirectory, candidates);
        AddAncestorCandidates(AppContext.BaseDirectory, candidates);

        return candidates
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(Directory.Exists);
    }

    private static void AddAncestorCandidates(string startPath, ICollection<string> candidates)
    {
        var directory = new DirectoryInfo(startPath);

        while (directory is not null)
        {
            candidates.Add(Path.Combine(directory.FullName, "Optimizerstuff"));
            directory = directory.Parent;
        }
    }

    private static int CountCommandLikeLines(string path, ref int loadingFailures)
    {
        try
        {
            return File.ReadLines(path).Count(line => IsCommandLikeLine(line.TrimStart()));
        }
        catch
        {
            loadingFailures++;
            return 0;
        }
    }

    private static bool IsCommandLikeLine(string trimmed)
    {
        return trimmed.StartsWith("netsh ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("ipconfig ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("reg ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("reg.exe ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("REG ADD ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("powershell ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("wmic ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("bcdedit ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("powercfg ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("PowerCfg ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("%JAVA_PATH%", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedImportedCommand(string command)
    {
        return IsCommandLikeLine(command.TrimStart())
            && !command.Contains("INVALID", StringComparison.OrdinalIgnoreCase)
            && !command.Contains("undefined", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExplicitlyUnavailable(RiskLevel riskLevel, string implementationStatus, string disabledReason)
    {
        if (riskLevel == RiskLevel.Dangerous ||
            implementationStatus.Contains("Disabled for safety", StringComparison.OrdinalIgnoreCase) ||
            implementationStatus.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return disabledReason.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
               disabledReason.Contains("no real optimisation", StringComparison.OrdinalIgnoreCase) ||
               disabledReason.Contains("no real optimization", StringComparison.OrdinalIgnoreCase) ||
               disabledReason.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
               disabledReason.Contains("undefined", StringComparison.OrdinalIgnoreCase) ||
               disabledReason.Contains("hardcoded", StringComparison.OrdinalIgnoreCase);
    }

    private static string CategorizeCommand(string command)
    {
        if (command.Contains("tcp", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("netsh", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("dns", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("ipconfig", StringComparison.OrdinalIgnoreCase))
        {
            return "Network";
        }

        if (command.Contains("powercfg", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("bcdedit", StringComparison.OrdinalIgnoreCase))
        {
            return "Power";
        }

        if (command.Contains("windowsupdate", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("deliveryoptimization", StringComparison.OrdinalIgnoreCase))
        {
            return "Drivers / Updates";
        }

        if (command.Contains("java", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("game", StringComparison.OrdinalIgnoreCase))
        {
            return "Gaming";
        }

        return "Uncategorized";
    }

    private static bool InferRequiresRestart(string command)
    {
        return command.Contains("bcdedit", StringComparison.OrdinalIgnoreCase)
            || command.Contains(@"HKLM\SYSTEM", StringComparison.OrdinalIgnoreCase)
            || command.Contains("Tcpip", StringComparison.OrdinalIgnoreCase)
            || command.Contains("reboot", StringComparison.OrdinalIgnoreCase)
            || command.Contains("restart", StringComparison.OrdinalIgnoreCase);
    }

    private static string InferRestartBadge(string command)
    {
        return InferRequiresRestart(command) ? "Restart likely" : "Restart unknown";
    }

    private static string SummarizeCommand(string command)
    {
        var text = command.Length > 54 ? $"{command[..54]}..." : command;
        return text.Replace("\t", " ");
    }

    private static string SanitizeId(string text)
    {
        var chars = text.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-').ToArray();
        return new string(chars).Trim('-');
    }
}
