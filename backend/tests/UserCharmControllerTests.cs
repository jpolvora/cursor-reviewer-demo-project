using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Tests;

public class UserCharmControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly UserCharmController _controller;
    private string _token = string.Empty;

    public UserCharmControllerTests()
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
            Username = "charmy",
            PasswordHash = "hash",
            Salt = "salt"
        };
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        _token = Guid.NewGuid().ToString("N");
        _dbContext.UserSessions.Add(new UserSession
        {
            Token = _token,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        _dbContext.SaveChanges();

        var charmService = new UserCharmService(_dbContext);
        _controller = new UserCharmController(_dbContext, charmService);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {_token}";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task GetCharm_ReturnsDefaultBeforeReroll()
    {
        var result = await _controller.GetCharm();

        var ok = Assert.IsType<OkObjectResult>(result);
        var charm = Assert.IsType<UserCharmResponse>(ok.Value);
        Assert.Null(charm.LuckyNumber);
        Assert.Equal("🎲", charm.CharmEmoji);
        Assert.False(charm.HasRolled);
    }

    [Fact]
    public async Task RerollCharm_Unauthorized_WithoutToken()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.RerollCharm();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RerollCharm_AssignsLuckyNumberAndPersists()
    {
        var result = await _controller.RerollCharm();

        var ok = Assert.IsType<OkObjectResult>(result);
        var charm = Assert.IsType<UserCharmResponse>(ok.Value);
        Assert.InRange(charm.LuckyNumber!.Value, 1, 999);
        Assert.False(string.IsNullOrWhiteSpace(charm.CharmTagline));
        Assert.NotNull(charm.CharmRolledAt);
        Assert.True(charm.HasRolled);

        var auditCount = await _dbContext.AuditLogs.CountAsync(a => a.Action == "charm_reroll");
        Assert.Equal(1, auditCount);

        var activityCount = await _dbContext.SessionActivities.CountAsync(a => a.ActionType == SessionActivityActions.CharmReroll);
        Assert.Equal(1, activityCount);
    }

    [Fact]
    public async Task GetCharm_Unauthorized_WithoutToken()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.GetCharm();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
