using Kennen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kennen.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Company).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Engagement).HasMaxLength(64);
        builder.Property(x => x.Message).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.InternalNotes).HasMaxLength(4000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        // The admin list is always "newest first, optionally filtered by status".
        builder.HasIndex(x => x.CreatedAtUtc).IsDescending();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Email);
    }
}
