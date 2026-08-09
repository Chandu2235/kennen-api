using Microsoft.AspNetCore.Identity;

namespace Kennen.Infrastructure.Identity;

/// <summary>
/// A staff account. There is deliberately no public self-registration endpoint - accounts
/// are provisioned by an administrator or by the startup seeder.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
