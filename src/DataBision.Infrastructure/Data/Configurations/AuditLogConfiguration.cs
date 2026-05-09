using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.ResourceType).HasMaxLength(100);
        b.Property(x => x.ResourceId).HasMaxLength(100);
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasIndex(x => new { x.CompanyId, x.CreatedAt });
        b.HasIndex(x => new { x.UserId, x.CreatedAt });

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
    }
}
