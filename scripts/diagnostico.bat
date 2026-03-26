@echo off
REM Script de Diagnostico - Toletus Integracao
REM Execute na maquina cliente para identificar problemas de conectividade

setlocal EnableDelayedExpansion

echo ===============================================
echo  DIAGNOSTICO TOLETUS INTEGRACAO
echo ===============================================
echo.

REM ------------------------------------------------
REM Leitura do IP da catraca configurado no servico
REM ------------------------------------------------
set "APPSETTINGS=C:\Servicos\ToletusIntegracaoServer\appsettings.json"
set "CATRACA_IP=192.168.18.200"

if exist "%APPSETTINGS%" (
    for /f "tokens=2 delims=:, " %%A in ('findstr /i "\"IP\"" "%APPSETTINGS%"') do (
        set "RAW=%%A"
        set "RAW=!RAW:"=!"
        set "CATRACA_IP=!RAW!"
    )
    echo IP da catraca lido do appsettings.json instalado: !CATRACA_IP!
) else (
    echo AVISO: appsettings.json do servico nao encontrado. Usando IP padrao: %CATRACA_IP%
)
echo.

REM ------------------------------------------------
echo [1] IP desta maquina na rede
echo ------------------------------------------------
for /f "tokens=2 delims=:" %%A in ('ipconfig ^| findstr /i "IPv4"') do (
    set "LOCAL_IP=%%A"
    set "LOCAL_IP=!LOCAL_IP: =!"
    echo   Adaptador: !LOCAL_IP!
)
echo.

REM Verificar se esta na mesma sub-rede /24 que a catraca
for /f "tokens=1-3 delims=." %%A in ("%CATRACA_IP%") do set "CATRACA_NET=%%A.%%B.%%C."
set "MESMA_REDE=NAO"
for /f "tokens=2 delims=:" %%A in ('ipconfig ^| findstr /i "IPv4"') do (
    set "TMP=%%A"
    set "TMP=!TMP: =!"
    if "!TMP:~0,12!"=="!CATRACA_NET:~0,12!" set "MESMA_REDE=SIM"
)
if "!MESMA_REDE!"=="SIM" (
    echo ^ ✓ Maquina esta na mesma sub-rede que a catraca (!CATRACA_NET!x)
) else (
    echo ^ ✗ ATENCAO: Maquina pode estar em sub-rede diferente da catraca (!CATRACA_NET!x)
    echo ^   Verifique se o adaptador de rede correto esta ativo.
)
echo.

REM ------------------------------------------------
echo [2] Conectividade com dispositivos (ping)
echo ------------------------------------------------
echo.

echo Testando CATRACA (%CATRACA_IP%)...
ping -n 2 -w 1000 %CATRACA_IP% >nul 2>&1
if %errorLevel% equ 0 (
    echo ^ ✓ Catraca responde ao ping
) else (
    echo ^ ✗ Catraca NAO responde ao ping - PROBLEMA!
    echo ^   Verifique: cabo de rede, IP correto, catraca ligada
)
echo.

echo Testando IDFACE (192.168.18.173)...
ping -n 2 -w 1000 192.168.18.173 >nul 2>&1
if %errorLevel% equ 0 (
    echo ^ ✓ iDFace responde ao ping
) else (
    echo ^ ✗ iDFace NAO responde ao ping
)
echo.

REM ------------------------------------------------
echo [3] Teste de porta TCP da catraca (porta 3000)
echo ------------------------------------------------
echo Tentando conexao TCP com %CATRACA_IP%:3000...
powershell -NoProfile -Command ^
  "try { $t = New-Object Net.Sockets.TcpClient; $t.Connect('%CATRACA_IP%', 3000); $t.Close(); Write-Host '  ^ OK Porta 3000 da catraca acessivel' } catch { Write-Host '  ^ FALHA Porta 3000 da catraca INACESSIVEL - verifique IP e rede' }"
echo.

REM ------------------------------------------------
echo [4] Servico Windows
echo ------------------------------------------------
sc query ToletusIntegracaoServer | findstr /i "STATE"
sc query ToletusIntegracaoServer | findstr /i "RUNNING" >nul 2>&1
if %errorLevel% equ 0 (
    echo ^ ✓ Servico ToletusIntegracaoServer esta RODANDO
) else (
    sc query ToletusIntegracaoServer >nul 2>&1
    if %errorLevel% equ 0 (
        echo ^ ✗ Servico existe mas NAO esta rodando - tente: sc start ToletusIntegracaoServer
    ) else (
        echo ^ ✗ Servico NAO instalado - Execute install-service.bat
    )
)
echo.

REM ------------------------------------------------
echo [5] Servidor HTTP local (porta 5000)
echo ------------------------------------------------
netstat -ano | findstr ":5000 " | findstr "LISTENING" >nul 2>&1
if %errorLevel% equ 0 (
    echo ^ ✓ Porta 5000 esta em escuta
) else (
    echo ^ ✗ Nada escutando na porta 5000 - servico pode estar parado ou com erro
)
echo.

echo Testando resposta HTTP do servidor local...
powershell -NoProfile -Command ^
  "try { $r = Invoke-WebRequest -Uri 'http://localhost:5000/api/catraca/status' -TimeoutSec 5 -UseBasicParsing; Write-Host ('  ^ OK HTTP ' + $r.StatusCode + ' - Servidor respondeu') } catch { Write-Host ('  ^ AVISO Sem resposta HTTP: ' + $_.Exception.Message) }"
echo.

REM ------------------------------------------------
echo [6] MySQL (localhost:3306)
echo ------------------------------------------------
powershell -NoProfile -Command ^
  "try { $t = New-Object Net.Sockets.TcpClient; $t.Connect('localhost', 3306); $t.Close(); Write-Host '  ^ OK MySQL acessivel na porta 3306' } catch { Write-Host '  ^ FALHA MySQL INACESSIVEL na porta 3306 - verifique se XAMPP/MySQL esta rodando' }"
echo.

REM ------------------------------------------------
echo [7] Executavel e configuracao instalados
echo ------------------------------------------------
if exist "C:\Servicos\ToletusIntegracaoServer\Toletus.IntegracaoServer.exe" (
    echo ^ ✓ Executavel encontrado
    for %%F in ("C:\Servicos\ToletusIntegracaoServer\Toletus.IntegracaoServer.exe") do echo ^   Tamanho: %%~zF bytes  Data: %%~tF
) else (
    echo ^ ✗ Executavel NAO encontrado em C:\Servicos\ToletusIntegracaoServer\
    echo ^   Execute install-service.bat!
)
echo.

if exist "%APPSETTINGS%" (
    echo ^ ✓ appsettings.json encontrado no servico:
    type "%APPSETTINGS%"
) else (
    echo ^ ✗ appsettings.json NAO encontrado no servico
)
echo.

REM ------------------------------------------------
echo [8] Firewall
echo ------------------------------------------------
netsh advfirewall firewall show rule name="Toletus Integration Server" | findstr /i "Enabled" >nul 2>&1
if %errorLevel% equ 0 (
    echo ^ ✓ Regra de firewall "Toletus Integration Server" existe
) else (
    echo ^ ✗ Regra de firewall NAO encontrada
    echo ^   Execute install-service.bat ou adicione manualmente a regra para porta 5000
)
echo.

REM ------------------------------------------------
echo [9] Dependencias .NET
echo ------------------------------------------------
dotnet --list-runtimes 2>nul | findstr /i "Microsoft.AspNetCore"
if %errorLevel% neq 0 (
    echo ^ ✗ ASP.NET Core Runtime NAO encontrado - baixe em https://dot.net
) else (
    echo ^ ✓ ASP.NET Core Runtime instalado
)
echo.

echo ===============================================
echo  DIAGNOSTICO CONCLUIDO
echo ===============================================
echo.
echo PROXIMOS PASSOS SE HOUVER FALHAS:
echo.
echo   Catraca sem ping:    Verifique cabo, IP e se a catraca esta ligada
echo   Porta 3000 fechada:  IP da catraca errado ou firewall na catraca
echo   Servico parado:      sc start ToletusIntegracaoServer
echo   Porta 5000 fechada:  Servico com erro - rode scripts\test-server.bat
echo   MySQL inacessivel:   Inicie o MySQL no XAMPP Control Panel
echo   IP sub-rede errada:  Ajuste o IP da maquina para 192.168.18.x
echo.
pause
