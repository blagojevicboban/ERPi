using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajPoljaKonta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mesto",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ulica",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZiroRacun",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mesto",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "Ulica",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "ZiroRacun",
                table: "Konta");
        }
    }
}
