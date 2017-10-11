
@echo off

set H=R:\KSP_1.3.1_dev
set GAMEDIR=IFILS

echo %H%

copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"
copy /Y %GAMEDIR%.version GameData\%GAMEDIR%

xcopy /y /s /I GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
