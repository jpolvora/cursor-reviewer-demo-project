using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [StringLength(64)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string Details { get; set; } = string.Empty;

    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
