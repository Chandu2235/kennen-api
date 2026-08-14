using Kennen.Api.Contracts.Content;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Identity;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers.Admin;

/// <summary>Authenticated AI subscription pricing management.</summary>
[ApiController]
[Route("api/admin/content/pricing")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
[Produces("application/json")]
public class PricingAdminController : ControllerBase
{
    private readonly KennenDbContext _db;

    public PricingAdminController(KennenDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PricingPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PricingPlanResponse>>> GetPlans(CancellationToken ct)
    {
        var plans = await _db.PricingPlans
            .AsNoTracking()
            .Include(p => p.Features)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        return Ok(plans.Select(PricingPlanResponse.From).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PricingPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PricingPlanResponse>> GetPlan(Guid id, CancellationToken ct)
    {
        var plan = await _db.PricingPlans
            .AsNoTracking()
            .Include(p => p.Features)
            .SingleOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null) return NotFound();
        return Ok(PricingPlanResponse.From(plan));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PricingPlanResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PricingPlanResponse>> CreatePlan(UpsertPricingPlanRequest request, CancellationToken ct)
    {
        var plan = new PricingPlan();
        Apply(request, plan);
        _db.PricingPlans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetPlans), null, PricingPlanResponse.From(plan));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PricingPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PricingPlanResponse>> UpdatePlan(Guid id, UpsertPricingPlanRequest request, CancellationToken ct)
    {
        var plan = await _db.PricingPlans
            .Include(p => p.Features)
            .SingleOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null) return NotFound();

        _db.PricingPlanFeatures.RemoveRange(plan.Features);
        Apply(request, plan);
        await _db.SaveChangesAsync(ct);
        return Ok(PricingPlanResponse.From(plan));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
    {
        var plan = await _db.PricingPlans.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();
        _db.PricingPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(UpsertPricingPlanRequest request, PricingPlan plan)
    {
        plan.Name = request.Name;
        plan.Subtitle = request.Subtitle;
        plan.Price = request.Price;
        plan.Period = request.Period;
        plan.DisplayOrder = request.DisplayOrder;
        plan.IsPopular = request.IsPopular;
        plan.IsPublished = request.IsPublished;

        var features = request.Features ?? Array.Empty<string>();
        for (var i = 0; i < features.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(features[i])) continue;
            plan.Features.Add(new PricingPlanFeature
            {
                Text = features[i].Trim(),
                DisplayOrder = i + 1
            });
        }
    }
}
