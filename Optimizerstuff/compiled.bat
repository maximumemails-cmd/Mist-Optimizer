@echo off
:wall
cls
echo                                     =====================================================
echo                                                     Enter the password
echo                                     =====================================================
set /p password=Enter password: 

if "%password%"=="1488" goto menu
else (
    goto wall
    echo Incorrect password. 
)
pause
:menu
cls
echo                ==================================================================
echo                                             HOLY DEEPSEEK
echo                ==================================================================
echo                 1. Network optimizaion          4. Hypixel network optimization 
echo                 2. Packet loss optimizaton      5. General ping optimization
echo                 3. FPS optimizaton      
echo                ==================================================================
set /p choice="Enter a number (1-5) to execute the corresponding code: "

if "%choice%"=="1" goto option1
if "%choice%"=="2" goto option2
if "%choice%"=="3" goto option3
if "%choice%"=="4" goto option4
if "%choice%"=="5" goto option5
echo Syntax Error! Please enter a number between 1 and 5.
pause
goto menu

:option1
echo Network optimizaion
REM Add your batch code for Option 1 here
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

pause
goto menu

:option2
echo Packet loss optimizaton 

ipconfig /flushdns
echo.

netsh int ip reset
echo.

ipconfig /release
ipconfig /renew
echo.

netsh int tcp set global autotuninglevel=disabled
echo.

wmic path win32_networkadapter where NetEnabled=true call setduplexmode (2)
echo.

echo Your packed loss has been lowered.
pause
goto menu

:option3
echo FPS optimizaton
echo Optimizing Minecraft 1.8.9 for better FPS...

set JAVA_PATH="C:\Program Files\Java\jre1.8.0_361\bin\java.exe"

set MINECRAFT_DIR=%APPDATA%\.minecraft

set JAVA_OPTIONS=-Xmx2G -Xms2G -XX:+UseG1GC -XX:+UnlockExperimentalVMOptions -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 -Dfml.ignorePatchDiscrepancies=true -Dfml.ignoreInvalidMinecraftCertificates=true -Djava.library.path=%MINECRAFT_DIR%\versions\1.8.9\1.8.9-natives -cp "%MINECRAFT_DIR%\libraries\*;%MINECRAFT_DIR%\versions\1.8.9\1.8.9.jar" net.minecraft.client.main.Main

%JAVA_PATH% %JAVA_OPTIONS%

echo FPS Optimized. 
pause
goto menu

:option4
cls
echo Hypixel network optimization 
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TcpNoDelay" /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TCPAckFrequency" /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "TCPDelAckTicks" /t REG_DWORD /d 0 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v "DisableTaskOffload" /t REG_DWORD /d 1 /f
cls
echo Hit Registration Optimizations Applied!
pause
goto menu

:option5
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
echo Ping Optimizations Completed!
pause
goto menu