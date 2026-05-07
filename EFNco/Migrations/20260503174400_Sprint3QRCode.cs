using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFNco.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3QRCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "QRCodeData",
                table: "ParkingPermits",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCodeData",
                table: "ParkingPermits");
        }
    }
}
