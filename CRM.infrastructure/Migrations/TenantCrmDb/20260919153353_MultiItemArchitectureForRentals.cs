using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class MultiItemArchitectureForRentals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Garment_RentalItemId",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "DressDescription",
                table: "RentalBookings");

            migrationBuilder.RenameColumn(
                name: "RentalItemId",
                table: "BookingDetails",
                newName: "GarmentId");

            migrationBuilder.RenameIndex(
                name: "IX_BookingDetails_RentalItemId",
                table: "BookingDetails",
                newName: "IX_BookingDetails_GarmentId");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "BookingDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Garment_GarmentId",
                table: "BookingDetails",
                column: "GarmentId",
                principalTable: "Garment",
                principalColumn: "GarmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Garment_GarmentId",
                table: "BookingDetails");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "BookingDetails");

            migrationBuilder.RenameColumn(
                name: "GarmentId",
                table: "BookingDetails",
                newName: "RentalItemId");

            migrationBuilder.RenameIndex(
                name: "IX_BookingDetails_GarmentId",
                table: "BookingDetails",
                newName: "IX_BookingDetails_RentalItemId");

            migrationBuilder.AddColumn<string>(
                name: "DressDescription",
                table: "RentalBookings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Garment_RentalItemId",
                table: "BookingDetails",
                column: "RentalItemId",
                principalTable: "Garment",
                principalColumn: "GarmentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
