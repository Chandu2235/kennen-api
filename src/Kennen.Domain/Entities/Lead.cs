using Kennen.Domain.Common;
using Kennen.Domain.Enums;

namespace Kennen.Domain.Entities;

/// <summary>
/// An enquiry captured from the public website (currently the contact form on the
/// marketing site). Leads are never deleted by the public API - only triaged.
/// </summary>
public class Lead : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Company { get; set; }

    /// <summary>Phone number supplied by the visitor.</summary>
    public string? Phone { get; set; }

    /// <summary>Type of enterprise engagement the visitor selected, e.g. "enterprise-consulting".</summary>
    public string? Engagement { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>Where the lead came from, e.g. "website-contact" or "consultation-cta".</summary>
    public string Source { get; set; } = "website-contact";

    public LeadStatus Status { get; set; } = LeadStatus.New;

    /// <summary>Free-text triage notes, visible only to authenticated staff.</summary>
    public string? InternalNotes { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
