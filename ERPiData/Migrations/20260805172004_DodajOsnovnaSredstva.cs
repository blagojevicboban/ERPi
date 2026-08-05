using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajOsnovnaSredstva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Komisije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Naziv = table.Column<string>(type: "TEXT", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    JeAktivna = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Komisije", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sredstva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventarskiBroj = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DatumNabavke = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumAktiviranja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IspravkaVrednosti = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SadasnjaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmortizacionaGrupa = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    KontoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ObracunskaJedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    StopaAmortizacije = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RezidualnaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PoreskaGrupa = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PoreskaStopa = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PoreskaNabavnaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PoreskaIspravkaVrednosti = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JeAktivno = table.Column<bool>(type: "INTEGER", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LegacySifra = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sredstva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sredstva_Konta_KontoId",
                        column: x => x.KontoId,
                        principalTable: "Konta",
                        principalColumn: "KontoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClanoviKomisije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KomisijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImePrezime = table.Column<string>(type: "TEXT", nullable: false),
                    Uloga = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanoviKomisije", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanoviKomisije_Komisije_KomisijaId",
                        column: x => x.KomisijaId,
                        principalTable: "Komisije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Popisi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatumPopisa = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    KomisijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Popisi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Popisi_Komisije_KomisijaId",
                        column: x => x.KomisijaId,
                        principalTable: "Komisije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SredstvaKartice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpisPromene = table.Column<string>(type: "TEXT", nullable: false),
                    ObracunskaJedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    KontoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AmortizacionaGrupa1 = table.Column<int>(type: "INTEGER", nullable: false),
                    AmortizacionaGrupa2 = table.Column<int>(type: "INTEGER", nullable: false),
                    StopaAmortizacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    KoeficijentRevalorizacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    Kolicina = table.Column<decimal>(type: "TEXT", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "TEXT", nullable: false),
                    IspravkaVrednosti = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SredstvaKartice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SredstvaKartice_Konta_KontoId",
                        column: x => x.KontoId,
                        principalTable: "Konta",
                        principalColumn: "KontoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SredstvaKartice_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SredstvaPrijave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    RedBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ObracunskaJedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    KontoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AmortizacionaGrupa1 = table.Column<int>(type: "INTEGER", nullable: false),
                    AmortizacionaGrupa2 = table.Column<int>(type: "INTEGER", nullable: false),
                    StopaAmortizacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    DatumAktiviranja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevalorizacionaGrupa = table.Column<int>(type: "INTEGER", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "TEXT", nullable: false),
                    OtpisanaVrednost = table.Column<decimal>(type: "TEXT", nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", nullable: false),
                    Kolicina = table.Column<decimal>(type: "TEXT", nullable: false),
                    InventarskiBroj = table.Column<string>(type: "TEXT", nullable: false),
                    BrojFakture = table.Column<string>(type: "TEXT", nullable: false),
                    DatumFakture = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BrojNalaznice = table.Column<int>(type: "INTEGER", nullable: false),
                    BrNal = table.Column<string>(type: "TEXT", nullable: false),
                    GodNal = table.Column<int>(type: "INTEGER", nullable: false),
                    Knjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SredstvaPrijave", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SredstvaPrijave_Konta_KontoId",
                        column: x => x.KontoId,
                        principalTable: "Konta",
                        principalColumn: "KontoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SredstvaPrijave_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SredstvaPrijave_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SredstvaRashodi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    RedBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kod = table.Column<int>(type: "INTEGER", nullable: false),
                    KodTekst = table.Column<string>(type: "TEXT", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DokumentBroj = table.Column<string>(type: "TEXT", nullable: false),
                    Podaci = table.Column<decimal>(type: "TEXT", nullable: false),
                    ObracunskaJedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    Knjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SredstvaRashodi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SredstvaRashodi_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PopisneStavke",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PopisId = table.Column<int>(type: "INTEGER", nullable: false),
                    SredstvoId = table.Column<int>(type: "INTEGER", nullable: false),
                    KnjiznaKolicina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PopisanaKolicina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KnjiznaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProcenjenaVrednost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PopisneStavke", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PopisneStavke_Popisi_PopisId",
                        column: x => x.PopisId,
                        principalTable: "Popisi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PopisneStavke_Sredstva_SredstvoId",
                        column: x => x.SredstvoId,
                        principalTable: "Sredstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClanoviKomisije_KomisijaId",
                table: "ClanoviKomisije",
                column: "KomisijaId");

            migrationBuilder.CreateIndex(
                name: "IX_Popisi_KomisijaId_Godina",
                table: "Popisi",
                columns: new[] { "KomisijaId", "Godina" });

            migrationBuilder.CreateIndex(
                name: "IX_PopisneStavke_PopisId",
                table: "PopisneStavke",
                column: "PopisId");

            migrationBuilder.CreateIndex(
                name: "IX_PopisneStavke_SredstvoId",
                table: "PopisneStavke",
                column: "SredstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sredstva_InventarskiBroj",
                table: "Sredstva",
                column: "InventarskiBroj");

            migrationBuilder.CreateIndex(
                name: "IX_Sredstva_KontoId",
                table: "Sredstva",
                column: "KontoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sredstva_LegacySifra",
                table: "Sredstva",
                column: "LegacySifra");

            migrationBuilder.CreateIndex(
                name: "IX_SredstvaKartice_KontoId",
                table: "SredstvaKartice",
                column: "KontoId");

            migrationBuilder.CreateIndex(
                name: "IX_SredstvaKartice_SredstvoId",
                table: "SredstvaKartice",
                column: "SredstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_SredstvaPrijave_KontoId",
                table: "SredstvaPrijave",
                column: "KontoId");

            migrationBuilder.CreateIndex(
                name: "IX_SredstvaPrijave_PartnerId",
                table: "SredstvaPrijave",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SredstvaPrijave_SredstvoId",
                table: "SredstvaPrijave",
                column: "SredstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_SredstvaRashodi_SredstvoId",
                table: "SredstvaRashodi",
                column: "SredstvoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClanoviKomisije");

            migrationBuilder.DropTable(
                name: "PopisneStavke");

            migrationBuilder.DropTable(
                name: "SredstvaKartice");

            migrationBuilder.DropTable(
                name: "SredstvaPrijave");

            migrationBuilder.DropTable(
                name: "SredstvaRashodi");

            migrationBuilder.DropTable(
                name: "Popisi");

            migrationBuilder.DropTable(
                name: "Sredstva");

            migrationBuilder.DropTable(
                name: "Komisije");
        }
    }
}
