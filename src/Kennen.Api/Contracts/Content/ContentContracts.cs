using System.ComponentModel.DataAnnotations;
using Kennen.Domain.Entities;

namespace Kennen.Api.Contracts.Content;

public class ContentSectionResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Eyebrow { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public IReadOnlyList<ContentItemResponse> Items { get; set; } = Array.Empty<ContentItemResponse>();

    /// <summary>
    /// When <paramref name="publishedOnly"/> is true, unpublished items are stripped so the
    /// public endpoint can share this projection with the admin one safely.
    /// </summary>
    public static ContentSectionResponse From(ContentSection section, bool publishedOnly) => new()
    {
        Id = section.Id,
        Key = section.Key,
        Eyebrow = section.Eyebrow,
        Heading = section.Heading,
        Description = section.Description,
        DisplayOrder = section.DisplayOrder,
        IsPublished = section.IsPublished,
        Items = section.Items
            .Where(i => !publishedOnly || i.IsPublished)
            .OrderBy(i => i.DisplayOrder)
            .Select(ContentItemResponse.From)
            .ToList()
    };
}

public class ContentItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }

    public static ContentItemResponse From(ContentItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Summary = item.Summary,
        Icon = item.Icon,
        DisplayOrder = item.DisplayOrder,
        IsPublished = item.IsPublished
    };
}

public class UpsertContentSectionRequest
{
    [Required]
    [MaxLength(64)]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Key must be lowercase letters, digits and hyphens only.")]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Eyebrow { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Heading { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}

public class UpsertContentItemRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Summary { get; set; }

    [MaxLength(32)]
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}

public class TestimonialResponse
{
    public Guid Id { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public string AuthorTitle { get; set; } = string.Empty;
    public string Organisation { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }

    public static TestimonialResponse From(Testimonial t) => new()
    {
        Id = t.Id,
        Quote = t.Quote,
        AuthorInitials = t.AuthorInitials,
        AuthorTitle = t.AuthorTitle,
        Organisation = t.Organisation,
        DisplayOrder = t.DisplayOrder,
        IsPublished = t.IsPublished
    };
}

public class UpsertTestimonialRequest
{
    [Required]
    [MaxLength(2000)]
    public string Quote { get; set; } = string.Empty;

    [Required]
    [MaxLength(8)]
    public string AuthorInitials { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AuthorTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Organisation { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}

public class StatMetricResponse
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }

    public static StatMetricResponse From(StatMetric s) => new()
    {
        Id = s.Id,
        Value = s.Value,
        Label = s.Label,
        Description = s.Description,
        DisplayOrder = s.DisplayOrder,
        IsPublished = s.IsPublished
    };
}

public class UpsertStatMetricRequest
{
    [Required]
    [MaxLength(32)]
    public string Value { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
