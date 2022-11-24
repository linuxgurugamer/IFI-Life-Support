
@echo off

set H=%KSPDIR%

rem set H=R:\KSP_1.12.3_IFI_US2
rem set H=R:\KSP_1.12.3_IFI_JNSQ

rem set H=R:\KSP_1.12.3_Career-Dev-JNSQ

rem set H=R:\KSP_1.12.3_Career-MissionController-JNSQ
rem set H=R:\KSP_1.12.4_MissionCtlrParallax


set GAMEDIR=IFILS
set GAMEDATA="GameData"

set VERSIONFILE=%GAMEDIR%.version

echo %H%

copy /Y "%1%2" "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y "%1%3".pdb "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y %VERSIONFILE% %GAMEDATA%\%GAMEDIR%

xcopy /y /s /I GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"

