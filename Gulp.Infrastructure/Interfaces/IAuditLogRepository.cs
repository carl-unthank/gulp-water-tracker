using Gulp.Shared.Models;
using Gulp.Shared.Interfaces;

namespace Gulp.Infrastructure.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action);
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);
    Task LogActionAsync(int userId, string action, string? details = null, string? ipAddress = null, string? userAgent = null);
}
