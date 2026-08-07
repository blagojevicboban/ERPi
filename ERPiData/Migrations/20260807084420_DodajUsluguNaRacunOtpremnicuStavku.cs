using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajUsluguNaRacunOtpremnicuStavku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JedinicaMereUsluge",
                table: "RacunOtpremnicaStavke",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpisUsluge",
                table: "RacunOtpremnicaStavke",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JedinicaMereUsluge",
                table: "RacunOtpremnicaStavke");

            migrationBuilder.DropColumn(
                name: "OpisUsluge",
                table: "RacunOtpremnicaStavke");
        }
    }
}
