using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests;

public class SessionActivityControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly SessionActivityService _service;
    private readonly SessionActivityController _controller;
    private int _userId;
    private string _token;

    public SessionActivityControllerTests()
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

        _token = Guid.NewGuid().ToString("N");
        _dbContext.UserSessions.Add(new UserSession
        {
            Token = _token,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        _dbContext.SaveChanges();
        _userId = user.Id;

        _service = new SessionActivityService(_dbContext, NullLogger<SessionActivityService>.Instance);
        _controller = new SessionActivityController(_dbContext, _service);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {_token}";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task List_Unauthorized_WithoutToken()
    {
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await _controller.List(userId: null);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOwnActivities_ByDefault()
    {
        _dbContext.SessionActivities.Add(new SessionActivity
        {
            UserId = _userId,
            ActionType = "Login",
            Description = "Logged in",
            OccurredAt = DateTime.UtcNow,
            IpAddress = "10.0.0.1"
        });
        _dbContext.SaveChanges();

        var result = await _controller.List(userId: null);
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ActivityListResponse>(ok.Value);
        Assert.Single(body.Items);
        Assert.Equal("Logged in", body.Items[0].Description);
    }

    [Fact]
    public async Task List_CanQueryAnyUser()
    {
        var otherUser = new User { Username = "other", PasswordHash = "h", Salt = "s" };
        _dbContext.Users.Add(otherUser);
        _dbContext.SaveChanges();

        _dbContext.SessionActivities.Add(new SessionActivity
        {
            UserId = otherUser.Id,
            ActionType = "Login",
            Description = "Other activity",
            OccurredAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var result = await _controller.List(userId: otherUser.Id);
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ActivityListResponse>(ok.Value);
        Assert.Single(body.Items);
    }

    [Fact]
    public async Task List_EchoesPageParams()
    {
        for (int i = 0; i < 5; i++)
        {
            _dbContext.SessionActivities.Add(new SessionActivity
            {
                UserId = _userId,
                ActionType = "Login",
                Description = $"Event {i}",
                OccurredAt = DateTime.UtcNow
            });
        }
        _dbContext.SaveChanges();

        var result = await _controller.List(pageSize: 2, userId: null);
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ActivityListResponse>(ok.Value);
        Assert.Equal(2, body.PageSize);
        Assert.Equal(1, body.Page);
    }

    [Fact]
    public async Task Export_ReturnsCsvFile()
    {
        _dbContext.SessionActivities.Add(new SessionActivity
        {
            UserId = _userId,
            ActionType = "Login",
            Description = "Logged in",
            OccurredAt = DateTime.UtcNow,
            IpAddress = "10.0.0.1"
        });
        _dbContext.SaveChanges();

        var result = await _controller.Export();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("OccurredAt,ActionType,Description,IpAddress", System.Text.Encoding.UTF8.GetString(fileResult.FileContents));
        Assert.Contains("Logged in", System.Text.Encoding.UTF8.GetString(fileResult.FileContents));
    }

    [Fact]
    public async Task Record_CreatesActivity()
    {
        var result = await _controller.Record(new RecordActivityRequest
        {
            ActionType = "Login",
            Description = "Manual record"
        });

        Assert.IsType<OkObjectResult>(result);

        var logs = await _dbContext.SessionActivities.ToListAsync();
        var entry = Assert.Single(logs);
        Assert.Equal(_userId, entry.UserId);
        Assert.Equal("Login", entry.ActionType);
    }

    [Fact]
    public async Task Record_ReturnsBadRequest_WhenActionTypeMissing()
    {
        var result = await _controller.Record(new RecordActivityRequest
        {
            ActionType = "",
            Description = "Missing action"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
