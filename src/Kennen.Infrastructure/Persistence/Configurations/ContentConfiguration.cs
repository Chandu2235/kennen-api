using Kennen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kennen.Infrastructure.Persistence.Configurations;

public class ContentSectionConfiguration : IEntityTypeConfiguration<ContentSection>
{
    public void Configure(EntityTypeBuilder<ContentSection> builder)
    {
        builder.ToTable("content_sections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Eyebrow).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Heading).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(3000);

        // The frontend fetches sections by key, so keys must be unique.
        builder.HasIndex(x => x.Key).IsUnique();

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Section!)
            .HasForeignKey(x => x.ContentSectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.Icon).HasMaxLength(32);

        builder.HasIndex(x => new { x.ContentSectionId, x.DisplayOrder });
    }
}

public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> builder)
    {
        builder.ToTable("testimonials");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quote).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AuthorInitials).HasMaxLength(8).IsRequired();
        builder.Property(x => x.AuthorTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Organisation).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.DisplayOrder);
    }
}

public class StatMetricConfiguration : IEntityTypeConfiguration<StatMetric>
{
    public void Configure(EntityTypeBuilder<StatMetric> builder)
    {
        builder.ToTable("stat_metrics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Value).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => x.DisplayOrder);
    }
}
