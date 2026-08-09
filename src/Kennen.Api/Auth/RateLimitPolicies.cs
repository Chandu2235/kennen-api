namespace Kennen.Api.Auth;

public static class RateLimitPolicies
{
    /// <summary>Applied to anonymous write endpoints (contact form, job applications).</summary>
    public const string PublicWrite = "public-write";

    /// <summary>Applied to login/refresh to blunt credential-stuffing attempts.</summary>
    public const string Authentication = "authentication";
}
