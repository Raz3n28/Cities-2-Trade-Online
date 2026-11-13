@echo off
REM Inicia o servidor de troca. Use: start-server.bat  (porta padrao 7777)
if exist "%~dp0TradeServer.exe" (
  "%~dp0TradeServer.exe" %*
) else (
  dotnet run --project "%~dp0..\src\Server" -- %*
)
pause
