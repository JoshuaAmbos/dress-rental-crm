using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class InitialCleanSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_RentalItems_RentalItemId",
                table: "BookingDetails");

            migrationBuilder.DropTable(
                name: "RentalItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalTerms",
                table: "RentalTerms");

            migrationBuilder.RenameTable(
                name: "RentalTerms",
                newName: "RentalTerm");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalTerm",
                table: "RentalTerm",
                column: "RentalTermId");

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.CompanyId);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDatabase",
                columns: table => new
                {
                    CompanyDatabaseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CredentialKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDatabase", x => x.CompanyDatabaseId);
                    table.ForeignKey(
                        name: "FK_CompanyDatabase_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Device",
                columns: table => new
                {
                    DeviceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_Device_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Garment",
                columns: table => new
                {
                    GarmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StyleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BustSize = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    WaistSize = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HipSize = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RentalRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReplacementValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Garment", x => x.GarmentId);
                    table.ForeignKey(
                        name: "FK_Garment_CompanyDatabase_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CompanyDatabase",
                        principalColumn: "CompanyDatabaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDatabase_CompanyId",
                table: "CompanyDatabase",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Device_CompanyId",
                table: "Device",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Garment_CompanyId",
                table: "Garment",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_Garment_RentalItemId",
                table: "BookingDetails",
                column: "RentalItemId",
                principalTable: "Garment",
                principalColumn: "GarmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetails_Garment_RentalItemId",
                table: "BookingDetails");

            migrationBuilder.DropTable(
                name: "Device");

            migrationBuilder.DropTable(
                name: "Garment");

            migrationBuilder.DropTable(
                name: "CompanyDatabase");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalTerm",
                table: "RentalTerm");

            migrationBuilder.RenameTable(
                name: "RentalTerm",
                newName: "RentalTerms");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalTerms",
                table: "RentalTerms",
                column: "RentalTermId");

            migrationBuilder.CreateTable(
                name: "RentalItems",
                columns: table => new
                {
                    RentalItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RentalRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReplacementValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StyleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalItems", x => x.RentalItemId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalItems_ItemCode",
                table: "RentalItems",
                column: "ItemCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetails_RentalItems_RentalItemId",
                table: "BookingDetails",
                column: "RentalItemId",
                principalTable: "RentalItems",
                principalColumn: "RentalItemId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
