using Microsoft.AspNetCore.Mvc;
using Toletus.IntegracaoServer.Models;
using Toletus.IntegracaoServer.Services;
using System.Text.Json;

namespace Toletus.IntegracaoServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessController : ControllerBase
{
    private readonly ILogger<AccessController> _logger;
    private readonly CatracaService _catracaService;
    private readonly MensalidadeService _mensalidadeService;

    public AccessController(
        ILogger<AccessController> logger,
        CatracaService catracaService,
        MensalidadeService mensalidadeService)
    {
        _logger = logger;
        _catracaService = catracaService;
        _mensalidadeService = mensalidadeService;
    }

    /// <summary>
    /// Endpoint para receber notificações do Control iD (iDFace)
    /// </summary>
    [HttpPost("notification")]
    public async Task<IActionResult> ReceiveNotification([FromBody] JsonElement notification)
    {
        try
        {
            _logger.LogInformation("Notificação recebida do iDFace: {Notification}", notification);

            // Extrair informações do usuário da notificação
            long userId = 0;
            string? userName = null;

            // Tentar extrair userId de diferentes formatos possíveis
            if (notification.TryGetProperty("user_id", out var userIdProp))
                userId = userIdProp.GetInt64();
            else if (notification.TryGetProperty("userId", out userIdProp))
                userId = userIdProp.GetInt64();
            else if (notification.TryGetProperty("id", out userIdProp))
                userId = userIdProp.GetInt64();

            // Tentar extrair userName
            if (notification.TryGetProperty("user_name", out var userNameProp))
                userName = userNameProp.GetString();
            else if (notification.TryGetProperty("userName", out userNameProp))
                userName = userNameProp.GetString();
            else if (notification.TryGetProperty("name", out userNameProp))
                userName = userNameProp.GetString();

            _logger.LogInformation("Reconhecido - UserId: {UserId}, UserName: {UserName}", userId, userName);

            // Validar mensalidade antes de liberar
            var (autorizado, mensagem, tipoUsuario) = await _mensalidadeService.ValidarAcesso(userId, userName);

            // Registrar log de acesso
            await _mensalidadeService.RegistrarLog(userId, userName, autorizado, mensagem, tipoUsuario);

            if (autorizado)
            {
                _logger.LogInformation("✅ ACESSO AUTORIZADO ({Tipo}): {Mensagem}", tipoUsuario, mensagem);
                _catracaService.LiberarEntrada();

                return Ok(new
                {
                    success = true,
                    authorized = true,
                    message = mensagem,
                    userType = tipoUsuario,
                    userId = userId,
                    userName = userName
                });
            }
            else
            {
                _logger.LogWarning("❌ ACESSO NEGADO ({Tipo}): {Mensagem}", tipoUsuario ?? "desconhecido", mensagem);

                return Ok(new
                {
                    success = true,
                    authorized = false,
                    message = mensagem,
                    userType = tipoUsuario,
                    userId = userId,
                    userName = userName
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar notificação");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint para liberar manualmente a entrada
    /// </summary>
    [HttpPost("release/entry")]
    public IActionResult ReleaseEntry()
    {
        try
        {
            _logger.LogInformation("Liberação manual de entrada solicitada");
            _catracaService.LiberarEntrada();
            return Ok(new { success = true, message = "Entrada liberada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao liberar entrada");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint para liberar manualmente a saída
    /// </summary>
    [HttpPost("release/exit")]
    public IActionResult ReleaseExit()
    {
        try
        {
            _logger.LogInformation("Liberação manual de saída solicitada");
            _catracaService.LiberarSaida();
            return Ok(new { success = true, message = "Saída liberada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao liberar saída");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Verifica status da conexão com a catraca
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            catracaConnected = _catracaService.IsConnected,
            timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// Endpoint de diagnóstico completo do sistema
    /// </summary>
    [HttpGet("diagnostico")]
    public async Task<IActionResult> GetDiagnostico()
    {
        try
        {
            var diagnostico = new
            {
                timestamp = DateTime.Now,
                servidor = new
                {
                    versao = ".NET 10.0",
                    ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processId = Environment.ProcessId
                },
                catraca = new
                {
                    conectada = _catracaService.IsConnected,
                    ip = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Catraca:IP"]
                },
                rede = new
                {
                    localEndpoint = HttpContext.Connection.LocalIpAddress?.ToString(),
                    remoteEndpoint = HttpContext.Connection.RemoteIpAddress?.ToString()
                },
                database = new
                {
                    connectionString = HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                        .GetConnectionString("MySQL")?.Replace("Pwd=", "Pwd=***") // Ocultar senha
                },
                ultimosLogs = await ObterUltimosLogs()
            };

            return Ok(diagnostico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar diagnóstico");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private async Task<object> ObterUltimosLogs()
    {
        try
        {
            // Retorna informações básicas sobre logs (você pode expandir isso)
            return new
            {
                mensagem = "Logs disponíveis via Event Viewer (Windows Service) ou console (modo manual)"
            };
        }
        catch
        {
            return new { erro = "Não foi possível obter logs" };
        }
    }
}
