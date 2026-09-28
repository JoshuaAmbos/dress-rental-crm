using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations.TenantCrm
{
    /// <inheritdoc />
    public partial class AddBranchingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "RentalBookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Garment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalBookings_BranchId",
                table: "RentalBookings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Garment_BranchId",
                table: "Garment",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Garment_Branches_BranchId",
                table: "Garment",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_RentalBookings_Branches_BranchId",
                table: "RentalBookings",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Garment_Branches_BranchId",
                table: "Garment");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalBookings_Branches_BranchId",
                table: "RentalBookings");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_RentalBookings_BranchId",
                table: "RentalBookings");

            migrationBuilder.DropIndex(
                name: "IX_Garment_BranchId",
                table: "Garment");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "RentalBookings");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Garment");
        }
    }
}
