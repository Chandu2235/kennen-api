using System.ComponentModel.DataAnnotations;
using Kennen.Api.Contracts.Common;
using Kennen.Domain.Entities;
using Kennen.Domain.Enums;

namespace Kennen.Api.Contracts.Leads;

/// <summary>Payload posted by the public contact form on the marketing site.</summary>
public class ContactRequest
{
    [Required(ErrorMessage = "Please enter your name.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Company { get; set; }

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [MaxLength(40)]
    public string? Phone { get; set; }

    [MaxLength(64)]
    public string? Engagement { get; set; }

    [Required(ErrorMessage = "Please enter your message.")]
    [MinLength(10, ErrorMessage = "Please provide a little more detail.")]
    [MaxLength(5000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? Source { get; set; }
}

/// <summary>
/// Deliberately minimal: the public endpoint confirms receipt and nothing else, so it
/// cannot be used to probe what the backend stored.
/// </summary>
public class ContactResponse
{
    public Guid ReferenceId { get; set; }

    public string Message { get; set; } = string.Empty;
}

public class LeadResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? Engagement { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public LeadStatus Status { get; set; }
    public string? InternalNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static LeadResponse From(Lead lead) => new()
    {
        Id = lead.Id,
        Name = lead.Name,
        Email = lead.Email,
        Company = lead.Company,
        Phone = lead.Phone,
        Engagement = lead.Engagement,
        Message = lead.Message,
        Source = lead.Source,
        Status = lead.Status,
        InternalNotes = lead.InternalNotes,
        CreatedAtUtc = lead.CreatedAtUtc,
        UpdatedAtUtc = lead.UpdatedAtUtc
    };
}

public class LeadQuery : PagedQuery
{
    public LeadStatus? Status { get; set; }

    /// <summary>Case-insensitive match against name, email or company.</summary>
    [MaxLength(200)]
    public string? Search { get; set; }
}

public class UpdateLeadRequest
{
    public LeadStatus? Status { get; set; }

    [MaxLength(4000)]
    public string? InternalNotes { get; set; }
}
