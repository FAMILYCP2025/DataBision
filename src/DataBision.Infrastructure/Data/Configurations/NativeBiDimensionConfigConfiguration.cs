using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class NativeBiDimensionConfigConfiguration : IEntityTypeConfiguration<NativeBiDimensionConfig>
{
    public void Configure(EntityTypeBuilder<NativeBiDimensionConfig> b)
    {
        b.ToTable("native_bi_dimension_configs", schema: "cfg");
        b.HasKey(x => new { x.CompanyId, x.DimensionNumber });
        b.Property(x => x.Label).HasMaxLength(200);
        b.Property(x => x.IsEnabled).HasDefaultValue(false);
        b.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
