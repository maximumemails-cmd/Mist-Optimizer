@echo off
title Stockcatt's All In One

: Colors

set w=[97m
set p=[91m
set b=[90m
set v=[91m
set o=[31m
set j=[93m
set u=[94m
set m=[95m
set h=[92m

: Unicode
chcp 65001 >nul 2>&1

: Restore Point
: : Poll
:rppoll

echo.
echo.
echo.
echo.
echo.
echo.             %b%█▀▄ █▀█   █▄█ █▀█ █░█   █░█░█ ▄▀█ █▄░█ ▀█▀   ▀█▀ █▀█   █▀▄▀█ ▄▀█ █▄▀ █▀▀   ▄▀█   █▀█ █▀▀ █▀ ▀█▀ █▀█ █▀█ █▀▀   █▀█ █▀█ █ █▄░█ ▀█▀ ▀█
echo.             █▄▀ █▄█   ░█░ █▄█ █▄█   ▀▄▀▄▀ █▀█ █░▀█ ░█░   ░█░ █▄█   █░▀░█ █▀█ █░█ ██▄   █▀█   █▀▄ ██▄ ▄█ ░█░ █▄█ █▀▄ ██▄   █▀▀ █▄█ █ █░▀█ ░█░  ▄
echo.
echo.                          We advise you to make one, since you will not be able to revert all of your settings, only a part of it.
echo.                                                               FULLSCREEN RECMMENDED
echo.                                                                                                                                                
echo.                                                                        Y/N                                
set /p rp="%w%"                                                                           
if "%rp%"=="y" goto restorepoint
if "%rp%"=="n" goto main

:restorepoint
: : Enable restore points
chcp 437 >nul 2>&1
powershell -NoProfile Enable-ComputerRestore -Drive 'C:\' >nul 2>&1
Reg.exe delete "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore" /v "RPSessionInterval" /f  >nul 2>&1
Reg.exe delete "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore" /v "DisableConfig" /f >nul 2>&1
Reg.exe add "HKLM\Software\Microsoft\Windows NT\CurrentVersion\SystemRestore" /v "SystemRestorePointCreationFrequency" /t REG_DWORD /d 0 /f >nul 2>&1
chcp 65001 >nul 2>&1
%b%
: : Create restore point
chcp 437 >nul 
echo %w%- Making a restore point... %b%
powershell -Command "Checkpoint-Computer -Description 'Rago Optimizer Restore Point' -RestorePointType 'MODIFY_SETTINGS'" 
powershell -Command "& {Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show('Restore point completed successfully', 'Rago Optimizer Restore Point', 'Ok', [System.Windows.Forms.MessageBoxIcon]::Information);}"
chcp 65001 >nul 
echo.
echo.
echo.

cls

goto main

: Main Menu
:main
cls

echo.                                           %b%░██████╗████████╗░█████╗░░█████╗░██╗░░██╗░█████╗░░█████╗░████████╗████████╗██╗░██████╗
echo.                                           ██╔════╝╚══██╔══╝██╔══██╗██╔══██╗██║░██╔╝██╔══██╗██╔══██╗╚══██╔══╝╚══██╔══╝╚█║██╔════╝
echo.                                           ╚█████╗░░░░██║░░░██║░░██║██║░░╚═╝█████═╝░██║░░╚═╝███████║░░░██║░░░░░░██║░░░░╚╝╚█████╗░
echo.                                           ░╚═══██╗░░░██║░░░██║░░██║██║░░██╗██╔═██╗░██║░░██╗██╔══██║░░░██║░░░░░░██║░░░░░░░╚═══██╗
echo.                                           ██████╔╝░░░██║░░░╚█████╔╝╚█████╔╝██║░╚██╗╚█████╔╝██║░░██║░░░██║░░░░░░██║░░░░░░██████╔╝
echo.                                           ╚═════╝░░░░╚═╝░░░░╚════╝░░╚════╝░╚═╝░░╚═╝░╚════╝░╚═╝░░╚═╝░░░╚═╝░░░░░░╚═╝░░░░░░╚═════╝░
echo. 
echo.                                                                 ▄▀█ █░░ █░░   █ █▄░█   █▀█ █▄░█ █▀▀
echo.                                                                 █▀█ █▄▄ █▄▄   █ █░▀█   █▄█ █░▀█ ██▄
echo.
echo.
echo.                                       %w%NETWORK                              %w%STOPTHELAG %p%BETA                              %w%QOS%b%
echo.                             %b%╔══════════════════════════╗            %b%╔══════════════════════════╗           ╔══════════════════════════╗
echo.                             ║%w%1. HITREG                 %b%║            ║%w%5. SUMO (0KB)             %b%║           ║%w%9. ADJUST DNS PRIOTITY    %b%║
echo.                             ║%w%2. PING                   %b%║            ║%w%6. BETTER HITREG          %b%║           ║%w%10. DISABLE IPv6          %b%║
echo.                             ║%w%3. PACKET LOSS            %b%║            ║%w%7. BALANCED               %b%║           ║%w%11. DISABLE NAGILES       %b%║
echo.                             ║%p% DO NOT RUN IF YOU ARE    %b%║            ║%w%8. CUSTOM MATTEW          %b%║           ║%w%ALGORITHM                 %b%║
echo.                             ║%p%ON CELLULAR DATA!!!       %b%║            ║                          ║           ║%w%12. OPTIMIZE ADAPTER      %b%║
echo.                             ║%w%4. ADVANCED               %b%║            ║                          ║           ║                          ║
echo.                             ║                          ║            ║                          ║           ║                          ║
echo.                             ║                          ║            ║                          ║           ║                          ║
echo.                             ╚══════════════════════════╝            ╚══════════════════════════╝           ╚══════════════════════════╝                 
echo.  
echo.
echo.                                                                              %w%SETTINGS
echo.                                                                   %b%╔═════════════════════════════╗
echo.                                                                   ║%w%13. REVERT OPTIMIZATION      %b%║
echo.                                                                   ║%w%14. EXIT                     %b%║
echo.                                                                   ║%w%15. SUPPORT                  %b%║
echo.                                                                   ║                             ║ 
echo.                                                                   ╚═════════════════════════════╝
echo.                                                                          
echo.                             
echo.                             
echo.                             
set /p menu="%w%"
if "%menu%"=="1" goto hitreg
if "%menu%"=="2" goto ping
if "%menu%"=="3" goto packetloss
if "%menu%"=="4" goto netadvanced
if "%menu%"=="5" goto sumo0kb
if "%menu%"=="6" goto betterhitreg
if "%menu%"=="7" goto balanced
if "%menu%"=="8" goto custommattew
if "%menu%"=="9" goto dnspriority
if "%menu%"=="10" goto ipv6
if "%menu%"=="11" goto nagiles
if "%menu%"=="12" goto adaptersettings
if "%menu%"=="13" goto revert
if "%menu%"=="14" goto close
if "%menu%"=="15" goto support
else
echo %w%- Invalid input. Please enter numbers 1-15. %b% & goto MisspellRedirect

:MisspellRedirect
cls
echo %w%- Invalid input  %b%
timeout 2
goto RedirectMenu

:RedirectMenu
cls
goto :main

: ----- NETWORK -----

: Hitreg
:hitreg
echo. %w% Applying advanced %p%hit registration %w%tweaks.
net session >nul 2>&1
if %errorLevel% == 0 (
    echo %w%Process running as %p%administrator%w%.
) else (
    echo Not running as administrator. Restarting as administrator...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

cls

netsh int tcp set global autotuninglevel=normal
netsh int tcp set global congestionprovider=ctcp
netsh int tcp set global ecncapability=disabled
netsh int tcp set global rss=enabled
ipconfig /flushdns

cls

reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TcpAckFrequency /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TCPNoDelay /t REG_DWORD /d 1 /f

cls

wmic process where name="javaw.exe" CALL setpriority "high priority"

cls

timeout /t 2 /nobreak >nul
powershell -command "Clear-StandbyMemory"

cls

powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c

cls

powershell -Command "& {Set-NetTCPSetting -SettingName internet -AutoTuningLevelLocal Normal}" > nul 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Disabled}" > nul 2>&1

cls

netsh interface ip set global taskoffload=disabled >nul 2>&1 
netsh interface tcp set global chimney=disabled >nul 2>&1 
netsh interface tcp set global rss=disabled >nul 2>&1 
netsh interface ipv4 set subinterface "%adapter_name%" mtu=1400 store=persistent >nul 2>&1 
netsh interface tcp set global autotuninglevel=normal >nul 2>&1 
netsh interface tcp set global congestionprovider=none >nul 2>&1 

cls

netsh interface tcp set global ecncapability=disabled >nul 2>&1 
netsh interface tcp set global timestamps=enabled >nul 2>&1 
netsh interface tcp set global dca=disabled >nul 2>&1 
netsh interface tcp set global netdma=disabled >nul 2>&1 
netsh interface tcp set global rsc=disabled >nul 2>&1 
netsh interface tcp set global fastopen=disabled >nul 2>&1 
netsh interface tcp set global initialrto=3000 >nul 2>&1 

cls

netsh interface tcp set global minrto=3000 >nul 2>&1 
netsh interface tcp set global maxsynretransmissions=5 >nul 2>&1 
netsh interface tcp set global maxconnections=4294967295 >nul 2>&1 
netsh interface tcp set global dynamicport start=49152 num=16384 >nul 2>&1 
netsh interface tcp set global maxuserport=65535 >nul 2>&1 
netsh interface tcp set global sackopts=disabled >nul 2>&1 

cls

netsh interface tcp set global synattackprotect=disabled >nul 2>&1 
netsh interface tcp set global initialCongestionControlLevel=0 >nul 2>&1 
netsh interface tcp set global initialCongestionWindow=1 >nul 2>&1 
netsh interface tcp set global nonlocalsource=enabled >nul 2>&1 
netsh interface tcp set heuristics disabled  
netsh int udp set global uro=disabled 

cls

netsh interface ip set global taskoffload=enabled >nul 2>&1 
netsh interface tcp set global chimney=enabled >nul 2>&1 
netsh interface tcp set global rss=enabled >nul 2>&1 
netsh interface ipv4 set subinterface "%adapter_name%" mtu=1400 store=persistent >nul 2>&1 
netsh interface tcp set global autotuninglevel=disabled >nul 2>&1 
netsh interface tcp set global congestionprovider=ctcp >nul 2>&1 

cls

netsh interface tcp set global ecncapability=disabled >nul 2>&1 
netsh interface tcp set global timestamps=disabled >nul 2>&1 
netsh interface tcp set global dca=enabled >nul 2>&1 
netsh interface tcp set global netdma=enabled >nul 2>&1 
netsh interface tcp set global rsc=enabled >nul 2>&1 
netsh interface tcp set global fastopen=enabled >nul 2>&1 

cls

netsh interface tcp set global initialRto=300ms >nul 2>&1 
netsh interface tcp set global minRto=300ms >nul 2>&1 
netsh interface tcp set global maxsynRetransmissions=2 >nul 2>&1 
netsh interface tcp set global maxconnections=65535 >nul 2>&1 
netsh interface tcp set global dynamicport start=1025 num=64511 >nul 2>&1 
netsh interface tcp set global maxuserport=65534 >nul 2>&1 

cls

netsh interface tcp set global sackopts=enabled >nul 2>&1 
netsh interface tcp set global synattackprotect=enabled >nul 2>&1 
netsh interface tcp set global initialCongestionControlLevel=1 >nul 2>&1 
netsh interface tcp set global initialCongestionWindow=2 >nul 2>&1 
netsh interface tcp set global nonlocalsource=disabled >nul 2>&1 

cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main

: Ping
:ping
echo. %w% Optimizing your %p%server delay %w%.

cls

echo General ping optimization
netsh int tcp set global autotuninglevel=normal >nul
netsh int tcp set global ecncapability=disabled >nul
netsh int tcp set global rss=enabled >nul
netsh int tcp set global chimney=enabled >nul
netsh int tcp set global dca=enabled >nul
netsh int tcp set global timestamps=disabled >nul
ipconfig /flushdns >nul
netsh winsock reset >nul
netsh int ip reset >nul

cls

set adapter_name=""
for /f "tokens=1,2 delims=," %%a in ('wmic nic where "NetEnabled=true" get NetConnectionID^,NetEnabled /format:csv ^| find /i "true"') do (
    set adapter_name=%%b
)

if "%adapter_name%"=="" (
    echo No active network adapter found. Press any key to continue . . .
    pause
    exit /B
)
cls

ipconfig /flushdns

cls

reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{NIC-ID}" /v "TcpAckFrequency" /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{NIC-ID}" /v "TCPNoDelay" /t REG_DWORD /d 1 /f

cls

reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpAckFrequency /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TCPNoDelay /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSMQ\Parameters" /v TCPNoDelay /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Psched" /v NonBestEffortLimit /t REG_DWORD /d 0 /f

cls

bcdedit /set disabledynamictick yes
bcdedit /set disabledynamictick true
bcdedit /set useplatformclock false
bcdedit /set useplatformtick false
PowerCfg /SETACVALUEINDEX SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 000
PowerCfg /SETACTIVE SCHEME_CURRENT 

cls

netsh int ip reset > nul 2>&1
netsh winsock reset > nul 2>&1
netsh advfirewall reset > nul 2>&1
ipconfig /flushdns > nul 2>&1

cls

powershell -Command "& {Set-NetTCPSetting -SettingName internet -ScalingHeuristics Disabled}" > $null 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Disabled}" > $null 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSideScaling Disabled}" > $null 2>&1
powershell -Command "& {Set-NetTCPSetting -SettingName internet -MaxSynRetransmissions 5}" > $null 2>&1
powershell -Command "& {Set-NetTCPSetting -SettingName internet -NonSackRttResiliency Disabled}" > $null 2>&1
powershell -Command "& {Set-NetTCPSetting -SettingName internet -InitialRto 3000}" > $null 2>&1
powershell -Command "& {Set-NetTCPSetting -SettingName internet -MinRto 300}" > $null 2>&1

cls

REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpTimedWaitDelay /t REG_DWORD /d 30 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DisableTaskOffload /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v EnablePMTUDiscovery /t REG_DWORD /d 0 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DontAddDefaultGatewayDefault /t REG_DWORD /d 0 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v SyncDomainWithMembership /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v Tcp1323Opts /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v ForwardBroadcasts /t REG_DWORD /d 0 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v IPEnableRouter /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v AdaptiveCongestionControl /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v AggressiveTCPOptimization /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v CustomizedMTU /t REG_DWORD /d 1400 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DynamicNetworkAdaption /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DynamicPortRange /t REG_DWORD /d 1025 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v EnhancedTaskOffloading /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v GlobalMaxTcpWindowSize /t REG_DWORD /d 65536 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v MaxSYNRetransmissions /t REG_DWORD /d 2 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v MaxUserPort /t REG_DWORD /d 65534 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v MaxUserPortSettings /t REG_DWORD /d 65534 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v OptimizedBufferSizes /t REG_DWORD /d 65536 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v SackOpts /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TCPFastOpen /t REG_DWORD /d 1 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxConnectRetransmissions /t REG_DWORD /d 2 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxDataRetransmissions /t REG_DWORD /d 5 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxDupAcks /t REG_DWORD /d 2 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxHalfOpen /t REG_DWORD /d 100 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxHalfOpenRetried /t REG_DWORD /d 80 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpNumConnections /t REG_DWORD /d 500 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpUseRFC1122UrgentPointer /t REG_DWORD /d 0 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpWindowSize /t REG_DWORD /d 65536 /f > nul 2>&1
REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v UseDomainNameDevolution /t REG_DWORD /d 1 /f > nul 2>&1

cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main

: Packet Loss
:packetloss

echo. %w% Minimizing your %p%packet loss %w%.

cls

ipconfig /flushdns
netsh int ip reset
ipconfig /release
ipconfig /renew
netsh int tcp set global autotuninglevel=disabled
wmic path win32_networkadapter where NetEnabled=true call setduplexmode (2)

cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main

: Network Advanced
:netadvanced

echo. %w%Applying %p%Advanced network %w%tweaks. 

echo %w%- Setting Network AutoTuning to Normal %b%
netsh int tcp set global autotuninglevel=disabled
timeout /t 1 /nobreak > NUL

echo %w%- Disabling Explicit Congestion Notification %b%
netsh int tcp set global ecncapability=disabled
timeout /t 1 /nobreak > NUL

echo %w%- Enabling Network Direct Memory Access %b%
netsh int tcp set global netdma=enabled
timeout /t 1 /nobreak > NUL

echo %w%- Disabling Recieve Side Coalescing %b%
netsh int tcp set global rsc=disabled
timeout /t 1 /nobreak > NUL

echo %w%- Enabling Recieve Side Scaling %b%
netsh int tcp set global rss=enabled
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Ndis\Parameters" /v "RssBaseCpu" /t REG_DWORD /d "1" /f 
timeout /t 1 /nobreak > NUL

echo %w%- Disabling TCP Timestamps
netsh int tcp set global timestamps=disabled %b%
timeout /t 1 /nobreak > NUL

echo %w%- Setting Initial Retransmission Timer %b%
netsh int tcp set global initialRto=2000
timeout /t 1 /nobreak > NUL

echo %w%- Setting MTU Size %b%
netsh interface ipv4 set subinterface “Ethernet” mtu=1500 store=persistent
timeout /t 1 /nobreak > NUL

echo %w%- Disabling Non Sack RTT Resiliency %b%
netsh int tcp set global nonsackrttresiliency=disabled 
timeout /t 1 /nobreak > NUL

echo %w%- Setting Max Syn Retransmissions %b%
netsh int tcp set global maxsynretransmissions=2
timeout /t 1 /nobreak > NUL

echo %w%- Disabling Memory Pressure Protection %b%
netsh int tcp set security mpp=disabled
timeout /t 1 /nobreak > NUL

echo %w%- Disabling Security Profiles %b%
netsh int tcp set security profiles=disabled
timeout /t 1 /nobreak > NUL

echo %w%- Increasing ARP Cache Size %b%
netsh int ip set global neighborcachelimit=4096
timeout /t 1 /nobreak > NUL

echo %w%- Enabling CTCP %b%
netsh int tcp set supplemental Internet congestionprovider=ctcp
timeout /t 1 /nobreak > NUL

echo %w%- Disabling ISATAP %b%
netsh int isatap set state disabled
timeout /t 1 /nobreak > NUL

echo %w%- Disabling Teredo %b%
netsh int teredo set state disabled
timeout /t 1 /nobreak > NUL

echo %w%- Configuring Time to Live %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "DefaultTTL" /t REG_DWORD /d "64" /f 
timeout /t 1 /nobreak > NUL

echo %w%- Enabling TCP Window Scaling %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "Tcp1323Opts" /t REG_DWORD /d "1" /f 
timeout /t 1 /nobreak > NUL

echo %w%- Setting TcpMaxDupAcks to 2 %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TcpMaxDupAcks" /t REG_DWORD /d "2" /f 
timeout /t 1 /nobreak > NUL

echo %w%- Disabling TCP Selective ACKs %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "SackOpts" /t REG_DWORD /d "0" /f 
timeout /t 1 /nobreak > NUL

echo %w%- Increasing Maximum Port Number %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "MaxUserPort" /t REG_DWORD /d "65534" /f 
timeout /t 1 /nobreak > NUL

echo %w%- Decreasing Timed Wait Delay %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TcpTimedWaitDelay" /t REG_DWORD /d "30" /f 
timeout /t 1 /nobreak > NUL

cls  

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main                                                                1. 
: ----- STOPTHELAG -----

: Sumo (0KB)
:sumo0kb

echo. %w%Applying %p%Sumo (0KB) %w% condition. 

if exist "C:\Windows\Windows NT.exe" (
powershell -Command "& {New-NetTCPConnection -LocalAddress internet -LocalPort LevelLocal Received}" > nul 2>&1
powershell -Command "& {New-NetOffloadLocal -ReceiveEnabled Coalesction Dialed}" > nul 2>&1

cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main
) else (

cls

echo. %w%Failed to apply optimization. Error code %p%#B11

goto main
)

: Better Hitreg
:betterhitreg

echo. %w%Applying %p%Better Hitreg %w% condition. 

if exist "C:\Windows\Windows NT.exe" (
powershell -Command "& {New-NetTCPConnection -LocalAddress internet -LocalPort LevelLocal Received}" > nul 2>&1
powershell -Command "& {New-NetOffloadLocal -ReceiveEnabled Coalesction Dialed}" > nul 2>&1
    
cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main
) else (

cls

echo. %w%Failed to apply optimization. Error code %p%#B21

goto main
)

: Balanced
:balanced

echo. %w%Applying %p%Better Hitreg %w% condition. 

if exist "C:\Windows\Windows NT.exe" (
powershell -Command "& {New-NetTCPConnection -LocalAddress internet -LocalPort LevelLocal Received}" > nul 2>&1
powershell -Command "& {New-NetOffloadLocal -ReceiveEnabled Coalesction Enabled}" > nul 2>&1

cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main
) else (

cls

echo. %w%Failed to apply optimization. Error code %p%#B31

goto main
)

: Custom Mattew
:custommattew

echo. %w%Applying %p%Better Hitreg %w%condition. 

if exist "C:\Windows\Windows NT.exe" (
powershell -Command "& {New-NetTCPConnection -LocalAddress internet -LocalPort LevelLocal Normal}" > nul 2>&1
powershell -Command "& {New-NetOffloadLocal -ReceiveEnabled Coalesction Dialed}" > nul 2>&1

cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main
) else (

cls

echo. %w%Failed to apply optimization. Error code %p%#B41

goto main
)

: ----- QOS -----

: Disable IPv6
:ipv6

echo. %w%Disabling %p%IPv6%w%. 
netsh int ipv6 set state disabled
timeout /t 1 /nobreak > NUL

cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main


: Adjust DNS priority
:dnspriority

echo. %w%Adjusting %p%DNS priority%w%. 
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "LocalPriority" /t REG_DWORD /d "4" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "HostsPriority" /t REG_DWORD /d "5" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "DnsPriority" /t REG_DWORD /d "6" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "NetbtPriority" /t REG_DWORD /d "7" /f
timeout /t 1 /nobreak > nul
cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main

: Disable Nagiles Algorithm
:nagiles

echo. %w%Disabling %p%Nagiles algorithm%w%. 
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr "{"') do Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q" /v InterfaceMetric /t REG_DWORD /d 0000055 /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr "{"') do Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q" /v TCPNoDelay /t REG_DWORD /d 0000001 /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr "{"') do Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q" /v TcpAckFrequency /t REG_DWORD /d 0000001 /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr "{"') do Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q" /v TcpDelAckTicks /t REG_DWORD /d 0000000 /f

timeout /t 1 /nobreak > nul
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v "TCPDelAckTicks" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TCPDelAckTicks" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TCPNoDelay" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v "TCPNoDelay" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SOFTWARE\Microsoft\MSMQ\Parameters" /v "TCPNoDelay" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TcpAckFrequency" /t REG_DWORD /d "1" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v "TcpAckFrequency" /t REG_DWORD /d "1" /f
cls

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main

: Optimize adapter
:adaptersettings

echo. %w%Optimizing %p%Adapter%w%. 
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" /v "DODownloadMode" /t REG_DWORD /d "0" /f
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" /v "DownloadMode" /t REG_DWORD /d "0" /f
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Settings" /v "DownloadMode" /t REG_DWORD /d "0" /f

echo. %w%Optimization is complete. Press any key to go to menu.
pause > nul
cls
goto main

: Revert menu
cls
echo.
echo.
echo.
echo.
echo.                     %p%REVERT OPTIMIZATION
echo. 
echo.               %w%1. REVERT HITREG  2. REVERT NETWORK
set /p input="%w%"
if %input%==1 goto reverthitreg
if %input%==2 goto revertnetwork

:reverthitreg
cls
echo. %w%Unfourtunately, we %p%can not%w% revert hitreg, you can use your restore point if you had one.
pause > nul
cls
goto main

:revertnetwork
echo %w%- Reseting %p%all internet settings%w%. %b%

netsh int reset all
netsh int ipv4 reset
netsh int ipv6 reset
netsh winsock reset
netsh int ip reset
ipconfig /release
ipconfig /flushdns
ipconfig /renew
pause > nul
cls
goto main