# Integração com MySQL - Validação de Mensalidades

Guia para integrar o servidor com seu sistema de mensalidades no MySQL.

---

## 🎯 **Fluxo com Validação de Mensalidade:**

```
1. Aluno aproxima rosto do iDFace
   ↓
2. iDFace reconhece e envia notification
   ↓
3. Servidor recebe:
   {
     "user_id": 123,
     "user_name": "João Silva"
   }
   ↓
4. Servidor consulta MySQL:
   SELECT * FROM mensalidades
   WHERE aluno_id = 123
   AND data_vencimento >= CURDATE()
   ↓
5. Decisão:
   ✅ Mensalidade OK → Libera catraca
   ❌ Mensalidade vencida → Bloqueia + log
   ↓
6. Servidor responde ao iDFace e registra log
```

---

## 📦 **1. Instalar Pacote MySQL**

```bash
cd src/Toletus.IntegracaoServer
dotnet add package MySql.Data
dotnet add package Dapper
```

---

## 🔧 **2. Configurar Conexão MySQL**

Edite `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://0.0.0.0:5000",
  "Catraca": {
    "IP": "192.168.18.200"
  },
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=seu_banco;Uid=seu_usuario;Pwd=sua_senha;"
  }
}
```

---

## 🗄️ **3. Estrutura do Banco de Dados (Exemplo)**

### **Opção A: Se você já tem as tabelas**

Apenas mapeie os campos existentes na consulta SQL.

### **Opção B: Tabelas sugeridas (se for criar do zero)**

```sql
-- Tabela de alunos
CREATE TABLE alunos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    cpf VARCHAR(14) UNIQUE,
    idface_user_id BIGINT UNIQUE, -- ID do usuário no iDFace
    ativo BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de mensalidades
CREATE TABLE mensalidades (
    id INT AUTO_INCREMENT PRIMARY KEY,
    aluno_id INT NOT NULL,
    mes_referencia DATE NOT NULL,
    data_vencimento DATE NOT NULL,
    data_pagamento DATE NULL,
    valor DECIMAL(10,2) NOT NULL,
    pago BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (aluno_id) REFERENCES alunos(id)
);

-- Índices para performance
CREATE INDEX idx_aluno_idface ON alunos(idface_user_id);
CREATE INDEX idx_mensalidade_aluno ON mensalidades(aluno_id, data_vencimento);

-- Tabela de logs de acesso (opcional)
CREATE TABLE logs_acesso (
    id INT AUTO_INCREMENT PRIMARY KEY,
    aluno_id INT NOT NULL,
    data_hora TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    autorizado BOOLEAN NOT NULL,
    motivo VARCHAR(255),
    FOREIGN KEY (aluno_id) REFERENCES alunos(id)
);
```

---

## 💻 **4. Implementar Validação Real**

Edite o arquivo `src/Toletus.IntegracaoServer/Services/MensalidadeService.cs`:

```csharp
using MySql.Data.MySqlClient;
using Dapper;

namespace Toletus.IntegracaoServer.Services;

public class MensalidadeService
{
    private readonly ILogger<MensalidadeService> _logger;
    private readonly string _connectionString;

    public MensalidadeService(ILogger<MensalidadeService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException("ConnectionString 'MySQL' não configurada");
    }

    public async Task<(bool Autorizado, string Mensagem)> ValidarAcesso(long userId, string? userName = null)
    {
        try
        {
            _logger.LogInformation("Validando acesso para usuário {UserId} - {UserName}", userId, userName);

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Consulta: Verificar se aluno existe e tem mensalidade em dia
            var query = @"
                SELECT
                    a.id,
                    a.nome,
                    a.ativo,
                    COALESCE(MAX(m.data_vencimento), '1900-01-01') as ultima_mensalidade,
                    CASE
                        WHEN a.ativo = 0 THEN 'INATIVO'
                        WHEN MAX(m.data_vencimento) >= CURDATE() THEN 'OK'
                        WHEN MAX(m.data_vencimento) IS NULL THEN 'SEM_MENSALIDADE'
                        ELSE 'VENCIDA'
                    END as status_mensalidade
                FROM alunos a
                LEFT JOIN mensalidades m ON a.id = m.aluno_id AND m.pago = TRUE
                WHERE a.idface_user_id = @UserId
                GROUP BY a.id, a.nome, a.ativo";

            var resultado = await connection.QueryFirstOrDefaultAsync<dynamic>(query, new { UserId = userId });

            // Aluno não encontrado
            if (resultado == null)
            {
                _logger.LogWarning("❌ Usuário {UserId} não encontrado no sistema", userId);
                await RegistrarLog(connection, null, false, "Usuário não cadastrado");
                return (false, "Usuário não cadastrado no sistema");
            }

            // Aluno inativo
            if (resultado.status_mensalidade == "INATIVO")
            {
                _logger.LogWarning("❌ Aluno {Nome} está inativo", resultado.nome);
                await RegistrarLog(connection, resultado.id, false, "Cadastro inativo");
                return (false, $"Acesso negado - Cadastro inativo");
            }

            // Sem mensalidades cadastradas
            if (resultado.status_mensalidade == "SEM_MENSALIDADE")
            {
                _logger.LogWarning("❌ Aluno {Nome} sem mensalidades cadastradas", resultado.nome);
                await RegistrarLog(connection, resultado.id, false, "Sem mensalidades cadastradas");
                return (false, "Acesso negado - Sem mensalidades cadastradas");
            }

            // Mensalidade vencida
            if (resultado.status_mensalidade == "VENCIDA")
            {
                DateTime vencimento = resultado.ultima_mensalidade;
                _logger.LogWarning("❌ Mensalidade vencida para {Nome} desde {Vencimento}",
                    resultado.nome, vencimento.ToString("dd/MM/yyyy"));

                await RegistrarLog(connection, resultado.id, false, $"Mensalidade vencida desde {vencimento:dd/MM/yyyy}");
                return (false, $"Acesso negado - Mensalidade vencida desde {vencimento:dd/MM/yyyy}");
            }

            // Autorizado!
            _logger.LogInformation("✅ Acesso autorizado para {Nome}", resultado.nome);
            await RegistrarLog(connection, resultado.id, true, "Acesso autorizado");
            return (true, $"Bem-vindo(a), {resultado.nome}!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar acesso no banco de dados");
            // Em caso de erro, nega acesso por segurança
            return (false, "Erro ao validar acesso - sistema temporariamente indisponível");
        }
    }

    private async Task RegistrarLog(MySqlConnection connection, int? alunoId, bool autorizado, string motivo)
    {
        try
        {
            if (alunoId == null) return;

            var insertLog = @"
                INSERT INTO logs_acesso (aluno_id, autorizado, motivo)
                VALUES (@AlunoId, @Autorizado, @Motivo)";

            await connection.ExecuteAsync(insertLog, new
            {
                AlunoId = alunoId,
                Autorizado = autorizado,
                Motivo = motivo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar log de acesso");
            // Não falha a operação principal se não conseguir registrar log
        }
    }
}
```

---

## 🧪 **5. Testar a Integração**

### **Teste 1: Inserir aluno de teste**

```sql
-- Inserir aluno vinculado ao usuário do iDFace
INSERT INTO alunos (nome, cpf, idface_user_id, ativo)
VALUES ('João Silva', '123.456.789-00', 1, TRUE);

-- Inserir mensalidade em dia
INSERT INTO mensalidades (aluno_id, mes_referencia, data_vencimento, valor, pago)
VALUES (1, '2026-01-01', '2026-01-31', 150.00, TRUE);
```

### **Teste 2: Testar via API**

```bash
# Simular reconhecimento com mensalidade OK
curl -X POST http://localhost:5000/api/access/notification \
  -H "Content-Type: application/json" \
  -d '{"user_id": 1, "user_name": "João Silva"}'

# Resultado esperado: Catraca libera
```

### **Teste 3: Mensalidade vencida**

```sql
-- Alterar vencimento para data passada
UPDATE mensalidades
SET data_vencimento = '2025-12-01'
WHERE aluno_id = 1;
```

```bash
# Testar novamente
curl -X POST http://localhost:5000/api/access/notification \
  -H "Content-Type: application/json" \
  -d '{"user_id": 1, "user_name": "João Silva"}'

# Resultado esperado: Acesso negado
```

---

## 📊 **6. Consultas Úteis**

### **Ver logs de acesso**

```sql
SELECT
    l.data_hora,
    a.nome,
    l.autorizado,
    l.motivo
FROM logs_acesso l
JOIN alunos a ON l.aluno_id = a.id
ORDER BY l.data_hora DESC
LIMIT 50;
```

### **Alunos com mensalidade vencida**

```sql
SELECT
    a.nome,
    MAX(m.data_vencimento) as ultima_mensalidade,
    DATEDIFF(CURDATE(), MAX(m.data_vencimento)) as dias_atraso
FROM alunos a
LEFT JOIN mensalidades m ON a.id = m.aluno_id AND m.pago = TRUE
WHERE a.ativo = TRUE
GROUP BY a.id, a.nome
HAVING MAX(m.data_vencimento) < CURDATE()
ORDER BY dias_atraso DESC;
```

---

## 🎯 **Próximos Passos**

1. ✅ Instalar pacotes MySQL
2. ✅ Configurar string de conexão
3. ✅ Criar/mapear tabelas no banco
4. ✅ Implementar consulta real
5. ✅ Testar com dados reais
6. ✅ Ativar captura automática no iDFace
7. ✅ Testar fluxo completo

---

**Pronto para produção!** 🚀

Quando implementar a consulta real, o sistema vai:
- ✅ Liberar apenas alunos com mensalidade em dia
- ❌ Bloquear alunos com mensalidade vencida
- 📝 Registrar todos os acessos no banco
- 📊 Gerar relatórios de acesso

