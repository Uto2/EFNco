using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFNco.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4EntryExitLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntryExitLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParkingDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    PlateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PermitId = table.Column<int>(type: "int", nullable: true),
                    VerifiedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryExitLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryExitLogs_ParkingPermits_PermitId",
                        column: x => x.PermitId,
                        principalTable: "ParkingPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EntryExitLogs_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntryExitLogs_PermitId",
                table: "EntryExitLogs",
                column: "PermitId");

            migrationBuilder.CreateIndex(
                name: "IX_EntryExitLogs_VerifiedByUserId",
                table: "EntryExitLogs",
                column: "VerifiedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryExitLogs");
        }
    }
}
