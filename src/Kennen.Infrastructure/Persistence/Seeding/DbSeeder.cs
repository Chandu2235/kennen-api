using Kennen.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kennen.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent bootstrap: safe to run on every startup. Each step checks for existing data,
/// so re-running never duplicates rows or resets an admin's changed password.
/// </summary>
public class DbSeeder
{
    private readonly KennenDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly SeedOptions _options;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        KennenDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        SeedOptions options,
        ILogger<DbSeeder> logger)
    {
        _db = db;
        _users = users;
        _roles = roles;
        _options = options;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync();
        await SeedAdminAsync();

        if (_options.SeedContent)
        {
            await SeedContentAsync(ct);
        }
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await _roles.RoleExistsAsync(role))
            {
                await _roles.CreateAsync(new ApplicationRole(role));
                _logger.LogInformation("Created role {Role}", role);
            }
        }
    }

    private async Task SeedAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.AdminEmail) || string.IsNullOrWhiteSpace(_options.AdminPassword))
        {
            _logger.LogWarning(
                "Seed:AdminEmail / Seed:AdminPassword are not configured - no admin account was created. " +
                "Set them via environment variables or user-secrets to enable admin login.");
            return;
        }

        var existing = await _users.FindByEmailAsync(_options.AdminEmail);
        if (existing is not null)
        {
            // Make sure an existing account still holds the Admin role, but leave its password alone.
            if (!await _users.IsInRoleAsync(existing, Roles.Admin))
            {
                await _users.AddToRoleAsync(existing, Roles.Admin);
            }
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _options.AdminEmail,
            Email = _options.AdminEmail,
            EmailConfirmed = true,
            FullName = _options.AdminFullName
        };

        var result = await _users.CreateAsync(admin, _options.AdminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create the seed admin account: {errors}");
        }

        await _users.AddToRoleAsync(admin, Roles.Admin);
        _logger.LogInformation("Created seed admin account {Email}", _options.AdminEmail);
    }

    private async Task SeedContentAsync(CancellationToken ct)
    {
        var existingKeys = await _db.ContentSections.Select(s => s.Key).ToListAsync(ct);
        var missing = ContentSeedData.Sections().Where(s => !existingKeys.Contains(s.Key)).ToList();
        if (missing.Count > 0)
        {
            _db.ContentSections.AddRange(missing);
            _logger.LogInformation("Seeding {Count} content section(s)", missing.Count);
        }

        if (!await _db.StatMetrics.AnyAsync(ct))
        {
            _db.StatMetrics.AddRange(ContentSeedData.Stats());
        }

        if (!await _db.Testimonials.AnyAsync(ct))
        {
            _db.Testimonials.AddRange(ContentSeedData.Testimonials());
        }

        await _db.SaveChangesAsync(ct);
    }
}
