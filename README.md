# Toletus Integração - JR Academia

Módulo de integração para controle de acesso da **JR Academia** (CT de Jiu Jitsu), conectando a catraca **Toletus LiteNet2** com o leitor facial **Control iD iDFace**.

## Funcionalidades

- **Reconhecimento facial** via Control iD iDFace
- **Liberação automática** da catraca Toletus LiteNet2
- **Validação de mensalidade** integrada ao banco MySQL
- **Controle de instrutores e alunos** com regras diferenciadas
- **Logs de acesso** para auditoria
- **Execução como serviço Windows**

## Arquitetura

```
┌─────────────────┐     HTTP POST      ┌──────────────────────┐     TCP/IP     ┌─────────────────┐
│   iDFace        │ ─────────────────► │  Servidor Integração │ ─────────────► │  Catraca        │
│   (192.168.18.x)│   Notificação      │  (ASP.NET Core)      │   Comandos     │  LiteNet2       │
└─────────────────┘                    └──────────────────────┘                │  (192.168.18.200)
                                               │                               └─────────────────┘
                                               │
                                               ▼
                                       ┌──────────────────┐
                                       │   MySQL          │
                                       │   (academia)     │
                                       │   - alunos       │
                                       │   - matriculas   │
                                       │   - pagamentos   │
                                       │   - instrutores  │
                                       └──────────────────┘
```

## Fluxo de Acesso

1. Pessoa aproxima o rosto do iDFace
2. iDFace reconhece e envia notificação HTTP ao servidor
3. Servidor consulta MySQL para validar:
   - **Instrutores** (ID >= 10000): Verifica se está ativo
   - **Alunos** (ID < 10000): Verifica matrícula ativa + mensalidade em dia
4. Se autorizado → Libera catraca
5. Se negado → Registra log (mensalidade vencida, inativo, etc.)

## Requisitos

- .NET 10.0
- MySQL 8.0+
- Catraca Toletus LiteNet2
- Leitor Facial Control iD iDFace

## Configuração

### appsettings.json

```json
{
  "Urls": "http://0.0.0.0:5000",
  "Catraca": {
    "IP": "192.168.18.200"
  },
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=academia;Uid=root;Pwd=;"
  }
}
```

### Configurar iDFace

No painel do iDFace, configurar notificações HTTP para apontar ao servidor:
- **URL:** `http://[IP_SERVIDOR]:5000/`
- **Método:** POST

## Executar

### Desenvolvimento

```bash
dotnet run --project src/Toletus.IntegracaoServer/Toletus.IntegracaoServer.csproj
```

### Produção (Windows Service)

```bash
# Publicar
dotnet publish src/Toletus.IntegracaoServer -c Release -o C:\Toletus

# Instalar serviço
sc create "ToletusIntegracao" binPath="C:\Toletus\Toletus.IntegracaoServer.exe"
sc start ToletusIntegracao
```

## Estrutura do Projeto

```
├── src/
│   └── Toletus.IntegracaoServer/     # Servidor principal ASP.NET Core
│       ├── Controllers/              # Endpoints HTTP
│       ├── Services/
│       │   ├── CatracaService.cs     # Comunicação com LiteNet2
│       │   └── MensalidadeService.cs # Validação de acesso (MySQL)
│       └── Program.cs
├── database/                          # Scripts SQL
├── docs/                              # Documentação detalhada
└── Demo/                              # Console app de testes
```

## Documentação

- [Guia de Instalação](docs/GUIA_INSTALACAO_PRODUCAO.md)
- [Integração MySQL](docs/INTEGRACAO_MYSQL.md)
- [Manual da Funcionária](docs/MANUAL_FUNCIONARIA.md)
- [Quick Start](docs/QUICK_START.md)

## Licença

Uso exclusivo JR Academia - CT de Jiu Jitsu.

---

Desenvolvido com base na SDK [Toletus LiteNet2](https://github.com/Toletus/litenet2-exemplointegracao).
