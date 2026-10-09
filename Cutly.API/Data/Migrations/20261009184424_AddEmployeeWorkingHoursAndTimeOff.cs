using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cutly.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeWorkingHoursAndTimeOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeTimeOffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalonEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTimeOffs", x => x.Id);
                    table.CheckConstraint("CK_EmployeeTimeOffs_DateRange", "\"EndDateTime\" > \"StartDateTime\"");
                    table.ForeignKey(
                        name: "FK_EmployeeTimeOffs_SalonEmployees_SalonEmployeeId",
                        column: x => x.SalonEmployeeId,
                        principalTable: "SalonEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkingHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalonEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    IsDayOff = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkingHours", x => x.Id);
                    table.CheckConstraint("CK_EmployeeWorkingHours_DayOfWeek", "\"DayOfWeek\" BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_EmployeeWorkingHours_TimeRange", "\"IsDayOff\" = TRUE OR \"EndTime\" > \"StartTime\"");
                    table.ForeignKey(
                        name: "FK_EmployeeWorkingHours_SalonEmployees_SalonEmployeeId",
                        column: x => x.SalonEmployeeId,
                        principalTable: "SalonEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTimeOffs_SalonEmployeeId",
                table: "EmployeeTimeOffs",
                column: "SalonEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkingHours_SalonEmployeeId_DayOfWeek",
                table: "EmployeeWorkingHours",
                columns: new[] { "SalonEmployeeId", "DayOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeTimeOffs");

            migrationBuilder.DropTable(
                name: "EmployeeWorkingHours");
        }
    }
}
