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

        // Create new session
        var token = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Token = token,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _dbContext.UserSessions.Add(session);
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

        var session = await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session != null)
        {
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
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
