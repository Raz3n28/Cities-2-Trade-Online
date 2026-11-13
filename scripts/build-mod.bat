@echo off
REM Compila o mod e copia para a pasta de Mods do jogo (feito pelo toolchain).
dotnet build "%~dp0..\src\Mod\TradeOnline.csproj" -c Release
echo.
if errorlevel 1 (echo ERRO: veja as mensagens acima e mande um print.) else (echo OK! Abra o jogo.)
pause
