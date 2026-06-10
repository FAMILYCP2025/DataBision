using DataBision.Infrastructure.Data.Staging.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Staging.Configurations;

public sealed class CompanyProcessEnabledConfiguration : IEntityTypeConfiguration<CompanyProcessEnabled>
{
    public void Configure(EntityTypeBuilder<CompanyProcessEnabled> builder)
    {
        builder.HasKey(e => new { e.CompanyId, e.ProcessCode })
               .HasName("pk_company_process_enabled");

        builder.Property(e => e.CompanyId)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.ProcessCode)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.IsEnabled)
               .HasDefaultValue(true);

        builder.Property(e => e.EnabledAtUtc)
               .HasColumnType("timestamp with time zone");

        builder.ToTable("company_process_enabled", "cfg");
    }
}
