namespace Kennen.Infrastructure.Persistence.Seeding;

/// <summary>
/// Bootstrap admin credentials. Supply these via environment variables or user-secrets -
/// never commit a real password to appsettings.json.
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    public string AdminFullName { get; set; } = "Kennen Administrator";

    /// <summary>When true, populates the CMS tables with the current marketing site copy.</summary>
    public bool SeedContent { get; set; } = true;
}
