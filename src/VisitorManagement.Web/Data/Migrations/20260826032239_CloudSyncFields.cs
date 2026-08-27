using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagement.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CloudSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudSyncError",
                table: "Visits",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CloudSynced",
                table: "Visits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CloudSyncedAt",
                table: "Visits",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudSyncError",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CloudSynced",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CloudSyncedAt",
                table: "Visits");
        }
    }
}
