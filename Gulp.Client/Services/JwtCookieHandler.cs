using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace Gulp.Client.Services;

public class JwtCookieHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;

    public JwtCookieHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the JWT token from the access_token cookie
            var token = await GetJwtFromCookie();
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception)
        {
            // If we can't get the token, continue without it
            // This prevents the handler from breaking API calls
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetJwtFromCookie()
    {
        try
        {
            // Get all cookies as a string
            var cookies = await _jsRuntime.InvokeAsync<string>("eval", "document.cookie");
            
            if (string.IsNullOrEmpty(cookies))
                return null;

            // Parse cookies to find access_token
            var cookiePairs = cookies.Split(';');
            foreach (var cookie in cookiePairs)
            {
                var parts = cookie.Trim().Split('=', 2);
                if (parts.Length == 2 && parts[0] == "access_token")
                {
                    return parts[1];
                }
            }
        }
        catch (Exception)
        {
            // If JavaScript execution fails, return null
        }

        return null;
    }
}
