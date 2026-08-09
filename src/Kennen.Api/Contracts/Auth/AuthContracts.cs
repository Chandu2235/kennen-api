using System.ComponentModel.DataAnnotations;

namespace Kennen.Api.Contracts.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }

    public CurrentUserResponse User { get; set; } = new();
}

public class CurrentUserResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
