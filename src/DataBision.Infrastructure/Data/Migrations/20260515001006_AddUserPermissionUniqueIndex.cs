using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissionUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_unique_module",
                table: "user_permissions",
                columns: new[] { "UserId", "CompanyId", "ModuleId" },
                unique: true,
                filter: "\"ReportId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_unique_report",
                table: "user_permissions",
                columns: new[] { "UserId", "CompanyId", "ModuleId", "ReportId" },
                unique: true,
                filter: "\"ReportId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_permissions_unique_module",
                table: "user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_user_permissions_unique_report",
                table: "user_permissions");
        }
    }
}
