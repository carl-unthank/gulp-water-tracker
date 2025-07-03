using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.Interfaces;
using Gulp.Shared.Models;

namespace Gulp.Infrastructure.Services;

public class DataSeedService : IDataSeedService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<DailyGoal> _dailyGoalRepository;
    private readonly IRepository<WaterIntake> _waterIntakeRepository;
    private readonly ILogger<DataSeedService> _logger;
    private readonly Random _random = new();

    public DataSeedService(
        UserManager<ApplicationUser> userManager,
        IRepository<User> userRepository,
        IRepository<DailyGoal> dailyGoalRepository,
        IRepository<WaterIntake> waterIntakeRepository,
        ILogger<DataSeedService> logger)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _dailyGoalRepository = dailyGoalRepository;
        _waterIntakeRepository = waterIntakeRepository;
        _logger = logger;
    }

    public async Task SeedTestDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting test data seeding...");

            // Seed admin user first
            await SeedAdminUserAsync();

            // Create test users
            var testUsers = new[]
            {
                new { Email = "john.doe@example.com", FirstName = "John", LastName = "Doe" },
                new { Email = "jane.smith@example.com", FirstName = "Jane", LastName = "Smith" },
                new { Email = "mike.johnson@example.com", FirstName = "Mike", LastName = "Johnson" },
                new { Email = "sarah.wilson@example.com", FirstName = "Sarah", LastName = "Wilson" },
                new { Email = "david.brown@example.com", FirstName = "David", LastName = "Brown" }
            };

            foreach (var userData in testUsers)
            {
                var existingUser = await _userManager.FindByEmailAsync(userData.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        FirstName = userData.FirstName,
                        LastName = userData.LastName,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, "TestUser123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");

                        // Create corresponding User record in custom User table
                        var customUser = new User
                        {
                            AspNetUserId = user.Id
                        };
                        await _userRepository.AddAsync(customUser);

                        // Seed data using the custom User ID
                        await SeedUserWithDataAsync(customUser.Id, 60); // 60 days of history
                        _logger.LogInformation("Created test user: {Email} with custom User ID: {CustomUserId}", userData.Email, customUser.Id);
                    }
                    else
                    {
                        _logger.LogError("Failed to create test user {Email}: {Errors}",
                            userData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    // User already exists, check if they have a custom User record
                    var existingCustomUsers = await _userRepository.FindAsync(u => u.AspNetUserId == existingUser.Id);
                    var existingCustomUser = existingCustomUsers.FirstOrDefault();

                    if (existingCustomUser == null)
                    {
                        // Create missing custom User record
                        var customUser = new User
                        {
                            AspNetUserId = existingUser.Id
                        };
                        await _userRepository.AddAsync(customUser);
                        _logger.LogInformation("Created missing custom User record for existing user: {Email} with custom User ID: {CustomUserId}", userData.Email, customUser.Id);
                    }
                    else
                    {
                        _logger.LogInformation("User {Email} already exists with custom User ID: {CustomUserId}", userData.Email, existingCustomUser.Id);
                    }
                }
            }

            _logger.LogInformation("Test data seeding completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test data seeding");
            throw;
        }
    }

    private async Task SeedAdminUserAsync()
    {
        try
        {
            const string adminEmail = "admin@gulp.com";
            const string adminPassword = "Admin123!";

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");

                    // Create corresponding User record in custom User table
                    var customUser = new User
                    {
                        AspNetUserId = adminUser.Id
                    };
                    await _userRepository.AddAsync(customUser);

                    _logger.LogInformation("Created admin user: {Email} with custom User ID: {CustomUserId}", adminEmail, customUser.Id);
                }
                else
                {
                    _logger.LogError("Failed to create admin user {Email}: {Errors}",
                        adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Ensure existing admin has the Admin role
                if (!await _userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    await _userManager.AddToRoleAsync(existingAdmin, "Admin");
                    _logger.LogInformation("Added Admin role to existing user: {Email}", adminEmail);
                }

                // Ensure custom User record exists
                var existingCustomUsers = await _userRepository.FindAsync(u => u.AspNetUserId == existingAdmin.Id);
                var existingCustomUser = existingCustomUsers.FirstOrDefault();

                if (existingCustomUser == null)
                {
                    var customUser = new User
                    {
                        AspNetUserId = existingAdmin.Id
                    };
                    await _userRepository.AddAsync(customUser);
                    _logger.LogInformation("Created missing custom User record for admin: {Email} with custom User ID: {CustomUserId}", adminEmail, customUser.Id);
                }
                else
                {
                    _logger.LogInformation("Admin user {Email} already exists with custom User ID: {CustomUserId}", adminEmail, existingCustomUser.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding admin user");
            throw;
        }
    }

    public async Task SeedUserWithDataAsync(int userId, int daysOfHistory = 30)
    {
        try
        {
            var startDate = DateTime.Today.AddDays(-daysOfHistory);
            var endDate = DateTime.Today;

            // Create varied daily goals
            await CreateDailyGoalsAsync(userId, startDate, endDate);

            // Create water intake data
            await CreateWaterIntakesAsync(userId, startDate, endDate);

            // Create some audit logs
            await CreateAuditLogsAsync(userId, startDate, endDate);

            // TODO: Implement save changes with new repository pattern
            // await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Seeded {Days} days of data for user {UserId}", daysOfHistory, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding data for user {UserId}", userId);
            throw;
        }
    }

    private async Task CreateDailyGoalsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var goals = new List<DailyGoal>();
        var currentGoal = 2000; // Start with 2L

        // Create goal changes at random intervals
        var goalChangeDate = startDate;
        while (goalChangeDate <= endDate)
        {
            goals.Add(new DailyGoal
            {
                UserId = userId,
                TargetAmountMl = currentGoal,
                EffectiveDate = goalChangeDate,
                CreatedAt = goalChangeDate.AddHours(_random.Next(8, 20))
            });

            // Randomly change goal every 7-21 days
            var daysUntilNextChange = _random.Next(7, 22);
            goalChangeDate = goalChangeDate.AddDays(daysUntilNextChange);

            // Vary the goal between 1.5L and 3L
            var goalOptions = new[] { 1500, 2000, 2500, 3000 };
            currentGoal = goalOptions[_random.Next(goalOptions.Length)];
        }

        foreach (var goal in goals)
        {
            await _dailyGoalRepository.AddAsync(goal);
        }
    }

    private async Task CreateWaterIntakesAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var intakes = new List<WaterIntake>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Use a default goal for seeding (we could make this more sophisticated later)
            var targetAmount = 2000; // Default 2L goal for seeding

            // Simulate different patterns
            var dayOfWeek = date.DayOfWeek;
            var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
            var isToday = date.Date == DateTime.Today;

            // Skip some days randomly (sick days, busy days, etc.)
            if (_random.NextDouble() < 0.1) continue; // 10% chance of no intake

            // Determine how much water to drink (80-120% of goal, with some variation)
            var completionRate = isWeekend ? 
                _random.NextDouble() * 0.6 + 0.7 : // Weekend: 70-130%
                _random.NextDouble() * 0.8 + 0.6;   // Weekday: 60-140%

            var totalAmount = (int)(targetAmount * completionRate);

            // Don't generate future data for today
            if (isToday)
            {
                var currentHour = DateTime.Now.Hour;
                var progressSoFar = Math.Min(currentHour / 20.0, 1.0); // Assume drinking until 8 PM
                totalAmount = (int)(totalAmount * progressSoFar);
            }

            // Generate individual intake entries throughout the day
            var dailyIntakes = GenerateDailyIntakes(userId, date, totalAmount, isToday);
            intakes.AddRange(dailyIntakes);
        }

        foreach (var intake in intakes)
        {
            await _waterIntakeRepository.AddAsync(intake);
        }
    }

    private List<WaterIntake> GenerateDailyIntakes(int userId, DateTime date, int totalAmount, bool isToday)
    {
        var intakes = new List<WaterIntake>();
        var remainingAmount = totalAmount;
        var currentTime = date.AddHours(7); // Start at 7 AM
        var endTime = isToday ? DateTime.Now : date.AddHours(22); // End at 10 PM or now if today

        // Common intake amounts with their probabilities
        var intakeOptions = new[]
        {
            new { Amount = 250, Weight = 30 },  // Cup
            new { Amount = 330, Weight = 20 },  // Can
            new { Amount = 500, Weight = 25 },  // Bottle
            new { Amount = 750, Weight = 15 },  // Large bottle
            new { Amount = 1000, Weight = 10 }  // Liter
        };

        var notes = new[]
        {
            "Morning coffee", "After workout", "With lunch", "Afternoon break",
            "Post-meeting", "Before dinner", "Evening water", null, null, null
        };

        while (remainingAmount > 0 && currentTime < endTime)
        {
            // Choose intake amount
            var availableOptions = intakeOptions.Where(o => o.Amount <= remainingAmount + 100).ToArray();
            if (!availableOptions.Any()) break;

            var totalWeight = availableOptions.Sum(o => o.Weight);
            var randomValue = _random.Next(totalWeight);
            var cumulativeWeight = 0;
            var selectedAmount = 250;

            foreach (var option in availableOptions)
            {
                cumulativeWeight += option.Weight;
                if (randomValue < cumulativeWeight)
                {
                    selectedAmount = Math.Min(option.Amount, remainingAmount);
                    break;
                }
            }

            intakes.Add(new WaterIntake
            {
                UserId = userId,
                AmountMl = selectedAmount,
                ConsumedAt = currentTime,
                Notes = _random.NextDouble() < 0.3 ? notes[_random.Next(notes.Length)] : null,
                CreatedAt = currentTime.AddMinutes(_random.Next(0, 30))
            });

            remainingAmount -= selectedAmount;

            // Add random interval between intakes (30 minutes to 3 hours)
            currentTime = currentTime.AddMinutes(_random.Next(30, 180));
        }

        return intakes;
    }

    private async Task CreateAuditLogsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var actions = new[]
        {
            "USER_LOGIN", "USER_LOGOUT", "WATER_INTAKE_CREATED", 
            "WATER_INTAKE_UPDATED", "DAILY_GOAL_CREATED"
        };

        var logs = new List<AuditLog>();

        // Generate some audit logs
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Skip some days
            if (_random.NextDouble() < 0.3) continue;

            var logCount = _random.Next(1, 6); // 1-5 logs per day
            for (var i = 0; i < logCount; i++)
            {
                var action = actions[_random.Next(actions.Length)];
                var timestamp = date.AddHours(_random.Next(6, 23)).AddMinutes(_random.Next(0, 60));

                logs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = GetActionDetails(action),
                    Timestamp = timestamp,
                    IpAddress = GenerateRandomIp(),
                    UserAgent = GetRandomUserAgent(),
                    CreatedAt = timestamp
                });
            }
        }

        // TODO: Implement audit logs with new repository pattern
        // foreach (var log in logs)
        // {
        //     await _unitOfWork.AuditLogs.AddAsync(log);
        // }
    }

    private string GetActionDetails(string action)
    {
        return action switch
        {
            "WATER_INTAKE_CREATED" => $"Added {_random.Next(200, 1000)}ml water intake",
            "WATER_INTAKE_UPDATED" => "Modified water intake amount",
            "DAILY_GOAL_CREATED" => $"Set daily goal to {_random.Next(1500, 3000)}ml",
            "USER_LOGIN" => "User logged in successfully",
            "USER_LOGOUT" => "User logged out",
            _ => "System action performed"
        };
    }

    private string GenerateRandomIp()
    {
        return $"{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}";
    }

    private string GetRandomUserAgent()
    {
        var userAgents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1",
            "Mozilla/5.0 (Android 11; Mobile; rv:89.0) Gecko/89.0 Firefox/89.0"
        };

        return userAgents[_random.Next(userAgents.Length)];
    }
}
