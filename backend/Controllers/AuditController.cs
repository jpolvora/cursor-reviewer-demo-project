using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AuditController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private string? GetTokenFromHeader()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var val = authHeader.ToString();
            if (val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return val.Substring("Bearer ".Length).Trim();
            }
        }
        return null;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token)) return null;

        var session = await _dbContext.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        return session?.User;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var total = await _dbContext.AuditLogs.CountAsync();
        var logs = await _dbContext.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                Username = a.User != null ? a.User.Username : "unknown",
                a.Action,
                a.Details,
                a.IpAddress,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = logs });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var total = await _dbContext.AuditLogs.CountAsync(a => a.UserId == user.Id);
        var logs = await _dbContext.AuditLogs
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Action,
                a.Details,
                a.IpAddress,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = logs });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var targetUser = await _dbContext.Users.FindAsync(userId);
        if (targetUser == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var total = await _dbContext.AuditLogs.CountAsync(a => a.UserId == userId);
        var logs = await _dbContext.AuditLogs
            .Where(a => a.UserId == userId)
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                Username = a.User != null ? a.User.Username : "unknown",
                a.Action,
                a.Details,
                a.IpAddress,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = logs });
    }
}
