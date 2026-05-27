@echo off
set w=[97m
set p=[91m
set b=[90m
set v=[91m
set o=[31m
set j=[93m
set u=[94m
set m=[95m
set h=[92m
%p% 

: Unicode
chcp 65001 >nul 2>&1

:wall
cls
echo                                     =====================================================
echo                                     =                   %w%ENTER THE KEY                   %p%=
echo                                     =====================================================
set /p password=Enter you key: 

if "%password%"=="1488" goto menu
else (
    goto wall
    echo You key is invalid. If you believe, that this is a mistake, please contact @ttackcots on Discord. 
)
pause
:menu
cls
echo.                                       
echo.                                                                                       
echo. 
echo.                                                                                         %m% ░██████╗████████╗░█████╗░░█████╗░██╗░░██╗░█████╗░░█████╗░████████╗████████╗██╗░██████╗
echo.                                                                                          ██╔════╝╚══██╔══╝██╔══██╗██╔══██╗██║░██╔╝██╔══██╗██╔══██╗╚══██╔══╝╚══██╔══╝╚█║██╔════╝
echo.                                                                                          ╚█████╗░░░░██║░░░██║░░██║██║░░╚═╝█████═╝░██║░░╚═╝███████║░░░██║░░░░░░██║░░░░╚╝╚█████╗░
echo.                                                                                          ░╚═══██╗░░░██║░░░██║░░██║██║░░██╗██╔═██╗░██║░░██╗██╔══██║░░░██║░░░░░░██║░░░░░░░╚═══██╗
echo.                                                                                          ██████╔╝░░░██║░░░╚█████╔╝╚█████╔╝██║░╚██╗╚█████╔╝██║░░██║░░░██║░░░░░░██║░░░░░░██████╔╝
echo.                                                                                          ╚═════╝░░░░╚═╝░░░░╚════╝░░╚════╝░╚═╝░░╚═╝░╚════╝░╚═╝░░╚═╝░░░╚═╝░░░░░░╚═╝░░░░░░╚═════╝░
echo. 
echo.                                                                                                    ░█████╗░██╗░░░░░██╗░░░░░  ██╗███╗░░██╗  ░█████╗░███╗░░██╗███████╗
echo.                                                                                                    ██╔══██╗██║░░░░░██║░░░░░  ██║████╗░██║  ██╔══██╗████╗░██║██╔════╝
echo.                                                                                                    ███████║██║░░░░░██║░░░░░  ██║██╔██╗██║  ██║░░██║██╔██╗██║█████╗░░
echo.                                                                                                    ██╔══██║██║░░░░░██║░░░░░  ██║██║╚████║  ██║░░██║██║╚████║██╔══╝░░
echo.                                                                                                    ██║░░██║███████╗███████╗  ██║██║░╚███║  ╚█████╔╝██║░╚███║███████╗
echo.                                                                                                    ╚═╝░░╚═╝╚══════╝╚══════╝  ╚═╝╚═╝░░╚══╝  ░╚════╝░╚═╝░░╚══╝╚══════╝
echo.                                        %m%╔══                                                                                                                                     %m%══╗
echo.                                          %b%╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗%w%   
echo                                                                                      %w%1. Hitreg                   5. Network
echo                                                                                      2. Ping                     6. QOS
echo                                                                                      3. Packet loss              7. StopTheLag conditionings
echo                                                                                      4. FPS                      8. Support
echo.                                          %b%╚══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝%w%
echo.                                        %m%╚══                                                                                                                                      %m%══╝ 

set /p choice="%w%"

if "%choice%"=="1" goto hitreg
if "%choice%"=="2" goto ping
if "%choice%"=="3" goto packetloss
if "%choice%"=="4" goto fps
if "%choice%"=="5" goto network
if "%choice%"=="6" goto qos
if "%choice%"=="7" goto stlconds
if "%choice%"=="8" goto support
if "%choice%"=="9" goto exit

echo Syntax Error, please enter a number (1-8).
pause
goto menu

: Hitreg Tweaks

:hitreg
echo Applying advanced hit registration tweaks..
echo Running code for Option 1...
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running as administrator.
) else (
    echo Not running as administrator. Restarting as administrator...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo Optimizing network settings...
netsh int tcp set global autotuninglevel=normal
netsh int tcp set global congestionprovider=ctcp
netsh int tcp set global ecncapability=disabled
netsh int tcp set global rss=enabled
ipconfig /flushdns

reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TcpAckFrequency /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TCPNoDelay /t REG_DWORD /d 1 /f

echo Setting Minecraft process priority to High...
wmic process where name="javaw.exe" CALL setpriority "high priority"
echo Minecraft process priority set to High.

timeout /t 2 /nobreak >nul
powershell -command "Clear-StandbyMemory"

powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c

echo Hitreg optimization complete
pause
goto menu

: Ping Optimization

:ping
echo Applying advanced ping tweaks...
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
echo Ping optimization complete.
pause
goto menu

: Packet Loss

:packetloss
echo You selected Option 3
echo Lowering your packet loss... 

ipconfig /flushdns
netsh int ip reset
ipconfig /release
ipconfig /renew
netsh int tcp set global autotuninglevel=disabled
wmic path win32_networkadapter where NetEnabled=true call setduplexmode (2)
echo Your packet loss has been lowered.
pause
goto menu

:fps
echo Optimizing your FPS.
echo FPS optimizaton
echo Optimizing Minecraft 1.8.9 for better FPS...

set JAVA_PATH="C:\Program Files\Java\jre1.8.0_361\bin\java.exe"

set MINECRAFT_DIR=%APPDATA%\.minecraft

set JAVA_OPTIONS=-Xmx2G -Xms2G -XX:+UseG1GC -XX:+UnlockExperimentalVMOptions -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 -Dfml.ignorePatchDiscrepancies=true -Dfml.ignoreInvalidMinecraftCertificates=true -Djava.library.path=%MINECRAFT_DIR%\versions\1.8.9\1.8.9-natives -cp "%MINECRAFT_DIR%\libraries\*;%MINECRAFT_DIR%\versions\1.8.9\1.8.9.jar" net.minecraft.client.main.Main

%JAVA_PATH% %JAVA_OPTIONS%

echo FPS Optimized. 
pause
goto menu

:network
chcp 65001 >nul 2>&1
cls
echo. 
echo.                                                                                         %m% ███╗░░██╗███████╗████████╗░██╗░░░░░░░██╗░█████╗░██████╗░██╗░░██╗  ████████╗░██╗░░░░░░░██╗███████╗░█████╗░██╗░░██╗░██████╗
echo.                                                                                          ████╗░██║██╔════╝╚══██╔══╝░██║░░██╗░░██║██╔══██╗██╔══██╗██║░██╔╝  ╚══██╔══╝░██║░░██╗░░██║██╔════╝██╔══██╗██║░██╔╝██╔════╝
echo.                                                                                          ██╔██╗██║█████╗░░░░░██║░░░░╚██╗████╗██╔╝██║░░██║██████╔╝█████═╝░  ░░░██║░░░░╚██╗████╗██╔╝█████╗░░███████║█████═╝░╚█████╗░
echo.                                                                                          ██║╚████║██╔══╝░░░░░██║░░░░░████╔═████║░██║░░██║██╔══██╗██╔═██╗░  ░░░██║░░░░░████╔═████║░██╔══╝░░██╔══██║██╔═██╗░░╚═══██╗
echo.                                                                                          ██║░╚███║███████╗░░░██║░░░░░╚██╔╝░╚██╔╝░╚█████╔╝██║░░██║██║░╚██╗  ░░░██║░░░░░╚██╔╝░╚██╔╝░███████╗██║░░██║██║░╚██╗██████╔╝
echo.                                                                                          ╚═╝░░╚══╝╚══════╝░░░╚═╝░░░░░░╚═╝░░░╚═╝░░░╚════╝░╚═╝░░╚═╝╚═╝░░╚═╝  ░░░╚═╝░░░░░░╚═╝░░░╚═╝░░╚══════╝╚═╝░░╚═╝╚═╝░░╚═╝╚═════╝░
echo.                                                                               %m%╔══                                                                                                                                     %m%══╗
echo.                                                                                  %b%╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗%w%   
echo.                                                                                                                                      %w%1. Disable IPv6                     
echo.                                                                                                                                      2. Adjust DNS priority                
echo.                                                                                                                                      3. Disable Nagiles Algorithm            
echo.                                                                                                                                      4. Optimize Network Adapter             
echo.                                                                                                                                      5. Back to menu
echo.                                                                                  %b%╚══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝%w%
echo.                                                                               %m%╚══                                                                                                                                      %m%══╝ 
set /p netchoice=%w%
if "%netchoice%"=="1" goto disableipv6
if "%netchoice%"=="2" goto dnspriority
if "%netchoice%"=="3" goto disnagilesalgo
if "%netchoice%"=="4" goto adapter
if "%netchoice%"=="5" goto menu

pause >nul
goto :network

:disableipv6
echo %m%- Disabling IPv6 %w%
netsh int ipv6 set state disabled
timeout /t 1 /nobreak > NUL
cls
pause >nul
goto :network

:dnspriority
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "LocalPriority" /t REG_DWORD /d "4" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "HostsPriority" /t REG_DWORD /d "5" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "DnsPriority" /t REG_DWORD /d "6" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "NetbtPriority" /t REG_DWORD /d "7" /f
timeout /t 1 /nobreak > nul
echo. DNS priority optimized.
cls
pause
goto: network

:disnagilesalgo
echo %w%- Disabling Nagiles Algorithm %b%
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
timeout /t 1 /nobreak > nul
echo. Nagiles algorithm disabled.
pause >nul
cls
goto: network

:adapter
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" /v "DODownloadMode" /t REG_DWORD /d "0" /f
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" /v "DownloadMode" /t REG_DWORD /d "0" /f
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Settings" /v "DownloadMode" /t REG_DWORD /d "0" /f
cls
echo. Adapter optimized.
pause > nul
cls
goto :net

goto menu

:qos
echo. Optimiying your Quality Of Service
cls
powershell -Command "& {Set-NetTCPSetting -SettingName internet -AutoTuningLevelLocal HighlyRestricted}" > nul 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Disabled}" > nul 2>&1
cls
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Version" /t REG_SZ /d "1.0" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Application Name" /t REG_SZ /d "javaw.exe" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Protocol" /t REG_SZ /d "*" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Local Port" /t REG_SZ /d "*" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Local IP" /t REG_SZ /d "*" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Local IP Prefix Length" /t REG_SZ /d "*" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Remote Port" /t REG_SZ /d "25565" /f >nul 2>&1
cls
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Remote IP" /t REG_SZ /d "*" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Remote IP Prefix Length" /t REG_SZ /d "*" /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "DSCP Value" /t REG_DWORD /d 46 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\QoS\javaw.exe" /v "Throttle Rate" /t REG_SZ /d "Disabled" /f >nul 2>&1
cls
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DefaultTTL /t REG_DWORD /d 64 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DisableTaskOffload /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxDataRetransmissions /t REG_DWORD /d 3 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpWindowSize /t REG_DWORD /d 65536 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v GlobalMaxTcpWindowSize /t REG_DWORD /d 65536 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v EnablePMTUDiscovery /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v SackOpts /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v Tcp1323Opts /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxDupAcks /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxHalfOpen /t REG_DWORD /d 100 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpMaxHalfOpenRetried /t REG_DWORD /d 80 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpUseRFC1122UrgentPointer /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v EnableDCA /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpDelAckTicks /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpAckFrequency /t REG_DWORD /d 1 /f >nul 2>&1
cls
pause
goto :main

:option7
echo. Coming soon ong ong
cls
pause
goto menu

:option8
echo. https://discord.gg/ueVYHThb3K / Contact @ttackcots on Discord.
pause
goto menu

:exit
echo Exiting the script...
pause
exit