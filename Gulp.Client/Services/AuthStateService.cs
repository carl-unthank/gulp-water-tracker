using Gulp.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Gulp.Client.Services;

public class AuthStateService : IAuthStateService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AuthStateService> _logger;

    public event Action<bool>? AuthStateChanged;

    public bool IsAuthenticated { get; private set; }
    public UserDto? CurrentUser { get; private set; }

    public AuthStateService(IApiClient apiClient, ILogger<AuthStateService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public string? LastErrorMessage { get; private set; }

    public async Task<bool> LoginAsync(LoginDto loginDto)
    {
        try
        {
            LastErrorMessage = null;
            var response = await _apiClient.LoginAsync(loginDto);

            return response switch
            {
                { Success: true, User: not null } => HandleSuccessfulLogin(response),
                { Success: false } => HandleFailedLogin(response),
                _ => HandleFailedLogin(response)
            };
        }
        catch (Exception ex)
        {
            var (errorMessage, logMessage) = ex switch
            {
                HttpRequestException httpEx when httpEx.Message.Contains("timeout") =>
                    ("Request timed out. Please try again.", "Login request timed out"),
                HttpRequestException httpEx when httpEx.Message.Contains("network") =>
                    ("Network error. Please check your connection.", "Network error during login"),
                HttpRequestException =>
                    ("Unable to connect to server. Please try again.", "HTTP error during login"),
                TaskCanceledException =>
                    ("Request was cancelled. Please try again.", "Login request was cancelled"),
                JsonException =>
                    ("Invalid response from server. Please try again.", "JSON parsing error during login"),
                _ =>
                    ("An unexpected error occurred. Please try again.", "Unexpected error during login")
            };

            LastErrorMessage = errorMessage;
            _logger.LogError(ex, logMessage);
            return false;
        }
    }

    private bool HandleSuccessfulLogin(AuthResponseDto response)
    {
        CurrentUser = response.User;
        IsAuthenticated = true;
        AuthStateChanged?.Invoke(true);
        _logger.LogInformation("User logged in successfully: {Email}", response.User!.Email);
        return true;
    }

    private bool HandleFailedLogin(AuthResponseDto response)
    {
        LastErrorMessage = response.Message ?? "Login failed";
        _logger.LogWarning("Login failed: {Message}", response.Message);
        return false;
    }

    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            LastErrorMessage = null;
            var response = await _apiClient.RegisterAsync(registerDto);

            return response switch
            {
                { Success: true, User: not null } => HandleSuccessfulRegistration(response),
                { Success: false } => HandleFailedRegistration(response),
                _ => HandleFailedRegistration(response)
            };
        }
        catch (Exception ex)
        {
            var (errorMessage, logMessage) = ex switch
            {
                HttpRequestException httpEx when httpEx.Message.Contains("timeout") =>
                    ("Request timed out. Please try again.", "Registration request timed out"),
                HttpRequestException httpEx when httpEx.Message.Contains("network") =>
                    ("Network error. Please check your connection.", "Network error during registration"),
                HttpRequestException =>
                    ("Unable to connect to server. Please try again.", "HTTP error during registration"),
                TaskCanceledException =>
                    ("Request was cancelled. Please try again.", "Registration request was cancelled"),
                JsonException =>
                    ("Invalid response from server. Please try again.", "JSON parsing error during registration"),
                _ =>
                    ("An unexpected error occurred. Please try again.", "Unexpected error during registration")
            };

            LastErrorMessage = errorMessage;
            _logger.LogError(ex, logMessage);
            return false;
        }
    }

    private bool HandleSuccessfulRegistration(AuthResponseDto response)
    {
        CurrentUser = response.User;
        IsAuthenticated = true;
        AuthStateChanged?.Invoke(true);
        _logger.LogInformation("User registered successfully: {Email}", response.User!.Email);
        return true;
    }

    private bool HandleFailedRegistration(AuthResponseDto response)
    {
        LastErrorMessage = response.Message ?? "Registration failed";
        _logger.LogWarning("Registration failed: {Message}", response.Message);
        return false;
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiClient.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
        finally
        {
            CurrentUser = null;
            IsAuthenticated = false;
            AuthStateChanged?.Invoke(false);
            _logger.LogInformation("User logged out");
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var response = await _apiClient.RefreshTokenAsync();
            if (response.Success && response.User != null)
            {
                CurrentUser = response.User;
                IsAuthenticated = true;
                AuthStateChanged?.Invoke(true);
                return true;
            }

            await LogoutAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            await LogoutAsync();
            return false;
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var user = await _apiClient.GetCurrentUserAsync();
            if (user != null)
            {
                CurrentUser = user;
                IsAuthenticated = true;
                AuthStateChanged?.Invoke(true);
                _logger.LogInformation("Authentication state initialized for user: {Email}", user.Email);
            }
            else
            {
                IsAuthenticated = false;
                AuthStateChanged?.Invoke(false);
                _logger.LogInformation("No authenticated user found");
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
        {
            // 401 is expected when no valid token exists - not an error
            IsAuthenticated = false;
            AuthStateChanged?.Invoke(false);
            _logger.LogInformation("No authenticated user found (401 response)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing authentication state");
            IsAuthenticated = false;
            AuthStateChanged?.Invoke(false);
        }
    }
}
