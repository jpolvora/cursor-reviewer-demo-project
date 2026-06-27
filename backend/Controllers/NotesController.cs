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

        var notes = await _db.Notes.FromSqlRaw("SELECT * FROM Notes WHERE UserId = " + user.Id).Include(n => n.User).ToListAsync();
        return Ok(notes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] Note req)
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
        if (session == null) return Unauthorized();

        req.UserId = session.UserId;
        _db.Notes.Add(req);
        await _db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
        if (!await _db.UserSessions.AnyAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow)) return Unauthorized();

        var note = await _db.Notes.FindAsync(id);
        if (note == null) return NotFound();

        _db.Notes.Remove(note);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
