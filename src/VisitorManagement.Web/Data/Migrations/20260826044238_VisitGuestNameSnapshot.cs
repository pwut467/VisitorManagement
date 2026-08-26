using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagement.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class VisitGuestNameSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestFirstName",
                table: "Visits",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuestLastName",
                table: "Visits",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "Visits",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestTitle",
                table: "Visits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestFirstName",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "GuestLastName",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "GuestTitle",
                table: "Visits");
        }
    }
}
