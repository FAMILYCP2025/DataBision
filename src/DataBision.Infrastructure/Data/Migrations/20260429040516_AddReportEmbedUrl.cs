using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportEmbedUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PowerBiWorkspaceId",
                table: "reports",
                newName: "WorkspaceId");

            migrationBuilder.RenameColumn(
                name: "PowerBiReportId",
                table: "reports",
                newName: "ReportId");

            migrationBuilder.RenameColumn(
                name: "PowerBiDatasetId",
                table: "reports",
                newName: "DatasetId");

            migrationBuilder.AddColumn<string>(
                name: "EmbedUrl",
                table: "reports",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbedUrl",
                table: "reports");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                table: "reports",
                newName: "PowerBiWorkspaceId");

            migrationBuilder.RenameColumn(
                name: "ReportId",
                table: "reports",
                newName: "PowerBiReportId");

            migrationBuilder.RenameColumn(
                name: "DatasetId",
                table: "reports",
                newName: "PowerBiDatasetId");
        }
    }
}
