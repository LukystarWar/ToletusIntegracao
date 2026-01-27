@echo off
REM Script de Diagnostico - Toletus Integracao
REM Execute na maquina cliente (.235) para identificar problemas

echo ===============================================
echo  DIAGNOSTICO TOLETUS INTEGRACAO
echo ===============================================
echo.

echo [1] Verificando conectividade com dispositivos...
echo ------------------------------------------------
echo.

echo Testando CATRACA (192.168.18.200)...
ping -n 2 192.168.18.200
if %errorLevel% equ 0 (
    echo ✓ Catraca responde
) else (
    echo ✗ Catraca NAO responde - PROBLEMA!
)
echo.

echo Testando IDFACE (192.168.18.173)...
ping -n 2 192.168.18.173
if %errorLevel% equ 0 (
    echo ✓ iDFace responde
) else (
    echo ✗ iDFace NAO responde - PROBLEMA!
)
echo.

echo Testando SERVIDOR DEV (192.168.18.234)...
ping -n 2 192.168.18.234
if %errorLevel% equ 0 (
    echo ✓ Servidor Dev responde
) else (
    echo ✗ Servidor Dev NAO responde
)
echo.

echo [2] Verificando servico Windows...
echo ------------------------------------------------
sc query ToletusIntegracaoServer
echo.

echo [3] Verificando porta 5000...
echo ------------------------------------------------
netstat -ano | findstr :5000
echo.

echo [4] Verificando Firewall...
echo ------------------------------------------------
netsh advfirewall firewall show rule name="Toletus Integration Server"
echo.

echo [5] Verificando executavel instalado...
echo ------------------------------------------------
if exist "C:\Servicos\ToletusIntegracaoServer\Toletus.IntegracaoServer.exe" (
    echo ✓ Executavel encontrado em C:\Servicos\ToletusIntegracaoServer\
    dir "C:\Servicos\ToletusIntegracaoServer\Toletus.IntegracaoServer.exe"
) else (
    echo ✗ Executavel NAO encontrado - Execute install-service.bat!
)
echo.

echo [6] Verificando dependencias .NET...
echo ------------------------------------------------
dotnet --list-runtimes
echo.

echo [7] Verificando configuracao IP na maquina...
echo ------------------------------------------------
ipconfig | findstr "IPv4 192.168"
echo.

echo ===============================================
echo  DIAGNOSTICO CONCLUIDO
echo ===============================================
echo.
echo PROXIMOS PASSOS:
echo.
echo Se o servico esta rodando mas nao funciona:
echo   1. Verifique logs do Windows: eventvwr.msc
echo   2. Teste manualmente: "C:\Servicos\ToletusIntegracaoServer\Toletus.IntegracaoServer.exe"
echo   3. Verifique appsettings.json na pasta do servico
echo.
echo Se o .exe crasha ao abrir:
echo   1. Execute no PowerShell para ver o erro:
echo      cd C:\Servicos\ToletusIntegracaoServer
echo      .\Toletus.IntegracaoServer.exe
echo   2. Verifique se tem .NET 10 instalado (requer ASP.NET Core Runtime)
echo.
pause
