using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class AddPaymentMethodToRentalBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "RentalBookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "RentalBookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "RentalBookings");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "RentalBookings");
        }
    }
}
