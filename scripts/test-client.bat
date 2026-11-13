@echo off
REM Cliente de teste (simula um jogador sem abrir o jogo).
set /p NOME=Seu nome: 
set /p HOST=IP do servidor (Enter = 127.0.0.1): 
if "%HOST%"=="" set HOST=127.0.0.1
if exist "%~dp0TestClient.exe" (
  "%~dp0TestClient.exe" --host %HOST% --name "%NOME%"
) else (
  dotnet run --project "%~dp0..\src\TestClient" -- --host %HOST% --name "%NOME%"
)
pause
