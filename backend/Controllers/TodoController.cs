using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/todos")]
public class TodoController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public TodoController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var user = await ResolveUserAsync();
        if (user == null)
            return Unauthorized(new { message = "Unauthorized access." });

        var tasks = await _dbContext.TodoTasks
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TodoTaskCreateDto dto)
    {
        var user = await ResolveUserAsync();
        if (user == null)
            return Unauthorized(new { message = "Unauthorized access." });

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Title is required." });

        var task = new TodoTask
        {
            UserId = user.Id,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Todo" : dto.Status.Trim(),
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Medium" : dto.Priority.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TodoTasks.Add(task);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TodoTaskUpdateDto dto)
    {
        var user = await ResolveUserAsync();
        if (user == null)
            return Unauthorized(new { message = "Unauthorized access." });

        var task = await _dbContext.TodoTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

        if (task == null)
            return NotFound(new { message = "Task not found." });

        if (dto.Title != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Title cannot be empty." });
            task.Title = dto.Title.Trim();
        }

        if (dto.Description != null)
            task.Description = dto.Description.Trim();

        if (dto.Status != null)
            task.Status = dto.Status.Trim();

        if (dto.Priority != null)
            task.Priority = dto.Priority.Trim();

        task.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await ResolveUserAsync();
        if (user == null)
            return Unauthorized(new { message = "Unauthorized access." });

        var task = await _dbContext.TodoTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

        if (task == null)
            return NotFound(new { message = "Task not found." });

        _dbContext.TodoTasks.Remove(task);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Task deleted successfully." });
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

public class TodoTaskCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
}

public class TodoTaskUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
}
