using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFNco.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitFileUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LicensePhotoContentType",
                table: "ParkingPermits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LicensePhotoData",
                table: "ParkingPermits",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePhotoFileName",
                table: "ParkingPermits",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationFileContentType",
                table: "ParkingPermits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RegistrationFileData",
                table: "ParkingPermits",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationFileName",
                table: "ParkingPermits",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicensePhotoContentType",
                table: "ParkingPermits");

            migrationBuilder.DropColumn(
                name: "LicensePhotoData",
                table: "ParkingPermits");

            migrationBuilder.DropColumn(
                name: "LicensePhotoFileName",
                table: "ParkingPermits");

            migrationBuilder.DropColumn(
                name: "RegistrationFileContentType",
                table: "ParkingPermits");

            migrationBuilder.DropColumn(
                name: "RegistrationFileData",
                table: "ParkingPermits");

            migrationBuilder.DropColumn(
                name: "RegistrationFileName",
                table: "ParkingPermits");
        }
    }
}
