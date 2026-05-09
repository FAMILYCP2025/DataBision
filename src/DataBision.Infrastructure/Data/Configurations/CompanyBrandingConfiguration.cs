using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class CompanyBrandingConfiguration : IEntityTypeConfiguration<CompanyBranding>
{
    public void Configure(EntityTypeBuilder<CompanyBranding> b)
    {
        b.ToTable("company_branding");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CompanyId).IsUnique();

        b.Property(x => x.PrimaryColor).HasMaxLength(7).IsRequired();
        b.Property(x => x.SecondaryColor).HasMaxLength(7).IsRequired();
        b.Property(x => x.AccentColor).HasMaxLength(7).IsRequired();
        b.Property(x => x.BackgroundColor).HasMaxLength(7).IsRequired();
        b.Property(x => x.SidebarColor).HasMaxLength(7).IsRequired();
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.FaviconUrl).HasMaxLength(500);
        b.Property(x => x.CompanyDisplayName).HasMaxLength(200);
    }
}
