using System.Linq.Expressions;

namespace Gulp.Shared.Interfaces;

/// <summary>
/// Read-only repository interface for querying entities
/// </summary>
public interface IReadOnlyRepository<T> where T : class
{
    // Simple ID-based access
    Task<T?> GetByIdAsync(int id);
    
    // Complex composable queries - returns IQueryable for maximum flexibility
    IQueryable<T> Query();
    
    // Common async patterns with expression trees
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(int id);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    
    // Pagination support
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool orderByDescending = false);
}

/// <summary>
/// Full repository interface with read and write operations
/// </summary>
public interface IRepository<T> : IReadOnlyRepository<T> where T : class
{
    // Create operations
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    
    // Update operations
    Task<T> UpdateAsync(T entity);
    Task UpdateRangeAsync(IEnumerable<T> entities);
    
    // Delete operations (soft delete)
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteAsync(T entity);
    Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate);
    
    // Hard delete (for GDPR compliance)
    Task<bool> HardDeleteAsync(int id);
    Task<bool> HardDeleteAsync(T entity);
}
