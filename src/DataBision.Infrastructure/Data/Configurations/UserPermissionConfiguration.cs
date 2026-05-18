using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBision.Infrastructure.Data.Configurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> b)
    {
        b.ToTable("user_permissions");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.UserId, x.CompanyId });

        // Prevent duplicate report-scoped permission rows for the same user.
        // Split into two filtered indexes because SQLite treats NULL as distinct
        // under a single composite unique constraint (so two rows with the same
        // UserId/CompanyId/ModuleId and NULL ReportId would otherwise be allowed).
        b.HasIndex(x => new { x.UserId, x.CompanyId, x.ModuleId, x.ReportId })
            .IsUnique()
            .HasFilter("\"ReportId\" IS NOT NULL")
            .HasDatabaseName("IX_user_permissions_unique_report");

        b.HasIndex(x => new { x.UserId, x.CompanyId, x.ModuleId })
            .IsUnique()
            .HasFilter("\"ReportId\" IS NULL")
            .HasDatabaseName("IX_user_permissions_unique_module");

        b.HasOne(x => x.User).WithMany(x => x.Permissions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Module).WithMany(x => x.Permissions).HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Report).WithMany().HasForeignKey(x => x.ReportId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
    }
}
