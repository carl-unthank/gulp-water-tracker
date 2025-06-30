using Microsoft.AspNetCore.Mvc;

namespace Gulp.Api.Extensions;

public static class ControllerExtensions
{
    /// <summary>
    /// Sets an HTTP-only authentication cookie with the provided JWT token
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="token">The JWT token to store in the cookie</param>
    /// <param name="expiry">The expiration date for the cookie</param>
    public static void SetAuthenticationCookie(this ControllerBase controller, string? token, DateTime? expiry)
    {
        // Add this logging
        var logger = controller.HttpContext.RequestServices.GetService<ILogger<ControllerBase>>();
        logger?.LogInformation("Setting authentication cookie for token: {TokenStart}...", token?.Substring(0, 10));
        
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Required for SameSite=None
            SameSite = SameSiteMode.None, // Allow cross-site cookies
            Expires = expiry ?? DateTimeOffset.UtcNow.AddHours(1).DateTime // Set proper expiry
        };
        
        if (!string.IsNullOrEmpty(token))
        {
            controller.Response.Cookies.Append("access_token", token, cookieOptions);
        }
        logger?.LogInformation("Cookie set with options: HttpOnly={HttpOnly}, Secure={Secure}, SameSite={SameSite}", 
            cookieOptions.HttpOnly, cookieOptions.Secure, cookieOptions.SameSite);
    }

    /// <summary>
    /// Removes the authentication cookie (for logout)
    /// </summary>
    /// <param name="controller">The controller instance</param>
    public static void RemoveAuthenticationCookie(this ControllerBase controller)
    {
        controller.Response.Cookies.Delete("access_token");
    }
}
