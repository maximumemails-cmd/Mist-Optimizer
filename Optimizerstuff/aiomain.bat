@echo off

chcp 65001 >nul 2>&1

: Color Codes
: white
set w=[97m 
: red
set p=[91m
: gray
set b=[90m
: lime
set v=[92m
: dark red
set o=[31m
: ylw
set j=[33m
: blue
set u=[94m
: dark purple
set m=[95m
: dark green
set h=[92m

setlocal enabledelayedexpansion

:: Initialize toggle states
set hitreg=%p%OFF%w%
set ping=%p%OFF%w%
set adapter=%p%OFF%w%
set packetloss=%p%OFF%w%
set lowerping=%p%OFF%w%
set nagiles=%p%Default%w%
set dnsprio=%p%Default%w%
set hypixel=%p%OFF%w%
set besthr=%p%OFF%w%
set pl=%p%Default%w%
set lan=%p%Default%w%

:menu
cls
echo.
echo.
echo                                       %w%██╗  ██╗██╗████████╗██╗     ███████╗██████╗
echo                                        ██║  ██║██║╚══██╔══╝██║     ██╔════╝██╔══██╗
echo                                        ███████║██║   ██║   ██║     █████╗  ██████╔╝
echo                                        ██╔══██║██║   ██║   ██║     ██╔══╝  ██╔══██╗
echo                                        ██║  ██║██║   ██║   ███████╗███████╗██║  ██║
echo                                        ╚═╝  ╚═╝╚═╝   ╚═╝   ╚══════╝╚══════╝╚═╝  ╚═╝
echo.
echo.
echo                                         An All-In-One minecraft optimization tool
echo.
echo.
echo                %j%1. %w%Network Optimizations           %j%2. %w%Quality Of Service           %j%3. %w%Conditionings
echo.
echo                                   %j%4. %w%Advanced Network             %j%5. %w%Routes Optimizer
echo.   
echo.
echo                                                 Select an option from above:
echo.
echo.
echo.
echo.
echo.
echo.
set /p main="%w%"

if "%main%"=="1" goto network
if "%main%"=="2" goto qos
if "%main%"=="3" goto conditionings
if "%main%"=="4" goto netadvanced
if "%main%"=="5" goto Routes
else goto syntaxerror

:syntaxerror
cls
echo.
echo.
echo %w%Misspell detected, use numbers 1-5 to select an option.
timeout 2
goto menu

:network
cls
echo.
echo.
echo %w%                          ███╗  ██╗███████╗████████╗██╗       ██╗ █████╗ ██████╗ ██╗  ██╗
echo                            ████╗ ██║██╔════╝╚══██╔══╝██║  ██╗  ██║██╔══██╗██╔══██╗██║ ██╔╝
echo                            ██╔██╗██║█████╗     ██║   ╚██╗████╗██╔╝██║  ██║██████╔╝█████═╝
echo                            ██║╚████║██╔══╝     ██║    ████╔═████║ ██║  ██║██╔══██╗██╔═██╗
echo                            ██║ ╚███║███████╗   ██║    ╚██╔╝ ╚██╔╝ ╚█████╔╝██║  ██║██║ ╚██╗
echo                            ╚═╝  ╚══╝╚══════╝   ╚═╝     ╚═╝   ╚═╝   ╚════╝ ╚═╝  ╚═╝╚═╝  ╚═╝
echo.
echo.
echo                                   Here is a list of optimizations you can apply
echo                                          Press "B" to go back to menu.
echo.
echo.                                            %j%1. %w%Better Hitreg - !hitreg!
echo                                     %b%Speeds up your hit registration time and
echo                                                 in-game interfaces.
echo.
echo                                             %j%2. %w%Better Ping - !ping!
echo                                     %b%Lowers your ping by 10-20 ms depending on
echo                                                   your internet.  
echo.
echo.                                            %j%3. %w%Adapter Tweaks - !adapter!
echo                                    %b%Optimizes your adapter settings, therefore
echo                                    lowers your ping and decresaes packet loss.
echo.
echo.                                            %j%4. %w%Packet Loss Tweaks - !packetloss!
echo                                        %b%Directly decreases your packet loss.
echo.
echo.                                          

set /p choice="%w%"

if "%choice%"=="1" goto toggle_hitreg
if "%choice%"=="2" goto toggle_ping
if "%choice%"=="3" goto toggle_adapter
if "%choice%"=="4" goto toggle_packetloss
if "%choice%"=="b" goto menu

:toggle_hitreg
if "%hitreg%"=="%p%OFF%w%" (
    set hitreg=%v%ON%w%
    net session >nul 2>&1
if %errorLevel% == 0 (
    echo %w%Process running as %p%administrator%w%.
) else (
    echo Not running as administrator. Restarting as administrator...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

cls

echo 1488

goto network
) else (
    set hitreg=%p%OFF%w%
    netsh int reset all
    cls
netsh int ipv4 reset
cls
netsh int ipv6 reset
cls
netsh winsock reset
cls
netsh int ip reset
cls
ipconfig /release
cls
ipconfig /flushdns
cls
ipconfig /renew
cls
goto network
)

:toggle_ping
if "%ping%"=="%p%OFF%w%" (
    set ping=%v%ON%w%

cls

echo 1488

cls
) else (
    set ping=%p%OFF%w%
        netsh int reset all
netsh int ipv4 reset
cls
netsh int ipv6 reset
cls
netsh winsock reset
cls
netsh int ip reset
cls
ipconfig /release
cls
ipconfig /flushdns
cls
ipconfig /renew
cls
goto network
)

:toggle_adapter
if "%adapter%"=="%p%OFF%w%" (
    set adapter=%v%ON%w%

echo 1488

) else (
    set adapter=%p%OFF%w%
netsh int ipv4 reset
cls
netsh int ipv6 reset
cls
netsh winsock reset
cls
netsh int ip reset
cls
ipconfig /release
cls
ipconfig /flushdns
cls
ipconfig /renew
cls
goto network
)

:toggle_packetloss
if "%packetloss%"=="%p%OFF%w%" (
    set packetloss=%v%ON%w%
) else (
    set packetloss=%p%OFF%w%
)
goto network

:printOption
set "optionText=%~1"
set "status=!optionText:~-7!"

:conditionings
cls
echo.
echo.
echo                 █████╗  █████╗ ███╗  ██╗██████╗ ██╗████████╗██╗ █████╗ ███╗  ██╗██╗███╗  ██╗ ██████╗  ██████╗
echo                ██╔══██╗██╔══██╗████╗ ██║██╔══██╗██║╚══██╔══╝██║██╔══██╗████╗ ██║██║████╗ ██║██╔════╝ ██╔════╝
echo                ██║  ╚═╝██║  ██║██╔██╗██║██║  ██║██║   ██║   ██║██║  ██║██╔██╗██║██║██╔██╗██║██║  ██╗╚█████╗ 
echo                ██║  ██╗██║  ██║██║╚████║██║  ██║██║   ██║   ██║██║  ██║██║╚████║██║██║╚████║██║ ╚██╗ ╚═══██╗
echo                ╚█████╔╝╚█████╔╝██║ ╚███║██████╔╝██║   ██║   ██║╚█████╔╝██║ ╚███║██║██║ ╚███║╚██████╔╝██████╔╝
echo                 ╚════╝  ╚════╝ ╚═╝  ╚══╝╚═════╝ ╚═╝   ╚═╝   ╚═╝ ╚════╝ ╚═╝  ╚══╝╚═╝╚═╝  ╚══╝ ╚═════╝ ╚═════╝ 
echo.
echo.
echo                              Here is a list of Hypixel-affecting conditionings you can apply
echo                                             Press "B" to got back to menu.
echo                                               Press "D" to disable both.   
echo.
echo.
echo.                                            %j%1. %w%Hypixel - !hypixel!
echo                                         %b%Recommended for Hypixel and Minemen
echo                                                  a universal preset
echo.
echo                                             %j%2. %w%Best Hitreg - !besthr!
echo                                         %b%Recommended for Minemen mostly.
echo.                       
echo.                             
echo.
set /p choice="%w%"

if "%choice%"=="1" goto toggle_hypixel
if "%choice%"=="2" goto toggle_besthr
if "%choice%"=="D" goto disableconds
if "%choice%"=="b" goto menu    
:netadvanced
cls
echo.
echo.
echo                             █████╗ ██████╗ ██╗   ██╗ █████╗ ███╗  ██╗ █████╗ ███████╗██████╗ 
echo                            ██╔══██╗██╔══██╗██║   ██║██╔══██╗████  ██║██╔══██╗██╔════╝██╔══██╗
echo                            ███████║██║  ██║╚██╗ ██╔╝███████║██╔██╗██║██║  ╚═╝█████╗  ██║  ██║
echo                            ██╔══██║██║  ██║ ╚████╔╝ ██╔══██║██║╚████║██║  ██╗██╔══╝  ██║  ██║
echo                            ██║  ██║██████╔╝  ╚██╔╝  ██║  ██║██║ ╚███║╚█████╔╝███████╗██████╔╝
echo                            ╚═╝  ╚═╝╚═════╝    ╚═╝   ╚═╝  ╚═╝╚═╝  ╚══╝ ╚════╝ ╚══════╝╚═════╝ 
echo.
echo.
echo                                   Here is a list of optimizations you can apply
echo                                          Press "B" to go back to menu.
echo.
echo.                                            %j%1. %w%Ping Booster - !lowerping!
echo                                     %b%Makes your hits and kb feel like lower ping
echo                                                      (50-70ms)
echo.
echo                                             %j%2. %w%Disable Nagiles Algorithm - !nagiles!
echo                                     %b%Disable Nagiles algorithm, that deletes lower
echo                                          value packets, directly affecting your hitreg.  
echo.
echo.                                            %j%3. %w%DNS Priority - !dnsprio!
echo                                    %b%Optimizes your adapter settings, therefore
echo                                    lowers your ping and decresaes packet loss.
echo.
echo. 
set /p choice="%w%"

if "%choice%"=="1" goto toggle_lowerping
if "%choice%"=="2" goto toggle_nagiles
if "%choice%"=="3" goto toggle_dnsprio
if "%choice%"=="b" goto menu    

:toggle_lowerping
if "%lowerping%"=="%p%OFF%w%" (
    set lowerping=%v%ON%w%
REG ADD "!RegistryQueryResult!\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties" /v "MSISupported" /t REG_DWORD /d 1 /f >nul 2>&1
REG ADD "!RegistryQueryResult!\Device Parameters\Interrupt Management\Affinity Policy" /v "DevicePriority" /t REG_DWORD /d 3 /f >nul 2>&1
) else (
    set lowerping=%p%OFF%w%
)
goto netadvanced

:toggle_nagiles
if "%nagiles%"=="%p%Default%w%" (
    set nagiles=%v%Disabled%w%
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
) else (
    set nagiles=%p%Default%w%
)
goto netadvanced

:toggle_dnsprio
if "%dnsprio%"=="%p%Default%w%" (
    set dnsprio=%v%Adjusted%w%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "LocalPriority" /t REG_DWORD /d "4" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "HostsPriority" /t REG_DWORD /d "5" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "DnsPriority" /t REG_DWORD /d "6" /f
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider" /v "NetbtPriority" /t REG_DWORD /d "7" /f
) else (
    set dnsprio=%p%Default%w%
)
goto netadvanced

:toggle_hypixel
if "%hypixel%"=="%p%OFF%w%" (
    set hypixel=%v%ON%w%
powershell -Command "& {Set-NetTCPSetting -SettingName internet -AutoTuningLevelLocal Restricted}" > nul 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Enabled}" > nul 2>&1
) else (
    set hypixel=%p%OFF%w%
)
cls
goto conditionings

:toggle_besthr
if "%besthr%"=="%p%OFF%w%" (
    set besthr=%v%ON%w%
powershell -Command "& {Set-NetTCPSetting -SettingName internet -AutoTuningLevelLocal Normal}" > nul 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Disabled}" > nul 2>&1
) else (
    set besthr=%p%OFF%w%
)
cls
goto conditionings

:disableconds
if "%besthr%"=="%v%ON%w%" (
    set besthr=%p%OFF%w%
powershell -Command "& {Set-NetTCPSetting -SettingName internet -AutoTuningLevelLocal Normal}" > nul 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Enabled}" > nul 2>&1
if "%hypixel%"=="%v%ON%w%" (
    set hypixel=%p%OFF%w%
powershell -Command "& {Set-NetTCPSetting -SettingName internet -AutoTuningLevelLocal Normal}" > nul 2>&1
powershell -Command "& {Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Enabled}" > nul 2>&1
)
) else (
cls
echo.
echo.
echo Both conditionings are currently disabled!
timeout 2
)
goto conditionings

:qos
cls
echo.
echo.
echo                            %w%██████╗ ██╗   ██╗ █████╗ ██╗     ██╗████████╗██╗   ██╗   █████╗ ███████╗
echo                            ██╔═══██╗██║   ██║██╔══██╗██║     ██║╚══██╔══╝╚██╗ ██╔╝  ██╔══██╗██╔════╝
echo                            ██║██╗██║██║   ██║███████║██║     ██║   ██║    ╚████╔╝   ██║  ██║█████╗  
echo                            ╚██████╔╝██║   ██║██╔══██║██║     ██║   ██║     ╚██╔╝    ██║  ██║██╔══╝  
echo                             ╚═██╔═╝ ╚██████╔╝██║  ██║███████╗██║   ██║      ██║     ╚█████╔╝██║     
echo                               ╚═╝    ╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝   ╚═╝      ╚═╝      ╚════╝ ╚═╝     
echo.
echo                                     ██████╗███████╗██████╗ ██╗   ██╗██╗ █████╗ ███████╗
echo                                    ██╔════╝██╔════╝██╔══██╗██║   ██║██║██╔══██╗██╔════╝
echo                                    ╚█████╗ █████╗  ██████╔╝╚██╗ ██╔╝██║██║  ╚═╝█████╗  
echo                                     ╚═══██╗██╔══╝  ██╔══██╗ ╚████╔╝ ██║██║  ██╗██╔══╝  
echo                                    ██████╔╝███████╗██║  ██║  ╚██╔╝  ██║╚█████╔╝███████╗
echo                                    ╚═════╝ ╚══════╝╚═╝  ╚═╝   ╚═╝   ╚═╝ ╚════╝ ╚══════╝
echo.
echo.
echo                                   Here is a list of optimizations you can apply
echo                                          Press "B" to go back to menu.
echo.
echo.                                        %j%1. %w%Advanced QOS tweaks - !pl!
echo                                     %b%Decreases your packet loss, speeds up your hitreg
echo.
echo                                         %j%2. %w%LAN Optimizations - !lan!
echo                                      %b%Optimizes your lan connection.                                                 
echo.
echo.

set /p qoschoice="%w%"

if "%qoschoice%"=="1" goto pl
if "%qoschoice%"=="2" goto lan
if "%qoschoice%"=="b" goto menu
else goto qossyntaxerror

:syntaxerror
cls
echo.
echo.
echo %w%Misspell detected, use numbers 1-2 to select an option.
timeout 2
goto qos

:pl
if "%pl%"=="%p%Default%w%" (
    set pl=%v%Applied%w%
    net session >nul 2>&1
cls

netsh int tcp set global autotuninglevel=disabled

netsh int tcp set global ecncapability=disabled

netsh int tcp set global netdma=enabled

netsh int tcp set global rsc=disabled

netsh int tcp set global rss=enabled

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Ndis\Parameters" /v "RssBaseCpu" /t REG_DWORD /d "1" /f 

netsh int tcp set global timestamps=disabled %b%

netsh int tcp set global initialRto=2000

netsh interface ipv4 set subinterface “Ethernet” mtu=1500 store=persistent

netsh int tcp set global nonsackrttresiliency=disabled 

netsh int tcp set global maxsynretransmissions=2

netsh int tcp set security mpp=disabled

netsh int tcp set security profiles=disabled

netsh int ip set global neighborcachelimit=4096

netsh int tcp set supplemental Internet congestionprovider=ctcp

netsh int isatap set state disabled

netsh int teredo set state disabled

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "DefaultTTL" /t REG_DWORD /d "64" /f 

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "Tcp1323Opts" /t REG_DWORD /d "1" /f 

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TcpMaxDupAcks" /t REG_DWORD /d "2" /f 

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "SackOpts" /t REG_DWORD /d "0" /f 

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "MaxUserPort" /t REG_DWORD /d "65534" /f 

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TcpTimedWaitDelay" /t REG_DWORD /d "30" /f 

cls

if %errorLevel% == 0 (
    echo %w%Process running as %p%administrator%w%.
) else (
    echo Not running as administrator. Restarting as administrator...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

goto qos
) else (
    set pl=%p%OFF%w%
    netsh int reset all
    cls
netsh int ipv4 reset
cls
netsh int ipv6 reset
cls
netsh winsock reset
cls
netsh int ip reset
cls
ipconfig /release
cls
ipconfig /flushdns
cls
ipconfig /renew
cls
goto qos
)

:lan
if "%lan%"=="%p%Default%w%" (
    set lan=%v%Applied%w%
    net session >nul 2>&1

Reg.exe add "HKLM\SYSTEM\CurrentControlSet\services\LanmanServer\Parameters" /v "autodisconnect" /t REG_DWORD /d "4294967295" /f 

echo %w%- Limiting SMB Sessions %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\services\LanmanServer\Parameters" /v "Size" /t REG_DWORD /d "3" /f 

echo %w%- Disabling Oplocks %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\services\LanmanServer\Parameters" /v "EnableOplocks" /t REG_DWORD /d "0" /f 

echo %w%- Setting IRP Stack Size %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\services\LanmanServer\Parameters" /v "IRPStackSize" /t REG_DWORD /d "20" /f 

echo %w%- Disabling Sharing Violations %b%
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\services\LanmanServer\Parameters" /v "SharingViolationDelay" /t REG_DWORD /d "0" /f 
Reg.exe add "HKLM\SYSTEM\CurrentControlSet\services\LanmanServer\Parameters" /v "SharingViolationRetries" /t REG_DWORD /d "0" /f 

cls
goto qos
) else (
    set lan=%p%Default%w%
    netsh int reset all
    cls
netsh int ipv4 reset
cls
netsh int ipv6 reset
cls
netsh winsock reset
cls
netsh int ip reset
cls
ipconfig /release
cls
ipconfig /flushdns
cls
ipconfig /renew
cls
goto qos
)