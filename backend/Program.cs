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
builder.Services.AddScoped<UserCharmService>();

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
    EnsureUserCharmColumns(dbContext);

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

static void EnsureUserCharmColumns(AppDbContext dbContext)
{
    var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    using (var command = dbContext.Database.GetDbConnection().CreateCommand())
    {
        command.CommandText = "PRAGMA table_info(Users);";
        dbContext.Database.OpenConnection();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            columns.Add(reader.GetString(1));
    }

    var alters = new List<string>();
    if (!columns.Contains("LuckyNumber"))
        alters.Add("ALTER TABLE Users ADD COLUMN LuckyNumber INTEGER NULL;");
    if (!columns.Contains("CharmEmoji"))
        alters.Add("ALTER TABLE Users ADD COLUMN CharmEmoji TEXT NOT NULL DEFAULT '🎲';");
    if (!columns.Contains("CharmTagline"))
        alters.Add("ALTER TABLE Users ADD COLUMN CharmTagline TEXT NULL;");
    if (!columns.Contains("CharmRolledAt"))
        alters.Add("ALTER TABLE Users ADD COLUMN CharmRolledAt TEXT NULL;");

    foreach (var sql in alters)
        dbContext.Database.ExecuteSqlRaw(sql);
}
