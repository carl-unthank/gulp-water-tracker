namespace Gulp.Shared.DTOs;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages - 1;
    public bool HasPreviousPage => Page > 0;
}

public class PagedRequest
{
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    
    // Validation
    public void Validate()
    {
        if (Page < 0) Page = 0;
        if (PageSize < 1) PageSize = 20;
        if (PageSize > 100) PageSize = 100; // Prevent abuse
    }
}

public class WaterIntakePagedRequest : PagedRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MinAmount { get; set; }
    public int? MaxAmount { get; set; }
}
