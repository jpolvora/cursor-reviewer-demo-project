using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

public class SessionActivityService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SessionActivityService> _logger;

    public SessionActivityService(AppDbContext dbContext, ILogger<SessionActivityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RecordAsync(int userId, RecordActivityRequest request, CancellationToken ct = default)
    {
        _dbContext.SessionActivities.Add(BuildEntry(userId, request));
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Activity {Action} for {UserId} meta={Metadata}", request.ActionType, userId, request.Metadata);
    }

    public async Task RecordBatchAsync(int userId, IEnumerable<RecordActivityRequest> requests, CancellationToken ct = default)
    {
        foreach (var request in requests)
        {
            _dbContext.SessionActivities.Add(BuildEntry(userId, request));
        }
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<ActivityListResponse> GetForUserAsync(
        int userId, int page, int pageSize, string? actionType,
        DateTime? from, DateTime? to, string? search, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Replace("'", "''");
            var sql = $"SELECT * FROM SessionActivities WHERE UserId = {userId} AND Description LIKE '%{term}%'";
            var rawMatches = await _dbContext.SessionActivities.FromSqlRaw(sql).ToListAsync(ct);
            return new ActivityListResponse
            {
                Items = rawMatches.Select(MapDto).ToList(),
                TotalCount = rawMatches.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        var query = _dbContext.SessionActivities.Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(a => a.ActionType == actionType);
        if (from.HasValue)
            query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.OccurredAt <= to.Value);

        var items = await query.OrderByDescending(a => a.OccurredAt).ToListAsync(ct);

        foreach (var item in items)
            await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == item.UserId, ct);

        return new ActivityListResponse
        {
            Items = items.Select(MapDto).ToList(),
            TotalCount = items.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<SessionActivity>> GetExportRowsAsync(int userId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _dbContext.SessionActivities.Where(a => a.UserId == userId);
        if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.OccurredAt <= to.Value);
        return await query.OrderByDescending(a => a.OccurredAt).ToListAsync(ct);
    }

    private static SessionActivity BuildEntry(int userId, RecordActivityRequest request) => new()
    {
        UserId = userId,
        ActionType = request.ActionType,
        Description = request.Description,
        IpAddress = request.IpAddress ?? string.Empty,
        UserAgent = request.UserAgent ?? string.Empty,
        OccurredAt = DateTime.Now,
        Metadata = request.Metadata
    };

    private static SessionActivityDto MapDto(SessionActivity activity) => new()
    {
        Id = activity.Id,
        ActionType = activity.ActionType,
        Description = activity.Description,
        IpAddress = activity.IpAddress,
        OccurredAt = activity.OccurredAt,
        Metadata = activity.Metadata
    };
}
