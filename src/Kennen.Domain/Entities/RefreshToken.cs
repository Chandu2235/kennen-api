using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>
/// A long-lived credential that lets an admin session obtain new access tokens.
/// Only the SHA-256 hash of the token is stored, so a database leak cannot be replayed.
/// </summary>
public class RefreshToken : EntityBase
{
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>Set when this token was rotated, pointing at its replacement, to detect replay.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;
}
