@echo off
REM Toletus Integration Server - Instalador de Servico Windows
REM Executar como Administrador

echo ============================================
echo  Toletus Integration Server - JR Academia
echo ============================================
echo.

REM Verificar privilegios de admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERRO: Execute como Administrador!
    pause
    exit /b 1
)

REM Parar servico se existir
echo Parando servico existente...
sc stop ToletusIntegracaoServer >nul 2>&1
timeout /t 2 /nobreak >nul

REM Remover servico se existir
echo Removendo servico antigo...
sc delete ToletusIntegracaoServer >nul 2>&1
timeout /t 2 /nobreak >nul

REM Publicar aplicacao
echo.
echo Publicando aplicacao...
cd /d "%~dp0"
dotnet publish src\Toletus.IntegracaoServer\Toletus.IntegracaoServer.csproj -c Release -o "C:\Servicos\ToletusIntegracaoServer"

if %errorLevel% neq 0 (
    echo ERRO: Falha ao publicar
    pause
    exit /b 1
)

REM Criar servico
echo.
echo Criando servico Windows...
sc create ToletusIntegracaoServer binPath= "C:\Servicos\ToletusIntegracaoServer\Toletus.IntegracaoServer.exe" start= auto DisplayName= "Toletus Integration Server"

if %errorLevel% neq 0 (
    echo ERRO: Falha ao criar servico
    pause
    exit /b 1
)

REM Configurar descricao
sc description ToletusIntegracaoServer "Servidor de integracao catraca LiteNet2 + iDFace - JR Academia"

REM Iniciar servico
echo.
echo Iniciando servico...
sc start ToletusIntegracaoServer

echo.
echo ============================================
echo  Servico instalado com sucesso!
echo ============================================
echo.
echo Comandos uteis:
echo   sc stop ToletusIntegracaoServer
echo   sc start ToletusIntegracaoServer
echo   sc query ToletusIntegracaoServer
echo   sc delete ToletusIntegracaoServer
echo.
pause
