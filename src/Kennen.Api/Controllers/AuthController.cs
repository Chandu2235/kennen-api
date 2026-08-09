using System.Security.Claims;
using Kennen.Api.Auth;
using Kennen.Api.Contracts.Auth;
using Kennen.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kennen.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        ITokenService tokens,
        ILogger<AuthController> logger)
    {
        _signIn = signIn;
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Exchanges staff credentials for an access token and a refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(request.Email);

        // Every failure path returns the same response so the endpoint cannot be used to
        // enumerate which email addresses have accounts.
        if (user is null || !user.IsActive)
        {
            return Unauthorized(InvalidCredentials());
        }

        var result = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login blocked for locked-out account {UserId}", user.Id);
            return Unauthorized(InvalidCredentials());
        }

        if (!result.Succeeded)
        {
            return Unauthorized(InvalidCredentials());
        }

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        await _users.UpdateAsync(user);

        return Ok(await _tokens.IssueAsync(user, ClientIp(), ct));
    }

    /// <summary>Rotates a refresh token for a new access/refresh token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var response = await _tokens.RefreshAsync(request.RefreshToken, ClientIp(), ct);
        if (response is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid refresh token",
                Detail = "The refresh token is invalid, expired or has already been used.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(response);
    }

    /// <summary>Revokes the supplied refresh token, ending that session.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await _tokens.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>Returns the identity behind the current access token.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> Me()
    {
        return Ok(new CurrentUserResponse
        {
            Id = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            FullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        });
    }

    private static ProblemDetails InvalidCredentials() => new()
    {
        Title = "Invalid credentials",
        Detail = "The email address or password is incorrect.",
        Status = StatusCodes.Status401Unauthorized
    };

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
