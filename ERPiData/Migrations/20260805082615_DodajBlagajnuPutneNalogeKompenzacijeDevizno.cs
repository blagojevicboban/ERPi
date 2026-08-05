using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajBlagajnuPutneNalogeKompenzacijeDevizno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DevizniDuguje",
                table: "StavkeNaloga",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DevizniPotrazuje",
                table: "StavkeNaloga",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BlagajnickiNalozi",
                columns: table => new
                {
                    BlagajnickiNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", nullable: false),
                    VrstaBlagajne = table.Column<int>(type: "INTEGER", nullable: false),
                    VrstaNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UplatilacIsplatilac = table.Column<string>(type: "TEXT", nullable: false),
                    Svrha = table.Column<string>(type: "TEXT", nullable: false),
                    BrojKontaProtu = table.Column<string>(type: "TEXT", nullable: false),
                    Iznos = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsKnjizeno = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlagajnickiNalozi", x => x.BlagajnickiNalogId);
                });

            migrationBuilder.CreateTable(
                name: "Kompenzacije",
                columns: table => new
                {
                    KompenzacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojDokumenta = table.Column<string>(type: "TEXT", nullable: false),
                    Vrsta = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivPartnera = table.Column<string>(type: "TEXT", nullable: false),
                    KontoPartnera1 = table.Column<string>(type: "TEXT", nullable: true),
                    Partner2Id = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivPartnera2 = table.Column<string>(type: "TEXT", nullable: true),
                    KontoPartnera2 = table.Column<string>(type: "TEXT", nullable: true),
                    Partner3Id = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivPartnera3 = table.Column<string>(type: "TEXT", nullable: true),
                    KontoPartnera3 = table.Column<string>(type: "TEXT", nullable: true),
                    UkupanIznosKompenzacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsKnjizeno = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kompenzacije", x => x.KompenzacijaId);
                });

            migrationBuilder.CreateTable(
                name: "KursneListeStavke",
                columns: table => new
                {
                    KursnaListaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValutaOznaka = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ValutaSifra = table.Column<int>(type: "INTEGER", nullable: false),
                    NazivValute = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Jedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    SrednjiKurs = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    KupovniKurs = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    ProdavniKurs = table.Column<decimal>(type: "decimal(18, 4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KursneListeStavke", x => x.KursnaListaStavkaId);
                });

            migrationBuilder.CreateTable(
                name: "NeoporeziviIznosiDnevnice",
                columns: table => new
                {
                    NeoporeziviIznosDnevniceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatumOd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IznosZemljaRsd = table.Column<decimal>(type: "decimal(10, 2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeoporeziviIznosiDnevnice", x => x.NeoporeziviIznosDnevniceId);
                });

            migrationBuilder.CreateTable(
                name: "PutniNalozi",
                columns: table => new
                {
                    PutniNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", nullable: false),
                    Vrsta = table.Column<int>(type: "INTEGER", nullable: false),
                    ZaposleniIme = table.Column<string>(type: "TEXT", nullable: false),
                    RadnoMesto = table.Column<string>(type: "TEXT", nullable: false),
                    Jmbg = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    Relacija = table.Column<string>(type: "TEXT", nullable: false),
                    SvrhaPutovanja = table.Column<string>(type: "TEXT", nullable: false),
                    PrevoznoSredstvo = table.Column<string>(type: "TEXT", nullable: false),
                    DatumPolaska = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumPovratka = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrajanjeSati = table.Column<double>(type: "REAL", nullable: false),
                    BrojDnevnica = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosDnevniceRsd = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoDnevnice = table.Column<decimal>(type: "TEXT", nullable: false),
                    TroskoviGoriva = table.Column<decimal>(type: "TEXT", nullable: false),
                    TroskoviSmestaja = table.Column<decimal>(type: "TEXT", nullable: false),
                    TroskoviPrevoza = table.Column<decimal>(type: "TEXT", nullable: false),
                    OstaliTroskovi = table.Column<decimal>(type: "TEXT", nullable: false),
                    Akontacija = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoZaIsplatu = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsKnjizeno = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutniNalozi", x => x.PutniNalogId);
                });

            migrationBuilder.CreateTable(
                name: "KompenzacijeStavke",
                columns: table => new
                {
                    KompenzacijaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KompenzacijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    StavkaNalogaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojDokumenta = table.Column<string>(type: "TEXT", nullable: false),
                    DatumDokumenta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Strana = table.Column<string>(type: "TEXT", nullable: false),
                    BrojKonta = table.Column<string>(type: "TEXT", nullable: false),
                    IznosFakture = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosPreostalo = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosZaKompenzaciju = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KompenzacijeStavke", x => x.KompenzacijaStavkaId);
                    table.ForeignKey(
                        name: "FK_KompenzacijeStavke_Kompenzacije_KompenzacijaId",
                        column: x => x.KompenzacijaId,
                        principalTable: "Kompenzacije",
                        principalColumn: "KompenzacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PutniNaloziTroskoviStavke",
                columns: table => new
                {
                    PutniNalogTrosakStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PutniNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    VrstaTroska = table.Column<string>(type: "TEXT", nullable: false),
                    BrojRacuna = table.Column<string>(type: "TEXT", nullable: false),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Iznos = table.Column<decimal>(type: "TEXT", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutniNaloziTroskoviStavke", x => x.PutniNalogTrosakStavkaId);
                    table.ForeignKey(
                        name: "FK_PutniNaloziTroskoviStavke_PutniNalozi_PutniNalogId",
                        column: x => x.PutniNalogId,
                        principalTable: "PutniNalozi",
                        principalColumn: "PutniNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KompenzacijeStavke_KompenzacijaId",
                table: "KompenzacijeStavke",
                column: "KompenzacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_PutniNaloziTroskoviStavke_PutniNalogId",
                table: "PutniNaloziTroskoviStavke",
                column: "PutniNalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlagajnickiNalozi");

            migrationBuilder.DropTable(
                name: "KompenzacijeStavke");

            migrationBuilder.DropTable(
                name: "KursneListeStavke");

            migrationBuilder.DropTable(
                name: "NeoporeziviIznosiDnevnice");

            migrationBuilder.DropTable(
                name: "PutniNaloziTroskoviStavke");

            migrationBuilder.DropTable(
                name: "Kompenzacije");

            migrationBuilder.DropTable(
                name: "PutniNalozi");

            migrationBuilder.DropColumn(
                name: "DevizniDuguje",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "DevizniPotrazuje",
                table: "StavkeNaloga");
        }
    }
}
