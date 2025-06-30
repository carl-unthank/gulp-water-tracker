using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Net.Http.Json;
using Gulp.Shared.DTOs;

namespace Gulp.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    public CustomAuthenticationStateProvider(HttpClient httpClient, ILogger<CustomAuthenticationStateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Check authentication status by calling the API
            var response = await _httpClient.GetAsync("api/sessions/current");

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                if (authResponse?.Success == true && authResponse.User != null)
                {
                    var user = authResponse.User;
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.FullName),
                        new(ClaimTypes.Email, user.Email),
                        new("FirstName", user.FirstName),
                        new("LastName", user.LastName)
                    };

                    var identity = new ClaimsIdentity(claims, "cookie");
                    var principal = new ClaimsPrincipal(identity);

                    return new AuthenticationState(principal);
                }
            }

            // Not authenticated
            var anonymousIdentity = new ClaimsIdentity();
            var anonymousPrincipal = new ClaimsPrincipal(anonymousIdentity);
            return new AuthenticationState(anonymousPrincipal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            return new AuthenticationState(principal);
        }
    }

    public void NotifyUserAuthentication(string email)
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
