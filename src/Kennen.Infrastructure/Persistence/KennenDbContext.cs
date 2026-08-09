using Kennen.Domain.Common;
using Kennen.Domain.Entities;
using Kennen.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kennen.Infrastructure.Persistence;

public class KennenDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public KennenDbContext(DbContextOptions<KennenDbContext> options) : base(options) { }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<ContentSection> ContentSections => Set<ContentSection>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<StatMetric> StatMetrics => Set<StatMetric>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(KennenDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Keeps <see cref="EntityBase.UpdatedAtUtc"/> honest without every handler having to
    /// remember it. CreatedAtUtc is left alone so seeded/imported values survive.
    /// </summary>
    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
