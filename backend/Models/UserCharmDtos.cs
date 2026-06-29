namespace Backend.Models;

public class UserCharmResponse
{
    public int? LuckyNumber { get; set; }
    public string CharmEmoji { get; set; } = "🎲";
    public string? CharmTagline { get; set; }
    public DateTime? CharmRolledAt { get; set; }
    public bool HasRolled => LuckyNumber.HasValue;
}
