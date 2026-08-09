using Kennen.Api.Contracts.Content;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers;

/// <summary>
/// Read-only CMS feed for the marketing site. Everything here is public and cacheable;
/// only published rows are ever returned.
/// </summary>
[ApiController]
[Route("api/content")]
[AllowAnonymous]
[Produces("application/json")]
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
public class ContentController : ControllerBase
{
    private readonly KennenDbContext _db;

    public ContentController(KennenDbContext db) => _db = db;

    /// <summary>Returns every published section with its published items, ready to render.</summary>
    [HttpGet("sections")]
    [ProducesResponseType(typeof(IReadOnlyList<ContentSectionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentSectionResponse>>> GetSections(CancellationToken ct)
    {
        var sections = await _db.ContentSections
            .AsNoTracking()
            .Where(s => s.IsPublished)
            .Include(s => s.Items)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        return Ok(sections.Select(s => ContentSectionResponse.From(s, publishedOnly: true)).ToList());
    }

    /// <summary>Returns a single section by its stable key, e.g. "services" or "industries".</summary>
    [HttpGet("sections/{key}")]
    [ProducesResponseType(typeof(ContentSectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentSectionResponse>> GetSection(string key, CancellationToken ct)
    {
        var section = await _db.ContentSections
            .AsNoTracking()
            .Include(s => s.Items)
            .SingleOrDefaultAsync(s => s.Key == key && s.IsPublished, ct);

        if (section is null)
        {
            return NotFound();
        }

        return Ok(ContentSectionResponse.From(section, publishedOnly: true));
    }

    [HttpGet("testimonials")]
    [ProducesResponseType(typeof(IReadOnlyList<TestimonialResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TestimonialResponse>>> GetTestimonials(CancellationToken ct)
    {
        var items = await _db.Testimonials
            .AsNoTracking()
            .Where(t => t.IsPublished)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => TestimonialResponse.From(t))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(IReadOnlyList<StatMetricResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StatMetricResponse>>> GetStats(CancellationToken ct)
    {
        var items = await _db.StatMetrics
            .AsNoTracking()
            .Where(s => s.IsPublished)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => StatMetricResponse.From(s))
            .ToListAsync(ct);

        return Ok(items);
    }
}
