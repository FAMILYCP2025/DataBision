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
            migrationBuilder.AddColumn<string>(
                name: "AnalyticsCompanyId",
                table: "companies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalyticsCompanyId",
                table: "companies");
        }
    }
}
