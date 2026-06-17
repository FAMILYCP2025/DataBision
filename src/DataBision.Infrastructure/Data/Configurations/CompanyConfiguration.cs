using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("companies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.AnalyticsCompanyId).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();

        b.HasOne(x => x.Branding)
            .WithOne(x => x.Company)
            .HasForeignKey<CompanyBranding>(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // EtlConfig excluded from schema until Step 12
        b.Ignore(x => x.EtlConfigs);
    }
}
