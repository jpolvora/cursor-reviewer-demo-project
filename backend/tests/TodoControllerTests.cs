using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers;
using Backend.Data;
using Backend.Models;

namespace Backend.Tests;

public class TodoControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly TodoController _controller;
    private readonly string _token;
    private readonly User _user;

    public TodoControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        _user = new User
        {
            Username = "dev_user",
            PasswordHash = "hash",
            Salt = "salt"
        };
        _dbContext.Users.Add(_user);
        _dbContext.SaveChanges();

        _token = Guid.NewGuid().ToString("N");
        _dbContext.UserSessions.Add(new UserSession
        {
            Token = _token,
            UserId = _user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        _dbContext.SaveChanges();

        _controller = new TodoController(_dbContext);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {_token}";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task List_ReturnsOkWithUserTasks()
    {
        var otherUser = new User { Username = "other", PasswordHash = "hash", Salt = "salt" };
        _dbContext.Users.Add(otherUser);
        await _dbContext.SaveChangesAsync();

        var task1 = new TodoTask { UserId = _user.Id, Title = "Task 1", Status = "Todo", Priority = "Medium" };
        var task2 = new TodoTask { UserId = otherUser.Id, Title = "Task Other User", Status = "Todo", Priority = "High" };
        _dbContext.TodoTasks.AddRange(task1, task2);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.List();

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsType<List<TodoTask>>(ok.Value);
        Assert.Single(tasks);
        Assert.Equal("Task 1", tasks[0].Title);
    }

    [Fact]
    public async Task Create_SavesAndReturnsCreatedTask()
    {
        var dto = new TodoTaskCreateDto { Title = "New Task", Description = "Desc", Priority = "High", Status = "Todo" };

        var result = await _controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var task = Assert.IsType<TodoTask>(created.Value);
        Assert.Equal("New Task", task.Title);
        Assert.Equal("Desc", task.Description);
        Assert.Equal("High", task.Priority);
        Assert.Equal("Todo", task.Status);
        Assert.Equal(_user.Id, task.UserId);

        var savedTask = await _dbContext.TodoTasks.FirstOrDefaultAsync(t => t.Id == task.Id);
        Assert.NotNull(savedTask);
        Assert.Equal("New Task", savedTask.Title);
    }

    [Fact]
    public async Task Update_ModifiesTaskDetails()
    {
        var task = new TodoTask { UserId = _user.Id, Title = "Original Title", Status = "Todo", Priority = "Medium" };
        _dbContext.TodoTasks.Add(task);
        await _dbContext.SaveChangesAsync();

        var updateDto = new TodoTaskUpdateDto { Title = "Updated Title", Status = "InProgress", Priority = "High" };

        var result = await _controller.Update(task.Id, updateDto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updatedTask = Assert.IsType<TodoTask>(ok.Value);
        Assert.Equal("Updated Title", updatedTask.Title);
        Assert.Equal("InProgress", updatedTask.Status);
        Assert.Equal("High", updatedTask.Priority);

        var freshDbTask = await _dbContext.TodoTasks.FindAsync(task.Id);
        Assert.Equal("Updated Title", freshDbTask!.Title);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var task = new TodoTask { UserId = _user.Id, Title = "To Delete", Status = "Todo", Priority = "Medium" };
        _dbContext.TodoTasks.Add(task);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Delete(task.Id);

        Assert.IsType<OkObjectResult>(result);
        var exists = await _dbContext.TodoTasks.AnyAsync(t => t.Id == task.Id);
        Assert.False(exists);
    }
}
