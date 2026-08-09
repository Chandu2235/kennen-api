using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>A single card inside a <see cref="ContentSection"/>.</summary>
public class ContentItem : EntityBase
{
    public Guid ContentSectionId { get; set; }

    public ContentSection? Section { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    /// <summary>Emoji or icon token rendered by the frontend, e.g. "&#127974;" or "01".</summary>
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
