using Kennen.Domain.Common;

namespace Kennen.Domain.Entities;

/// <summary>
/// An AI subscription or service-tier plan shown on the marketing site.
/// The <see cref="Slug"/> is the stable, URL-friendly identifier the frontend uses.
/// </summary>
public class PricingPlan : EntityBase
{
    public string Slug { get; set; } = string.Empty;

    /// <summary>Stable content grouping used by the public API, e.g. "ai" or "qa-testing".</summary>
    public string Category { get; set; } = "ai";

    public string Name { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    /// <summary>Displayed price, e.g. "$999" or "Custom".</summary>
    public string Price { get; set; } = string.Empty;

    /// <summary>Billing period, e.g. "/month" or "/year".</summary>
    public string? BillingPeriod { get; set; }

    /// <summary>Short description shown under the plan name.</summary>
    public string? Description { get; set; }

    /// <summary>Whether this is the recommended / highlighted tier.</summary>
    public bool IsFeatured { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public ICollection<PricingPlanFeature> Features { get; set; } = new List<PricingPlanFeature>();
}
