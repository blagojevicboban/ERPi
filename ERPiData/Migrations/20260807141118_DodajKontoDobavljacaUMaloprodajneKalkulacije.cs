using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajKontoDobavljacaUMaloprodajneKalkulacije : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KontoDobavljacaId",
                table: "MaloprodajneKalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacije_KontoDobavljacaId",
                table: "MaloprodajneKalkulacije",
                column: "KontoDobavljacaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaloprodajneKalkulacije_Konta_KontoDobavljacaId",
                table: "MaloprodajneKalkulacije",
                column: "KontoDobavljacaId",
                principalTable: "Konta",
                principalColumn: "KontoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaloprodajneKalkulacije_Konta_KontoDobavljacaId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_MaloprodajneKalkulacije_KontoDobavljacaId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropColumn(
                name: "KontoDobavljacaId",
                table: "MaloprodajneKalkulacije");
        }
    }
}
