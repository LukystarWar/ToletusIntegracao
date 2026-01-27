@echo off
REM Menu principal - Toletus Integracao

:MENU
cls
echo ===============================================
echo       TOLETUS INTEGRACAO - MENU PRINCIPAL
echo ===============================================
echo.
echo  [1] Garantir servico rodando (USE APOS REBOOT!)
echo  [2] Diagnostico completo
echo  [3] Testar servidor manualmente
echo  [4] Testar comunicacao com iDFace
echo  [5] Verificar dependencias
echo.
echo  [6] Instalar auto-start na inicializacao
echo  [7] Reinstalar servico
echo.
echo  [8] Abrir pasta de scripts
echo  [9] Ver guia de troubleshooting
echo.
echo  [0] Sair
echo.
echo ===============================================
echo.

set /p opcao="Escolha uma opcao: "

if "%opcao%"=="1" goto GARANTIR
if "%opcao%"=="2" goto DIAGNOSTICO
if "%opcao%"=="3" goto TEST_SERVER
if "%opcao%"=="4" goto TEST_IDFACE
if "%opcao%"=="5" goto CHECK_DEPS
if "%opcao%"=="6" goto INSTALL_STARTUP
if "%opcao%"=="7" goto REINSTALL
if "%opcao%"=="8" goto OPEN_SCRIPTS
if "%opcao%"=="9" goto TROUBLESHOOTING
if "%opcao%"=="0" goto SAIR

echo.
echo Opcao invalida!
timeout /t 2 >nul
goto MENU

:GARANTIR
cls
call scripts\garantir-servico-rodando.bat
goto MENU

:DIAGNOSTICO
cls
call scripts\diagnostico.bat
goto MENU

:TEST_SERVER
cls
call scripts\test-server.bat
goto MENU

:TEST_IDFACE
cls
call scripts\test-idface.bat
goto MENU

:CHECK_DEPS
cls
call scripts\check-dependencies.bat
goto MENU

:INSTALL_STARTUP
cls
echo ===============================================
echo  INSTALAR AUTO-START
echo ===============================================
echo.
echo ATENCAO: Requer privilegios de Administrador
echo.
pause
call scripts\instalar-startup.bat
goto MENU

:REINSTALL
cls
echo ===============================================
echo  REINSTALAR SERVICO
echo ===============================================
echo.
echo ATENCAO: Requer privilegios de Administrador
echo.
pause
call install-service.bat
goto MENU

:OPEN_SCRIPTS
cls
explorer.exe scripts
goto MENU

:TROUBLESHOOTING
cls
start notepad.exe TROUBLESHOOTING.md
goto MENU

:SAIR
echo.
echo Ate logo!
timeout /t 1 >nul
exit /b 0
