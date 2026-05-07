using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFNco.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitQRToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Unique token used as the scannable URL slug for QR codes.
            // Stored as a GUID string so it is unguessable.
            migrationBuilder.AddColumn<string>(
                name: "QRToken",
                table: "ParkingPermits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // Optional: add a unique index so lookups are fast
            migrationBuilder.CreateIndex(
                name: "IX_ParkingPermits_QRToken",
                table: "ParkingPermits",
                column: "QRToken",
                unique: true,
                filter: "[QRToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingPermits_QRToken",
                table: "ParkingPermits");

            migrationBuilder.DropColumn(
                name: "QRToken",
                table: "ParkingPermits");
        }
    }
}
