using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajSefIPfr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PfrRacuni",
                columns: table => new
                {
                    PfrRacunId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrojRacuna = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipRacuna = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PfrBroj = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    QrKodUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PfrRacuni", x => x.PfrRacunId);
                    table.ForeignKey(
                        name: "FK_PfrRacuni_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId");
                });

            migrationBuilder.CreateTable(
                name: "SefDokumenti",
                columns: table => new
                {
                    SefDokumentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrojDokumenta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatumDokumenta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumSlanja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CirId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TipDokumenta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Osnovica = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Ukupno = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UblXmlContent = table.Column<string>(type: "TEXT", nullable: true),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SefDokumenti", x => x.SefDokumentId);
                    table.ForeignKey(
                        name: "FK_SefDokumenti_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PfrRacuni_PartnerId",
                table: "PfrRacuni",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SefDokumenti_PartnerId",
                table: "SefDokumenti",
                column: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PfrRacuni");

            migrationBuilder.DropTable(
                name: "SefDokumenti");
        }
    }
}
