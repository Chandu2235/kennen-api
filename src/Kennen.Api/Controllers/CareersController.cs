using Kennen.Api.Auth;
using Kennen.Api.Contracts.Careers;
using Kennen.Api.Storage;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kennen.Api.Controllers;

/// <summary>Public careers listing and application intake.</summary>
[ApiController]
[Route("api/careers")]
[AllowAnonymous]
[Produces("application/json")]
public class CareersController : ControllerBase
{
    private readonly KennenDbContext _db;
    private readonly IFileStorage _storage;
    private readonly FileStorageOptions _storageOptions;
    private readonly ILogger<CareersController> _logger;

    public CareersController(
        KennenDbContext db,
        IFileStorage storage,
        IOptions<FileStorageOptions> storageOptions,
        ILogger<CareersController> logger)
    {
        _db = db;
        _storage = storage;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    /// <summary>Lists every open role, newest first.</summary>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<JobPostingSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobPostingSummaryResponse>>> GetJobs(
        [FromQuery] string? department,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _db.JobPostings
            .AsNoTracking()
            .Where(j => j.IsPublished && (j.ClosesAtUtc == null || j.ClosesAtUtc > now));

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(j => j.Department == department);
        }

        var jobs = await query
            .OrderByDescending(j => j.PublishedAtUtc)
            .Select(j => JobPostingSummaryResponse.From(j))
            .ToListAsync(ct);

        return Ok(jobs);
    }

    [HttpGet("jobs/{slug}")]
    [ProducesResponseType(typeof(JobPostingDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobPostingDetailResponse>> GetJob(string slug, CancellationToken ct)
    {
        var job = await _db.JobPostings
            .AsNoTracking()
            .SingleOrDefaultAsync(j => j.Slug == slug && j.IsPublished, ct);

        if (job is null)
        {
            return NotFound();
        }

        return Ok(JobPostingDetailResponse.FromDetail(job));
    }

    /// <summary>Submits an application with a résumé attachment (multipart/form-data).</summary>
    [HttpPost("jobs/{slug}/apply")]
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply(
        string slug,
        [FromForm] JobApplicationRequest request,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var job = await _db.JobPostings.SingleOrDefaultAsync(j => j.Slug == slug, ct);

        if (job is null || !job.IsPublished)
        {
            return NotFound();
        }

        if (!job.IsOpenForApplications(now))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Role closed",
                Detail = "This role is no longer accepting applications.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (!ValidateResume(request.Resume, out var resumeError))
        {
            ModelState.AddModelError(nameof(request.Resume), resumeError);
            return ValidationProblem(ModelState);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.JobApplications.AnyAsync(a => a.JobPostingId == job.Id && a.Email == email, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Already applied",
                Detail = "An application for this role already exists for that email address.",
                Status = StatusCodes.Status409Conflict
            });
        }

        await using var upload = request.Resume.OpenReadStream();
        var storageKey = await _storage.SaveAsync(upload, request.Resume.FileName, request.Resume.ContentType, ct);

        var application = new JobApplication
        {
            JobPostingId = job.Id,
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            LinkedInUrl = string.IsNullOrWhiteSpace(request.LinkedInUrl) ? null : request.LinkedInUrl.Trim(),
            CoverLetter = string.IsNullOrWhiteSpace(request.CoverLetter) ? null : request.CoverLetter.Trim(),
            ResumeFileName = Path.GetFileName(request.Resume.FileName),
            ResumeStorageKey = storageKey,
            ResumeContentType = request.Resume.ContentType,
            ResumeSizeBytes = request.Resume.Length,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        try
        {
            _db.JobApplications.Add(application);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Lost a race against a concurrent duplicate submission; drop the orphaned upload.
            await _storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }

        _logger.LogInformation("Received application {ApplicationId} for job {Slug}", application.Id, slug);

        return Accepted(new
        {
            referenceId = application.Id,
            message = "Thank you for applying. Our talent team will review your application and be in touch."
        });
    }

    private bool ValidateResume(IFormFile file, out string error)
    {
        error = string.Empty;

        if (file.Length == 0)
        {
            error = "The uploaded file is empty.";
            return false;
        }

        if (file.Length > _storageOptions.MaxResumeBytes)
        {
            var maxMb = _storageOptions.MaxResumeBytes / 1024d / 1024d;
            error = $"Résumé must be {maxMb:0.#} MB or smaller.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!_storageOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            error = $"Only {string.Join(", ", _storageOptions.AllowedExtensions)} files are accepted.";
            return false;
        }

        // Extension and content type must agree, so a renamed executable is rejected.
        if (!_storageOptions.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            error = "The file type could not be verified. Please upload a PDF or Word document.";
            return false;
        }

        return true;
    }
}
