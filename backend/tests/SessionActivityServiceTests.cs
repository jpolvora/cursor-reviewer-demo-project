using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Tests;

public class SessionActivityServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly SessionActivityService _service;
    private int _userId;

    public SessionActivityServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        var user = new User
        {
            Username = "tester",
            PasswordHash = "hash",
            Salt = "salt"
        };
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _userId = user.Id;

        _service = new SessionActivityService(_dbContext, NullLogger<SessionActivityService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task RecordAsync_AddsEntryAndSaves()
    {
        await _service.RecordAsync(_userId, new RecordActivityRequest
        {
            ActionType = SessionActivityActions.Login,
            Description = "User logged in",
            IpAddress = "127.0.0.1",
            UserAgent = "Mozilla/5.0",
            Metadata = "{}"
        });

        var entries = await _dbContext.SessionActivities.ToListAsync();
        var entry = Assert.Single(entries);
        Assert.Equal(_userId, entry.UserId);
        Assert.Equal(SessionActivityActions.Login, entry.ActionType);
        Assert.Equal("127.0.0.1", entry.IpAddress);
        Assert.Equal("Mozilla/5.0", entry.UserAgent);
    }

    [Fact]
    public async Task RecordBatchAsync_AddsMultipleEntries()
    {
        var requests = new[]
        {
            new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Login" },
            new RecordActivityRequest { ActionType = SessionActivityActions.Logout, Description = "Logout" },
            new RecordActivityRequest { ActionType = SessionActivityActions.ProfileUpdate, Description = "Profile changed" }
        };

        await _service.RecordBatchAsync(_userId, requests);

        var entries = await _dbContext.SessionActivities.ToListAsync();
        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyOwnActivities()
    {
        var otherUser = new User { Username = "other", PasswordHash = "h", Salt = "s" };
        _dbContext.Users.Add(otherUser);
        _dbContext.SaveChanges();

        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Mine" });
        await _service.RecordAsync(otherUser.Id, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Not mine" });

        var result = await _service.GetForUserAsync(_userId, 1, 25, null, null, null, null);

        Assert.Single(result.Items);
        Assert.Equal("Mine", result.Items[0].Description);
    }

    [Fact]
    public async Task GetForUserAsync_FiltersByActionType()
    {
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Login" });
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.ProfileUpdate, Description = "Profile" });

        var result = await _service.GetForUserAsync(_userId, 1, 25, SessionActivityActions.Login, null, null, null);

        Assert.Single(result.Items);
        Assert.Equal(SessionActivityActions.Login, result.Items[0].ActionType);
    }

    [Fact]
    public async Task GetForUserAsync_FiltersByDateRange()
    {
        var before = DateTime.Now;
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Old" });
        var midpoint = DateTime.Now;
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Recent" });

        var result = await _service.GetForUserAsync(_userId, 1, 25, null, midpoint, null, null);

        Assert.Single(result.Items);
        Assert.Equal("Recent", result.Items[0].Description);
    }

    [Fact]
    public async Task GetForUserAsync_SearchUsesLikeQuery()
    {
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Failed login from Chrome" });
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Successful login from Firefox" });

        var result = await _service.GetForUserAsync(_userId, 1, 25, null, null, null, "Chrome");

        Assert.Single(result.Items);
        Assert.Contains("Chrome", result.Items[0].Description);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsEmpty_WhenNoMatch()
    {
        var result = await _service.GetForUserAsync(_userId, 1, 25, null, null, null, null);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetExportRowsAsync_ReturnsAllRowsForUser()
    {
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "A" });
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Logout, Description = "B" });

        var rows = await _service.GetExportRowsAsync(_userId, null, null);

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task GetExportRowsAsync_FiltersByDateRange()
    {
        var before = DateTime.Now;
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Old" });
        var midpoint = DateTime.Now;
        await _service.RecordAsync(_userId, new RecordActivityRequest { ActionType = SessionActivityActions.Login, Description = "Recent" });

        var rows = await _service.GetExportRowsAsync(_userId, midpoint, null);

        Assert.Single(rows);
        Assert.Equal("Recent", rows[0].Description);
    }
}
