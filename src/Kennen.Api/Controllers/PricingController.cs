using Kennen.Api.Contracts.Content;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers;

/// <summary>Read-only AI subscription pricing feed for the marketing site.</summary>
[ApiController]
[Route("api/content/pricing")]
[AllowAnonymous]
[Produces("application/json")]
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
public class PricingController : ControllerBase
{
    private readonly KennenDbContext _db;

    public PricingController(KennenDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PricingPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PricingPlanResponse>>> GetPlans(CancellationToken ct)
    {
        var plans = await _db.PricingPlans
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .Include(p => p.Features)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        return Ok(plans.Select(PricingPlanResponse.From).ToList());
    }
}
