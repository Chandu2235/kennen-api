using Kennen.Api.Contracts.Content;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Identity;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers.Admin;

/// <summary>
/// Authenticated CMS management. Unlike the public feed these endpoints return
/// unpublished rows too, so drafts can be edited before going live.
/// </summary>
[ApiController]
[Route("api/admin/content")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
[Produces("application/json")]
public class ContentAdminController : ControllerBase
{
    private readonly KennenDbContext _db;

    public ContentAdminController(KennenDbContext db) => _db = db;

    [HttpGet("sections")]
    [ProducesResponseType(typeof(IReadOnlyList<ContentSectionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentSectionResponse>>> GetSections(CancellationToken ct)
    {
        var sections = await _db.ContentSections
            .AsNoTracking()
            .Include(s => s.Items)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        return Ok(sections.Select(s => ContentSectionResponse.From(s, publishedOnly: false)).ToList());
    }

    [HttpPost("sections")]
    [ProducesResponseType(typeof(ContentSectionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContentSectionResponse>> CreateSection(UpsertContentSectionRequest request, CancellationToken ct)
    {
        if (await _db.ContentSections.AnyAsync(s => s.Key == request.Key, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate key",
                Detail = $"A content section with key '{request.Key}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var section = new ContentSection();
        Apply(request, section);

        _db.ContentSections.Add(section);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetSections), null, ContentSectionResponse.From(section, publishedOnly: false));
    }

    [HttpPut("sections/{id:guid}")]
    [ProducesResponseType(typeof(ContentSectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContentSectionResponse>> UpdateSection(Guid id, UpsertContentSectionRequest request, CancellationToken ct)
    {
        var section = await _db.ContentSections.Include(s => s.Items).SingleOrDefaultAsync(s => s.Id == id, ct);
        if (section is null)
        {
            return NotFound();
        }

        if (await _db.ContentSections.AnyAsync(s => s.Key == request.Key && s.Id != id, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate key",
                Detail = $"Another content section already uses key '{request.Key}'.",
                Status = StatusCodes.Status409Conflict
            });
        }

        Apply(request, section);
        await _db.SaveChangesAsync(ct);

        return Ok(ContentSectionResponse.From(section, publishedOnly: false));
    }

    [HttpDelete("sections/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSection(Guid id, CancellationToken ct)
    {
        var section = await _db.ContentSections.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (section is null)
        {
            return NotFound();
        }

        _db.ContentSections.Remove(section);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("sections/{sectionId:guid}/items")]
    [ProducesResponseType(typeof(ContentItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentItemResponse>> CreateItem(Guid sectionId, UpsertContentItemRequest request, CancellationToken ct)
    {
        if (!await _db.ContentSections.AnyAsync(s => s.Id == sectionId, ct))
        {
            return NotFound();
        }

        var item = new ContentItem { ContentSectionId = sectionId };
        Apply(request, item);

        _db.ContentItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetSections), null, ContentItemResponse.From(item));
    }

    [HttpPut("items/{id:guid}")]
    [ProducesResponseType(typeof(ContentItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentItemResponse>> UpdateItem(Guid id, UpsertContentItemRequest request, CancellationToken ct)
    {
        var item = await _db.ContentItems.SingleOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
        {
            return NotFound();
        }

        Apply(request, item);
        await _db.SaveChangesAsync(ct);
        return Ok(ContentItemResponse.From(item));
    }

    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        var item = await _db.ContentItems.SingleOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
        {
            return NotFound();
        }

        _db.ContentItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("testimonials")]
    [ProducesResponseType(typeof(IReadOnlyList<TestimonialResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TestimonialResponse>>> GetTestimonials(CancellationToken ct)
    {
        var items = await _db.Testimonials
            .AsNoTracking()
            .OrderBy(t => t.DisplayOrder)
            .Select(t => TestimonialResponse.From(t))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("testimonials")]
    [ProducesResponseType(typeof(TestimonialResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TestimonialResponse>> CreateTestimonial(UpsertTestimonialRequest request, CancellationToken ct)
    {
        var testimonial = new Testimonial();
        Apply(request, testimonial);

        _db.Testimonials.Add(testimonial);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetTestimonials), null, TestimonialResponse.From(testimonial));
    }

    [HttpPut("testimonials/{id:guid}")]
    [ProducesResponseType(typeof(TestimonialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestimonialResponse>> UpdateTestimonial(Guid id, UpsertTestimonialRequest request, CancellationToken ct)
    {
        var testimonial = await _db.Testimonials.SingleOrDefaultAsync(t => t.Id == id, ct);
        if (testimonial is null)
        {
            return NotFound();
        }

        Apply(request, testimonial);
        await _db.SaveChangesAsync(ct);
        return Ok(TestimonialResponse.From(testimonial));
    }

    [HttpDelete("testimonials/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTestimonial(Guid id, CancellationToken ct)
    {
        var testimonial = await _db.Testimonials.SingleOrDefaultAsync(t => t.Id == id, ct);
        if (testimonial is null)
        {
            return NotFound();
        }

        _db.Testimonials.Remove(testimonial);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(IReadOnlyList<StatMetricResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StatMetricResponse>>> GetStats(CancellationToken ct)
    {
        var items = await _db.StatMetrics
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .Select(s => StatMetricResponse.From(s))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("stats")]
    [ProducesResponseType(typeof(StatMetricResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<StatMetricResponse>> CreateStat(UpsertStatMetricRequest request, CancellationToken ct)
    {
        var stat = new StatMetric();
        Apply(request, stat);

        _db.StatMetrics.Add(stat);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetStats), null, StatMetricResponse.From(stat));
    }

    [HttpPut("stats/{id:guid}")]
    [ProducesResponseType(typeof(StatMetricResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StatMetricResponse>> UpdateStat(Guid id, UpsertStatMetricRequest request, CancellationToken ct)
    {
        var stat = await _db.StatMetrics.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (stat is null)
        {
            return NotFound();
        }

        Apply(request, stat);
        await _db.SaveChangesAsync(ct);
        return Ok(StatMetricResponse.From(stat));
    }

    [HttpDelete("stats/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStat(Guid id, CancellationToken ct)
    {
        var stat = await _db.StatMetrics.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (stat is null)
        {
            return NotFound();
        }

        _db.StatMetrics.Remove(stat);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(UpsertContentSectionRequest request, ContentSection section)
    {
        section.Key = request.Key;
        section.Eyebrow = request.Eyebrow;
        section.Heading = request.Heading;
        section.Description = request.Description;
        section.DisplayOrder = request.DisplayOrder;
        section.IsPublished = request.IsPublished;
    }

    private static void Apply(UpsertContentItemRequest request, ContentItem item)
    {
        item.Title = request.Title;
        item.Summary = request.Summary;
        item.Icon = request.Icon;
        item.DisplayOrder = request.DisplayOrder;
        item.IsPublished = request.IsPublished;
    }

    private static void Apply(UpsertTestimonialRequest request, Testimonial testimonial)
    {
        testimonial.Quote = request.Quote;
        testimonial.AuthorInitials = request.AuthorInitials;
        testimonial.AuthorTitle = request.AuthorTitle;
        testimonial.Organisation = request.Organisation;
        testimonial.DisplayOrder = request.DisplayOrder;
        testimonial.IsPublished = request.IsPublished;
    }

    private static void Apply(UpsertStatMetricRequest request, StatMetric stat)
    {
        stat.Value = request.Value;
        stat.Label = request.Label;
        stat.Description = request.Description;
        stat.DisplayOrder = request.DisplayOrder;
        stat.IsPublished = request.IsPublished;
    }
}
