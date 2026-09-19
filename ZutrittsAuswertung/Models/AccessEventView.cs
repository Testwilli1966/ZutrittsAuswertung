namespace ZutrittsAuswertung.Models;

/// <summary>
/// Ein Zutrittsereignis aus access_history.sqlite3, angereichert mit
/// Stammdaten aus members.sqlite3 (Verknüpfung über card_id).
/// </summary>
public class AccessEventView
{
    public DateTime Timestamp { get; set; }
    public string CardId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? GroupName { get; set; }
    public bool Allowed { get; set; }
    public string Access { get; set; } = "";

    public string? PaymentStatus { get; set; }
    public double? OpenAmount { get; set; }
    public string? AttentionStatus { get; set; }
    public bool FreeAccess { get; set; }
    public int? TenPackUsed { get; set; }
    public int? TenPackLimit { get; set; }
}
