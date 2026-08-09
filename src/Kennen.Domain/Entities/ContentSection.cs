using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>
/// A repeating card block on the marketing site (services, industries, AI capabilities,
/// why-us, adoption framework). The <see cref="Key"/> is the stable identifier the
/// frontend fetches by, so it must not change once published.
/// </summary>
public class ContentSection : EntityBase
{
    public string Key { get; set; } = string.Empty;

    public string Eyebrow { get; set; } = string.Empty;

    public string Heading { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public ICollection<ContentItem> Items { get; set; } = new List<ContentItem>();
}
