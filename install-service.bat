@echo off
REM Windows Service Installation Script for Toletus Integration Server
REM Run this as Administrator

echo ============================================
echo Toletus Integration Server - Service Setup
echo ============================================
echo.

REM Check for admin privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click and select "Run as Administrator"
    pause
    exit /b 1
)

echo Step 1: Publishing application...
cd /d "%~dp0"
dotnet publish src\Toletus.IntegracaoServer\Toletus.IntegracaoServer.csproj -c Release -o "C:\ToletusIntegracao" --self-contained false

if %errorLevel% neq 0 (
    echo ERROR: Failed to publish application
    pause
    exit /b 1
)

echo.
echo Step 2: Creating Windows Service...
sc create ToletusIntegracaoServer binPath= "C:\ToletusIntegracao\Toletus.IntegracaoServer.exe" start= auto DisplayName= "Toletus Integration Server"

if %errorLevel% neq 0 (
    echo ERROR: Failed to create service
    pause
    exit /b 1
)

echo.
echo Step 3: Setting service description...
sc description ToletusIntegracaoServer "Integration server for LiteNet2 turnstile and Control iD iDFace facial recognition"

echo.
echo Step 4: Starting service...
sc start ToletusIntegracaoServer

echo.
echo ============================================
echo Service installed successfully!
echo ============================================
echo.
echo Service Name: ToletusIntegracaoServer
echo Install Path: C:\ToletusIntegracao
echo.
echo To manage the service:
echo - Start:   sc start ToletusIntegracaoServer
echo - Stop:    sc stop ToletusIntegracaoServer
echo - Status:  sc query ToletusIntegracaoServer
echo - Uninstall: sc delete ToletusIntegracaoServer
echo.
echo Or use Windows Services (services.msc)
echo.
pause
