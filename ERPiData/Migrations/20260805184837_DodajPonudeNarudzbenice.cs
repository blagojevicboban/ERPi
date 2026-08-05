using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajPonudeNarudzbenice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NarudzbeniceDobavljacima",
                columns: table => new
                {
                    NarudzbenicaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNarudzbenice = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RokIsporuke = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UkupnoNeto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoBruto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarudzbeniceDobavljacima", x => x.NarudzbenicaId);
                    table.ForeignKey(
                        name: "FK_NarudzbeniceDobavljacima_Kalkulacije_KalkulacijaId",
                        column: x => x.KalkulacijaId,
                        principalTable: "Kalkulacije",
                        principalColumn: "KalkulacijaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NarudzbeniceDobavljacima_Magacini_MagacinId",
                        column: x => x.MagacinId,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NarudzbeniceDobavljacima_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PonudePredracuni",
                columns: table => new
                {
                    PonudaPredracunId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojDokumenta = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    VrstaDokumenta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RokVazenja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UkupnoNeto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoBruto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    RacunOtpremnicaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PonudePredracuni", x => x.PonudaPredracunId);
                    table.ForeignKey(
                        name: "FK_PonudePredracuni_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PonudePredracuni_RacuniOtpremnice_RacunOtpremnicaId",
                        column: x => x.RacunOtpremnicaId,
                        principalTable: "RacuniOtpremnice",
                        principalColumn: "RacunOtpremnicaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NarudzbeniceStavke",
                columns: table => new
                {
                    NarudzbenicaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NarudzbenicaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: true),
                    KolicinaNarucena = table.Column<decimal>(type: "decimal(18, 3)", nullable: false),
                    KolicinaPristigla = table.Column<decimal>(type: "decimal(18, 3)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PdvStopa = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    IznosNeto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosBruto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarudzbeniceStavke", x => x.NarudzbenicaStavkaId);
                    table.ForeignKey(
                        name: "FK_NarudzbeniceStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NarudzbeniceStavke_NarudzbeniceDobavljacima_NarudzbenicaId",
                        column: x => x.NarudzbenicaId,
                        principalTable: "NarudzbeniceDobavljacima",
                        principalColumn: "NarudzbenicaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PonudeStavke",
                columns: table => new
                {
                    PonudaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PonudaPredracunId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 3)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RabatProcenat = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    PdvStopa = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    IznosNeto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosBruto = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PonudeStavke", x => x.PonudaStavkaId);
                    table.ForeignKey(
                        name: "FK_PonudeStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PonudeStavke_PonudePredracuni_PonudaPredracunId",
                        column: x => x.PonudaPredracunId,
                        principalTable: "PonudePredracuni",
                        principalColumn: "PonudaPredracunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NarudzbeniceDobavljacima_KalkulacijaId",
                table: "NarudzbeniceDobavljacima",
                column: "KalkulacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_NarudzbeniceDobavljacima_MagacinId",
                table: "NarudzbeniceDobavljacima",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_NarudzbeniceDobavljacima_PartnerId",
                table: "NarudzbeniceDobavljacima",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_NarudzbeniceStavke_ArtikalId",
                table: "NarudzbeniceStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_NarudzbeniceStavke_NarudzbenicaId",
                table: "NarudzbeniceStavke",
                column: "NarudzbenicaId");

            migrationBuilder.CreateIndex(
                name: "IX_PonudePredracuni_PartnerId",
                table: "PonudePredracuni",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PonudePredracuni_RacunOtpremnicaId",
                table: "PonudePredracuni",
                column: "RacunOtpremnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_PonudeStavke_ArtikalId",
                table: "PonudeStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_PonudeStavke_PonudaPredracunId",
                table: "PonudeStavke",
                column: "PonudaPredracunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NarudzbeniceStavke");

            migrationBuilder.DropTable(
                name: "PonudeStavke");

            migrationBuilder.DropTable(
                name: "NarudzbeniceDobavljacima");

            migrationBuilder.DropTable(
                name: "PonudePredracuni");
        }
    }
}
