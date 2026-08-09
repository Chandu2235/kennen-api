using System.ComponentModel.DataAnnotations;
using Kennen.Api.Contracts.Common;
using Kennen.Domain.Entities;
using Kennen.Domain.Enums;

namespace Kennen.Api.Contracts.Careers;

public class JobPostingSummaryResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public WorkArrangement WorkArrangement { get; set; }
    public string? ExperienceLevel { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset? ClosesAtUtc { get; set; }

    public static JobPostingSummaryResponse From(JobPosting job) => new()
    {
        Id = job.Id,
        Slug = job.Slug,
        Title = job.Title,
        Department = job.Department,
        Location = job.Location,
        EmploymentType = job.EmploymentType,
        WorkArrangement = job.WorkArrangement,
        ExperienceLevel = job.ExperienceLevel,
        IsPublished = job.IsPublished,
        PublishedAtUtc = job.PublishedAtUtc,
        ClosesAtUtc = job.ClosesAtUtc
    };
}

public class JobPostingDetailResponse : JobPostingSummaryResponse
{
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> Responsibilities { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Requirements { get; set; } = Array.Empty<string>();
    public bool IsOpenForApplications { get; set; }

    public static JobPostingDetailResponse FromDetail(JobPosting job) => new()
    {
        Id = job.Id,
        Slug = job.Slug,
        Title = job.Title,
        Department = job.Department,
        Location = job.Location,
        EmploymentType = job.EmploymentType,
        WorkArrangement = job.WorkArrangement,
        ExperienceLevel = job.ExperienceLevel,
        IsPublished = job.IsPublished,
        PublishedAtUtc = job.PublishedAtUtc,
        ClosesAtUtc = job.ClosesAtUtc,
        Description = job.Description,
        Responsibilities = job.Responsibilities,
        Requirements = job.Requirements,
        IsOpenForApplications = job.IsOpenForApplications(DateTimeOffset.UtcNow)
    };
}

public class UpsertJobPostingRequest
{
    [Required]
    [MaxLength(160)]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Slug must be lowercase letters, digits and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string Location { get; set; } = string.Empty;

    [EnumDataType(typeof(EmploymentType))]
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    [EnumDataType(typeof(WorkArrangement))]
    public WorkArrangement WorkArrangement { get; set; } = WorkArrangement.Hybrid;

    [MaxLength(80)]
    public string? ExperienceLevel { get; set; }

    [Required]
    [MaxLength(20000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(30)]
    public List<string> Responsibilities { get; set; } = new();

    [MaxLength(30)]
    public List<string> Requirements { get; set; } = new();

    public bool IsPublished { get; set; }

    public DateTimeOffset? ClosesAtUtc { get; set; }
}

/// <summary>
/// Bound from multipart/form-data because it carries the résumé file alongside the fields.
/// </summary>
public class JobApplicationRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(40)]
    public string? Phone { get; set; }

    [Url]
    [MaxLength(400)]
    public string? LinkedInUrl { get; set; }

    [MaxLength(10000)]
    public string? CoverLetter { get; set; }

    [Required(ErrorMessage = "Please attach your résumé.")]
    public IFormFile Resume { get; set; } = default!;
}

public class JobApplicationResponse
{
    public Guid Id { get; set; }
    public Guid JobPostingId { get; set; }
    public string? JobTitle { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? CoverLetter { get; set; }
    public string ResumeFileName { get; set; } = string.Empty;
    public long ResumeSizeBytes { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? InternalNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public static JobApplicationResponse From(JobApplication a) => new()
    {
        Id = a.Id,
        JobPostingId = a.JobPostingId,
        JobTitle = a.JobPosting?.Title,
        FullName = a.FullName,
        Email = a.Email,
        Phone = a.Phone,
        LinkedInUrl = a.LinkedInUrl,
        CoverLetter = a.CoverLetter,
        ResumeFileName = a.ResumeFileName,
        ResumeSizeBytes = a.ResumeSizeBytes,
        Status = a.Status,
        InternalNotes = a.InternalNotes,
        CreatedAtUtc = a.CreatedAtUtc
    };
}

public class JobApplicationQuery : PagedQuery
{
    public Guid? JobPostingId { get; set; }

    public ApplicationStatus? Status { get; set; }

    [MaxLength(200)]
    public string? Search { get; set; }
}

public class UpdateJobApplicationRequest
{
    public ApplicationStatus? Status { get; set; }

    [MaxLength(4000)]
    public string? InternalNotes { get; set; }
}
