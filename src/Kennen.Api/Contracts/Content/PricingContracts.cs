using System.ComponentModel.DataAnnotations;
using Kennen.Domain.Entities;

namespace Kennen.Api.Contracts.Content;

public class PricingPlanResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Price { get; set; } = string.Empty;
    public string? BillingPeriod { get; set; }
    public string? Description { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public IReadOnlyList<PricingPlanFeatureResponse> Features { get; set; } = Array.Empty<PricingPlanFeatureResponse>();

    public static PricingPlanResponse From(PricingPlan plan, bool publishedOnly) => new()
    {
        Id = plan.Id,
        Slug = plan.Slug,
        Name = plan.Name,
        Subtitle = plan.Subtitle,
        Price = plan.Price,
        BillingPeriod = plan.BillingPeriod,
        Description = plan.Description,
        IsFeatured = plan.IsFeatured,
        DisplayOrder = plan.DisplayOrder,
        IsPublished = plan.IsPublished,
        Features = plan.Features
            .Where(f => !publishedOnly || f.IsPublished)
            .OrderBy(f => f.DisplayOrder)
            .Select(PricingPlanFeatureResponse.From)
            .ToList()
    };
}

public class PricingPlanFeatureResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }

    public static PricingPlanFeatureResponse From(PricingPlanFeature feature) => new()
    {
        Id = feature.Id,
        Text = feature.Text,
        Icon = feature.Icon,
        DisplayOrder = feature.DisplayOrder,
        IsPublished = feature.IsPublished
    };
}

public class UpsertPricingPlanRequest
{
    [Required]
    [MaxLength(64)]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Slug must be lowercase letters, digits and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Subtitle { get; set; }

    [Required]
    [MaxLength(64)]
    public string Price { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? BillingPeriod { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsFeatured { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}

public class UpsertPricingPlanFeatureRequest
{
    [Required]
    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
