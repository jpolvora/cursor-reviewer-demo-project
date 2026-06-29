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

    private static bool IsAdmin(User user) =>
        user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase);

    private const int MaxPageSize = 100;

    private static (int page, int pageSize) NormalizePaging(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return (page, pageSize);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? action = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        if (!IsAdmin(user))
        {
            return Forbid();
        }

        var (safePage, safePageSize) = NormalizePaging(page, pageSize);

        var query = _dbContext.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        var total = await query.CountAsync();
        var logs = await query
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
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

        return Ok(new { total, page = safePage, pageSize = safePageSize, data = logs });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var (safePage, safePageSize) = NormalizePaging(page, pageSize);

        var total = await _dbContext.AuditLogs.CountAsync(a => a.UserId == user.Id);
        var logs = await _dbContext.AuditLogs
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.Timestamp)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
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

        return Ok(new { total, page = safePage, pageSize = safePageSize, data = logs });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        if (currentUser.Id != userId && !IsAdmin(currentUser))
        {
            return Forbid();
        }

        var targetUser = await _dbContext.Users.FindAsync(userId);
        if (targetUser == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var (safePage, safePageSize) = NormalizePaging(page, pageSize);

        var total = await _dbContext.AuditLogs.CountAsync(a => a.UserId == userId);
        var logs = await _dbContext.AuditLogs
            .Where(a => a.UserId == userId)
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
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

        return Ok(new { total, page = safePage, pageSize = safePageSize, data = logs });
    }
}
