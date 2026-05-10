using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFNco.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7AuthorizedPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthorizedPersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhotoData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PhotoContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhotoFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PermitId = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizedPersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorizedPersons_ParkingPermits_PermitId",
                        column: x => x.PermitId,
                        principalTable: "ParkingPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorizedPersons_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParkingDurationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitType = table.Column<int>(type: "int", nullable: false),
                    MaxHours = table.Column<double>(type: "float", nullable: false),
                    GraceMinutes = table.Column<int>(type: "int", nullable: false),
                    AutoViolation = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingDurationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingDurationSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizedPersons_AddedByUserId",
                table: "AuthorizedPersons",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizedPersons_PermitId",
                table: "AuthorizedPersons",
                column: "PermitId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingDurationSettings_PermitType",
                table: "ParkingDurationSettings",
                column: "PermitType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingDurationSettings_UpdatedByUserId",
                table: "ParkingDurationSettings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizedPersons");

            migrationBuilder.DropTable(
                name: "ParkingDurationSettings");
        }
    }
}
