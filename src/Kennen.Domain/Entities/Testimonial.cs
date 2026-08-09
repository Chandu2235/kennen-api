using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

public class Testimonial : EntityBase
{
    public string Quote { get; set; } = string.Empty;

    /// <summary>Initials badge shown next to the quote, e.g. "CT".</summary>
    public string AuthorInitials { get; set; } = string.Empty;

    /// <summary>Role of the speaker, e.g. "Chief Technology Officer".</summary>
    public string AuthorTitle { get; set; } = string.Empty;

    /// <summary>Attributed organisation, e.g. "Leading Private Sector Bank".</summary>
    public string Organisation { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
