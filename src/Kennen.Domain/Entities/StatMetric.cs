using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>An outcome figure in the "Measurable impact" strip.</summary>
public class StatMetric : EntityBase
{
    /// <summary>Displayed figure including its suffix, e.g. "40%". The frontend animates the numeric part.</summary>
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
