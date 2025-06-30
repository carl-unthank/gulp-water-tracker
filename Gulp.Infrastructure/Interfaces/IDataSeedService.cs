namespace Gulp.Infrastructure.Services;

public interface IDataSeedService
{
    Task SeedTestDataAsync();
    Task SeedUserWithDataAsync(int userId, int daysOfHistory = 30);
}
