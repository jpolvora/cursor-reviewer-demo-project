using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Backend.Tests;

public class AuditControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly AuditController _controller;
    private int _adminUserId;
    private int _normalUserId;
    private string _adminToken;
    private string _normalToken;

    public AuditControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        // Seed admin user
        var admin = new User
        {
            Username = "admin",
            PasswordHash = "hash",
            Salt = "salt"
        };
        _dbContext.Users.Add(admin);

        // Seed normal user
        var normal = new User
        {
            Username = "user1",
            PasswordHash = "hash",
            Salt = "salt"
        };
        _dbContext.Users.Add(normal);
        _dbContext.SaveChanges();

        _adminUserId = admin.Id;
        _normalUserId = normal.Id;

        _adminToken = "admin_token";
        _dbContext.UserSessions.Add(new UserSession
        {
            Token = _adminToken,
            UserId = _adminUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        _normalToken = "normal_token";
        _dbContext.UserSessions.Add(new UserSession
        {
            Token = _normalToken,
            UserId = _normalUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        _dbContext.SaveChanges();

        _controller = new AuditController(_dbContext);
    }

    private void SetToken(string token)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static T? GetPropertyValue<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;
        var prop = obj.GetType().GetProperty(propertyName);
        return prop != null ? (T?)prop.GetValue(obj) : default;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task GetAll_Forbidden_ForNonAdmin()
    {
        SetToken(_normalToken);

        var result = await _controller.GetAll();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetAll_Ok_ForAdmin()
    {
        SetToken(_adminToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _normalUserId,
            Action = "test_action",
            Details = "details",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var total = GetPropertyValue<int>(okResult.Value, "total");
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetAll_FiltersByAction()
    {
        SetToken(_adminToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _normalUserId,
            Action = "action1",
            Details = "details",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _normalUserId,
            Action = "action2",
            Details = "details",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var result = await _controller.GetAll(action: "action1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var total = GetPropertyValue<int>(okResult.Value, "total");
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetMyLogs_ReturnsOnlyCallerEntries()
    {
        SetToken(_normalToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _normalUserId,
            Action = "my_action",
            Details = "details",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _adminUserId,
            Action = "admin_action",
            Details = "details",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var result = await _controller.GetMyLogs();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var total = GetPropertyValue<int>(okResult.Value, "total");
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetByUser_Forbidden_WhenQueryingOtherUser()
    {
        SetToken(_normalToken);

        var result = await _controller.GetByUser(_adminUserId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetByUser_Ok_ForAdmin()
    {
        SetToken(_adminToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _normalUserId,
            Action = "some_action",
            Details = "details",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var result = await _controller.GetByUser(_normalUserId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var total = GetPropertyValue<int>(okResult.Value, "total");
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetByUser_Ok_ForSelf()
    {
        SetToken(_normalToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _normalUserId,
            Action = "some_action",
            Details = "details",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        var result = await _controller.GetByUser(_normalUserId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var total = GetPropertyValue<int>(okResult.Value, "total");
        Assert.Equal(1, total);
    }
}
