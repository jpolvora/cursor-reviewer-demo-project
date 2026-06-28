using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/activity")]
public class SessionActivityController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly SessionActivityService _activityService;

    public SessionActivityController(AppDbContext dbContext, SessionActivityService activityService)
    {
        _dbContext = dbContext;
        _activityService = activityService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? actionType = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] string? search = null)
    {
        var currentUser = await ResolveUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Unauthorized access." });

        var targetUserId = userId ?? currentUser.Id;

        try
        {
            var result = await _activityService.GetForUserAsync(
                targetUserId, page, pageSize, actionType, ParseUtcDate(from), ParseUtcDate(to), search);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, detail = ex.StackTrace });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? from = null, [FromQuery] string? to = null, [FromQuery] int? userId = null)
    {
        var targetUserId = userId ?? 0;
        if (userId == null)
        {
            var currentUser = await ResolveUserAsync();
            if (currentUser != null) targetUserId = currentUser.Id;
        }

        var rows = await _activityService.GetExportRowsAsync(targetUserId, ParseUtcDate(from), ParseUtcDate(to));
        var csv = new StringBuilder("OccurredAt,ActionType,Description,IpAddress\n");

        foreach (var row in rows)
            csv.AppendLine($"{row.OccurredAt:o},{row.ActionType},{row.Description},{row.IpAddress}");

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"activity-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("record")]
    public async Task<IActionResult> Record([FromBody] RecordActivityRequest request)
    {
        var currentUser = await ResolveUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Unauthorized access." });

        if (string.IsNullOrWhiteSpace(request.ActionType))
            return BadRequest(new { message = "ActionType is required." });

        var payload = request.Metadata;
        if (request.ActionType == SessionActivityActions.PasswordChange && payload == null)
            payload = "passwordRotation=true";

        await _activityService.RecordAsync(currentUser.Id, new RecordActivityRequest
        {
            ActionType = request.ActionType,
            Description = request.Description,
            IpAddress = request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.UserAgent ?? Request.Headers.UserAgent.ToString(),
            Metadata = payload
        });

        return Ok(new { message = "Activity recorded." });
    }

    private async Task<User?> ResolveUserAsync()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token)) return null;

        var session = await _dbContext.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        return session?.User;
    }

    private string? GetTokenFromHeader()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader)) return null;
        var val = authHeader.ToString();
        return val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? val.Substring("Bearer ".Length).Trim()
            : null;
    }

    private static DateTime? ParseUtcDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateTime.TryParse(value, out var parsed) ? parsed : null;
}
