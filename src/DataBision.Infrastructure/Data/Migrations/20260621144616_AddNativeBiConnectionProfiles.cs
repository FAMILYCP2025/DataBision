using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNativeBiConnectionProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NativeBiConnectionProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileName = table.Column<string>(type: "TEXT", nullable: false),
                    EnvironmentName = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceLayerBaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    CompanyDb = table.Column<string>(type: "TEXT", nullable: false),
                    SapUserName = table.Column<string>(type: "TEXT", nullable: false),
                    SecretRef = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IgnoreSslErrors = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    FetchConcurrency = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NativeBiConnectionProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NativeBiConnectionProfiles_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NativeBiConnectionProfiles_CompanyId",
                table: "NativeBiConnectionProfiles",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NativeBiConnectionProfiles");
        }
    }
}
