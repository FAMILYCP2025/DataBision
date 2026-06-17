using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAnalyticsId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cfg");

            migrationBuilder.AddColumn<string>(
                name: "AnalyticsCompanyId",
                table: "companies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "company_process_enabled",
                schema: "cfg",
                columns: table => new
                {
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProcessCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    EnabledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_process_enabled", x => new { x.CompanyId, x.ProcessCode });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_process_enabled",
                schema: "cfg");

            migrationBuilder.DropColumn(
                name: "AnalyticsCompanyId",
                table: "companies");
        }
    }
}
