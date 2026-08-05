using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajRobnoIMaterijalno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaloprodajneKalkulacije",
                columns: table => new
                {
                    MaloprodajnaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraProdavnice = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojKalkulacije = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacinaPrima = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SifraMagacinaDaje = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SifraDobavljaca = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BrojOtpremnice = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumOtpremnice = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BrojRacuna = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TransportniTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TroskoviUskladistenja = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UtovarIstovar = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TransportnoOsiguranje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    OstaliTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTrgovinskiKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    SvegaTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RabatPri = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SvegaNabavno = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Razlika = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    MarzaProcenat = table.Column<decimal>(type: "decimal(9, 4)", nullable: false),
                    Porez = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PoreskaStopaProcenat = table.Column<decimal>(type: "decimal(9, 4)", nullable: false),
                    ProdajnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RabatIznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaloprodajneKalkulacije", x => x.MaloprodajnaKalkulacijaId);
                    table.ForeignKey(
                        name: "FK_MaloprodajneKalkulacije_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                });

            migrationBuilder.CreateTable(
                name: "Materijali",
                columns: table => new
                {
                    MaterijalId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Pakovanje = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materijali", x => x.MaterijalId);
                });

            migrationBuilder.CreateTable(
                name: "MaterijalneKartice",
                columns: table => new
                {
                    MaterijalnaKarticaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumPromene = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpisPromene = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Ulaz = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Izlaz = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Stanje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    CenaIzlaz = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Duguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Potrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterijalneKartice", x => x.MaterijalnaKarticaId);
                });

            migrationBuilder.CreateTable(
                name: "NivelacijeCena",
                columns: table => new
                {
                    NivelacijaCenaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNivelacije = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumNivelacije = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: true),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    UkupnoRazlika = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NivelacijeCena", x => x.NivelacijaCenaId);
                    table.ForeignKey(
                        name: "FK_NivelacijeCena_Magacini_MagacinId",
                        column: x => x.MagacinId,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId");
                    table.ForeignKey(
                        name: "FK_NivelacijeCena_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                });

            migrationBuilder.CreateTable(
                name: "PrimopredajaNalozi",
                columns: table => new
                {
                    PrimopredajaNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacinaDaje = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SifraMagacinaPrima = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    VrstaDokumenta = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    StopaPdv = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrimopredajaNalozi", x => x.PrimopredajaNalogId);
                    table.ForeignKey(
                        name: "FK_PrimopredajaNalozi_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                });

            migrationBuilder.CreateTable(
                name: "TrebovanjeNalozi",
                columns: table => new
                {
                    TrebovanjeNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrebovanjeNalozi", x => x.TrebovanjeNalogId);
                });

            migrationBuilder.CreateTable(
                name: "UlazNalozi",
                columns: table => new
                {
                    UlazNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BrojRacuna = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UlazNalozi", x => x.UlazNalogId);
                });

            migrationBuilder.CreateTable(
                name: "UvozneKalkulacije",
                columns: table => new
                {
                    UvoznaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojKalkulacije = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatumKalkulacije = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InoPartnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    InoBrojFakture = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatumInoFakture = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valuta = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    KursValute = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    UkupnoDevize = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoFakturaRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CarinaRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SpedicijaRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PrevozRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    OstaliZavisniTroskoviRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnaNabavnaVrednostRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UvozneKalkulacije", x => x.UvoznaKalkulacijaId);
                    table.ForeignKey(
                        name: "FK_UvozneKalkulacije_Magacini_MagacinId",
                        column: x => x.MagacinId,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UvozneKalkulacije_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                    table.ForeignKey(
                        name: "FK_UvozneKalkulacije_Partneri_InoPartnerId",
                        column: x => x.InoPartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaloprodajneKalkulacijeStavke",
                columns: table => new
                {
                    MaloprodajnaKalkulacijaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaloprodajnaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    NabavnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Troskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RazlikaProcenat = table.Column<decimal>(type: "decimal(18, 6)", nullable: false),
                    RazlikaIznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ProdajnaVrednostBezPoreza = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PorezProcenat = table.Column<decimal>(type: "decimal(9, 4)", nullable: false),
                    PorezIznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PosebanPorezProcenat = table.Column<decimal>(type: "decimal(9, 4)", nullable: false),
                    PosebanPorezIznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PrenetiPorez = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PrenetiPosebanPorez = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PorezZaUplatu = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Taksa = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ProdajnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ProdajnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    TarifniBroj = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    BrojRazduzenja = table.Column<int>(type: "INTEGER", nullable: true),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTrgovinskiKnjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaloprodajneKalkulacijeStavke", x => x.MaloprodajnaKalkulacijaStavkaId);
                    table.ForeignKey(
                        name: "FK_MaloprodajneKalkulacijeStavke_MaloprodajneKalkulacije_MaloprodajnaKalkulacijaId",
                        column: x => x.MaloprodajnaKalkulacijaId,
                        principalTable: "MaloprodajneKalkulacije",
                        principalColumn: "MaloprodajnaKalkulacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NivelacijeStavke",
                columns: table => new
                {
                    NivelacijaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NivelacijaCenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: true),
                    KolicinaZaliha = table.Column<decimal>(type: "decimal(18, 3)", nullable: false),
                    StaraCena = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NovaCena = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RazlikaPoJedinici = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnaRazlika = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NivelacijeStavke", x => x.NivelacijaStavkaId);
                    table.ForeignKey(
                        name: "FK_NivelacijeStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId");
                    table.ForeignKey(
                        name: "FK_NivelacijeStavke_NivelacijeCena_NivelacijaCenaId",
                        column: x => x.NivelacijaCenaId,
                        principalTable: "NivelacijeCena",
                        principalColumn: "NivelacijaCenaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrimopredajaStavke",
                columns: table => new
                {
                    PrimopredajaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrimopredajaNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrimopredajaStavke", x => x.PrimopredajaStavkaId);
                    table.ForeignKey(
                        name: "FK_PrimopredajaStavke_PrimopredajaNalozi_PrimopredajaNalogId",
                        column: x => x.PrimopredajaNalogId,
                        principalTable: "PrimopredajaNalozi",
                        principalColumn: "PrimopredajaNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrebovanjeStavke",
                columns: table => new
                {
                    TrebovanjeStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrebovanjeNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    KontoTroska = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrebovanjeStavke", x => x.TrebovanjeStavkaId);
                    table.ForeignKey(
                        name: "FK_TrebovanjeStavke_TrebovanjeNalozi_TrebovanjeNalogId",
                        column: x => x.TrebovanjeNalogId,
                        principalTable: "TrebovanjeNalozi",
                        principalColumn: "TrebovanjeNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UlazStavke",
                columns: table => new
                {
                    UlazStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UlazNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UlazStavke", x => x.UlazStavkaId);
                    table.ForeignKey(
                        name: "FK_UlazStavke_UlazNalozi_UlazNalogId",
                        column: x => x.UlazNalogId,
                        principalTable: "UlazNalozi",
                        principalColumn: "UlazNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UvozneStavke",
                columns: table => new
                {
                    UvoznaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UvoznaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    InoCenaDevize = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    InoIznosDevize = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    InoIznosRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CarinaProcenat = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CarinaIznosRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RasporedjeniZavisniTroskoviRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnaNabavnaVrednostRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NabavnaCenaPoJediniciRsd = table.Column<decimal>(type: "decimal(18, 4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UvozneStavke", x => x.UvoznaStavkaId);
                    table.ForeignKey(
                        name: "FK_UvozneStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UvozneStavke_UvozneKalkulacije_UvoznaKalkulacijaId",
                        column: x => x.UvoznaKalkulacijaId,
                        principalTable: "UvozneKalkulacije",
                        principalColumn: "UvoznaKalkulacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacije_NalogId",
                table: "MaloprodajneKalkulacije",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacijeStavke_MaloprodajnaKalkulacijaId",
                table: "MaloprodajneKalkulacijeStavke",
                column: "MaloprodajnaKalkulacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijeCena_MagacinId",
                table: "NivelacijeCena",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijeCena_NalogId",
                table: "NivelacijeCena",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijeStavke_ArtikalId",
                table: "NivelacijeStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijeStavke_NivelacijaCenaId",
                table: "NivelacijeStavke",
                column: "NivelacijaCenaId");

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaNalozi_NalogId",
                table: "PrimopredajaNalozi",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaStavke_PrimopredajaNalogId",
                table: "PrimopredajaStavke",
                column: "PrimopredajaNalogId");

            migrationBuilder.CreateIndex(
                name: "IX_TrebovanjeStavke_TrebovanjeNalogId",
                table: "TrebovanjeStavke",
                column: "TrebovanjeNalogId");

            migrationBuilder.CreateIndex(
                name: "IX_UlazStavke_UlazNalogId",
                table: "UlazStavke",
                column: "UlazNalogId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneKalkulacije_InoPartnerId",
                table: "UvozneKalkulacije",
                column: "InoPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneKalkulacije_MagacinId",
                table: "UvozneKalkulacije",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneKalkulacije_NalogId",
                table: "UvozneKalkulacije",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneStavke_ArtikalId",
                table: "UvozneStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneStavke_UvoznaKalkulacijaId",
                table: "UvozneStavke",
                column: "UvoznaKalkulacijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaloprodajneKalkulacijeStavke");

            migrationBuilder.DropTable(
                name: "Materijali");

            migrationBuilder.DropTable(
                name: "MaterijalneKartice");

            migrationBuilder.DropTable(
                name: "NivelacijeStavke");

            migrationBuilder.DropTable(
                name: "PrimopredajaStavke");

            migrationBuilder.DropTable(
                name: "TrebovanjeStavke");

            migrationBuilder.DropTable(
                name: "UlazStavke");

            migrationBuilder.DropTable(
                name: "UvozneStavke");

            migrationBuilder.DropTable(
                name: "MaloprodajneKalkulacije");

            migrationBuilder.DropTable(
                name: "NivelacijeCena");

            migrationBuilder.DropTable(
                name: "PrimopredajaNalozi");

            migrationBuilder.DropTable(
                name: "TrebovanjeNalozi");

            migrationBuilder.DropTable(
                name: "UlazNalozi");

            migrationBuilder.DropTable(
                name: "UvozneKalkulacije");
        }
    }
}
