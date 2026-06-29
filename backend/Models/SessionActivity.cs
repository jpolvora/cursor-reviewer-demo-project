using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class SessionActivity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    [Required]
    [MaxLength(64)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(256)]
    public string UserAgent { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public string? Metadata { get; set; }
}

public static class SessionActivityActions
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string ProfileUpdate = "ProfileUpdate";
    public const string PasswordChange = "PasswordChange";
    public const string DocumentAccess = "DocumentAccess";
    public const string ExportRequested = "ExportRequested";
    public const string CharmReroll = "CharmReroll";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Login,
        Logout,
        ProfileUpdate,
        PasswordChange,
        DocumentAccess,
        ExportRequested,
        CharmReroll
    };
}

public class RecordActivityRequest
{
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
}

public class ActivityListResponse
{
    public List<SessionActivityDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SessionActivityDto
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? Metadata { get; set; }
}
