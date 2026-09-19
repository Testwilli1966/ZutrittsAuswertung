namespace ZutrittsAuswertung.Models;

public class Member
{
    public string CardId { get; set; } = "";
    public string? MyaxxessName { get; set; }
    public string? FirstName { get; set; }
    public string? FamilyName { get; set; }
    public string? PaymentStatus { get; set; }
    public double? OpenAmount { get; set; }
    public string? AccessRule { get; set; }
    public string? AttentionStatus { get; set; }
    public bool FreeAccess { get; set; }
    public bool ActiveInMyaxxess { get; set; }
    public int TenPackUsed { get; set; }
    public int TenPackLimit { get; set; }

    public string FullName => !string.IsNullOrWhiteSpace(MyaxxessName)
        ? MyaxxessName!
        : $"{FirstName} {FamilyName}".Trim();
}
