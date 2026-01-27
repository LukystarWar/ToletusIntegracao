@echo off
REM Script para testar o servidor manualmente e capturar erros
REM Execute na maquina cliente para identificar por que o .exe crasha

echo ===============================================
echo  TESTE MANUAL DO SERVIDOR
echo ===============================================
echo.

echo Este script vai:
echo   1. Parar o servico Windows
echo   2. Executar o .exe manualmente para capturar erros
echo   3. Mostrar qualquer erro que cause o crash
echo.

REM Verificar privilegios de admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo AVISO: Recomendado executar como Administrador
    echo.
)

echo Parando servico (se estiver rodando)...
sc stop ToletusIntegracaoServer >nul 2>&1
timeout /t 3 /nobreak >nul
echo.

echo ===============================================
echo  EXECUTANDO SERVIDOR...
echo  Pressione Ctrl+C para parar
echo ===============================================
echo.

REM Executar da pasta publicada (producao)
if exist "C:\Servicos\ToletusIntegracaoServer\Toletus.IntegracaoServer.exe" (
    echo Executando de: C:\Servicos\ToletusIntegracaoServer\
    cd /d "C:\Servicos\ToletusIntegracaoServer"
    Toletus.IntegracaoServer.exe
) else (
    echo ERRO: Servidor nao encontrado em C:\Servicos\ToletusIntegracaoServer\
    echo Execute install-service.bat primeiro!
    echo.
    pause
    exit /b 1
)

echo.
echo ===============================================
echo Servidor foi parado
echo ===============================================
echo.
pause
