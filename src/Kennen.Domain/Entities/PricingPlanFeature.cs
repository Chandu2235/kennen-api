using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>A single feature line inside a <see cref="PricingPlan"/>.</summary>
public class PricingPlanFeature : EntityBase
{
    public Guid PricingPlanId { get; set; }

    public PricingPlan? Plan { get; set; }

    public string Text { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
