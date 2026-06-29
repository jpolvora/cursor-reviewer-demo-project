using System;

namespace Backend.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
