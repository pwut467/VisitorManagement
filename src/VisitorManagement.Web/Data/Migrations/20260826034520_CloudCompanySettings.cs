using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagement.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CloudCompanySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudDatabase",
                table: "CompanyProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "CloudEnabled",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CloudPassword",
                table: "CompanyProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudServer",
                table: "CompanyProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "CloudUseWindowsAuth",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CloudUserId",
                table: "CompanyProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudDatabase",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CloudEnabled",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CloudPassword",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CloudServer",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CloudUseWindowsAuth",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CloudUserId",
                table: "CompanyProfiles");
        }
    }
}
