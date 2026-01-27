@echo off
REM Script para testar comunicacao com iDFace
REM Execute na maquina cliente para verificar se o iDFace consegue enviar requisicoes

echo ===============================================
echo  TESTE DE COMUNICACAO COM IDFACE
echo ===============================================
echo.

set SERVER_IP=192.168.18.235
set SERVER_PORT=5000
set IDFACE_IP=192.168.18.173

echo Configuracao:
echo   Servidor: %SERVER_IP%:%SERVER_PORT%
echo   iDFace:   %IDFACE_IP%
echo.

echo [1] Testando se servidor esta ouvindo na porta 5000...
echo ------------------------------------------------
netstat -ano | findstr :5000
if %errorLevel% equ 0 (
    echo ✓ Porta 5000 esta em uso
) else (
    echo ✗ Porta 5000 NAO esta ouvindo - Servidor nao esta rodando!
    echo    Execute: sc start ToletusIntegracaoServer
    pause
    exit /b 1
)
echo.

echo [2] Testando endpoint de heartbeat...
echo ------------------------------------------------
curl -v http://%SERVER_IP%:5000/device_is_alive.fcgi
echo.
echo.

echo [3] Testando endpoint de session...
echo ------------------------------------------------
curl -v http://%SERVER_IP%:5000/session_is_valid.fcgi
echo.
echo.

echo [4] Testando identificacao de usuario (simulado)...
echo ------------------------------------------------
curl -v -X POST http://%SERVER_IP%:5000/new_user_identified.fcgi -H "Content-Type: application/x-www-form-urlencoded" -d "user_id=1&user_name=TESTE"
echo.
echo.

echo [5] Testando liberacao manual de entrada...
echo ------------------------------------------------
curl -v http://%SERVER_IP%:5000/liberar/entrada
echo.
echo.

echo ===============================================
echo  TESTE CONCLUIDO
echo ===============================================
echo.
echo INTERPRETACAO DOS RESULTADOS:
echo.
echo - Se recebeu "true" nos testes 2 e 3: Servidor OK
echo - Se recebeu HTML verde no teste 5: Liberacao manual OK
echo - Se recebeu "Connection refused": Servidor nao esta rodando
echo - Se recebeu timeout: Problema de firewall ou rede
echo.
echo CONFIGURACAO DO IDFACE:
echo   URL de notificacao: http://%SERVER_IP%:5000/new_user_identified.fcgi
echo   Heartbeat: http://%SERVER_IP%:5000/device_is_alive.fcgi
echo.
pause
