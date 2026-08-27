using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagement.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class VisitCardAndWebcamPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardPhotoPath",
                table: "Visits",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardPhotoPath",
                table: "Visitors",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardPhotoPath",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CardPhotoPath",
                table: "Visitors");
        }
    }
}
