using System.ComponentModel.DataAnnotations;
using Kennen.Api.Contracts.Leads;

namespace Kennen.Api.Tests;

/// <summary>
/// Guards the contract the marketing site's client-side validation mirrors. If these rules
/// change, script.js must change with them.
/// </summary>
public class ContactRequestValidationTests
{
    [Fact]
    public void AFullyPopulatedRequestIsValid()
    {
        Assert.Empty(Validate(Valid()));
    }

    [Fact]
    public void CompanyIsOptional()
    {
        var request = Valid();
        request.Company = null;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("two@@at.signs")]
    [InlineData("trailing@at.sign@")]
    public void AnInvalidEmailIsRejected(string email)
    {
        var request = Valid();
        request.Email = email;

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(request.Email)));
    }

    /// <summary>
    /// [EmailAddress] follows the spec and does not require a dotted domain. The site's
    /// client-side regex is deliberately stricter, so this shape is only ever accepted by
    /// callers that bypass the form - it is recorded here so the difference is intentional.
    /// </summary>
    [Fact]
    public void ADomainWithoutADotIsAcceptedByTheServer()
    {
        var request = Valid();
        request.Email = "someone@intranet";

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void AnEmptyNameIsRejected()
    {
        var request = Valid();
        request.Name = "";

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(request.Name)));
    }

    [Fact]
    public void AMessageShorterThanTenCharactersIsRejected()
    {
        var request = Valid();
        request.Message = "too short";

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(request.Message)));
    }

    [Fact]
    public void AMessageBeyondTheFiveThousandCharacterLimitIsRejected()
    {
        var request = Valid();
        request.Message = new string('a', 5001);

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(request.Message)));
    }

    private static ContactRequest Valid() => new()
    {
        Name = "Jane Doe",
        Email = "jane@company.com",
        Company = "Company Inc.",
        Message = "We would like to discuss an enterprise AI transformation programme."
    };

    private static IReadOnlyList<ValidationResult> Validate(ContactRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
