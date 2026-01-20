# Servidor de Integração - LiteNet2 + Control iD

Servidor HTTP que integra a catraca LiteNet2 com o leitor facial Control iD (iDFace).

---

## 🚀 Como Executar

### Método 1: Linha de Comando

```bash
dotnet run --project src/Toletus.IntegracaoServer/Toletus.IntegracaoServer.csproj
```

### Método 2: Executável Direto

```bash
cd src/Toletus.IntegracaoServer/bin/Debug/net10.0
./Toletus.IntegracaoServer.exe
```

---

## 📋 Configuração

O servidor roda na porta **5000** por padrão.

Edite `appsettings.json` para configurar:

```json
{
  "Urls": "http://0.0.0.0:5000",
  "Catraca": {
    "IP": "192.168.18.200"
  }
}
```

---

## 🔌 Endpoints Disponíveis

### 1. Página Inicial (Status)
```http
GET http://localhost:5000/
```

Retorna informações sobre o servidor e endpoints disponíveis.

### 2. Receber Notificações do iDFace
```http
POST http://localhost:5000/api/access/notification
Content-Type: application/json

{
  "type": "access_granted",
  "userId": 123,
  "userName": "João Silva",
  "timestamp": "2026-01-15T10:30:00"
}
```

**Ação:** Libera automaticamente a entrada da catraca.

### 3. Liberar Entrada Manualmente
```http
POST http://localhost:5000/api/access/release/entry
```

**Resposta:**
```json
{
  "success": true,
  "message": "Entrada liberada"
}
```

### 4. Liberar Saída Manualmente
```http
POST http://localhost:5000/api/access/release/exit
```

### 5. Verificar Status
```http
GET http://localhost:5000/api/access/status
```

**Resposta:**
```json
{
  "catracaConnected": true,
  "timestamp": "2026-01-15T10:30:00"
}
```

---

## ⚙️ Configurar iDFace para Enviar Notificações

1. **Acesse a interface web do iDFace:** http://192.168.18.173/

2. **Faça login** (padrão: admin/admin)

3. **Navegue até Configurações > Servidor**

4. **Configure o endereço do servidor:**
   ```
   IP do Servidor: [IP_DO_SEU_COMPUTADOR]
   Porta: 5000
   Endpoint: /api/access/notification
   ```

   **URL completa exemplo:**
   ```
   http://192.168.1.100:5000/api/access/notification
   ```

5. **Salve e teste** a conexão

---

## 🔄 Fluxo de Funcionamento

```
┌─────────────────┐
│   USUÁRIO       │
│ (aproxima rosto)│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  iDFace         │  1. Reconhece face
│ (192.168.18.173)│  2. Valida autorização
└────────┬────────┘
         │
         │ HTTP POST /api/access/notification
         ▼
┌─────────────────┐
│ SERVIDOR        │  3. Recebe notificação
│ (este projeto)  │  4. Processa evento
│ Porta 5000      │  5. Envia comando TCP
└────────┬────────┘
         │
         │ TCP Comando ReleaseEntry
         ▼
┌─────────────────┐
│  Catraca        │  6. Libera entrada
│ LiteNet2 #66    │  7. LED verde (6 seg)
│ 192.168.18.200  │  8. Aguarda passagem
└─────────────────┘
```

---

## 🧪 Testar a Integração

### Teste 1: Verificar se o servidor iniciou
```bash
curl http://localhost:5000/
```

### Teste 2: Verificar status da catraca
```bash
curl http://localhost:5000/api/access/status
```

### Teste 3: Liberar manualmente (Windows PowerShell)
```powershell
Invoke-WebRequest -Uri http://localhost:5000/api/access/release/entry -Method POST
```

### Teste 4: Liberar manualmente (curl)
```bash
curl -X POST http://localhost:5000/api/access/release/entry
```

### Teste 5: Simular notificação do iDFace
```bash
curl -X POST http://localhost:5000/api/access/notification \
  -H "Content-Type: application/json" \
  -d '{"type":"access","userId":1,"userName":"Teste"}'
```

---

## 📝 Logs

O servidor exibe logs detalhados:

```
=== SERVIDOR DE INTEGRAÇÃO ===
Catraca LiteNet2 + Leitor Facial Control iD
Aguardando notificações...

info: Toletus.IntegracaoServer.Services.CatracaService[0]
      Iniciando serviço de catraca...
info: Toletus.IntegracaoServer.Services.CatracaService[0]
      Conectado à catraca em 192.168.18.200
info: Toletus.IntegracaoServer.Controllers.AccessController[0]
      Notificação recebida do iDFace: {...}
info: Toletus.IntegracaoServer.Services.CatracaService[0]
      Liberando entrada da catraca...
info: Toletus.IntegracaoServer.Services.CatracaService[0]
      Resposta da catraca: GyreTimeout - 0
```

---

## 🔧 Troubleshooting

### Problema: Servidor não conecta à catraca
- Verifique se o IP `192.168.18.200` está correto
- Teste conectividade: `ping 192.168.18.200`
- Verifique firewall do Windows

### Problema: iDFace não envia notificações
- Verifique se o IP do servidor está correto no iDFace
- Verifique se a porta 5000 está liberada no firewall
- Teste se o iDFace consegue acessar: `http://[IP_SERVIDOR]:5000/`

### Problema: Catraca não libera
- Verifique logs do servidor
- Teste liberação manual via API
- Verifique se a aplicação ConsoleApp está fechada (não pode ter 2 conexões simultâneas)

---

## 🎯 Próximos Passos

### Melhorias Sugeridas:

1. **Validação de Autorização**
   - Verificar se o usuário reconhecido tem permissão de acesso
   - Consultar banco de dados ou API externa

2. **Registro de Logs**
   - Salvar logs de acesso em banco de dados
   - Exportar relatórios

3. **Dashboard Web**
   - Criar interface para visualizar acessos em tempo real
   - Gráficos e estatísticas

4. **Notificações**
   - Enviar alertas por email/SMS em casos especiais
   - Integrar com sistemas de segurança

5. **API de Consulta**
   - Endpoints para consultar histórico
   - API para cadastro de usuários

---

## 📚 Referências

- [Documentação LiteNet2](https://github.com/Toletus/LiteNet2-ManuaisDeIntegracao)
- [API Control iD](https://www.controlid.com.br/docs/access-api-pt/)
- [Guia de Integração Completo](GUIA_INTEGRACAO.md)

---

**Desenvolvido em:** 15/01/2026
**Versão:** 1.0
