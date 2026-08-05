using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajRobnoKretanje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RobnaKretanja",
                columns: table => new
                {
                    RobnoKretanjeNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MagacinIdDaje = table.Column<int>(type: "INTEGER", nullable: false),
                    MagacinIdPrima = table.Column<int>(type: "INTEGER", nullable: false),
                    VrstaDokumenta = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    StopaPdv = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobnaKretanja", x => x.RobnoKretanjeNalogId);
                    table.ForeignKey(
                        name: "FK_RobnaKretanja_Magacini_MagacinIdDaje",
                        column: x => x.MagacinIdDaje,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RobnaKretanja_Magacini_MagacinIdPrima",
                        column: x => x.MagacinIdPrima,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RobnaKretanja_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                });

            migrationBuilder.CreateTable(
                name: "RobnaKretanjaStavke",
                columns: table => new
                {
                    RobnoKretanjeStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RobnoKretanjeNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobnaKretanjaStavke", x => x.RobnoKretanjeStavkaId);
                    table.ForeignKey(
                        name: "FK_RobnaKretanjaStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RobnaKretanjaStavke_RobnaKretanja_RobnoKretanjeNalogId",
                        column: x => x.RobnoKretanjeNalogId,
                        principalTable: "RobnaKretanja",
                        principalColumn: "RobnoKretanjeNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobnaKretanja_MagacinIdDaje",
                table: "RobnaKretanja",
                column: "MagacinIdDaje");

            migrationBuilder.CreateIndex(
                name: "IX_RobnaKretanja_MagacinIdPrima",
                table: "RobnaKretanja",
                column: "MagacinIdPrima");

            migrationBuilder.CreateIndex(
                name: "IX_RobnaKretanja_NalogId",
                table: "RobnaKretanja",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_RobnaKretanjaStavke_ArtikalId",
                table: "RobnaKretanjaStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_RobnaKretanjaStavke_RobnoKretanjeNalogId",
                table: "RobnaKretanjaStavke",
                column: "RobnoKretanjeNalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RobnaKretanjaStavke");

            migrationBuilder.DropTable(
                name: "RobnaKretanja");
        }
    }
}
