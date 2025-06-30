using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Gulp.Shared.Models;
using Gulp.Infrastructure.Models;

namespace Gulp.Infrastructure.Data;

public class GulpDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public GulpDbContext(DbContextOptions<GulpDbContext> options) : base(options)
    {
    }

    public new DbSet<User> Users { get; set; }
    public DbSet<WaterIntake> WaterIntakes { get; set; }
    public DbSet<DailyGoal> DailyGoals { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            
            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure User (business entity)
        builder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AspNetUserId).IsRequired();

            // Configure relationship with AspNetUser in code, not navigation properties
            entity.Property(e => e.AspNetUserId).IsRequired();

            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Index for performance
            entity.HasIndex(e => e.AspNetUserId).IsUnique();
        });

        // Configure WaterIntake
        builder.Entity<WaterIntake>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountMl).IsRequired();
            entity.Property(e => e.ConsumedAt).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.WaterIntakes)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Filtered indexes for performance (exclude deleted records)
            entity.HasIndex(e => new { e.UserId, e.ConsumedAt })
                  .HasFilter("IsDeleted = 0");
            entity.HasIndex(e => e.UserId)
                  .HasFilter("IsDeleted = 0");
        });

        // Configure DailyGoal
        builder.Entity<DailyGoal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetAmountMl).IsRequired();
            entity.Property(e => e.EffectiveDate).IsRequired();
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.DailyGoals)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Filtered indexes for performance (exclude deleted records)
            entity.HasIndex(e => new { e.UserId, e.EffectiveDate })
                  .HasFilter("IsDeleted = 0");
            entity.HasIndex(e => e.UserId)
                  .HasFilter("IsDeleted = 0");
        });

        // Configure AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Index for performance
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.HasIndex(e => e.Action);
        });

        // Configure RefreshToken
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
            entity.Property(e => e.JwtId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ExpiryDate).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.RevokedReason).HasMaxLength(200);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Indexes for performance
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.JwtId);
            entity.HasIndex(e => new { e.UserId, e.ExpiryDate });
            entity.HasIndex(e => e.IsRevoked);
        });

        // Seed roles
        builder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole<int> { Id = 2, Name = "User", NormalizedName = "USER" }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        // Handle ApplicationUser timestamps separately
        var userEntries = ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in userEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
