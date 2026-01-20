using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Toletus.IntegracaoServer.Services;

namespace Toletus.IntegracaoServer.Controllers;

/// <summary>
/// Endpoints .fcgi específicos do Control iD (iDFace)
/// </summary>
[ApiController]
public class ControlIdFcgiController : ControllerBase
{
    private readonly ILogger<ControlIdFcgiController> _logger;
    private readonly CatracaService _catracaService;
    private readonly MensalidadeService _mensalidadeService;

    public ControlIdFcgiController(
        ILogger<ControlIdFcgiController> logger,
        CatracaService catracaService,
        MensalidadeService mensalidadeService)
    {
        _logger = logger;
        _catracaService = catracaService;
        _mensalidadeService = mensalidadeService;
    }

    /// <summary>
    /// CRITICAL: Session validation - Called when iDFace tests connection
    /// </summary>
    [HttpPost("session_is_valid.fcgi")]
    [HttpGet("session_is_valid.fcgi")]
    public IActionResult SessionIsValid()
    {
        _logger.LogInformation("✅ SESSION VALIDATION requested by iDFace");

        Response.Headers["Content-Type"] = "application/json";
        return Ok(new { session_is_valid = true });
    }

    /// <summary>
    /// CRITICAL: User identified - Called when face is recognized
    /// </summary>
    [HttpPost("new_user_identified.fcgi")]
    public async Task<IActionResult> NewUserIdentified()
    {
        try
        {
            // Read raw body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("👤 NEW USER IDENTIFIED EVENT");
            _logger.LogInformation("Raw Body: {Body}", body);
            _logger.LogInformation("Headers: {Headers}", JsonSerializer.Serialize(Request.Headers));

            // Extract user info
            long userId = 0;
            string? userName = null;

            // Check if it's form-urlencoded data
            if (Request.ContentType?.Contains("application/x-www-form-urlencoded") == true || body.Contains("&"))
            {
                _logger.LogInformation("Parsing form-urlencoded data");

                // Parse form data manually
                var formData = body.Split('&')
                    .Select(pair => pair.Split('='))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(
                        parts => parts[0],
                        parts => Uri.UnescapeDataString(parts[1])
                    );

                if (formData.TryGetValue("user_id", out var userIdStr) && long.TryParse(userIdStr, out var parsedUserId))
                    userId = parsedUserId;

                if (formData.TryGetValue("user_name", out var userNameStr))
                    userName = userNameStr;

                // Log all received data for debugging
                _logger.LogInformation("Form data: {FormData}", JsonSerializer.Serialize(formData));
            }
            else
            {
                // Try to parse JSON
                JsonElement data;
                try
                {
                    data = JsonSerializer.Deserialize<JsonElement>(body);

                    // Try different property names the iDFace might send
                    if (data.TryGetProperty("user_id", out var userIdProp))
                        userId = userIdProp.GetInt64();
                    else if (data.TryGetProperty("userId", out userIdProp))
                        userId = userIdProp.GetInt64();
                    else if (data.TryGetProperty("id", out userIdProp))
                        userId = userIdProp.GetInt64();

                    if (data.TryGetProperty("user_name", out var userNameProp))
                        userName = userNameProp.GetString();
                    else if (data.TryGetProperty("userName", out userNameProp))
                        userName = userNameProp.GetString();
                    else if (data.TryGetProperty("name", out userNameProp))
                        userName = userNameProp.GetString();
                }
                catch
                {
                    _logger.LogWarning("Body is not JSON or form data");
                }
            }

            _logger.LogInformation("Parsed - UserId: {UserId}, UserName: {UserName}", userId, userName);

            // Extract confidence if available
            int? confidence = null;
            if (Request.ContentType?.Contains("application/x-www-form-urlencoded") == true || body.Contains("&"))
            {
                var formData = body.Split('&')
                    .Select(pair => pair.Split('='))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(parts => parts[0], parts => Uri.UnescapeDataString(parts[1]));

                if (formData.TryGetValue("confidence", out var confidenceStr) && int.TryParse(confidenceStr, out var parsedConfidence))
                    confidence = parsedConfidence;
            }

            // Validate access (check payment/mensalidade)
            var (autorizado, mensagem, tipoUsuario) = await _mensalidadeService.ValidarAcesso(userId, userName);

            // Log access attempt
            await _mensalidadeService.RegistrarLog(userId, userName, autorizado, mensagem, tipoUsuario, confidence);

            if (autorizado)
            {
                _logger.LogInformation("✅ ACCESS GRANTED: {Message}", mensagem);

                // Release turnstile
                _catracaService.LiberarEntrada();

                Response.Headers["Content-Type"] = "application/json";
                return Ok(new { result = 1 }); // 1 = authorized
            }
            else
            {
                _logger.LogWarning("❌ ACCESS DENIED: {Message}", mensagem);

                Response.Headers["Content-Type"] = "application/json";
                return Ok(new { result = 0, message = mensagem }); // 0 = denied
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user identification");

            Response.Headers["Content-Type"] = "application/json";
            return Ok(new { result = 0, message = "Error processing request" });
        }
    }

    /// <summary>
    /// OPTIONAL: Device heartbeat/keepalive
    /// </summary>
    [HttpPost("device_is_alive.fcgi")]
    [HttpGet("device_is_alive.fcgi")]
    public IActionResult DeviceIsAlive()
    {
        _logger.LogInformation("💓 Heartbeat from iDFace");
        return Ok();
    }

    /// <summary>
    /// OPTIONAL: User image capture
    /// </summary>
    [HttpPost("new_user_image.fcgi")]
    public IActionResult NewUserImage()
    {
        _logger.LogInformation("📸 User image captured by iDFace");

        Response.Headers["Content-Type"] = "application/json";
        return Ok(new { result = 1 });
    }

    /// <summary>
    /// Login endpoint (if iDFace needs authentication)
    /// </summary>
    [HttpPost("login.fcgi")]
    [HttpGet("login.fcgi")]
    public IActionResult Login([FromForm] string? login, [FromForm] string? password)
    {
        _logger.LogInformation("🔐 Login attempt - User: {Login}", login ?? "unknown");

        // Accept admin/admin as per your requirement
        if (login == "admin" && password == "admin")
        {
            _logger.LogInformation("✅ Login successful");
            Response.Headers["Content-Type"] = "application/json";
            return Ok(new
            {
                session = "valid",
                user_id = 1,
                user_name = "admin"
            });
        }

        _logger.LogWarning("❌ Login failed");
        Response.Headers["Content-Type"] = "application/json";
        return Unauthorized(new { error = "Invalid credentials" });
    }
}
