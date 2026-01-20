# Guia de Integração - Catraca LiteNet2 + Leitor Facial Control iD

## Resumo dos Testes Realizados

Data: 15/01/2026

---

## 1. Dispositivos Identificados

### Catraca - Toletus LiteNet2
- **Modelo:** LiteNet2 #66
- **IP:** 192.168.18.200
- **Porta:** 7878 (TCP)
- **Protocolo:** LiteNet2 (Toletus)
- **Status:** ✅ Comunicando corretamente

### Leitor Facial - Control iD
- **Fabricante:** Control iD
- **IP:** 192.168.18.173
- **Porta:** 80 (HTTP)
- **Protocolo:** Control iD REST API
- **Servidor Web:** lighttpd/1.4.51
- **Status:** ✅ Online e acessível

---

## 2. Testes Realizados

### 2.1 Busca de Dispositivos LiteNet2
✅ **SUCESSO**
```bash
dotnet run --project "src\Toletus.LiteNet2.ConsoleApp\Toletus.LiteNet2.ConsoleApp.csproj"
```
- Dispositivo encontrado: `LiteNet2 #66 192.168.18.200:7878`

### 2.2 Conexão com a Catraca
✅ **SUCESSO**
- Conexão TCP estabelecida
- Comandos sendo recebidos e respondidos

### 2.3 Comando de Liberação
⚠️ **PARCIAL**
- **Comando enviado:** `ReleaseEntry` (0x0001)
- **Resposta recebida:** `GyreTimeout` (0x0305)
- **Interpretação:** A catraca libera por 6 segundos (configuração atual) e retorna timeout se ninguém passar

### 2.4 Configurações da Catraca
✅ **CONSULTADO**
- **Duração de Liberação:** 6000ms (6 segundos)
- Comando: `GetReleaseDuration` retornou 6000

### 2.5 Leitor Facial
✅ **IDENTIFICADO**
- Interface web Control iD acessível
- Login disponível em: http://192.168.18.173/
- Protocolo diferente da catraca (não é LiteNet2)

---

## 3. Arquitetura de Integração

```
┌─────────────────────────────────────────────────────────────┐
│                   SUA APLICAÇÃO                             │
│                                                             │
│  ┌────────────────────┐        ┌───────────────────────┐   │
│  │   LiteNet2 SDK     │        │  Control iD API       │   │
│  │   (TCP Cliente)    │        │  (HTTP Cliente)       │   │
│  └─────────┬──────────┘        └──────────┬────────────┘   │
│            │                              │                │
└────────────┼──────────────────────────────┼────────────────┘
             │                              │
             │ TCP :7878                    │ HTTP :80
             │                              │
    ┌────────▼────────┐            ┌────────▼────────┐
    │  CATRACA        │            │ LEITOR FACIAL   │
    │  LiteNet2 #66   │            │  Control iD     │
    │  192.168.18.200 │            │ 192.168.18.173  │
    └─────────────────┘            └─────────────────┘
```

---

## 4. Estratégias de Integração

### Opção 1: Integração por Software (RECOMENDADA)

Sua aplicação controla os dois dispositivos separadamente:

**Fluxo de Acesso:**
1. Usuário aproxima o rosto do leitor facial
2. Control iD reconhece a face via API
3. Sua aplicação recebe o evento de identificação
4. Sua aplicação envia comando de liberação para a catraca via LiteNet2
5. Catraca libera e usuário passa
6. Registra o evento no seu sistema

**Vantagens:**
- Controle total do fluxo
- Logs centralizados
- Regras de negócio customizadas
- Fácil manutenção

### Opção 2: Integração por Hardware (Wiegand)

Configurar o leitor facial para enviar sinal Wiegand diretamente para a catraca.

**Vantagens:**
- Funciona offline
- Menor latência

**Desvantagens:**
- Menos controle
- Difícil implementar regras complexas

---

## 5. Recursos da API Control iD

### Modos de Operação

1. **Standalone** (Recomendado)
   - Identificação e autorização no terminal
   - Mais rápido e confiável

2. **Online Pro**
   - Identificação no terminal
   - Autorização no servidor

3. **Online Enterprise**
   - Identificação e autorização no servidor

### Monitoramento de Eventos

A API Control iD oferece serviço de monitoramento para eventos assíncronos:

- Logs de acesso
- Logs de alarme
- Cadastro remoto de credenciais
- Giros de catraca
- Aberturas de porta
- Mudanças de modo de operação

### Comunicação Push

- Terminal envia requisições HTTP periodicamente para o servidor
- Servidor responde com comandos a executar
- Terminal reporta resultados da execução

---

## 6. Comandos LiteNet2 Disponíveis

### Comandos de Liberação
- `ReleaseEntry` (0x0001) - Liberar entrada
- `ReleaseExit` (0x0002) - Liberar saída
- `ReleaseEntryAndExit` (0x0006) - Liberar ambos

### Comandos de Consulta (Get)
- `GetId` - Obter ID do dispositivo
- `GetFlowControl` - Obter modo de controle de fluxo
- `GetReleaseDuration` - Obter tempo de liberação
- `GetMessageLine1/2` - Obter mensagens do display
- `GetFirmwareVersion` - Obter versão do firmware
- `GetCounters` - Obter contadores de passagem

### Comandos de Configuração (Set)
- `SetId` - Definir ID
- `SetFlowControl` - Definir modo de controle
- `SetReleaseDuration` - Definir tempo de liberação
- `SetMessageLine1/2` - Definir mensagens do display

### Eventos de Identificação
- `IdentificationByRfId` (0x0301) - RFID detectado
- `IdentificationByBarCode` (0x0302) - Código de barras
- `IdentificationByKeyboard` (0x0303) - Teclado
- `PositiveIdentificationByFingerprintReader` (0x0306) - Digital OK
- `NegativeIdentificationByFingerprintReader` (0x0307) - Digital negada

### Eventos da Catraca
- `Gyre` (0x0304) - Catraca girou (alguém passou)
- `GyreTimeout` (0x0305) - Timeout (ninguém passou no tempo limite)

---

## 7. Próximos Passos

### 7.1 Testes Adicionais Necessários

- [ ] Testar configuração de Flow Control
- [ ] Verificar eventos de passagem (Gyre)
- [ ] Testar liberação de saída
- [ ] Configurar timeout maior se necessário
- [ ] Acessar API do Control iD (autenticação)
- [ ] Testar monitoramento de eventos do leitor facial

### 7.2 Desenvolvimento

- [ ] Criar classe de integração com Control iD API
- [ ] Implementar monitoramento de eventos faciais
- [ ] Criar serviço de sincronização entre os dispositivos
- [ ] Implementar logs de acesso
- [ ] Criar interface de administração

---

## 8. Recursos e Documentação

### LiteNet2 (Catraca)
- 📥 [Gerenciador LiteNet2](https://generic-spaces.actuar.cloud/suporte/Gerenciador%20Litenet%202.rar)
- 📄 [Manual de Integração](https://github.com/Toletus/LiteNet2-ManuaisDeIntegracao)
- 💻 Este repositório com SDK C#

### Control iD (Leitor Facial)
- 📄 [Documentação API (PT)](https://www.controlid.com.br/docs/access-api-pt/)
- 📄 [Documentação API (EN)](https://www.controlid.com.br/docs/access-api-en/)
- 💻 [Exemplos no GitHub](https://github.com/controlid/integracao)
- 📄 [Manual iDFace](https://www.controlid.com.br/docs/idface-pt/)

---

## 9. Exemplo de Código - Integração Básica

```csharp
using System;
using System.Net;
using Toletus.LiteNet2.Base;
using Toletus.LiteNet2.Command.Enums;

namespace IntegracaoCatraca
{
    class Program
    {
        static void Main(string[] args)
        {
            // Conectar à catraca LiteNet2
            var ip = IPAddress.Parse("192.168.18.200");
            var catraca = new LiteNet2BoardBase(ip);

            // Eventos
            catraca.OnResponse += (response) =>
            {
                Console.WriteLine($"Resposta: {response.Command}");
            };

            catraca.OnIdentification += (board, identification) =>
            {
                Console.WriteLine($"Identificação: {identification}");
            };

            // Conectar
            catraca.Connect();
            Console.WriteLine("Conectado à catraca!");

            // Liberar entrada
            Console.WriteLine("Liberando entrada...");
            catraca.Send(Commands.ReleaseEntry);

            // Aguardar eventos
            Console.ReadKey();

            // Desconectar
            catraca.Close();
        }
    }
}
```

---

## 10. Conclusões

### Status Atual
- ✅ **Catraca LiteNet2:** Totalmente compatível e funcionando
- ✅ **Leitor Facial Control iD:** Identificado e acessível
- ✅ **Comunicação:** Estabelecida com sucesso
- ⚠️ **Integração completa:** Requer desenvolvimento adicional

### Viabilidade
**TOTALMENTE VIÁVEL** ✅

Ambos os dispositivos são compatíveis e possuem APIs bem documentadas. A integração pode ser feita facilmente usando:
- SDK LiteNet2 existente (C#) para a catraca
- API REST Control iD para o leitor facial
- Sua aplicação intermediária coordenando os dois

### Recomendação
Prosseguir com a **Opção 1 (Integração por Software)** para ter controle total do fluxo de acesso e poder implementar regras de negócio customizadas.

---

**Documento gerado em:** 15/01/2026
**Versão:** 1.0
