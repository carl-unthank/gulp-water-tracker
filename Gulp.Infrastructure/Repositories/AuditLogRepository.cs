using Microsoft.EntityFrameworkCore;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.Models;

namespace Gulp.Infrastructure.Repositories;

public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(GulpDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action)
    {
        return await _dbSet
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _dbSet
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task LogActionAsync(int userId, string action, string? details = null, string? ipAddress = null, string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        await AddAsync(auditLog);
    }
}
