using Kennen.Api.Contracts.Auth;
using Kennen.Infrastructure.Identity;

namespace Kennen.Api.Auth;

public interface ITokenService
{
    /// <summary>Issues a fresh access token plus a new refresh token for the given user.</summary>
    Task<AuthResponse> IssueAsync(ApplicationUser user, string? ip, CancellationToken ct = default);

    /// <summary>
    /// Rotates a refresh token. Returns null when the presented token is unknown, expired
    /// or already revoked - callers must translate that into a 401 without further detail.
    /// </summary>
    Task<AuthResponse?> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default);

    /// <summary>Revokes a single refresh token. Safe to call with an unknown token.</summary>
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}
