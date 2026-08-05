using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Firme",
                columns: table => new
                {
                    FirmaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Adresa = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PttIMesto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SifraOpstine = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    SifraDelatnosti = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ZiroRacun = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PosebanRacun = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    PodracunPoslovneJedinice = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Pib = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    MaticniBroj = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    JbkjsBroj = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Zastupnik = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    FunkcijaZastupnika = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    SefApiKey = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    SefEnvironment = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PfrUrl = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    PfrPacKod = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PfrKasirName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PfrSimulatorMod = table.Column<bool>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firme", x => x.FirmaId);
                });

            migrationBuilder.CreateTable(
                name: "Konta",
                columns: table => new
                {
                    KontoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojKonta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NazivKonta = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VrstaKonta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsSintetika = table.Column<bool>(type: "INTEGER", nullable: false),
                    Klasa = table.Column<int>(type: "INTEGER", nullable: false),
                    StariKonto = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konta", x => x.KontoId);
                });

            migrationBuilder.CreateTable(
                name: "Korisnici",
                columns: table => new
                {
                    KorisnikId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LozinkaHash = table.Column<string>(type: "TEXT", nullable: false),
                    ImeIPrezime = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Uloga = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PoslednjaPrijava = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnici", x => x.KorisnikId);
                });

            migrationBuilder.CreateTable(
                name: "MestaTroska",
                columns: table => new
                {
                    MestoTroskaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tip = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAktivno = table.Column<bool>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MestaTroska", x => x.MestoTroskaId);
                });

            migrationBuilder.CreateTable(
                name: "Partneri",
                columns: table => new
                {
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraPartnera = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Adresa = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PttIMesto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Pib = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    MaticniBroj = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Jmbg = table.Column<string>(type: "TEXT", maxLength: 13, nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ZiroRacun = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BankovniRacun = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NazivBanke = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    KontoPartnera = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    JeDobavljac = table.Column<bool>(type: "INTEGER", nullable: false),
                    JeKupac = table.Column<bool>(type: "INTEGER", nullable: false),
                    JeRadnik = table.Column<bool>(type: "INTEGER", nullable: false),
                    JeBanka = table.Column<bool>(type: "INTEGER", nullable: false),
                    JePoreskaUprava = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partneri", x => x.PartnerId);
                });

            migrationBuilder.InsertData(
                table: "Korisnici",
                columns: new[] { "KorisnikId", "ImeIPrezime", "IsActive", "KorisnickoIme", "LozinkaHash", "PoslednjaPrijava", "Uloga" },
                values: new object[] { 1, "Administrator", true, "admin", "PBKDF2$100000$CnYWiALqycqWTueq6ayEvQ==$hvm9e8z3e+KVeRsego3azOuoTp3q64deikPgUB9/D4o=", null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_Firme_Sifra",
                table: "Firme",
                column: "Sifra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Konta_BrojKonta",
                table: "Konta",
                column: "BrojKonta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Korisnici_KorisnickoIme",
                table: "Korisnici",
                column: "KorisnickoIme",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MestaTroska_Sifra",
                table: "MestaTroska",
                column: "Sifra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partneri_SifraPartnera",
                table: "Partneri",
                column: "SifraPartnera",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Firme");

            migrationBuilder.DropTable(
                name: "Konta");

            migrationBuilder.DropTable(
                name: "Korisnici");

            migrationBuilder.DropTable(
                name: "MestaTroska");

            migrationBuilder.DropTable(
                name: "Partneri");
        }
    }
}
