# 🔧 Guia de Troubleshooting - Toletus Integração

## 🚨 Problema: Serviço para de funcionar após reiniciar o notebook

### Causa
O serviço Windows às vezes não inicia automaticamente após reboot, mesmo configurado como "Automático".

### Solução Rápida
Execute o script: **`garantir-servico-rodando.bat`**

Este script vai:
1. Verificar se o serviço está rodando
2. Se não estiver, vai iniciar automaticamente
3. Testar se está funcionando

---

## 🔄 Solução Permanente: Auto-start garantido

### Opção 1: Reinstalar o serviço com configurações melhoradas
```batch
install-service.bat
```

O instalador agora configura:
- ✅ Início automático atrasado (delayed-auto) - espera a rede estar pronta
- ✅ Recuperação automática se crashar (tenta reiniciar 3x)
- ✅ Firewall configurado automaticamente

### Opção 2: Adicionar verificador na inicialização do Windows
```batch
instalar-startup.bat
```

Isso instala um script que roda no boot do Windows e garante que o serviço esteja ativo.

---

## 🔍 Diagnóstico Completo

### 1. Verificar conectividade com dispositivos
```batch
diagnostico.bat
```

Testa:
- ✅ Ping na Catraca (192.168.18.200)
- ✅ Ping no iDFace (192.168.18.173)
- ✅ Status do serviço Windows
- ✅ Porta 5000 está aberta
- ✅ Firewall configurado
- ✅ Arquivos instalados corretamente

### 2. Testar servidor manualmente (capturar erros)
```batch
test-server.bat
```

Executa o servidor em modo console para ver erros detalhados.

### 3. Testar comunicação com iDFace
```batch
test-idface.bat
```

Simula requisições do iDFace para o servidor:
- ✅ Heartbeat (`/device_is_alive.fcgi`)
- ✅ Session validation (`/session_is_valid.fcgi`)
- ✅ User identification
- ✅ Liberação manual

### 4. Verificar dependências
```batch
check-dependencies.bat
```

Verifica:
- ✅ .NET 10 Runtime instalado
- ✅ ASP.NET Core Runtime
- ✅ Arquivos DLL da catraca
- ✅ Configurações (appsettings.json)

---

## 📊 Monitoramento via API

### Status da conexão
```
http://192.168.18.235:5000/api/Access/status
```

Resposta:
```json
{
  "catracaConnected": true,
  "timestamp": "2026-01-27T10:30:00"
}
```

### Diagnóstico completo
```
http://192.168.18.235:5000/api/Access/diagnostico
```

Mostra:
- Informações do servidor
- Status da catraca
- Configuração de rede
- Configuração do banco de dados

### Liberar entrada manualmente
```
http://192.168.18.235:5000/liberar/entrada
```

### Liberar saída manualmente
```
http://192.168.18.235:5000/liberar/saida
```

---

## 🔥 Problemas Comuns

### Problema 1: ".exe crasha ao abrir"
**Causa:** Falta .NET 10 Runtime

**Solução:**
1. Execute: `check-dependencies.bat`
2. Se não tiver .NET 10, baixe em: https://dotnet.microsoft.com/download/dotnet/10.0
3. Instale: **ASP.NET Core Runtime 10.0.x (Hosting Bundle)**

### Problema 2: "Serviço não inicia automaticamente após reboot"
**Causa:** Serviço tenta iniciar antes da rede estar pronta

**Solução:**
```batch
# Reinstalar com delayed-auto
install-service.bat

# OU adicionar script de startup
instalar-startup.bat
```

### Problema 3: "Catraca não responde"
**Causa:** Problemas de rede ou IP incorreto

**Solução:**
1. Execute: `diagnostico.bat`
2. Verifique se catraca está ligada: `ping 192.168.18.200`
3. Verifique IP no arquivo: `C:\Servicos\ToletusIntegracaoServer\appsettings.json`

### Problema 4: "iDFace não envia notificações"
**Causa:** URL incorreta na configuração do iDFace

**Solução:**
1. Acesse interface web do iDFace (192.168.18.173)
2. Configure URLs de notificação:
   - **User identified:** `http://192.168.18.235:5000/new_user_identified.fcgi`
   - **Heartbeat:** `http://192.168.18.235:5000/device_is_alive.fcgi`
   - **Session validation:** `http://192.168.18.235:5000/session_is_valid.fcgi`

### Problema 5: "Porta 5000 já está em uso"
**Causa:** Outro programa usando a porta

**Solução:**
```batch
# Ver quem está usando a porta
netstat -ano | findstr :5000

# Matar o processo (substitua PID)
taskkill /PID <número> /F
```

---

## 📝 Logs do Sistema

### Ver logs do serviço Windows
1. Abra Event Viewer: `eventvwr.msc`
2. Navegue: Windows Logs → Application
3. Filtre por fonte: `.NET Runtime` ou `ToletusIntegracaoServer`

### Executar em modo console (ver logs em tempo real)
```batch
test-server.bat
```

---

## 🛠️ Comandos Úteis

### Gerenciar serviço
```batch
# Verificar status
sc query ToletusIntegracaoServer

# Iniciar
sc start ToletusIntegracaoServer

# Parar
sc stop ToletusIntegracaoServer

# Reiniciar
sc stop ToletusIntegracaoServer && timeout /t 2 && sc start ToletusIntegracaoServer

# Remover
sc delete ToletusIntegracaoServer
```

### Testar endpoints
```batch
# Status
curl http://localhost:5000/api/Access/status

# Heartbeat
curl http://localhost:5000/device_is_alive.fcgi

# Liberar entrada
curl http://localhost:5000/liberar/entrada
```

---

## 📞 Checklist de Verificação Rápida

Execute após reiniciar o notebook:

- [ ] `garantir-servico-rodando.bat` - Garante que serviço está ativo
- [ ] `ping 192.168.18.200` - Catraca responde
- [ ] `ping 192.168.18.173` - iDFace responde
- [ ] `curl http://localhost:5000/api/Access/status` - Servidor responde
- [ ] Testar reconhecimento facial no iDFace

Se tudo OK, sistema está funcionando! ✅

---

## 🔄 Configuração de IPs

**Topologia da rede:**
- **Servidor Cliente:** 192.168.18.235 (onde roda este servidor)
- **Servidor Dev:** 192.168.18.234
- **iDFace:** 192.168.18.173
- **Catraca:** 192.168.18.200

Para alterar IP da catraca:
1. Edite: `C:\Servicos\ToletusIntegracaoServer\appsettings.json`
2. Altere: `"Catraca": { "IP": "192.168.18.200" }`
3. Reinicie: `sc stop ToletusIntegracaoServer && sc start ToletusIntegracaoServer`
