using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class NativeBiItemUdfFilterConfigConfiguration : IEntityTypeConfiguration<NativeBiItemUdfFilterConfig>
{
    public void Configure(EntityTypeBuilder<NativeBiItemUdfFilterConfig> b)
    {
        b.ToTable("native_bi_item_udf_filter_configs", schema: "cfg");
        b.HasKey(x => new { x.CompanyId, x.UdfFieldName });
        b.Property(x => x.UdfFieldName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Label).HasMaxLength(200);
        b.Property(x => x.IsEnabled).HasDefaultValue(true);
        b.Property(x => x.IsMultiSelect).HasDefaultValue(false);
        b.Property(x => x.DisplayOrder).HasDefaultValue(0);
        b.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
