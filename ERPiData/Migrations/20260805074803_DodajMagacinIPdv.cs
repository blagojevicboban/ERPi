using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajMagacinIPdv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artikli",
                columns: table => new
                {
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Pakovanje = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TarifniBroj = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Barkod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NabavnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    ProdajnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    PdvStopa = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    KlasifikacionaSifra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artikli", x => x.ArtikalId);
                });

            migrationBuilder.CreateTable(
                name: "Magacini",
                columns: table => new
                {
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NazivMagacina = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OdgovornoLice = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VrstaMagacina = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Magacini", x => x.MagacinId);
                });

            migrationBuilder.CreateTable(
                name: "PdvZapisi",
                columns: table => new
                {
                    PdvZapisId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    TipKnjige = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BrojDokumenta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatumDokumenta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumPoreskogDogadjaja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Osnovica = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    StopaPdv = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Ukupno = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdvZapisi", x => x.PdvZapisId);
                    table.ForeignKey(
                        name: "FK_PdvZapisi_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                    table.ForeignKey(
                        name: "FK_PdvZapisi_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId");
                });

            migrationBuilder.CreateTable(
                name: "Kalkulacije",
                columns: table => new
                {
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrojKalkulacije = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojFaktureDobavljaca = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumFakture = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VrstaKalkulacije = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    UkupnoNabavna = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoProdajna = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kalkulacije", x => x.KalkulacijaId);
                    table.ForeignKey(
                        name: "FK_Kalkulacije_Magacini_MagacinId",
                        column: x => x.MagacinId,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kalkulacije_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StavkeKalkulacije",
                columns: table => new
                {
                    StavkaKalkulacijeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    NabavnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    RabatProcenat = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    MarzaProcenat = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    PdvStopa = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    ProdajnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    IznosNabavni = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosProdajni = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StavkeKalkulacije", x => x.StavkaKalkulacijeId);
                    table.ForeignKey(
                        name: "FK_StavkeKalkulacije_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StavkeKalkulacije_Kalkulacije_KalkulacijaId",
                        column: x => x.KalkulacijaId,
                        principalTable: "Kalkulacije",
                        principalColumn: "KalkulacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artikli_SifraArtikla",
                table: "Artikli",
                column: "SifraArtikla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kalkulacije_MagacinId",
                table: "Kalkulacije",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_Kalkulacije_PartnerId",
                table: "Kalkulacije",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Magacini_SifraMagacina",
                table: "Magacini",
                column: "SifraMagacina",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PdvZapisi_NalogId",
                table: "PdvZapisi",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PdvZapisi_PartnerId",
                table: "PdvZapisi",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeKalkulacije_ArtikalId",
                table: "StavkeKalkulacije",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeKalkulacije_KalkulacijaId",
                table: "StavkeKalkulacije",
                column: "KalkulacijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PdvZapisi");

            migrationBuilder.DropTable(
                name: "StavkeKalkulacije");

            migrationBuilder.DropTable(
                name: "Artikli");

            migrationBuilder.DropTable(
                name: "Kalkulacije");

            migrationBuilder.DropTable(
                name: "Magacini");
        }
    }
}
