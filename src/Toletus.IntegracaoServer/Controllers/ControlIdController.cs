using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Toletus.IntegracaoServer.Controllers;

/// <summary>
/// Endpoints adicionais compatíveis com Control iD
/// </summary>
[ApiController]
[Route("api")]
public class ControlIdController : ControllerBase
{
    private readonly ILogger<ControlIdController> _logger;

    public ControlIdController(ILogger<ControlIdController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de teste de conexão do Control iD
    /// </summary>
    [HttpGet("test")]
    [HttpPost("test")]
    public IActionResult Test()
    {
        _logger.LogInformation("Control iD testando conexão");
        return Ok(new { status = "online", message = "Servidor conectado" });
    }

    /// <summary>
    /// Endpoint de health check
    /// </summary>
    [HttpGet("health")]
    [HttpPost("health")]
    public IActionResult Health()
    {
        _logger.LogInformation("Health check solicitado");
        return Ok(new { healthy = true });
    }

    /// <summary>
    /// Endpoint genérico para capturar todas as requisições do Control iD
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> Events([FromBody] JsonElement data)
    {
        _logger.LogInformation("Evento recebido do Control iD: {Data}", data);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Push endpoint para modo Online
    /// </summary>
    [HttpGet("push")]
    [HttpPost("push")]
    public IActionResult Push()
    {
        _logger.LogInformation("Push request do Control iD");

        // Responde sem comandos (modo passivo)
        return Ok(new { commands = new object[] { } });
    }
}
