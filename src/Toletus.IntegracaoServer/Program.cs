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

// Servir arquivos estáticos (HTML, CSS, JS) - ANTES das rotas
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Rotas GET para liberação manual via navegador (favoritos)
app.MapGet("/liberar/entrada", (ILogger<Program> logger, CatracaService catracaService) =>
{
    logger.LogInformation("🔓 Liberação manual de ENTRADA via navegador");
    catracaService.LiberarEntrada();
    return Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Entrada Liberada</title>
    <style>
        body { font-family: Arial; background: #4CAF50; color: white; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; text-align: center; }
        .container { background: rgba(0,0,0,0.2); padding: 60px; border-radius: 20px; }
        h1 { font-size: 48px; margin: 0 0 20px 0; }
        p { font-size: 24px; margin: 0; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>✅ ENTRADA LIBERADA</h1>
        <p>A catraca foi liberada com sucesso!</p>
    </div>
</body>
</html>", "text/html");
});

app.MapGet("/liberar/saida", (ILogger<Program> logger, CatracaService catracaService) =>
{
    logger.LogInformation("🔓 Liberação manual de SAÍDA via navegador");
    catracaService.LiberarSaida();
    return Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Saída Liberada</title>
    <style>
        body { font-family: Arial; background: #2196F3; color: white; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; text-align: center; }
        .container { background: rgba(0,0,0,0.2); padding: 60px; border-radius: 20px; }
        h1 { font-size: 48px; margin: 0 0 20px 0; }
        p { font-size: 24px; margin: 0; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>✅ SAÍDA LIBERADA</h1>
        <p>A catraca foi liberada com sucesso!</p>
    </div>
</body>
</html>", "text/html");
});

// Rota fallback para notificações iDFace
app.MapPost("/{**path}", async (HttpContext context, string path) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var catracaService = context.RequestServices.GetRequiredService<CatracaService>();
    var mensalidadeService = context.RequestServices.GetRequiredService<MensalidadeService>();

    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        logger.LogInformation("POST recebido em /{Path}: {Body}", path, body);

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

            await mensalidadeService.RegistrarLog(userId, userName, autorizado, mensagem, tipoUsuario);

            return Results.Ok(new { success = true, authorized = autorizado, message = mensagem, userType = tipoUsuario });
        }

        return Results.Ok(new { success = true, message = "Requisição recebida" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao processar POST");
        return Results.Ok(new { success = true });
    }
});

Console.WriteLine("=== SERVIDOR DE INTEGRAÇÃO ===");
Console.WriteLine("Catraca LiteNet2 + Leitor Facial Control iD");
Console.WriteLine("Aguardando notificações...");
Console.WriteLine();

app.Run();
