@echo off
REM Script para garantir que o servico esta rodando
REM Execute este script apos reiniciar o Windows ou quando suspeitar que o servico parou

echo ===============================================
echo  VERIFICANDO SERVICO TOLETUS
echo ===============================================
echo.

REM Verificar se servico existe
sc query ToletusIntegracaoServer >nul 2>&1
if %errorLevel% neq 0 (
    echo ✗ Servico NAO esta instalado!
    echo.
    echo Execute install-service.bat primeiro!
    echo.
    pause
    exit /b 1
)

echo [1] Verificando status do servico...
echo ------------------------------------------------
sc query ToletusIntegracaoServer | findstr "STATE"

REM Verificar se esta rodando
sc query ToletusIntegracaoServer | findstr "RUNNING" >nul 2>&1
if %errorLevel% equ 0 (
    echo ✓ Servico esta RODANDO!
    echo.

    echo [2] Testando endpoint...
    curl -s http://localhost:5000/api/Access/status
    echo.
    echo.

    echo ===============================================
    echo  TUDO OK!
    echo ===============================================
    echo.
    pause
    exit /b 0
)

echo ✗ Servico esta PARADO!
echo.

echo [2] Tentando iniciar servico...
echo ------------------------------------------------
sc start ToletusIntegracaoServer

timeout /t 5 /nobreak >nul

echo.
echo [3] Verificando novamente...
echo ------------------------------------------------
sc query ToletusIntegracaoServer | findstr "STATE"

sc query ToletusIntegracaoServer | findstr "RUNNING" >nul 2>&1
if %errorLevel% equ 0 (
    echo.
    echo ✓ Servico iniciado com sucesso!
    echo.

    echo [4] Testando endpoint...
    curl -s http://localhost:5000/api/Access/status
    echo.
    echo.

    echo ===============================================
    echo  SERVICO RESTAURADO!
    echo ===============================================
) else (
    echo.
    echo ✗ Falha ao iniciar servico!
    echo.
    echo Possíveis causas:
    echo   1. Erro no aplicativo (verifique Event Viewer)
    echo   2. Porta 5000 em uso por outro programa
    echo   3. Arquivo .exe corrompido ou faltando DLLs
    echo.
    echo Execute: test-server.bat para ver o erro detalhado
    echo.
)

echo.
pause
