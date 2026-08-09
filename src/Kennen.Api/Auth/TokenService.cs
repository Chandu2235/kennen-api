using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kennen.Api.Contracts.Auth;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Identity;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Kennen.Api.Auth;

public class TokenService : ITokenService
{
    private readonly KennenDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtOptions _options;

    public TokenService(KennenDbContext db, UserManager<ApplicationUser> users, IOptions<JwtOptions> options)
    {
        _db = db;
        _users = users;
        _options = options.Value;
    }

    public async Task<AuthResponse> IssueAsync(ApplicationUser user, string? ip, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var roles = await _users.GetRolesAsync(user);

        var (refreshToken, entity) = CreateRefreshToken(user.Id, ip, now);
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        return BuildResponse(user, roles, refreshToken, entity.ExpiresAtUtc, now);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = Hash(refreshToken);

        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null || !existing.IsActive(now))
        {
            return null;
        }

        var user = await _users.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        // Rotate: the presented token dies with this request and points at its successor,
        // so a stolen copy being replayed later is detectable and useless.
        var (newToken, entity) = CreateRefreshToken(user.Id, ip, now);
        existing.RevokedAtUtc = now;
        existing.ReplacedByTokenId = entity.Id;
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        var roles = await _users.GetRolesAsync(user);
        return BuildResponse(user, roles, newToken, entity.ExpiresAtUtc, now);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null || existing.RevokedAtUtc is not null)
        {
            return;
        }

        existing.RevokedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private AuthResponse BuildResponse(
        ApplicationUser user,
        IEnumerable<string> roles,
        string refreshToken,
        DateTimeOffset refreshExpiry,
        DateTimeOffset now)
    {
        var accessExpiry = now.AddMinutes(_options.AccessTokenMinutes);
        var roleList = roles.ToList();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName)
        };
        claims.AddRange(roleList.Select(r => new Claim(ClaimTypes.Role, r)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpiry.UtcDateTime,
            signingCredentials: credentials);

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
            AccessTokenExpiresAtUtc = accessExpiry,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiry,
            User = new CurrentUserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roleList
            }
        };
    }

    private (string Token, RefreshToken Entity) CreateRefreshToken(Guid userId, string? ip, DateTimeOffset now)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(token),
            ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays),
            CreatedByIp = ip
        };
        return (token, entity);
    }

    /// <summary>Refresh tokens are high-entropy random values, so a plain SHA-256 lookup hash is sufficient.</summary>
    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

/// <summary>
/// The registered-claim names we emit. Avoids taking a dependency on the constants in
/// JwtRegisteredClaimNames shifting between IdentityModel versions.
/// </summary>
internal static class JwtRegisteredClaimNames
{
    public const string Sub = "sub";
    public const string Jti = "jti";
}
