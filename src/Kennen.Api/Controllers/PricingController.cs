using Kennen.Api.Contracts.Content;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers;

/// <summary>
/// Read-only public pricing feed for AI subscription plans. Only published plans and features
/// are returned, ordered by display order.
/// </summary>
[ApiController]
[Route("api/pricing")]
[AllowAnonymous]
[Produces("application/json")]
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
public class PricingController : ControllerBase
{
    private readonly KennenDbContext _db;

    public PricingController(KennenDbContext db) => _db = db;

    [HttpGet("plans")]
    [ProducesResponseType(typeof(IReadOnlyList<PricingPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PricingPlanResponse>>> GetPlans(CancellationToken ct)
    {
        var plans = await _db.PricingPlans
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .Include(p => p.Features)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        return Ok(plans.Select(p => PricingPlanResponse.From(p, publishedOnly: true)).ToList());
    }

    [HttpGet("plans/{slug}")]
    [ProducesResponseType(typeof(PricingPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PricingPlanResponse>> GetPlan(string slug, CancellationToken ct)
    {
        var plan = await _db.PricingPlans
            .AsNoTracking()
            .Include(p => p.Features)
            .SingleOrDefaultAsync(p => p.Slug == slug && p.IsPublished, ct);

        if (plan is null)
        {
            return NotFound();
        }

        return Ok(PricingPlanResponse.From(plan, publishedOnly: true));
    }
}
