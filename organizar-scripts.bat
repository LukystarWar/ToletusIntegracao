@echo off
REM Script para organizar os arquivos .bat na pasta scripts

echo ===============================================
echo  ORGANIZANDO SCRIPTS
echo ===============================================
echo.

REM Criar pasta scripts se nao existir
if not exist "scripts" mkdir scripts

REM Mover scripts de diagnostico para pasta
echo Movendo scripts para pasta scripts/...
echo.

if exist "diagnostico.bat" (
    move /Y "diagnostico.bat" "scripts\"
    echo ✓ diagnostico.bat
)

if exist "test-server.bat" (
    move /Y "test-server.bat" "scripts\"
    echo ✓ test-server.bat
)

if exist "test-idface.bat" (
    move /Y "test-idface.bat" "scripts\"
    echo ✓ test-idface.bat
)

if exist "check-dependencies.bat" (
    move /Y "check-dependencies.bat" "scripts\"
    echo ✓ check-dependencies.bat
)

if exist "garantir-servico-rodando.bat" (
    move /Y "garantir-servico-rodando.bat" "scripts\"
    echo ✓ garantir-servico-rodando.bat
)

if exist "auto-iniciar-servico.bat" (
    move /Y "auto-iniciar-servico.bat" "scripts\"
    echo ✓ auto-iniciar-servico.bat
)

if exist "instalar-startup.bat" (
    move /Y "instalar-startup.bat" "scripts\"
    echo ✓ instalar-startup.bat
)

echo.
echo ===============================================
echo  SCRIPTS ORGANIZADOS!
echo ===============================================
echo.
echo Agora use: menu.bat para acessar tudo
echo.
pause
