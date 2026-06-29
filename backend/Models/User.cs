namespace Backend.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;

    /// <summary>Random lucky number (1–999) assigned on charm reroll.</summary>
    public int? LuckyNumber { get; set; }

    /// <summary>Display emoji paired with the user's charm.</summary>
    public string CharmEmoji { get; set; } = "🎲";

    /// <summary>Playful tagline generated alongside the lucky number.</summary>
    public string? CharmTagline { get; set; }

    public DateTime? CharmRolledAt { get; set; }
}
