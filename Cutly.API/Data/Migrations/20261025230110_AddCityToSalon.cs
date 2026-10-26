using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutly.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCityToSalon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Salons",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Salons");
        }
    }
}
