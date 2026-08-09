using Kennen.Api.Auth;
using Kennen.Api.Contracts.Leads;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Api.Controllers;

/// <summary>The single public write endpoint used by the marketing site's contact form.</summary>
[ApiController]
[Route("api/contact")]
[AllowAnonymous]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private const int DuplicateWindowMinutes = 5;

    private readonly KennenDbContext _db;
    private readonly ILogger<ContactController> _logger;

    public ContactController(KennenDbContext db, ILogger<ContactController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    [ProducesResponseType(typeof(ContactResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ContactResponse>> Submit(ContactRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-DuplicateWindowMinutes);

        // A double-clicked submit button should not create two leads. Returning the existing
        // reference keeps the endpoint idempotent from the visitor's point of view.
        var recent = await _db.Leads
            .Where(l => l.Email == email && l.CreatedAtUtc >= cutoff)
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (recent is not null && recent.Message == request.Message.Trim())
        {
            return Accepted(Acknowledge(recent.Id));
        }

        var lead = new Lead
        {
            Name = request.Name.Trim(),
            Email = email,
            Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim(),
            Message = request.Message.Trim(),
            Source = string.IsNullOrWhiteSpace(request.Source) ? "website-contact" : request.Source.Trim(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(Request.Headers.UserAgent.ToString(), 512)
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Captured lead {LeadId} from source {Source}", lead.Id, lead.Source);

        return Accepted(Acknowledge(lead.Id));
    }

    private static ContactResponse Acknowledge(Guid id) => new()
    {
        ReferenceId = id,
        Message = "Thank you for your message. Our team will get back to you shortly."
    };

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) ? null : value.Length <= maxLength ? value : value[..maxLength];
}
