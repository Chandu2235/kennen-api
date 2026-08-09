using Kennen.Domain.Entities;

namespace Kennen.Api.Tests;

public class JobPostingTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsOpenForApplications_TrueForAPublishedRoleWithNoClosingDate()
    {
        var job = new JobPosting { IsPublished = true, ClosesAtUtc = null };

        Assert.True(job.IsOpenForApplications(Now));
    }

    [Fact]
    public void IsOpenForApplications_FalseForAnUnpublishedDraft()
    {
        var job = new JobPosting { IsPublished = false, ClosesAtUtc = null };

        Assert.False(job.IsOpenForApplications(Now));
    }

    [Fact]
    public void IsOpenForApplications_FalseOnceTheClosingDateHasPassed()
    {
        var job = new JobPosting { IsPublished = true, ClosesAtUtc = Now.AddSeconds(-1) };

        Assert.False(job.IsOpenForApplications(Now));
    }

    [Fact]
    public void IsOpenForApplications_TrueWhileTheClosingDateIsStillInTheFuture()
    {
        var job = new JobPosting { IsPublished = true, ClosesAtUtc = Now.AddDays(1) };

        Assert.True(job.IsOpenForApplications(Now));
    }
}

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsActive_TrueForAnUnexpiredUnrevokedToken()
    {
        var token = new RefreshToken { ExpiresAtUtc = Now.AddDays(7) };

        Assert.True(token.IsActive(Now));
    }

    [Fact]
    public void IsActive_FalseOnceExpired()
    {
        var token = new RefreshToken { ExpiresAtUtc = Now.AddSeconds(-1) };

        Assert.False(token.IsActive(Now));
    }

    [Fact]
    public void IsActive_FalseOnceRevokedEvenIfNotYetExpired()
    {
        var token = new RefreshToken { ExpiresAtUtc = Now.AddDays(7), RevokedAtUtc = Now };

        Assert.False(token.IsActive(Now));
    }
}
