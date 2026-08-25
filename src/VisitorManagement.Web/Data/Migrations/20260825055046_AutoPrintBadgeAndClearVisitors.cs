using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagement.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutoPrintBadgeAndClearVisitors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPrintBadge",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "SeedRevision",
                table: "CompanyProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("DELETE FROM [VisitItems];");
            migrationBuilder.Sql("DELETE FROM [Visits];");
            migrationBuilder.Sql("DELETE FROM [Visitors];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPrintBadge",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "SeedRevision",
                table: "CompanyProfiles");
        }
    }
}
