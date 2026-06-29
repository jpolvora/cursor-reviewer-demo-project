using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

public class UserCharmService
{
    private static readonly string[] CharmEmojis =
    [
        "🎲", "🍀", "✨", "🌟", "🔮", "🎯", "🦄", "🎰", "💫", "🌈"
    ];

    private static readonly string[] TaglineTemplates =
    [
        "Fortune favors the bold.",
        "Today smells like victory.",
        "The universe winks at you.",
        "Chaos, but in a fun way.",
        "Your merge conflicts fear you.",
        "SQLite believes in you.",
        "Lint warnings scatter before you.",
        "CI green lights await.",
        "Bug reports write themselves… elsewhere.",
        "Coffee levels: optimal."
    ];

    private readonly AppDbContext _dbContext;

    public UserCharmService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserCharmResponse?> GetCharmAsync(int userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user == null ? null : Map(user);
    }

    public async Task<UserCharmResponse?> RerollCharmAsync(
        int userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return null;

        var luckyNumber = Random.Shared.Next(1, 1000);
        var emoji = CharmEmojis[Random.Shared.Next(CharmEmojis.Length)];
        var tagline = $"{TaglineTemplates[Random.Shared.Next(TaglineTemplates.Length)]} (#{luckyNumber})";

        user.LuckyNumber = luckyNumber;
        user.CharmEmoji = emoji;
        user.CharmTagline = tagline;
        user.CharmRolledAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "charm_reroll",
            Details = $"Rolled lucky number {luckyNumber} with emoji {emoji}.",
            IpAddress = ipAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        _dbContext.SessionActivities.Add(new SessionActivity
        {
            UserId = userId,
            ActionType = SessionActivityActions.CharmReroll,
            Description = $"Rerolled user charm to {luckyNumber} {emoji}.",
            IpAddress = ipAddress ?? string.Empty,
            UserAgent = userAgent ?? string.Empty,
            OccurredAt = DateTime.UtcNow,
            Metadata = $"luckyNumber={luckyNumber};emoji={emoji}"
        });

        await _dbContext.SaveChangesAsync(ct);
        return Map(user);
    }

    private static UserCharmResponse Map(User user) => new()
    {
        LuckyNumber = user.LuckyNumber,
        CharmEmoji = user.CharmEmoji,
        CharmTagline = user.CharmTagline,
        CharmRolledAt = user.CharmRolledAt
    };
}
