# 📂 Scripts de Diagnóstico e Manutenção

Esta pasta contém scripts utilitários para troubleshooting e manutenção do sistema Toletus Integração.

## 🚀 Scripts Principais

### 1. **garantir-servico-rodando.bat** ⭐
**USE ESTE APÓS REINICIAR O NOTEBOOK**

Verifica se o serviço está rodando e inicia automaticamente se necessário.

```batch
cd scripts
garantir-servico-rodando.bat
```

**O que faz:**
- ✅ Verifica status do serviço Windows
- ✅ Inicia automaticamente se estiver parado
- ✅ Testa conectividade (porta 5000)
- ✅ Mostra resultado final

**Quando usar:**
- Após reiniciar o notebook
- Quando sistema parar de liberar catraca
- Para verificar se tudo está OK

---

## 🔧 Scripts de Diagnóstico

### 2. **diagnostico.bat**
Diagnóstico completo do sistema.

```batch
diagnostico.bat
```

**Verifica:**
- Conectividade com Catraca (192.168.18.200)
- Conectividade com iDFace (192.168.18.173)
- Status do serviço Windows
- Porta 5000 está aberta
- Firewall configurado
- Arquivos instalados
- Dependências .NET

### 3. **test-server.bat**
Executa o servidor em modo console para capturar erros.

```batch
test-server.bat
```

**Quando usar:**
- Quando o .exe crasha
- Para ver logs em tempo real
- Para debugar problemas

### 4. **test-idface.bat**
Testa comunicação com iDFace.

```batch
test-idface.bat
```

**Testa:**
- Endpoint de heartbeat
- Endpoint de session validation
- Simulação de identificação de usuário
- Liberação manual

### 5. **check-dependencies.bat**
Verifica dependências do sistema.

```batch
check-dependencies.bat
```

**Verifica:**
- .NET 10 Runtime instalado
- ASP.NET Core Runtime
- Arquivos DLL
- Configuração (appsettings.json)

---

## 🔄 Scripts de Instalação

### 6. **instalar-startup.bat** ⚠️ (Requer Admin)
Instala verificador automático na inicialização do Windows.

```batch
# Execute como Administrador
instalar-startup.bat
```

**O que faz:**
- Copia script de auto-start para pasta Startup do Windows
- Garante que serviço seja verificado/iniciado em todo boot
- Adiciona delay de 10s para rede estar pronta

**Recomendado:** Execute uma vez após instalar o sistema

---

## 📊 Fluxo de Troubleshooting Recomendado

### Problema: "Catraca não libera após reiniciar"

1. Execute: `garantir-servico-rodando.bat`
   - Se resolver → OK!
   - Se não resolver → Passo 2

2. Execute: `diagnostico.bat`
   - Identifica qual componente está com problema
   - Mostra sugestões de correção

3. Se serviço não inicia: `test-server.bat`
   - Mostra erro exato
   - Geralmente é falta de .NET ou porta ocupada

4. Se serviço OK mas iDFace não funciona: `test-idface.bat`
   - Verifica se endpoints estão respondendo
   - Testa liberação manual

### Problema: "Sempre tenho que reiniciar após boot"

**Solução permanente:**
```batch
# Como Administrador
instalar-startup.bat
```

Isso instala um watchdog que verifica e inicia o serviço automaticamente em todo boot.

---

## 🆘 Quick Reference

| Problema | Script |
|----------|--------|
| Serviço parou após reboot | `garantir-servico-rodando.bat` |
| .exe crasha | `test-server.bat` + `check-dependencies.bat` |
| Catraca não responde | `diagnostico.bat` |
| iDFace não envia notificações | `test-idface.bat` |
| Verificar tudo | `diagnostico.bat` |
| Auto-start permanente | `instalar-startup.bat` (admin) |

---

## 💡 Dicas

1. **Adicione favoritos no navegador:**
   - http://192.168.18.235:5000/liberar/entrada
   - http://192.168.18.235:5000/liberar/saida
   - http://192.168.18.235:5000/api/Access/status

2. **Atalho na área de trabalho:**
   - Crie atalho para `garantir-servico-rodando.bat`
   - Execute sempre que suspeitar de problema

3. **Verificação diária:**
   - Abra: http://192.168.18.235:5000/api/Access/status
   - Se retornar `{"catracaConnected": true, ...}` → Sistema OK

---

## 📞 Contato

Para mais informações, veja: **TROUBLESHOOTING.md** na pasta raiz do projeto.
