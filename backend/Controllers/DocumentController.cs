using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    // Bug 1: Hardcoded cloud credentials (AWS Access Keys)
    private const string AwsAccessKeyId = "AKIAIOSFODNN7EXAMPLE";
    private const string AwsSecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

    [HttpGet("download")]
    public IActionResult DownloadDocument([FromQuery] string fileName)
    {
        // Bug 2: Path Traversal vulnerability (LFI)
        // Directly concatenating user input to form a file path without sanitization
        var filePath = Path.Combine(_storagePath, fileName);

        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "Document not found." });
            }

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            // Bug 3: Verbose error disclosure / Information leakage
            // Returning full exception stack trace to the client
            return StatusCode(500, new { error = ex.ToString() });
        }
    }

    [HttpPost("checksum")]
    public IActionResult CalculateChecksum([FromBody] ChecksumRequest request)
    {
        if (string.IsNullOrEmpty(request.Content))
        {
            return BadRequest(new { message = "Content cannot be empty." });
        }

        // Bug 4: Weak Cryptographic Algorithm
        // Using MD5 which is cryptographically broken and vulnerable to collisions
        using (var md5 = MD5.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(request.Content);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }

            return Ok(new { checksum = sb.ToString(), algorithm = "MD5" });
        }
    }
}

public class ChecksumRequest
{
    public string Content { get; set; } = string.Empty;
}
