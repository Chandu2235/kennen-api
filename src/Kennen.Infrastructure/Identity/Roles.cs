namespace Kennen.Infrastructure.Identity;

/// <summary>Role names used in <c>[Authorize(Roles = ...)]</c> policies.</summary>
public static class Roles
{
    /// <summary>Full access, including user management and destructive operations.</summary>
    public const string Admin = "Admin";

    /// <summary>Can manage site content and triage leads and applications, but not users.</summary>
    public const string Editor = "Editor";

    public static readonly string[] All = { Admin, Editor };
}
