namespace Kennen.Api.Extensions;

public class CorsSettings
{
    public const string SectionName = "Cors";

    public const string PolicyName = "KennenWebsite";

    /// <summary>
    /// Exact origins allowed to call the API from a browser. Must include the scheme and
    /// no trailing slash, e.g. "https://kennen-technologies.com".
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
