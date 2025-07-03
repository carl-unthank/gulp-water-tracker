namespace Gulp.Shared.DTOs;

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int AdminUsers { get; set; }
    public int DeletedUsers { get; set; }
}
