using Kennen.Api.Contracts.Careers;
using Kennen.Api.Contracts.Common;
using Kennen.Api.Storage;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Identity;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/careers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
[Produces("application/json")]
public class CareersAdminController : ControllerBase
{
    private readonly KennenDbContext _db;
    private readonly IFileStorage _storage;

    public CareersAdminController(KennenDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<JobPostingSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobPostingSummaryResponse>>> GetJobs(CancellationToken ct)
    {
        var jobs = await _db.JobPostings
            .AsNoTracking()
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => JobPostingSummaryResponse.From(j))
            .ToListAsync(ct);

        return Ok(jobs);
    }

    [HttpGet("jobs/{id:guid}")]
    [ProducesResponseType(typeof(JobPostingDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobPostingDetailResponse>> GetJob(Guid id, CancellationToken ct)
    {
        var job = await _db.JobPostings.AsNoTracking().SingleOrDefaultAsync(j => j.Id == id, ct);
        return job is null ? NotFound() : Ok(JobPostingDetailResponse.FromDetail(job));
    }

    [HttpPost("jobs")]
    [ProducesResponseType(typeof(JobPostingDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingDetailResponse>> CreateJob(UpsertJobPostingRequest request, CancellationToken ct)
    {
        if (await _db.JobPostings.AnyAsync(j => j.Slug == request.Slug, ct))
        {
            return Conflict(SlugConflict(request.Slug));
        }

        var job = new JobPosting();
        Apply(request, job);

        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, JobPostingDetailResponse.FromDetail(job));
    }

    [HttpPut("jobs/{id:guid}")]
    [ProducesResponseType(typeof(JobPostingDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobPostingDetailResponse>> UpdateJob(Guid id, UpsertJobPostingRequest request, CancellationToken ct)
    {
        var job = await _db.JobPostings.SingleOrDefaultAsync(j => j.Id == id, ct);
        if (job is null)
        {
            return NotFound();
        }

        if (await _db.JobPostings.AnyAsync(j => j.Slug == request.Slug && j.Id != id, ct))
        {
            return Conflict(SlugConflict(request.Slug));
        }

        Apply(request, job);
        await _db.SaveChangesAsync(ct);

        return Ok(JobPostingDetailResponse.FromDetail(job));
    }

    /// <summary>
    /// Removes a role. Blocked once applications exist, because those records must be
    /// retained - unpublish the role instead.
    /// </summary>
    [HttpDelete("jobs/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteJob(Guid id, CancellationToken ct)
    {
        var job = await _db.JobPostings.SingleOrDefaultAsync(j => j.Id == id, ct);
        if (job is null)
        {
            return NotFound();
        }

        if (await _db.JobApplications.AnyAsync(a => a.JobPostingId == id, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Role has applications",
                Detail = "This role cannot be deleted because applications are attached to it. Unpublish it instead.",
                Status = StatusCodes.Status409Conflict
            });
        }

        _db.JobPostings.Remove(job);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("applications")]
    [ProducesResponseType(typeof(PagedResult<JobApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<JobApplicationResponse>>> GetApplications(
        [FromQuery] JobApplicationQuery query,
        CancellationToken ct)
    {
        var applications = _db.JobApplications.AsNoTracking().Include(a => a.JobPosting).AsQueryable();

        if (query.JobPostingId.HasValue)
        {
            applications = applications.Where(a => a.JobPostingId == query.JobPostingId.Value);
        }

        if (query.Status.HasValue)
        {
            applications = applications.Where(a => a.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            applications = applications.Where(a =>
                EF.Functions.ILike(a.FullName, term) || EF.Functions.ILike(a.Email, term));
        }

        var total = await applications.CountAsync(ct);
        var items = await applications
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<JobApplicationResponse>
        {
            Items = items.Select(JobApplicationResponse.From).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        });
    }

    [HttpGet("applications/{id:guid}")]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationResponse>> GetApplication(Guid id, CancellationToken ct)
    {
        var application = await _db.JobApplications
            .AsNoTracking()
            .Include(a => a.JobPosting)
            .SingleOrDefaultAsync(a => a.Id == id, ct);

        return application is null ? NotFound() : Ok(JobApplicationResponse.From(application));
    }

    [HttpPatch("applications/{id:guid}")]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationResponse>> UpdateApplication(
        Guid id,
        UpdateJobApplicationRequest request,
        CancellationToken ct)
    {
        var application = await _db.JobApplications.Include(a => a.JobPosting).SingleOrDefaultAsync(a => a.Id == id, ct);
        if (application is null)
        {
            return NotFound();
        }

        if (request.Status.HasValue)
        {
            application.Status = request.Status.Value;
        }

        if (request.InternalNotes is not null)
        {
            application.InternalNotes = request.InternalNotes;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(JobApplicationResponse.From(application));
    }

    /// <summary>Streams the stored résumé back to an authenticated reviewer.</summary>
    [HttpGet("applications/{id:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadResume(Guid id, CancellationToken ct)
    {
        var application = await _db.JobApplications.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct);
        if (application is null)
        {
            return NotFound();
        }

        var stream = await _storage.OpenAsync(application.ResumeStorageKey, ct);
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, application.ResumeContentType, application.ResumeFileName);
    }

    private static ProblemDetails SlugConflict(string slug) => new()
    {
        Title = "Duplicate slug",
        Detail = $"A job posting with slug '{slug}' already exists.",
        Status = StatusCodes.Status409Conflict
    };

    private static void Apply(UpsertJobPostingRequest request, JobPosting job)
    {
        // Stamp the publish date the first time a draft goes live, and keep it thereafter.
        if (request.IsPublished && job.PublishedAtUtc is null)
        {
            job.PublishedAtUtc = DateTimeOffset.UtcNow;
        }

        job.Slug = request.Slug;
        job.Title = request.Title;
        job.Department = request.Department;
        job.Location = request.Location;
        job.EmploymentType = request.EmploymentType;
        job.WorkArrangement = request.WorkArrangement;
        job.ExperienceLevel = request.ExperienceLevel;
        job.Description = request.Description;
        job.Responsibilities = request.Responsibilities;
        job.Requirements = request.Requirements;
        job.IsPublished = request.IsPublished;
        job.ClosesAtUtc = request.ClosesAtUtc;
    }
}
