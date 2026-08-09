using Kennen.Domain.Common;
using Kennen.Domain.Enums;

namespace Kennen.Domain.Entities;

public class JobPosting : EntityBase
{
    /// <summary>URL-safe unique identifier used by the public careers page, e.g. "senior-dotnet-engineer".</summary>
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    public WorkArrangement WorkArrangement { get; set; } = WorkArrangement.Hybrid;

    public string? ExperienceLevel { get; set; }

    /// <summary>Markdown body describing the role.</summary>
    public string Description { get; set; } = string.Empty;

    public List<string> Responsibilities { get; set; } = new();

    public List<string> Requirements { get; set; } = new();

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    /// <summary>After this instant the role stops accepting applications.</summary>
    public DateTimeOffset? ClosesAtUtc { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

    /// <summary>A role only accepts applications while published and before its closing date.</summary>
    public bool IsOpenForApplications(DateTimeOffset now) =>
        IsPublished && (ClosesAtUtc is null || ClosesAtUtc > now);
}
