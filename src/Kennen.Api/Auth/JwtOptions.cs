using System.ComponentModel.DataAnnotations;

namespace Kennen.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 bytes and supplied out-of-band
    /// (environment variable or user-secrets), never committed.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters for HS256.")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Short-lived by design; the refresh token carries session longevity.</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
