
@echo off

set H=R:\KSP_1.3.1_dev
set GAMEDIR=IFILS
set DEPEND1=CommunityCategoryKit

echo %H%

copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"
copy /Y %GAMEDIR%.version GameData\%GAMEDIR%

xcopy /y /s /I GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
xcopy /y /s /I GameData\%DEPEND1% "%H%\GameData\%DEPEND1%"