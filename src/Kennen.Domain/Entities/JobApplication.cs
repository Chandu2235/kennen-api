using Kennen.Domain.Common;
using Kennen.Domain.Enums;

namespace Kennen.Domain.Entities;

public class JobApplication : EntityBase
{
    public Guid JobPostingId { get; set; }

    public JobPosting? JobPosting { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? CoverLetter { get; set; }

    /// <summary>Original file name as uploaded, retained for display only - never used as a path.</summary>
    public string ResumeFileName { get; set; } = string.Empty;

    /// <summary>Opaque storage key resolved by the file storage provider.</summary>
    public string ResumeStorageKey { get; set; } = string.Empty;

    public string ResumeContentType { get; set; } = string.Empty;

    public long ResumeSizeBytes { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Received;

    public string? InternalNotes { get; set; }

    public string? IpAddress { get; set; }
}
