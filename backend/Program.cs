using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Controllers;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=auth_demo.db"));

// Enable Controllers
builder.Services.AddControllers();
builder.Services.AddScoped<SessionActivityService>();

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowAngular");

app.MapControllers();

// Initialize Database & Seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // EnsureCreated creates the SQLite database file and tables if they don't exist
    dbContext.Database.EnsureCreated();

    // Seed default admin user
    if (!dbContext.Users.Any())
    {
        var salt = AuthController.GenerateSalt();
        var adminUser = new User
        {
            Username = "admin",
            Salt = salt,
            PasswordHash = AuthController.HashPassword("admin123", salt)
        };
        dbContext.Users.Add(adminUser);
        dbContext.SaveChanges();
        Console.WriteLine("--> Default user 'admin' with password 'admin123' seeded.");
    }
}

app.Run();
