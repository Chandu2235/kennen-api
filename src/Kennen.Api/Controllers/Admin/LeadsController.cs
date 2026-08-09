using Kennen.Api.Contracts.Common;
using Kennen.Api.Contracts.Leads;
using Kennen.Infrastructure.Identity;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/leads")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
[Produces("application/json")]
public class LeadsController : ControllerBase
{
    private readonly KennenDbContext _db;

    public LeadsController(KennenDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LeadResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LeadResponse>>> List([FromQuery] LeadQuery query, CancellationToken ct)
    {
        var leads = _db.Leads.AsNoTracking();

        if (query.Status.HasValue)
        {
            leads = leads.Where(l => l.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            leads = leads.Where(l =>
                EF.Functions.ILike(l.Name, term) ||
                EF.Functions.ILike(l.Email, term) ||
                (l.Company != null && EF.Functions.ILike(l.Company, term)));
        }

        var total = await leads.CountAsync(ct);
        var items = await leads
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => LeadResponse.From(l))
            .ToListAsync(ct);

        return Ok(new PagedResult<LeadResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadResponse>> Get(Guid id, CancellationToken ct)
    {
        var lead = await _db.Leads.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, ct);
        return lead is null ? NotFound() : Ok(LeadResponse.From(lead));
    }

    /// <summary>Triages a lead. Only the status and internal notes are mutable.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(LeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadResponse>> Update(Guid id, UpdateLeadRequest request, CancellationToken ct)
    {
        var lead = await _db.Leads.SingleOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
        {
            return NotFound();
        }

        if (request.Status.HasValue)
        {
            lead.Status = request.Status.Value;
        }

        if (request.InternalNotes is not null)
        {
            lead.InternalNotes = request.InternalNotes;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(LeadResponse.From(lead));
    }

    /// <summary>Permanently removes a lead. Restricted to administrators.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var lead = await _db.Leads.SingleOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
        {
            return NotFound();
        }

        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
