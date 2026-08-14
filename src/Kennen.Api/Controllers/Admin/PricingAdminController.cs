using Kennen.Api.Contracts.Content;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Identity;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers.Admin;

/// <summary>Authenticated AI subscription plan management.</summary>
[ApiController]
[Route("api/admin/pricing")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
[Produces("application/json")]
public class PricingAdminController : ControllerBase
{
    private readonly KennenDbContext _db;

    public PricingAdminController(KennenDbContext db) => _db = db;

    [HttpGet("plans")]
    [ProducesResponseType(typeof(IReadOnlyList<PricingPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PricingPlanResponse>>> GetPlans(CancellationToken ct)
    {
        var plans = await _db.PricingPlans
            .AsNoTracking()
            .Include(p => p.Features)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        return Ok(plans.Select(p => PricingPlanResponse.From(p, publishedOnly: false)).ToList());
    }

    [HttpPost("plans")]
    [ProducesResponseType(typeof(PricingPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PricingPlanResponse>> CreatePlan(UpsertPricingPlanRequest request, CancellationToken ct)
    {
        if (await _db.PricingPlans.AnyAsync(p => p.Slug == request.Slug, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate slug",
                Detail = $"A pricing plan with slug '{request.Slug}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var plan = new PricingPlan();
        Apply(request, plan);

        _db.PricingPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPlans), null, PricingPlanResponse.From(plan, publishedOnly: false));
    }

    [HttpPut("plans/{id:guid}")]
    [ProducesResponseType(typeof(PricingPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PricingPlanResponse>> UpdatePlan(Guid id, UpsertPricingPlanRequest request, CancellationToken ct)
    {
        var plan = await _db.PricingPlans.Include(p => p.Features).SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
        {
            return NotFound();
        }

        if (await _db.PricingPlans.AnyAsync(p => p.Slug == request.Slug && p.Id != id, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate slug",
                Detail = $"Another pricing plan already uses slug '{request.Slug}'.",
                Status = StatusCodes.Status409Conflict
            });
        }

        Apply(request, plan);
        await _db.SaveChangesAsync(ct);

        return Ok(PricingPlanResponse.From(plan, publishedOnly: false));
    }

    [HttpDelete("plans/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
    {
        var plan = await _db.PricingPlans.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
        {
            return NotFound();
        }

        _db.PricingPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("plans/{planId:guid}/features")]
    [ProducesResponseType(typeof(PricingPlanFeatureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PricingPlanFeatureResponse>> CreateFeature(Guid planId, UpsertPricingPlanFeatureRequest request, CancellationToken ct)
    {
        if (!await _db.PricingPlans.AnyAsync(p => p.Id == planId, ct))
        {
            return NotFound();
        }

        var feature = new PricingPlanFeature { PricingPlanId = planId };
        Apply(request, feature);

        _db.PricingPlanFeatures.Add(feature);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPlans), null, PricingPlanFeatureResponse.From(feature));
    }

    [HttpPut("features/{id:guid}")]
    [ProducesResponseType(typeof(PricingPlanFeatureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PricingPlanFeatureResponse>> UpdateFeature(Guid id, UpsertPricingPlanFeatureRequest request, CancellationToken ct)
    {
        var feature = await _db.PricingPlanFeatures.SingleOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null)
        {
            return NotFound();
        }

        Apply(request, feature);
        await _db.SaveChangesAsync(ct);

        return Ok(PricingPlanFeatureResponse.From(feature));
    }

    [HttpDelete("features/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeature(Guid id, CancellationToken ct)
    {
        var feature = await _db.PricingPlanFeatures.SingleOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null)
        {
            return NotFound();
        }

        _db.PricingPlanFeatures.Remove(feature);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(UpsertPricingPlanRequest request, PricingPlan plan)
    {
        plan.Slug = request.Slug;
        plan.Name = request.Name;
        plan.Subtitle = request.Subtitle;
        plan.Price = request.Price;
        plan.BillingPeriod = request.BillingPeriod;
        plan.Description = request.Description;
        plan.IsFeatured = request.IsFeatured;
        plan.DisplayOrder = request.DisplayOrder;
        plan.IsPublished = request.IsPublished;
    }

    private static void Apply(UpsertPricingPlanFeatureRequest request, PricingPlanFeature feature)
    {
        feature.Text = request.Text;
        feature.Icon = request.Icon;
        feature.DisplayOrder = request.DisplayOrder;
        feature.IsPublished = request.IsPublished;
    }
}
