@echo off
REM Script para instalar o auto-inicializador na pasta Startup do Windows
REM Execute como Administrador

echo ===============================================
echo  INSTALAR AUTO-INICIALIZADOR
echo ===============================================
echo.

REM Verificar privilegios de admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERRO: Execute como Administrador!
    pause
    exit /b 1
)

REM Copiar script para pasta de startup (todos os usuarios)
set STARTUP_DIR=C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp
set SCRIPT_NAME=ToletusAutoStart.bat

echo Copiando script para pasta de inicializacao...
echo Destino: %STARTUP_DIR%\%SCRIPT_NAME%
echo.

copy /Y "%~dp0auto-iniciar-servico.bat" "%STARTUP_DIR%\%SCRIPT_NAME%"

if %errorLevel% neq 0 (
    echo ERRO: Falha ao copiar script
    pause
    exit /b 1
)

echo ✓ Script instalado com sucesso!
echo.
echo O servico Toletus agora sera verificado e iniciado automaticamente
echo sempre que o Windows iniciar (com delay de 10 segundos).
echo.
echo Para remover:
echo   del "%STARTUP_DIR%\%SCRIPT_NAME%"
echo.

REM Listar scripts de startup
echo Scripts na pasta de inicializacao:
dir "%STARTUP_DIR%\*.bat" /b
echo.

pause
