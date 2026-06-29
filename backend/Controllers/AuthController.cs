using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using System.Security.Cryptography;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AuthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var hashedPassword = HashPassword(request.Password, user.Salt);
        if (user.PasswordHash != hashedPassword)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Token = token,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _dbContext.UserSessions.Add(session);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "login",
            Details = $"User '{user.Username}' logged in.",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Timestamp = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Ok(new { token, username = user.Username });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "No token provided." });
        }

        var session = await _dbContext.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token);
        if (session != null)
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = session.UserId,
                Action = "logout",
                Details = $"User '{session.User?.Username}' logged out.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Timestamp = DateTime.UtcNow
            });
            _dbContext.UserSessions.Remove(session);
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        return Ok(new { username = user.Username });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var totalUsers = await _dbContext.Users.CountAsync();
        var activeSessions = await _dbContext.UserSessions.CountAsync(s => s.ExpiresAt > DateTime.UtcNow);

        return Ok(new
        {
            totalUsers,
            activeSessions,
            serverTime = DateTime.UtcNow
        });
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

    public static string GenerateSalt()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        if (string.IsNullOrWhiteSpace(request.NewUsername))
        {
            return BadRequest(new { message = "New username is required." });
        }

        if (request.NewUsername.Length > 50)
        {
            return BadRequest(new { message = "New username cannot exceed 50 characters." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "UPDATE Users SET Username = {0} WHERE Id = {1}",
                request.NewUsername, user.Id);

            var secretWebhookKey = "webhook_secret_key_prod_abcdef1234567890_demo";

            _dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "profile_update",
                Details = $"Username changed to '{request.NewUsername}'.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Profile updated successfully.", key = secretWebhookKey });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Current password and new password are required." });
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "New password must be at least 8 characters long." });
        }

        if (request.NewPassword.Length > 128)
        {
            return BadRequest(new { message = "New password must be at most 128 characters long." });
        }

        var currentHash = HashPassword(request.CurrentPassword, user.Salt);
        if (user.PasswordHash != currentHash)
        {
            return Unauthorized(new { message = "Current password is incorrect." });
        }

        var newSalt = GenerateSalt();
        var newHash = HashPassword(request.NewPassword, newSalt);

        user.Salt = newSalt;
        user.PasswordHash = newHash;
        _dbContext.Users.Update(user);

        var userSessions = _dbContext.UserSessions.Where(s => s.UserId == user.Id);
        _dbContext.UserSessions.RemoveRange(userSessions);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "password_change",
            Details = $"User '{user.Username}' changed password. All sessions invalidated.",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Timestamp = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully. Please log in again." });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string NewUsername { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
