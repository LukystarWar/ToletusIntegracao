@echo off
REM Script para verificar dependencias necessarias

echo ===============================================
echo  VERIFICACAO DE DEPENDENCIAS
echo ===============================================
echo.

echo [1] Verificando .NET Runtime instalado...
echo ------------------------------------------------
dotnet --list-runtimes
echo.

echo [2] Verificando ASP.NET Core Runtime...
echo ------------------------------------------------
dotnet --list-runtimes | findstr "Microsoft.AspNetCore.App"
echo.

echo [3] Versoes necessarias...
echo ------------------------------------------------
echo REQUERIDO: Microsoft.AspNetCore.App 10.0.x ou Microsoft.NETCore.App 10.0.x
echo.

echo [4] Se nao tiver .NET 10, baixe em:
echo    https://dotnet.microsoft.com/download/dotnet/10.0
echo    Instale: ASP.NET Core Runtime 10.0.x (Hosting Bundle para Windows)
echo.

echo [5] Verificando arquivo de configuracao...
echo ------------------------------------------------
if exist "C:\Servicos\ToletusIntegracaoServer\appsettings.json" (
    echo ✓ appsettings.json encontrado
    type "C:\Servicos\ToletusIntegracaoServer\appsettings.json"
) else (
    echo ✗ appsettings.json NAO encontrado
)
echo.

echo [6] Verificando DLLs da catraca...
echo ------------------------------------------------
if exist "C:\Servicos\ToletusIntegracaoServer\Toletus.LiteNet2.Base.dll" (
    echo ✓ Toletus.LiteNet2.Base.dll encontrado
) else (
    echo ✗ Toletus.LiteNet2.Base.dll NAO encontrado - PROBLEMA!
)

if exist "C:\Servicos\ToletusIntegracaoServer\Toletus.LiteNet2.Command.dll" (
    echo ✓ Toletus.LiteNet2.Command.dll encontrado
) else (
    echo ✗ Toletus.LiteNet2.Command.dll NAO encontrado - PROBLEMA!
)
echo.

echo ===============================================
echo  VERIFICACAO CONCLUIDA
echo ===============================================
echo.
pause
