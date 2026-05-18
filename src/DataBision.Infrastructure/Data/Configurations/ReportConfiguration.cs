using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.ToTable("reports");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.WorkspaceId).HasMaxLength(100);
        b.Property(x => x.ReportId).HasMaxLength(100);
        b.Property(x => x.DatasetId).HasMaxLength(100);
        b.Property(x => x.EmbedUrl).HasMaxLength(500);
        b.Property(x => x.UpdatedAt);

        b.HasOne(x => x.Module)
            .WithMany(x => x.Reports)
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne(x => x.Company)
            .WithMany(x => x.Reports)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
