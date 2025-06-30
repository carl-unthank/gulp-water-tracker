using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Services;
using Gulp.Infrastructure.Repositories;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.Interfaces;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/gulp-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework
builder.Services.AddDbContext<GulpDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with HTTP-only cookies
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // For development
})
.AddEntityFrameworkStores<GulpDbContext>()
.AddDefaultTokenProviders();

// Configure Cookie Authentication for same-origin (hosted) scenario
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true; // Crucial for XSS protection
    options.ExpireTimeSpan = TimeSpan.FromHours(3); // Session duration
    options.SlidingExpiration = true; // Refresh on activity

    // For same-origin (hosted) scenarios - use default SameSite=Lax (secure and works perfectly)
    options.Cookie.SameSite = SameSiteMode.Lax; // Default and secure for same-origin
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS in prod, HTTP in dev
    options.Cookie.Name = "GulpAuth"; // Custom name

    // Prevent redirects for API calls - return 401/403 instead
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// No CORS needed for hosted scenario - same origin

// Register Infrastructure services

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWaterIntakeService, WaterIntakeService>();
builder.Services.AddScoped<IDailyGoalService, DailyGoalService>();

// Register Generic Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped(typeof(IReadOnlyRepository<>), typeof(BaseRepository<>));

// Register Data Seeding Service
builder.Services.AddScoped<DataSeedService>();

// Configure Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gulp API",
        Version = "v1",
        Description = "Water intake tracking API with JWT authentication via HTTP-only cookies"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gulp API v1");
        c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();

// Serve Blazor WASM static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers for API endpoints
app.MapControllers();

// Fallback to serve Blazor WASM app for client-side routing
// This will serve index.html for any route that doesn't match API or Swagger
app.MapFallbackToFile("index.html");

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GulpDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Log.Information("Database connection verified successfully");

        // Seed test data in development
        if (app.Environment.IsDevelopment())
        {
            var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
            await seedService.SeedTestDataAsync();
            Log.Information("Test data seeding completed");
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to connect to database during startup");
        throw;
    }
}

Log.Information("Gulp API starting up...");

app.Run();
