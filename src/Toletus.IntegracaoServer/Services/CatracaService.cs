using System.Net;
using Toletus.LiteNet2.Base;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;

namespace Toletus.IntegracaoServer.Services;

/// <summary>
/// Serviço para gerenciar a conexão e comandos da catraca LiteNet2
/// </summary>
public class CatracaService : IHostedService, IDisposable
{
    private readonly ILogger<CatracaService> _logger;
    private LiteNet2BoardBase? _catraca;
    private readonly string _catracaIp;
    private bool _isConnected = false;

    public event Action<ResponseCommand>? OnCatracaResponse;
    public event Action<Identification>? OnCatracaIdentification;
    public event Action<ConnectionStatus>? OnConnectionStatusChanged;

    public CatracaService(ILogger<CatracaService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _catracaIp = configuration["Catraca:IP"] ?? "192.168.18.200";
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════");
        _logger.LogInformation("🔧 Iniciando serviço de catraca...");
        _logger.LogInformation("IP configurado: {IP}", _catracaIp);

        try
        {
            _logger.LogInformation("📡 Parseando IP: {IP}", _catracaIp);
            var ip = IPAddress.Parse(_catracaIp);

            _logger.LogInformation("🔌 Criando instância LiteNet2BoardBase...");
            _catraca = new LiteNet2BoardBase(ip);

            _logger.LogInformation("📋 Configurando eventos...");
            // Configurar eventos
            _catraca.OnResponse += HandleResponse;
            _catraca.OnIdentification += HandleIdentification;
            _catraca.OnConnectionStatusChanged += HandleConnectionStatusChanged;

            _logger.LogInformation("🔗 Conectando à catraca em {IP}...", _catracaIp);
            // Conectar
            _catraca.Connect();
            _logger.LogInformation("✅ Conectado à catraca em {IP}", _catracaIp);
            _isConnected = true;

            _logger.LogInformation("⚙️ Configurando modo padrão (entrada controlada, saída livre)...");
            // Configurar modo padrão: entrada controlada, saída livre
            _catraca.Send(Commands.SetFlowControlExtended, 0);
            _logger.LogInformation("🔵 Catraca configurada: entrada controlada, saída livre");
            _logger.LogInformation("═══════════════════════════════════════════════════");
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "❌ ERRO: IP inválido: {IP}", _catracaIp);
            _logger.LogError("Verifique o appsettings.json - IP deve estar no formato: 192.168.X.X");
            _isConnected = false;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            _logger.LogError(ex, "❌ ERRO DE REDE: Não foi possível conectar à catraca em {IP}", _catracaIp);
            _logger.LogError("Possíveis causas:");
            _logger.LogError("  1. Catraca está desligada");
            _logger.LogError("  2. Catraca não está na rede (verifique: ping {IP})", _catracaIp);
            _logger.LogError("  3. Firewall bloqueando comunicação UDP");
            _logger.LogError("  4. IP incorreto no appsettings.json");
            _isConnected = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERRO INESPERADO ao conectar à catraca");
            _logger.LogError("Tipo do erro: {ErrorType}", ex.GetType().Name);
            _logger.LogError("Mensagem: {Message}", ex.Message);
            _isConnected = false;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Encerrando serviço de catraca...");

        _catraca?.Close();
        _isConnected = false;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Libera entrada da catraca
    /// </summary>
    public void LiberarEntrada()
    {
        _logger.LogInformation("🚪 LiberarEntrada() chamado - IsConnected: {IsConnected}, Catraca: {Catraca}",
            _isConnected, _catraca != null ? "OK" : "NULL");

        if (!_isConnected || _catraca == null)
        {
            _logger.LogWarning("⚠️ Tentativa de liberar catraca desconectada");
            return;
        }

        _logger.LogInformation("✅ Enviando comando ReleaseEntry para catraca...");
        _catraca.Send(Commands.ReleaseEntry);
        _logger.LogInformation("✅ Comando enviado!");
    }

    /// <summary>
    /// Libera saída da catraca
    /// </summary>
    public void LiberarSaida()
    {
        if (!_isConnected || _catraca == null)
        {
            _logger.LogWarning("Tentativa de liberar catraca desconectada");
            return;
        }

        _logger.LogInformation("Liberando saída da catraca...");
        _catraca.Send(Commands.ReleaseExit);
    }

    /// <summary>
    /// Sinaliza acesso negado na catraca (LED vermelho por alguns segundos)
    /// </summary>
    /// <param name="duracaoMs">Duração do LED vermelho em milissegundos (padrão: 3000ms)</param>
    public async Task SinalizarAcessoNegadoAsync(int duracaoMs = 3000)
    {
        if (!_isConnected || _catraca == null)
        {
            _logger.LogWarning("Tentativa de sinalizar acesso negado em catraca desconectada");
            return;
        }

        _logger.LogInformation("🔴 Sinalizando acesso negado (LED vermelho por {Duracao}ms)...", duracaoMs);

        // Modo 8 = EntryBlockedWithExitBlocked (LED vermelho temporário)
        _catraca.Send(Commands.SetFlowControlExtended, 8);

        await Task.Delay(duracaoMs);

        // Modo 0 = EntryControlledWithExitFree (entrada controlada, saída livre)
        _catraca.Send(Commands.SetFlowControlExtended, 0);

        _logger.LogInformation("🔵 Voltando ao estado normal (entrada controlada, saída livre)");
    }

    /// <summary>
    /// Sinaliza acesso negado na catraca (versão síncrona)
    /// </summary>
    /// <param name="duracaoMs">Duração do LED vermelho em milissegundos (padrão: 3000ms)</param>
    public void SinalizarAcessoNegado(int duracaoMs = 3000)
    {
        if (!_isConnected || _catraca == null)
        {
            _logger.LogWarning("Tentativa de sinalizar acesso negado em catraca desconectada");
            return;
        }

        _logger.LogInformation("🔴 Sinalizando acesso negado (LED vermelho por {Duracao}ms)...", duracaoMs);

        // Modo 8 = EntryBlockedWithExitBlocked (LED vermelho temporário)
        _catraca.Send(Commands.SetFlowControlExtended, (byte)8);

        Thread.Sleep(duracaoMs);

        // Modo 0 = EntryControlledWithExitFree (entrada controlada, saída livre)
        _catraca.Send(Commands.SetFlowControlExtended, (byte)0);

        _logger.LogInformation("🔵 Voltando ao estado normal (entrada controlada, saída livre)");
    }

    /// <summary>
    /// Obtém status da conexão
    /// </summary>
    public bool IsConnected => _isConnected;

    private void HandleResponse(ResponseCommand response)
    {
        _logger.LogInformation("Resposta da catraca: {Command} - {Data}", response.Command, response.Data);
        OnCatracaResponse?.Invoke(response);
    }

    private void HandleIdentification(LiteNet2BoardBase board, Identification identification)
    {
        _logger.LogInformation("Identificação na catraca: {Id}", identification);
        OnCatracaIdentification?.Invoke(identification);
    }

    private void HandleConnectionStatusChanged(LiteNet2BoardBase board, ConnectionStatus status)
    {
        _isConnected = status == ConnectionStatus.Connected;
        _logger.LogInformation("Status de conexão alterado: {Status}", status);
        OnConnectionStatusChanged?.Invoke(status);
    }

    public void Dispose()
    {
        _catraca?.Close();
        GC.SuppressFinalize(this);
    }
}
