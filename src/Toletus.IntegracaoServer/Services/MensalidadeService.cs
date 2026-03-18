using MySql.Data.MySqlClient;

namespace Toletus.IntegracaoServer.Services;

/// <summary>
/// Serviço para validar acesso no banco de dados da academia
/// - Instrutores (ID >= 10000): Verifica se está cadastrado e ativo
/// - Alunos (ID < 10000): Verifica matrícula ativa + mensalidade em dia
/// </summary>
public class MensalidadeService
{
    private readonly ILogger<MensalidadeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    private const int INSTRUTOR_ID_MINIMO = 10000;

    public MensalidadeService(ILogger<MensalidadeService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException("MySQL connection string not configured");
    }

    /// <summary>
    /// Valida acesso baseado no ID do iDFace
    /// </summary>
    /// <param name="userId">ID do usuário no iDFace (= id do aluno OU id do instrutor)</param>
    /// <param name="userName">Nome do usuário (opcional para logs)</param>
    /// <returns>True se pode acessar, False se bloqueado</returns>
    public async Task<(bool Autorizado, string Mensagem, string? TipoUsuario)> ValidarAcesso(long userId, string? userName = null)
    {
        try
        {
            _logger.LogInformation("Validando acesso para ID {UserId} - {UserName}", userId, userName);

            // Instrutor: ID >= 10000
            if (userId >= INSTRUTOR_ID_MINIMO)
            {
                return await ValidarInstrutor(userId, userName);
            }

            // Aluno: ID < 10000
            return await ValidarAluno(userId, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar acesso para ID {UserId}", userId);
            return (false, "Erro ao validar acesso - Entre em contato com a administração", null);
        }
    }

    /// <summary>
    /// Valida acesso de instrutor - apenas verifica se existe e está ativo
    /// </summary>
    private async Task<(bool Autorizado, string Mensagem, string? TipoUsuario)> ValidarInstrutor(long idFace, string? userName)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT id, nome, ativo
            FROM instrutores
            WHERE id = @IdFace
            LIMIT 1";

        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdFace", idFace);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            _logger.LogWarning("❌ ID {IdFace} não é um instrutor cadastrado", idFace);
            return (false, "Instrutor não cadastrado no sistema", null);
        }

        string nome = reader.GetString(reader.GetOrdinal("nome"));
        bool ativo = reader.GetBoolean(reader.GetOrdinal("ativo"));

        if (!ativo)
        {
            _logger.LogWarning("❌ Instrutor {Nome} (ID: {IdFace}) está inativo", nome, idFace);
            return (false, $"Instrutor {nome} inativo no sistema", "instrutor");
        }

        _logger.LogInformation("✅ Acesso autorizado para INSTRUTOR {Nome} (ID: {IdFace})", nome, idFace);
        return (true, $"Bem-vindo, {nome}!", "instrutor");
    }

    /// <summary>
    /// Valida acesso de aluno - verifica matrícula ativa e mensalidade
    /// Lógica baseada no sistema PHP da academia
    /// </summary>
    private async Task<(bool Autorizado, string Mensagem, string? TipoUsuario)> ValidarAluno(long alunoId, string? userName)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Query baseada na lógica do PHP (lista.php e RelatorioInadimplencia.php)
        // Busca: aluno + última matrícula + modalidade + último pagamento
        var query = @"
            SELECT
                a.id AS aluno_id,
                a.nome,
                a.ativo,
                m.id AS matricula_id,
                m.status AS status_matricula,
                m.dia_vencimento,
                mo.nome AS modalidade,
                mo.valor AS valor_mensalidade,
                mo.isenta_pagamento,
                h.horario AS horario_aula,
                p.data_pagamento AS ultimo_pagamento,
                p.mes_referencia
            FROM alunos a
            INNER JOIN (
                SELECT m1.*
                FROM matriculas m1
                INNER JOIN (
                    SELECT id_aluno, MAX(id) AS max_id
                    FROM matriculas
                    GROUP BY id_aluno
                ) m2 ON m1.id = m2.max_id
            ) m ON m.id_aluno = a.id
            INNER JOIN modalidades mo ON mo.id = m.id_modalidade
            LEFT JOIN horarios h ON h.id = m.id_horario
            LEFT JOIN (
                SELECT p1.*
                FROM pagamentos p1
                INNER JOIN (
                    SELECT id_matricula, MAX(id) AS max_id
                    FROM pagamentos
                    GROUP BY id_matricula
                ) p2 ON p1.id = p2.max_id
            ) p ON m.id = p.id_matricula
            WHERE a.id = @AlunoId
            LIMIT 1";

        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@AlunoId", alunoId);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            _logger.LogWarning("❌ Aluno ID {AlunoId} não encontrado no sistema", alunoId);
            return (false, "Aluno não cadastrado no sistema", null);
        }

        string nome = reader.GetString(reader.GetOrdinal("nome"));
        bool ativo = reader.GetBoolean(reader.GetOrdinal("ativo"));
        string statusMatricula = reader.GetString(reader.GetOrdinal("status_matricula"));
        bool isentoPagamento = reader.GetBoolean(reader.GetOrdinal("isenta_pagamento"));
        string modalidade = reader.GetString(reader.GetOrdinal("modalidade"));

        // 1. Aluno inativo
        if (!ativo)
        {
            _logger.LogWarning("❌ Aluno {Nome} (ID: {AlunoId}) está inativo", nome, alunoId);
            return (false, $"Aluno {nome} inativo no sistema", "aluno");
        }

        // 2. Matrícula trancada ou cancelada
        if (statusMatricula != "ativa")
        {
            _logger.LogWarning("❌ Matrícula de {Nome} está {Status}", nome, statusMatricula);
            return (false, $"Matrícula {statusMatricula}", "aluno");
        }

        // 3. Modalidade isenta de pagamento (ex: Jiu Jitsu Social)
        // Para modalidades isentas, verificar se está no horário permitido (±30 minutos)
        if (isentoPagamento)
        {
            // Buscar horário da matrícula
            TimeSpan? horarioAula = reader.IsDBNull(reader.GetOrdinal("horario_aula"))
                ? null
                : ((MySqlDataReader)reader).GetTimeSpan(reader.GetOrdinal("horario_aula"));

            if (horarioAula.HasValue)
            {
                TimeSpan agora = DateTime.Now.TimeOfDay;
                TimeSpan tolerancia = TimeSpan.FromMinutes(30);
                TimeSpan inicioPermitido = horarioAula.Value - tolerancia;
                TimeSpan fimPermitido = horarioAula.Value + tolerancia;

                // Verificar se está fora do horário permitido
                if (agora < inicioPermitido || agora > fimPermitido)
                {
                    string horarioFormatado = horarioAula.Value.ToString(@"hh\:mm");
                    _logger.LogWarning("❌ Acesso NEGADO para {Nome} ({Modalidade}) - Fora do horário permitido ({Horario})",
                        nome, modalidade, horarioFormatado);
                    return (false, $"Acesso permitido apenas às {horarioFormatado} (±30min)", "aluno");
                }
            }

            _logger.LogInformation("✅ Acesso autorizado para {Nome} - Modalidade {Modalidade} isenta (horário OK)",
                nome, modalidade);
            return (true, $"Bem-vindo, {nome}!", "aluno");
        }

        // 4. Verificar pagamento do mês atual
        int diaVencimento = reader.IsDBNull(reader.GetOrdinal("dia_vencimento"))
            ? 10 // default dia 10
            : reader.GetInt32(reader.GetOrdinal("dia_vencimento"));

        DateTime hoje = DateTime.Today;
        string mesAtual = hoje.ToString("yyyy-MM");

        // Calcular data de vencimento do mês atual
        int ultimoDiaMes = DateTime.DaysInMonth(hoje.Year, hoje.Month);
        int diaVenc = Math.Min(diaVencimento, ultimoDiaMes);
        DateTime vencimentoAtual = new DateTime(hoje.Year, hoje.Month, diaVenc);

        // Verificar último pagamento
        DateTime? ultimoPagamento = reader.IsDBNull(reader.GetOrdinal("ultimo_pagamento"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("ultimo_pagamento"));

        string? mesReferencia = reader.IsDBNull(reader.GetOrdinal("mes_referencia"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("mes_referencia")).ToString("yyyy-MM");

        // Lógica de status (baseada no PHP lista.php)
        bool autorizado;

        if (mesReferencia == mesAtual)
        {
            // Pagou o mês atual
            autorizado = true;
            _logger.LogInformation("✅ Acesso autorizado para {Nome} - Mensalidade PAGA", nome);
        }
        else if (ultimoPagamento == null && vencimentoAtual > hoje)
        {
            // Nunca pagou mas ainda não venceu (aluno novo)
            autorizado = true;
            _logger.LogInformation("✅ Acesso autorizado para {Nome} - Aguardando primeiro pagamento (vence {Venc:dd/MM})", nome, vencimentoAtual);
        }
        else if (vencimentoAtual >= hoje)
        {
            // Ainda não venceu este mês
            autorizado = true;
            _logger.LogInformation("✅ Acesso autorizado para {Nome} - Mensalidade pendente, vence {Venc:dd/MM}", nome, vencimentoAtual);
        }
        else
        {
            // Vencido - não pagou e já passou do vencimento
            int diasAtraso = (hoje - vencimentoAtual).Days;
            autorizado = false;
            _logger.LogWarning("❌ Acesso NEGADO para {Nome} - Mensalidade vencida há {Dias} dias (venceu {Venc:dd/MM})",
                nome, diasAtraso, vencimentoAtual);
        }

        string mensagem = autorizado
            ? $"Bem-vindo, {nome}!"
            : $"Mensalidade vencida desde {vencimentoAtual:dd/MM/yyyy}";

        return (autorizado, mensagem, "aluno");
    }

    /// <summary>
    /// Registra tentativa de acesso no banco de dados
    /// </summary>
    public async Task RegistrarLog(long userId, string? userName, bool autorizado, string motivo, string? tipoUsuario = null, int? confidence = null)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var insertQuery = @"
                INSERT INTO logs_acesso
                    (idface_id, nome_usuario, tipo_usuario, autorizado, motivo, confidence, data_hora)
                VALUES
                    (@IdFace, @UserName, @TipoUsuario, @Autorizado, @Motivo, @Confidence, NOW())";

            using var cmdInsert = new MySqlCommand(insertQuery, connection);
            cmdInsert.Parameters.AddWithValue("@IdFace", userId);
            cmdInsert.Parameters.AddWithValue("@UserName", userName ?? "");
            cmdInsert.Parameters.AddWithValue("@TipoUsuario", tipoUsuario ?? (userId >= INSTRUTOR_ID_MINIMO ? "instrutor" : "aluno"));
            cmdInsert.Parameters.AddWithValue("@Autorizado", autorizado);
            cmdInsert.Parameters.AddWithValue("@Motivo", motivo);
            cmdInsert.Parameters.AddWithValue("@Confidence", confidence.HasValue ? confidence.Value : DBNull.Value);

            await cmdInsert.ExecuteNonQueryAsync();
            _logger.LogInformation("📝 Log registrado: {UserName} (ID: {UserId}) - {Autorizado}",
                userName, userId, autorizado ? "AUTORIZADO" : "NEGADO");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar log de acesso");
        }
    }
}
