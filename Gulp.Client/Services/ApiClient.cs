using System.Net.Http.Json;
using System.Text.Json;
using Gulp.Shared.DTOs;
using Gulp.Shared.Common;

namespace Gulp.Client.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    // Authentication
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users", registerDto, _jsonOptions);
        return await HandleAuthResponseAsync(response);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/sessions", loginDto, _jsonOptions);
        return await HandleAuthResponseAsync(response);
    }

    public async Task<bool> LogoutAsync()
    {
        var response = await _httpClient.DeleteAsync("api/sessions/current");
        return response.IsSuccessStatusCode;
    }

    public async Task<AuthResponseDto> RefreshTokenAsync()
    {
        var response = await _httpClient.PutAsync("api/sessions/current", null);
        return await HandleAuthResponseAsync(response);
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/users/current");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    return JsonSerializer.Deserialize<UserDto>(content, _jsonOptions);
                }
            }
            return null;
        }
        catch (HttpRequestException)
        {
            // Network or HTTP errors - user is likely not authenticated
            return null;
        }
        catch (Exception ex)
        {
            // Log other exceptions but don't throw
            Console.WriteLine($"Error getting current user: {ex.Message}");
            return null;
        }
    }

    // Water Intakes
    public async Task<List<WaterIntakeDto>> GetWaterIntakesAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        var query = $"api/intakes?page={page}&pageSize={pageSize}";
        if (startDate.HasValue)
            query += $"&startDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue)
            query += $"&endDate={endDate.Value:yyyy-MM-dd}";

        return await _httpClient.GetFromJsonAsync<List<WaterIntakeDto>>(query, _jsonOptions) ?? new List<WaterIntakeDto>();
    }

    public async Task<List<WaterIntakeDto>> GetTodayIntakesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<WaterIntakeDto>>("api/intakes/today", _jsonOptions) ?? new List<WaterIntakeDto>();
    }

    public async Task<WaterIntakeDto?> GetWaterIntakeAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WaterIntakeDto>($"api/intakes/{id}", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<WaterIntakeDto> CreateWaterIntakeAsync(CreateWaterIntakeDto createDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/intakes", createDto, _jsonOptions);
        return await HandleResponseAsync<WaterIntakeDto>(response);
    }

    public async Task<WaterIntakeDto> UpdateWaterIntakeAsync(int id, UpdateWaterIntakeDto updateDto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/intakes/{id}", updateDto, _jsonOptions);
        return await HandleResponseAsync<WaterIntakeDto>(response);
    }

    public async Task<bool> DeleteWaterIntakeAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/intakes/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<int> GetTotalForDateAsync(DateTime date)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<int>($"api/intakes/total/{date:yyyy-MM-dd}", _jsonOptions);
        }
        catch
        {
            return 0;
        }
    }

    public async Task<Dictionary<DateTime, int>> GetDailyTotalsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var query = $"api/intakes/daily-totals?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            return await _httpClient.GetFromJsonAsync<Dictionary<DateTime, int>>(query, _jsonOptions) ?? new Dictionary<DateTime, int>();
        }
        catch
        {
            return new Dictionary<DateTime, int>();
        }
    }

    public async Task<List<QuickAmountDto>> GetQuickAmountsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<QuickAmountDto>>("api/intakes/quick-amounts", _jsonOptions) ?? new List<QuickAmountDto>();
    }

    // Daily Goals
    public async Task<DailyGoalDto?> GetCurrentGoalAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DailyGoalDto>("api/goals/current", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DailyGoalDto?> GetGoalForDateAsync(DateTime date)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DailyGoalDto>($"api/goals/date/{date:yyyy-MM-dd}", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<DailyGoalDto>> GetGoalHistoryAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<DailyGoalDto>>("api/goals/history", _jsonOptions) ?? new List<DailyGoalDto>();
    }

    public async Task<DailyGoalDto> CreateDailyGoalAsync(DailyGoalDto createDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/goals", createDto, _jsonOptions);
        return await HandleResponseAsync<DailyGoalDto>(response);
    }

    public async Task<DailyGoalDto> UpdateCurrentGoalAsync(DailyGoalDto updateDto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/goals/current", updateDto, _jsonOptions);
        return await HandleResponseAsync<DailyGoalDto>(response);
    }

    public async Task<bool> DeleteDailyGoalAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/goals/{id}");
        return response.IsSuccessStatusCode;
    }

    // Dashboard
    public async Task<DashboardDto> GetDashboardAsync()
    {
        return await HandleResponseAsync<DashboardDto>(await _httpClient.GetAsync("api/dashboard"));
    }

    public async Task<InsightsDto> GetInsightsAsync()
    {
        return await _httpClient.GetFromJsonAsync<InsightsDto>("api/dashboard/insights", _jsonOptions) ?? new InsightsDto();
    }

    public async Task<List<HistoryDto>> GetHistoryAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 30)
    {
        var query = $"api/dashboard/history?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&page={page}&pageSize={pageSize}";
        return await _httpClient.GetFromJsonAsync<List<HistoryDto>>(query, _jsonOptions) ?? new List<HistoryDto>();
    }

    public async Task<List<DailyProgressDto>> GetDailyProgressAsync(DateTime startDate, DateTime endDate)
    {
        var query = $"api/dashboard/daily-progress?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
        return await _httpClient.GetFromJsonAsync<List<DailyProgressDto>>(query, _jsonOptions) ?? new List<DailyProgressDto>();
    }

    // Admin
    public async Task<AdminStatsDto?> GetAdminStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AdminStatsDto>("api/admin/stats", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PagedResult<AdminUserDto>?> GetAdminUsersAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }

            var query = string.Join("&", queryParams);
            return await _httpClient.GetFromJsonAsync<PagedResult<AdminUserDto>>($"api/admin/users?{query}", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<Result<AdminUserDto>> UpdateUserAsync(int userId, AdminUpdateUserDto user)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{userId}", user);
            if (response.IsSuccessStatusCode)
            {
                var updatedUser = await response.Content.ReadFromJsonAsync<AdminUserDto>();
                return Result<AdminUserDto>.Success(updatedUser!);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return Result<AdminUserDto>.Failure($"Failed to update user: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result<AdminUserDto>.Failure($"Error updating user: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteUserAsync(int userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return Result<bool>.Success(true);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                return Result<bool>.Failure($"Failed to delete user: {response.StatusCode} - {content}");
            }
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Error deleting user: {ex.Message}");
        }
    }

    private async Task<AuthResponseDto> HandleAuthResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            try
            {
                // Parse the Result<AuthResponseDto> wrapper
                var resultWrapper = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

                if (resultWrapper.TryGetProperty("isSuccess", out var isSuccessElement) &&
                    isSuccessElement.GetBoolean() &&
                    resultWrapper.TryGetProperty("value", out var valueElement))
                {
                    // Extract the actual AuthResponseDto from the "value" property
                    var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(valueElement.GetRawText(), _jsonOptions);
                    return authResponse ?? new AuthResponseDto { Success = false, Message = "Failed to parse response" };
                }
                else
                {
                    // Handle failure case from Result wrapper
                    var errorMessage = resultWrapper.TryGetProperty("errorMessage", out var errorElement)
                        ? errorElement.GetString() ?? "Login failed"
                        : "Login failed";

                    return new AuthResponseDto { Success = false, Message = errorMessage };
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse auth response: {ex.Message}");
                return new AuthResponseDto { Success = false, Message = "Failed to parse server response" };
            }
        }
        else
        {
            return CreateAuthErrorResponse<AuthResponseDto>(response.StatusCode, content);
        }
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response) where T : new()
    {
        var content = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode, typeof(T), content) switch
        {
            // Success cases
            (true, _, null or "") => new T(),
            (true, _, var successContent) => JsonSerializer.Deserialize<T>(successContent, _jsonOptions) ?? new T(),

            // Error cases for AuthResponseDto - return error response instead of throwing
            (false, var type, var errorContent) when type == typeof(AuthResponseDto) =>
                CreateAuthErrorResponse<T>(response.StatusCode, errorContent),

            // Error cases for other types - throw exception
            (false, _, var errorContent) =>
                throw new HttpRequestException($"API request failed: {response.StatusCode} - {errorContent}")
        };
    }

    private static T CreateAuthErrorResponse<T>(System.Net.HttpStatusCode statusCode, string content)
    {
        try
        {
            // Try to parse the error response from API
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var errorMessage = errorResponse.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString() ?? GetDefaultErrorMessage(statusCode)
                : GetDefaultErrorMessage(statusCode);

            var failedAuth = new AuthResponseDto
            {
                Success = false,
                Message = errorMessage
            };

            return (T)(object)failedAuth;
        }
        catch
        {
            // If parsing fails, return generic error based on status code
            var failedAuth = new AuthResponseDto
            {
                Success = false,
                Message = GetDefaultErrorMessage(statusCode)
            };

            return (T)(object)failedAuth;
        }
    }

    private static string GetDefaultErrorMessage(System.Net.HttpStatusCode statusCode) => statusCode switch
    {
        System.Net.HttpStatusCode.Unauthorized => "Invalid email or password",
        System.Net.HttpStatusCode.BadRequest => "Invalid request. Please check your input",
        System.Net.HttpStatusCode.Conflict => "User already exists with this email",
        System.Net.HttpStatusCode.InternalServerError => "Server error. Please try again later",
        System.Net.HttpStatusCode.ServiceUnavailable => "Service temporarily unavailable",
        System.Net.HttpStatusCode.RequestTimeout => "Request timed out. Please try again",
        _ => $"Authentication failed: {statusCode}"
    };
}
