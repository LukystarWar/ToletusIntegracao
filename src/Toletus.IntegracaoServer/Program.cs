using Toletus.IntegracaoServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Forçar URL para aceitar conexões externas
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Configurar para rodar como Windows Service
builder.Host.UseWindowsService();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Adicionar serviços
builder.Services.AddSingleton<CatracaService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<CatracaService>());
builder.Services.AddSingleton<MensalidadeService>();

// Configurar CORS para aceitar requisições do iDFace
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// MIDDLEWARE DE DEBUG - Loga TODAS as requisições
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("═══════════════════════════════════════════════════");
    logger.LogInformation("📥 {Method} {Path}{Query}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString);
    logger.LogInformation("Content-Type: {ContentType}", context.Request.ContentType ?? "(none)");

    // Log headers importantes
    foreach (var header in context.Request.Headers)
    {
        if (header.Key.StartsWith("X-") || header.Key == "User-Agent" || header.Key == "Host")
        {
            logger.LogInformation("Header {Key}: {Value}", header.Key, header.Value);
        }
    }

    await next();
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapControllers();

// Rota raiz - GET para teste de conexão do Control iD (resposta simples)
app.MapGet("/", () => Results.Ok());

// Rota raiz - POST para capturar notificações do iDFace
app.MapPost("/", async (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var catracaService = context.RequestServices.GetRequiredService<CatracaService>();
    var mensalidadeService = context.RequestServices.GetRequiredService<MensalidadeService>();

    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        logger.LogInformation("POST na raiz recebido do iDFace: {Body}", body);

        // Tentar processar como notificação
        if (!string.IsNullOrWhiteSpace(body))
        {
            var notification = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);

            long userId = 0;
            string? userName = null;

            if (notification.TryGetProperty("user_id", out var userIdProp))
                userId = userIdProp.GetInt64();
            else if (notification.TryGetProperty("userId", out userIdProp))
                userId = userIdProp.GetInt64();
            else if (notification.TryGetProperty("id", out userIdProp))
                userId = userIdProp.GetInt64();

            if (notification.TryGetProperty("user_name", out var userNameProp))
                userName = userNameProp.GetString();
            else if (notification.TryGetProperty("userName", out userNameProp))
                userName = userNameProp.GetString();
            else if (notification.TryGetProperty("name", out userNameProp))
                userName = userNameProp.GetString();

            logger.LogInformation("Reconhecido - UserId: {UserId}, UserName: {UserName}", userId, userName);

            var (autorizado, mensagem, tipoUsuario) = await mensalidadeService.ValidarAcesso(userId, userName);

            if (autorizado)
            {
                logger.LogInformation("✅ ACESSO AUTORIZADO ({Tipo}): {Mensagem}", tipoUsuario, mensagem);
                catracaService.LiberarEntrada();
            }
            else
            {
                logger.LogWarning("❌ ACESSO NEGADO ({Tipo}): {Mensagem}", tipoUsuario ?? "desconhecido", mensagem);
            }

            // Registrar log de acesso
            await mensalidadeService.RegistrarLog(userId, userName, autorizado, mensagem, tipoUsuario);

            return Results.Ok(new { success = true, authorized = autorizado, message = mensagem, userType = tipoUsuario });
        }

        return Results.Ok(new { success = true, message = "Requisição recebida" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao processar POST na raiz");
        return Results.Ok(new { success = true });
    }
});

Console.WriteLine("=== SERVIDOR DE INTEGRAÇÃO ===");
Console.WriteLine("Catraca LiteNet2 + Leitor Facial Control iD");
Console.WriteLine("Aguardando notificações...");
Console.WriteLine();

app.Run();
