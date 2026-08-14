using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>AI subscription pricing plan displayed on the marketing site.</summary>
public class PricingPlan : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    /// <summary>E.g. "$499" or "Custom".</summary>
    public string Price { get; set; } = string.Empty;

    /// <summary>E.g. "/month" or "/project".</summary>
    public string? Period { get; set; }

    /// <summary>Bullet list of features, stored one per line.</summary>
    public ICollection<PricingPlanFeature> Features { get; set; } = new List<PricingPlanFeature>();

    public int DisplayOrder { get; set; }

    public bool IsPopular { get; set; }

    public bool IsPublished { get; set; } = true;
}
