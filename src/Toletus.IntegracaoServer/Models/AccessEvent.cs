namespace Toletus.IntegracaoServer.Models;

/// <summary>
/// Evento de acesso recebido do Control iD
/// </summary>
public class AccessEvent
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Event { get; set; }
    public string? DeviceId { get; set; }
    public bool Authorized { get; set; }
}

/// <summary>
/// Notificação do iDFace/Control iD
/// </summary>
public class ControlIdNotification
{
    public string? Type { get; set; }
    public AccessEvent? AccessEvent { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}
