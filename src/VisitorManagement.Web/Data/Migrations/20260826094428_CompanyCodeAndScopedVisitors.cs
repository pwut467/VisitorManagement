using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagement.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompanyCodeAndScopedVisitors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visitors_NationalId",
                table: "Visitors");

            migrationBuilder.AlterColumn<string>(
                name: "VisitNumber",
                table: "Visits",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "CompanyProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SKNY");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE CompanyProfiles SET CompanyCode = 'SKNY' WHERE CompanyCode IS NULL OR CompanyCode = '';
                UPDATE CompanyProfiles SET IsActive = 0;
                UPDATE TOP (1) CompanyProfiles SET IsActive = 1;
                """);

            migrationBuilder.AddColumn<int>(
                name: "CompanyProfileId",
                table: "Visits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostCompanyCode",
                table: "Visits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SKNY");

            migrationBuilder.AddColumn<int>(
                name: "CompanyProfileId",
                table: "Visitors",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                DECLARE @CompanyId int = (SELECT TOP 1 Id FROM CompanyProfiles ORDER BY Id);
                DECLARE @CompanyCode nvarchar(20) = (SELECT TOP 1 CompanyCode FROM CompanyProfiles ORDER BY Id);
                IF @CompanyId IS NOT NULL
                BEGIN
                    UPDATE Visits SET CompanyProfileId = @CompanyId, HostCompanyCode = ISNULL(NULLIF(@CompanyCode, ''), 'SKNY') WHERE CompanyProfileId IS NULL OR CompanyProfileId = 0;
                    UPDATE Visitors SET CompanyProfileId = @CompanyId WHERE CompanyProfileId IS NULL OR CompanyProfileId = 0;
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyProfileId",
                table: "Visits",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyProfileId",
                table: "Visitors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CompanyProfileId",
                table: "Visits",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_CompanyProfileId_NationalId",
                table: "Visitors",
                columns: new[] { "CompanyProfileId", "NationalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_CompanyCode",
                table: "CompanyProfiles",
                column: "CompanyCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitors_CompanyProfiles_CompanyProfileId",
                table: "Visitors",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_CompanyProfiles_CompanyProfileId",
                table: "Visits",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visitors_CompanyProfiles_CompanyProfileId",
                table: "Visitors");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_CompanyProfiles_CompanyProfileId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_CompanyProfileId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visitors_CompanyProfileId_NationalId",
                table: "Visitors");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_CompanyCode",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "HostCompanyCode",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CompanyProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "VisitNumber",
                table: "Visits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_NationalId",
                table: "Visitors",
                column: "NationalId",
                unique: true);
        }
    }
}
