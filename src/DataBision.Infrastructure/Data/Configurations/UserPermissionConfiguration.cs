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

        b.HasOne(x => x.User).WithMany(x => x.Permissions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Module).WithMany(x => x.Permissions).HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Report).WithMany().HasForeignKey(x => x.ReportId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
    }
}
