using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/quotes")]
public class QuotesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private static readonly string[] Quotes =
    [
        "First, solve the problem. Then, write the code. — John Johnson",
        "Any fool can write code that a computer can understand. Good programmers write code that humans can understand. — Martin Fowler",
        "Simplicity is the soul of efficiency. — Austin Freeman",
        "Make it work, make it right, make it fast. — Kent Beck",
        "Code is like humor. When you have to explain it, it's bad. — Cory House",
        "Fix the cause, not the symptom. — Steve Maguire",
        "Walking on water and developing software from a specification are easy if both are frozen. — Edward V. Berard",
        "The best error message is the one that never shows up. — Thomas Fuchs",
        "Optimism is an occupational hazard of programming: feedback is the treatment. — Kent Beck",
        "Deleted code is debugged code. — Jeff Sickel"
    ];

    public QuotesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("random")]
    public async Task<IActionResult> GetRandomQuote()
    {
        if (!await IsAuthenticatedAsync())
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var index = Random.Shared.Next(Quotes.Length);
        return Ok(new QuoteResponse
        {
            Text = Quotes[index],
            Index = index,
            Total = Quotes.Length
        });
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyQuote()
    {
        if (!await IsAuthenticatedAsync())
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var dayOfYear = DateTime.UtcNow.DayOfYear;
        var index = dayOfYear % Quotes.Length;

        return Ok(new QuoteResponse
        {
            Text = Quotes[index],
            Index = index,
            Total = Quotes.Length,
            Label = "quote-of-the-day"
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

    private async Task<bool> IsAuthenticatedAsync()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token)) return false;

        return await _dbContext.UserSessions
            .AnyAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
    }
}

public class QuoteResponse
{
    public string Text { get; set; } = string.Empty;
    public int Index { get; set; }
    public int Total { get; set; }
    public string? Label { get; set; }
}
