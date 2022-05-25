
@echo off

set H=%KSPDIR%

set H=R:\KSP_1.12.3_IFI_US2
rem set H=R:\KSP_1.12.3_Career-Dev-JNSQ

set GAMEDIR=IFILS
set GAMEDATA="GameData"

rem set DEPEND1=CommunityCategoryKit
set DEPEND2=Squad
rem set DEPEND3=CommunityResourcePack
set VERSIONFILE=%GAMEDIR%.version

echo %H%

copy /Y "%1%2" "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y "%1%3".pdb "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y %VERSIONFILE% %GAMEDATA%\%GAMEDIR%

xcopy /y /s /I GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
rem xcopy /y /s /I GameData\%DEPEND2% "%H%\GameData\%DEPEND2%"

