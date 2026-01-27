@echo off
REM Script para auto-iniciar o servico Toletus
REM Copie este arquivo para a pasta de inicializacao do Windows:
REM   C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp
REM OU
REM   %APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup

REM Aguardar 10 segundos apos boot para garantir que rede esta ativa
timeout /t 10 /nobreak >nul

REM Verificar se servico existe
sc query ToletusIntegracaoServer >nul 2>&1
if %errorLevel% neq 0 (
    REM Servico nao existe, nao faz nada
    exit /b 0
)

REM Verificar se ja esta rodando
sc query ToletusIntegracaoServer | findstr "RUNNING" >nul 2>&1
if %errorLevel% equ 0 (
    REM Ja esta rodando, nao precisa fazer nada
    exit /b 0
)

REM Servico existe mas nao esta rodando, tentar iniciar
sc start ToletusIntegracaoServer >nul 2>&1

exit /b 0
