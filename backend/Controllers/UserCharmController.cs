using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth/charm")]
public class UserCharmController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserCharmService _charmService;

    public UserCharmController(AppDbContext dbContext, UserCharmService charmService)
    {
        _dbContext = dbContext;
        _charmService = charmService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCharm()
    {
        var user = await ResolveUserAsync();
        if (user == null)
            return Unauthorized(new { message = "Unauthorized access." });

        var charm = await _charmService.GetCharmAsync(user.Id);
        return charm == null
            ? NotFound(new { message = "User not found." })
            : Ok(charm);
    }

    [HttpPost("reroll")]
    public async Task<IActionResult> RerollCharm()
    {
        var user = await ResolveUserAsync();
        if (user == null)
            return Unauthorized(new { message = "Unauthorized access." });

        var charm = await _charmService.RerollCharmAsync(
            user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return charm == null
            ? NotFound(new { message = "User not found." })
            : Ok(charm);
    }

    private async Task<User?> ResolveUserAsync()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token))
            return null;

        var session = await _dbContext.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        return session?.User;
    }

    private string? GetTokenFromHeader()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return null;

        var val = authHeader.ToString();
        return val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? val["Bearer ".Length..].Trim()
            : null;
    }
}
