using System.ComponentModel.DataAnnotations;
using Kennen.Domain.Entities;

namespace Kennen.Api.Contracts.Content;

public class PricingPlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Price { get; set; } = string.Empty;
    public string? Period { get; set; }
    public IReadOnlyList<string> Features { get; set; } = Array.Empty<string>();
    public bool IsPopular { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }

    public static PricingPlanResponse From(PricingPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Subtitle = plan.Subtitle,
        Price = plan.Price,
        Period = plan.Period,
        Features = plan.Features.OrderBy(f => f.DisplayOrder).Select(f => f.Text).ToList(),
        IsPopular = plan.IsPopular,
        DisplayOrder = plan.DisplayOrder,
        IsPublished = plan.IsPublished
    };
}

public class UpsertPricingPlanRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Subtitle { get; set; }

    [Required]
    [MaxLength(32)]
    public string Price { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? Period { get; set; }

    public IReadOnlyList<string> Features { get; set; } = Array.Empty<string>();

    public int DisplayOrder { get; set; }

    public bool IsPopular { get; set; }

    public bool IsPublished { get; set; } = true;
}
