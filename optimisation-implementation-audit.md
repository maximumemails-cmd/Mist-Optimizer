# Optimisation Implementation Audit

Static audit date: 2026-05-27

Scope scanned: `Optimizerstuff/**/*.bat`

Batch files found:

- `Optimizerstuff/main.bat`
- `Optimizerstuff/compiled.bat`
- `Optimizerstuff/aio.bat`
- `Optimizerstuff/aiomain.bat`

Important handling notes:

- No batch file was executed.
- Commands are consolidated by unique command/setting so duplicate copies do not create duplicate UI toggles.
- The current app startup scan finds 382 executable command/setting lines across the four batch files. These are represented below as 67 audited command/setting rows.
- Only one batch-derived optimisation was implemented: `ipconfig /flushdns`, exposed once as `Flush DNS Cache`.
- Commands that change broad networking state, boot configuration, power plans, device registry paths, security profiles, Windows Update delivery behavior, process priority, or launch external programs were skipped.
- No Defender, firewall-disable, UAC-disable, SmartScreen-disable, shadow-copy deletion, user creation, downloader, or encoded PowerShell payload was found. One `netsh advfirewall reset` was found; it was marked dangerous and skipped because it changes firewall configuration broadly.

## Implemented Optimisations

| Optimisation id | Name | Source command | Category | Requires admin | Requires restart | Reversible | Preview | Apply | Revert | Verification |
|---|---|---|---|---|---|---|---|---|---|---|
| `network-flush-dns` | Flush DNS Cache | `ipconfig /flushdns` | No restart | No | No | Yes, no persistent setting | Logs current/new state and source references | Runs fixed Windows-only `ipconfig /flushdns`; no user input is passed | Logs that no persistent revert is needed | Requires process exit code `0`; otherwise fails |

## Audit Table

| Batch file path | Command / setting | What it does | Risk level | Requires admin | Requires restart | Reversible | Should implement | Reason |
|---|---|---|---|---|---|---|---|---|
| All files | `@echo off`, labels, `goto`, menu branching | Console/menu control only | Safe | No | No | Yes | No | Not an optimisation. |
| All files | ANSI color variables and banner `echo` output | Console display only | Safe | No | No | Yes | No | UI-only batch behavior. |
| All files | `chcp 65001`, `chcp 437` | Changes console code page for the batch process | Safe | No | No | Yes | No | Batch console-only behavior. |
| All files | `cls`, `pause`, `timeout` | Console flow control | Safe | No | No | Yes | No | Not an optimisation. |
| `compiled.bat`, `aio.bat`, `aiomain.bat` | Password/key prompts using `set /p` | Gates batch menu access | Caution | No | No | Yes | No | Not useful in the app; hardcoded key is poor practice. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `net session >nul 2>&1` | Checks admin state | Safe | No | No | Yes | No | App already has a backend admin check. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `powershell -Command "Start-Process '%~f0' -Verb RunAs"` | Relaunches batch elevated | Caution | Yes | No | Unknown | No | The app must not self-elevate or run batch files. |
| `main.bat` | `powershell -NoProfile Enable-ComputerRestore -Drive 'C:\'` | Enables System Restore on C: | Caution | Yes | No | Unknown | No | Backup-related, not an optimisation; needs a dedicated Windows restore implementation. |
| `main.bat` | Delete `RPSessionInterval`, `DisableConfig`; set `SystemRestorePointCreationFrequency=0` | Alters System Restore policy/creation frequency | Dangerous | Yes | Unknown | Unknown | No | Broad registry changes to restore configuration. |
| `main.bat` | `Checkpoint-Computer -Description ...` | Creates a restore point | Caution | Yes | No | No | No | Useful backup idea, but not an optimisation and needs a dedicated safe Windows-only workflow. |
| `main.bat` | PowerShell `System.Windows.Forms.MessageBox` | Displays batch popup | Safe | No | No | Yes | No | UI-only behavior. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `ipconfig /flushdns` | Clears DNS resolver cache | Safe | No | No | Yes | Yes | Implemented once as a fixed, logged Windows-only action. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `netsh int tcp set global autotuninglevel=normal/disabled`; PowerShell `Set-NetTCPSetting -AutoTuningLevelLocal Normal/Restricted/HighlyRestricted` | Changes TCP receive auto-tuning | Caution | Yes | Unknown | Yes | No | Conflicting values across files; can hurt networking/games/VPNs. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `netsh int tcp set global congestionprovider=ctcp/none`; supplemental CTCP | Changes congestion provider | Caution | Yes | Unknown | Yes | No | Conflicting, environment-sensitive TCP tuning. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `netsh int tcp set global ecncapability=disabled` | Disables ECN | Caution | Yes | Unknown | Yes | No | Network-stack tuning with unclear benefit. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `netsh int tcp set global rss=enabled/disabled`; PowerShell `ReceiveSideScaling Disabled` | Changes receive-side scaling | Caution | Yes | Unknown | Yes | No | Conflicting values and can reduce network performance. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `netsh int tcp set global chimney=enabled/disabled` | Changes TCP chimney offload | Caution | Yes | Unknown | Yes | No | Deprecated/adapter-sensitive network tuning. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `netsh int tcp set global dca=enabled/disabled`, `EnableDCA` | Changes direct cache access/offload behavior | Caution | Yes | Unknown | Yes | No | Adapter-specific and conflicting. |
| `main.bat`, `aiomain.bat` | `netsh int tcp set global netdma=enabled/disabled` | Changes NetDMA | Caution | Yes | Unknown | Yes | No | Legacy adapter feature with unclear support. |
| `main.bat`, `aio.bat`, `aiomain.bat` | PowerShell `Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Enabled/Disabled` and `netsh ... rsc=enabled/disabled` | Changes receive segment coalescing | Caution | Yes | Unknown | Yes | No | Conflicting values; can affect network adapters and VPNs. |
| `main.bat`, `aiomain.bat` | `netsh interface ip set global taskoffload=enabled/disabled`, `DisableTaskOffload` | Changes task offload | Caution | Yes | Unknown | Yes | No | Conflicting and hardware-specific. |
| `main.bat`, `aiomain.bat` | `netsh interface ipv4 set subinterface ... mtu=1400/1500 store=persistent` | Persists adapter MTU | Dangerous | Yes | Unknown | Yes | No | Hardcoded/undefined adapter names can break networking. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | TCP timestamps enabled/disabled | Changes TCP timestamp behavior | Caution | Yes | Unknown | Yes | No | Conflicting values and unclear user benefit. |
| `main.bat`, `aiomain.bat` | `initialRto`, `minRto`, `MaxSynRetransmissions` changes | Changes retransmission timing | Caution | Yes | Unknown | Yes | No | Network behavior risk and conflicting values. |
| `main.bat` | `maxconnections`, `dynamicport`, `maxuserport`, `MaxUserPort` changes | Changes TCP connection/port behavior | Caution | Yes | Unknown | Yes | No | Registry/netsh tuning with unclear benefit. |
| `main.bat`, `aio.bat`, `aiomain.bat` | `SackOpts`, `sackopts`, NonSack RTT resiliency | Changes SACK behavior | Caution | Yes | Unknown | Yes | No | Conflicting values and can harm TCP reliability. |
| `main.bat` | `synattackprotect=disabled/enabled` | Changes SYN attack protection | Dangerous | Yes | Unknown | Yes | No | Disabling network protection is security-weakening. |
| `main.bat` | `initialCongestionControlLevel`, `initialCongestionWindow` | Changes TCP startup behavior | Caution | Yes | Unknown | Unknown | No | Unclear support and benefit. |
| `main.bat` | `nonlocalsource=enabled/disabled` | Changes source-address behavior | Dangerous | Yes | Unknown | Yes | No | Could weaken networking safety and compatibility. |
| `main.bat` | `netsh interface tcp set heuristics disabled` | Changes TCP heuristics | Caution | Yes | Unknown | Yes | No | System-wide TCP tuning with unclear benefit. |
| `main.bat` | `netsh int udp set global uro=disabled` | Changes UDP receive offload | Caution | Yes | Unknown | Yes | No | Adapter-specific network tuning. |
| `main.bat`, `compiled.bat`, `aio.bat`, `aiomain.bat` | `netsh winsock reset`, `netsh int ip reset`, `netsh int reset all`, IPv4/IPv6 reset | Resets network stack | Dangerous | Yes | Yes | Unknown | No | Disruptive; can break connectivity and requires restart. |
| `main.bat`, `aio.bat`, `aiomain.bat` | `ipconfig /release`, `ipconfig /renew` | Drops and renews IP leases | Caution | No | No | Yes | No | Interrupts connectivity; not an optimisation. |
| `main.bat` | `netsh advfirewall reset` | Resets Windows Firewall configuration | Dangerous | Yes | Unknown | Unknown | No | Broad firewall/security configuration change. |
| `main.bat`, `compiled.bat`, `aio.bat` | `wmic path win32_networkadapter ... setduplexmode (2)` | Changes adapter duplex mode | Dangerous | Yes | Unknown | Unknown | No | Hardware-specific and can break networking. |
| `main.bat`, `compiled.bat`, `aio.bat` | `wmic process where name="javaw.exe" CALL setpriority "high priority"` | Sets Java process priority | Caution | No | No | Yes | No | Silently changes user processes; app policy forbids this. |
| `main.bat`, `compiled.bat`, `aio.bat` | `powershell -command "Clear-StandbyMemory"` | Attempts standby memory clear | Caution | Unknown | No | No | No | Nonstandard command; Windows manages memory automatically. |
| `main.bat`, `compiled.bat`, `aio.bat` | `powercfg /setactive 8c5e7fda...` | Switches to High Performance power plan | Caution | Yes | No | Yes | No | Real but not batch-derived safely enough; requires plan backup and user-facing power workflow. |
| `main.bat` | `PowerCfg /SETACVALUEINDEX ... IDLEDISABLE 000`; `PowerCfg /SETACTIVE SCHEME_CURRENT` | Changes current power plan processor idle setting | Caution | Yes | Unknown | Yes | No | Needs careful plan backup/restore and laptop guidance. |
| `main.bat` | `bcdedit /set disabledynamictick yes/true` | Changes boot timer behavior | Dangerous | Yes | Yes | Yes | No | Boot configuration tweak with recovery/performance risk. |
| `main.bat` | `bcdedit /set useplatformclock false`, `useplatformtick false` | Changes boot timer source | Dangerous | Yes | Yes | Yes | No | Boot configuration tweak; can harm stability. |
| `main.bat`, `aio.bat`, `aiomain.bat` | Adapter GUID registry `TcpAckFrequency`, `TCPNoDelay`, `TcpDelAckTicks`, `InterfaceMetric` | Disables Nagle-like behavior / changes adapter TCP values | Caution | Yes | Unknown | Yes | No | Adapter-specific, duplicated, and not reliably beneficial. |
| `main.bat`, `compiled.bat`, `aio.bat` | Global TCP registry `TcpAckFrequency`, `TCPNoDelay`, `TCPDelAckTicks`, `DisableTaskOffload` | Adds nonstandard global TCP tuning values | Caution | Yes | Unknown | Yes | No | Unclear purpose and possible placebo/global side effects. |
| `main.bat`, `aio.bat` | `HKLM\SOFTWARE\Microsoft\MSMQ\Parameters TCPNoDelay` | Changes MSMQ TCP behavior | Caution | Yes | Unknown | Yes | No | Not relevant for most users and can affect services. |
| `main.bat`, `aio.bat` | `HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched NonBestEffortLimit=0` | Changes QoS reservable bandwidth policy | Caution | Yes | Unknown | Yes | No | Common myth tweak; unclear benefit. |
| `aio.bat` | QoS policy under `HKLM\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe` | Adds DSCP/throttle policy for Java/Minecraft traffic | Caution | Yes | Unknown | Yes | No | Needs a dedicated user-scoped app policy editor; not implemented from batch. |
| `main.bat`, `aio.bat` | `netsh int ipv6 set state disabled` | Disables IPv6 | Dangerous | Yes | Unknown | Yes | No | Can break Windows networking, VPNs, Xbox services, and normal laptop use. |
| `main.bat`, `aio.bat`, `aiomain.bat` | `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider` priorities | Changes DNS/name-resolution priority values | Caution | Yes | Unknown | Yes | No | Registry tweak with unclear modern benefit. |
| `main.bat`, `aio.bat` | Delivery Optimization `DODownloadMode`, `DownloadMode` set to `0` | Changes Windows update delivery optimization | Caution | Yes | Unknown | Yes | No | Affects Windows Update delivery behavior. |
| `main.bat`, `aio.bat` | `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters` values: `TcpTimedWaitDelay`, `EnablePMTUDiscovery`, `DontAddDefaultGatewayDefault`, `SyncDomainWithMembership`, `Tcp1323Opts`, `ForwardBroadcasts`, `IPEnableRouter` | Broad TCP/IP parameter writes | Dangerous | Yes | Unknown | Yes | No | Broad registry edits, some can change routing behavior. |
| `main.bat`, `aio.bat` | TCP/IP values: `AdaptiveCongestionControl`, `AggressiveTCPOptimization`, `CustomizedMTU`, `DynamicNetworkAdaption`, `DynamicPortRange`, `EnhancedTaskOffloading`, `OptimizedBufferSizes` | Adds nonstandard or unclear TCP tuning values | Dangerous | Yes | Unknown | Unknown | No | Unclear purpose; likely placebo or harmful. |
| `main.bat`, `aio.bat` | TCP/IP values: `GlobalMaxTcpWindowSize`, `TcpWindowSize`, `TcpMaxConnectRetransmissions`, `TcpMaxDataRetransmissions`, `TcpMaxDupAcks`, `TcpMaxHalfOpen`, `TcpMaxHalfOpenRetried`, `TcpNumConnections` | Broad TCP window/retry/connection tuning | Caution | Yes | Unknown | Yes | No | Environment-sensitive and duplicated with conflicts. |
| `main.bat`, `aio.bat` | TCP/IP values: `TcpUseRFC1122UrgentPointer`, `UseDomainNameDevolution`, `DefaultTTL` | Changes global TCP/IP behavior | Caution | Yes | Unknown | Yes | No | Not clearly beneficial for gaming optimization. |
| `main.bat`, `aiomain.bat` | `HKLM\SYSTEM\CurrentControlSet\Services\Ndis\Parameters RssBaseCpu=1` | Changes RSS CPU base | Caution | Yes | Unknown | Yes | No | Hardware-specific. |
| `main.bat`, `aiomain.bat` | `netsh int tcp set security mpp=disabled`, `profiles=disabled` | Disables TCP security features/profiles | Dangerous | Yes | Unknown | Unknown | No | Security-weakening network change. |
| `main.bat`, `aiomain.bat` | `netsh int ip set global neighborcachelimit=4096` | Increases neighbor/ARP cache limit | Caution | Yes | Unknown | Yes | No | Niche network setting; unclear value. |
| `main.bat`, `aiomain.bat` | `netsh int isatap set state disabled`, `netsh int teredo set state disabled` | Disables transition tunnel adapters | Caution | Yes | Unknown | Yes | No | Can affect IPv6/VPN/network compatibility. |
| `main.bat` | Invalid `New-NetTCPConnection` / `New-NetOffloadLocal` PowerShell blocks gated by `C:\Windows\Windows NT.exe` | Appears to be fake/unreliable conditioning | Dangerous | Unknown | Unknown | Unknown | No | Invalid or suspicious commands; not useful. |
| `compiled.bat`, `aio.bat` | `set JAVA_PATH=...`, `set MINECRAFT_DIR=...`, large `JAVA_OPTIONS`, `%JAVA_PATH% %JAVA_OPTIONS%` | Launches Minecraft via a hardcoded Java path and classpath | Dangerous | No | No | No | No | Runs an external program and can break user launch profiles. |
| `aiomain.bat` | `echo 1488` placeholder branches | Does nothing useful | Safe | No | No | Yes | No | Placeholder/fake optimizer behavior. |
| `aiomain.bat` | `REG ADD !RegistryQueryResult!\Device Parameters\... MSISupported`, `DevicePriority` | Edits device interrupt/device-priority registry path | Dangerous | Yes | Unknown | Unknown | No | Undefined registry root and device-level risk. |
| `aiomain.bat` | Hypixel/Best Hitreg PowerShell conditioning presets | Changes TCP auto-tuning and RSC presets | Caution | Yes | Unknown | Yes | No | Duplicates conflicting network tuning. |
| `aiomain.bat` | LanmanServer `autodisconnect`, `Size`, `EnableOplocks`, `IRPStackSize`, `SharingViolationDelay`, `SharingViolationRetries` | Changes SMB/server service behavior | Dangerous | Yes | Unknown | Yes | No | Can affect file sharing and normal Windows behavior. |
| `main.bat`, `aiomain.bat` | Revert blocks using broad network resets plus release/renew | Attempts broad network reset as undo | Dangerous | Yes | Yes | Unknown | No | Revert is too broad and can break connectivity. |
| `aio.bat`, `main.bat` | Support URL / Discord echo text | Prints support contact info | Safe | No | No | Yes | No | Not an optimisation. |
| `aiomain.bat` | Menu references `Routes Optimizer` without implementation block | Missing optimizer | Safe | No | No | Unknown | No | No actual command exists to implement. |
| All files | Repeated duplicate network bundles: Hitreg, Ping, Packet Loss, Advanced, QoS, StopTheLag | Bundles many commands above | Caution | Mixed | Mixed | Mixed | No | Duplicated/conflicting bundle wrappers; individual commands audited above. |

## Deduplication And Conflict Decisions

| Decision | Count | Notes |
|---|---:|---|
| `.bat` files found | 4 | Recursive scan of `Optimizerstuff/**/*.bat`. |
| Command-like lines scanned | 582 | Static text scan only. |
| Audited command/setting rows | 67 | Duplicates consolidated by unique behavior/setting. |
| Optimisations implemented | 1 | `network-flush-dns`. |
| Skipped as dangerous | 23 | Includes firewall reset, boot edits, IPv6 disable, broad resets, security-profile disables, device registry edits, unclear broad registry edits, Java launch. |
| Skipped as duplicates | 39 | Repeated DNS flush and repeated network bundles/settings across files. |
| Skipped due to conflicts | 16 | Conflicting TCP/RSS/RSC/DCA/offload/MTU/timer values. |
| Skipped as not useful/non-optimisation | 26 | Console UI, menu flow, hardcoded key prompts, support text, placeholders. |

## Implementation Notes

- Visible batch-derived toggles with no real backend action were removed from the central catalog.
- The UI now shows the full visible catalogue again: implemented rows are selectable, while unsafe/unimplemented/conflicting rows are visible but disabled with a reason.
- `network-flush-dns` supports preview, apply, verification, logging, and state recording.
- `optimizer-state.json` is written to the app's local data folder when an implemented action runs.
- Preview mode reports source references, intended change, admin/restart/revert flags, and does not modify the system.
- Revert mode reads `optimizer-state.json`; for DNS flush it verifies that no persistent setting needs restoring.
