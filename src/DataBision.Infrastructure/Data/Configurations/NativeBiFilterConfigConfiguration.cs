using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class NativeBiFilterConfigConfiguration : IEntityTypeConfiguration<NativeBiFilterConfig>
{
    public void Configure(EntityTypeBuilder<NativeBiFilterConfig> b)
    {
        b.ToTable("native_bi_filter_configs", schema: "cfg");
        b.HasKey(x => new { x.CompanyId, x.FilterKey });
        b.Property(x => x.FilterKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.Label).HasMaxLength(200);
        b.Property(x => x.DefaultValue).HasMaxLength(500);
        b.Property(x => x.IsEnabled).HasDefaultValue(true);
        b.Property(x => x.IsAdvanced).HasDefaultValue(false);
        b.Property(x => x.DisplayOrder).HasDefaultValue(0);
        b.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
