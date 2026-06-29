using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DocumentController> _logger;
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

    public DocumentController(AppDbContext dbContext, ILogger<DocumentController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
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

    private async Task<User?> GetCurrentUserAsync()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token)) return null;

        var session = await _dbContext.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        return session?.User;
    }

    private async Task<bool> IsAuthenticatedAsync()
    {
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token)) return false;

        var sessionExists = await _dbContext.UserSessions
            .AnyAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        return sessionExists;
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadDocument([FromQuery] string fileName)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return BadRequest(new { message = "Filename is required." });
        }

        var storageRoot = Path.GetFullPath(_storagePath);
        var filePath = Path.GetFullPath(Path.Combine(storageRoot, fileName));

        if (!filePath.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invalid file path." });
        }

        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "Document not found." });
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "document_download",
                Details = $"Downloaded file '{fileName}'.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            return File(bytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while downloading file {FileName}", fileName);
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }

    [HttpPost("checksum")]
    public async Task<IActionResult> CalculateChecksum([FromBody] ChecksumRequest request)
    {
        if (!await IsAuthenticatedAsync())
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        if (string.IsNullOrEmpty(request.Content))
        {
            return BadRequest(new { message = "Content cannot be empty." });
        }

        using (var sha256 = SHA256.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(request.Content);
            var hashBytes = sha256.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }

            return Ok(new { checksum = sb.ToString(), algorithm = "SHA256" });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListDocuments()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        try
        {
            var files = await Task.Run(() => Directory.GetFiles(_storagePath));

            _dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "document_list",
                Details = $"User '{user.Username}' listed documents.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var documentInfos = new List<object>();

            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                documentInfos.Add(new
                {
                    Name = fileInfo.Name,
                    Size = fileInfo.Length,
                    CreatedAt = fileInfo.CreationTimeUtc
                });
            }

            return Ok(documentInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while listing documents.");
            return StatusCode(500, new { message = "An unexpected error occurred while listing documents." });
        }
    }
}

public class ChecksumRequest
{
    public string Content { get; set; } = string.Empty;
}
