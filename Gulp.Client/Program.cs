using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Gulp.Client;
using Gulp.Client.Services;
using Gulp.Client.Services.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for same-origin (hosted) scenario
// The base address will be the same as the hosting server (API)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Configure authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Register services
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddSingleton<WaterIntakeNotificationService>();
builder.Services.AddSingleton<ActionMenuService>();

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

await builder.Build().RunAsync();
