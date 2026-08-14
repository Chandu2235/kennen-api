using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>A single line item in a <see cref="PricingPlan"/> feature list.</summary>
public class PricingPlanFeature : EntityBase
{
    public Guid PricingPlanId { get; set; }

    public PricingPlan? Plan { get; set; }

    public string Text { get; set; } = string.Empty;

    /// <summary>Emoji or icon token, e.g. ✓ or —.</summary>
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
