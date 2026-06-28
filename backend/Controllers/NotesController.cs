using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetNotes()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
        var user = await _db.UserSessions.Include(s => s.User).Where(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow).Select(s => s.User).FirstOrDefaultAsync();
        if (user == null) return Unauthorized();

        var notes = await _db.Notes
            .AsNoTracking()
            .Where(n => n.UserId == user.Id)
            .Select(n => new { n.Id, n.Title, n.Content, n.UserId })
            .ToListAsync();
        return Ok(notes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
        if (session == null) return Unauthorized();

        var note = new Note
        {
            Title = req.Title,
            Content = req.Content,
            UserId = session.UserId
        };
        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
        return Ok(new { note.Id, note.Title, note.Content, note.UserId });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
        if (session == null) return Unauthorized();

        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == session.UserId);
        if (note == null) return NotFound();

        _db.Notes.Remove(note);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class CreateNoteRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000)]
    public string Content { get; set; } = "";
}
