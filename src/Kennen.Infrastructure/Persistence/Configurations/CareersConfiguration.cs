using Kennen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kennen.Infrastructure.Persistence.Configurations;

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("job_postings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ExperienceLevel).HasMaxLength(80);
        builder.Property(x => x.Description).HasMaxLength(20000).IsRequired();
        builder.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.WorkArrangement).HasConversion<string>().HasMaxLength(32);

        // Mapped to Postgres text[] by Npgsql, which keeps the bullet lists ordered and
        // queryable without needing a child table or dynamic JSON serialisation.
        builder.Property(x => x.Responsibilities).HasColumnType("text[]");
        builder.Property(x => x.Requirements).HasColumnType("text[]");

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsPublished, x.PublishedAtUtc });

        builder.HasMany(x => x.Applications)
            .WithOne(x => x.JobPosting!)
            .HasForeignKey(x => x.JobPostingId)
            // Applications are records of real people applying; never cascade-delete them.
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.LinkedInUrl).HasMaxLength(400);
        builder.Property(x => x.CoverLetter).HasMaxLength(10000);
        builder.Property(x => x.ResumeFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ResumeStorageKey).HasMaxLength(400).IsRequired();
        builder.Property(x => x.ResumeContentType).HasMaxLength(160).IsRequired();
        builder.Property(x => x.InternalNotes).HasMaxLength(4000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => x.CreatedAtUtc).IsDescending();
        builder.HasIndex(x => x.Status);
        // Guards against the same person submitting an application twice for one role.
        builder.HasIndex(x => new { x.JobPostingId, x.Email }).IsUnique();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
