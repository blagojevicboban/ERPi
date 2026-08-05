using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajObrazunZarada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banke",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ZiroRacun = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banke", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bolovanja",
                columns: table => new
                {
                    BolovanjeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojRadnika = table.Column<int>(type: "INTEGER", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumPocetkaSprecenosti = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumOd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumDo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Osnov = table.Column<int>(type: "INTEGER", nullable: false),
                    PrvaIsplata = table.Column<bool>(type: "INTEGER", nullable: false),
                    BrojDoznake = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DatumUnosa = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bolovanja", x => x.BolovanjeId);
                });

            migrationBuilder.CreateTable(
                name: "Doprinosi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ProcRadn = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    ProcPosl = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    B60ProcR = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    B60ProcP = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    Bp60ProcP = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Bp60FProcP = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    PorProcP = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    NepProcP = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    InvProcP = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Svrha1 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Svrha2 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Primalac1 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Primalac2 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ZiroRacun = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ZiroRacP = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PozivNaB = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PozivNa2 = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SifPlac = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SifPlacP = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NajnizaOsnovica = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NajvisaOsnovica = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doprinosi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Isplate",
                columns: table => new
                {
                    IsplataId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    Rod = table.Column<int>(type: "INTEGER", nullable: false),
                    Vrsta = table.Column<int>(type: "INTEGER", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DatumIsplate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Isplate", x => x.IsplataId);
                });

            migrationBuilder.CreateTable(
                name: "Kategorije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Koeficijent = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    StopaPio = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    StopaZdravstvo = table.Column<decimal>(type: "decimal(6,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategorije", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KontaKnjizenja",
                columns: table => new
                {
                    KontoKnjizenjaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kljuc = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Konto = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Strana = table.Column<int>(type: "INTEGER", nullable: false),
                    Redosled = table.Column<int>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KontaKnjizenja", x => x.KontoKnjizenjaId);
                });

            migrationBuilder.CreateTable(
                name: "Normativi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    VrednostBoda = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Tip = table.Column<char>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Normativi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObracunAuditi",
                columns: table => new
                {
                    ObracunAuditId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojRadnika = table.Column<int>(type: "INTEGER", nullable: true),
                    ImeRadnika = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Akcija = table.Column<int>(type: "INTEGER", nullable: false),
                    KorisnikId = table.Column<int>(type: "INTEGER", nullable: true),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Detalji = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Vreme = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObracunAuditi", x => x.ObracunAuditId);
                });

            migrationBuilder.CreateTable(
                name: "ObracunVerzije",
                columns: table => new
                {
                    ObracunVerzijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    RadnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsplataId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrojRadnika = table.Column<int>(type: "INTEGER", nullable: false),
                    ImeRadnika = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Verzija = table.Column<int>(type: "INTEGER", nullable: false),
                    Razlog = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Vreme = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BioZakljucan = table.Column<bool>(type: "INTEGER", nullable: false),
                    BioStorniran = table.Column<bool>(type: "INTEGER", nullable: false),
                    Bruto = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PorezNaDohodak = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosiRadnik = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosiPoslodavac = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoIsplata = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Snimak = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObracunVerzije", x => x.ObracunVerzijaId);
                });

            migrationBuilder.CreateTable(
                name: "PlatniRazredi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R1 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R2 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R3 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R4 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R5 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R6 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R7 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R8 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    R9 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P1 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P2 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P3 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P4 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P5 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P6 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P7 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P8 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    P9 = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatniRazredi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoreskeOlaksice",
                columns: table => new
                {
                    PoreskaOlaksicaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PravniOsnov = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Mehanizam = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcenatPoreza = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcenatDoprinosa = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    VaziOd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VaziDo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Aktivna = table.Column<bool>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoreskeOlaksice", x => x.PoreskaOlaksicaId);
                });

            migrationBuilder.CreateTable(
                name: "PoreskeStope",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    GranjaOd = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    GranicaDo = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Stopa = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    FiksniIznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    GodisnjuVazenja = table.Column<int>(type: "INTEGER", nullable: false),
                    MesecVazenja = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoreskeStope", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Porezi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    Zarada = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    AkPorez = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    AkPorez2 = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    AkPorez3 = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    AkPorez4 = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Prvast = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Drugast = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Trecast = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    LinPorez3 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    SifPlac1 = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ZiroR1 = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PozivNa1 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PozivNa3 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Svrha1 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Svrha2 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Primalac1 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Primalac2 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    SifPlac2 = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ZiroR2 = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PozivNa2 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PozivNa4 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PosPorez = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Svrha3 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Svrha4 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Primalac3 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Primalac4 = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ProcDrzav = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcNocni = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcPreko = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcMinul = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcNedel = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcBolov = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcPlac = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcPlZa = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcInval = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    FondCasova = table.Column<int>(type: "INTEGER", nullable: false),
                    CasZaOb = table.Column<int>(type: "INTEGER", nullable: false),
                    VrBoda = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    ProcIzdrz = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Akont = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ProsBrut = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    TopliObrokCena = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Porezi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PppPdPrijave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    VrstaPrijave = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    KlijentskaOznaka = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatumPlacanja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VrstaIzmene = table.Column<int>(type: "INTEGER", nullable: false),
                    JipdKojiSeMenja = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BrojResenja = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OsnovIzmene = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojZaposlenih = table.Column<int>(type: "INTEGER", nullable: false),
                    ZbirPoreza = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    ZbirDoprinosa = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Jipd = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Bop = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IznosZaUplatu = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    RacunZaUplatu = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    ModelPozivaNaBroj = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    SvrhaUplate = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumPodnosenja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DatumStatusa = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PutanjaFajla = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PppPdPrijave", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Praznici",
                columns: table => new
                {
                    PraznikId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Neradni = table.Column<bool>(type: "INTEGER", nullable: false),
                    RucniUnos = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Praznici", x => x.PraznikId);
                });

            migrationBuilder.CreateTable(
                name: "Radnici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    MestoTroskaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojRadnika = table.Column<int>(type: "INTEGER", nullable: false),
                    ImeIPrezime = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Jmbg = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    MaticniBroj = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DatumRodjenja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MestoRodjenja = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    AdresaStanovanja = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Mesto = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SifraOpstine = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Lbo = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    DatumZaposlenja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DatumPrestanka = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Kategorija = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Radno_Mesto = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    BrojRadneJedinice = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraMestaTroska = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MinuliRadGodine = table.Column<int>(type: "INTEGER", nullable: false),
                    Koeficijent = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Koeficijent1 = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    OsnovnaPlata = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    StopaPio = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    StopaZdravstvo = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    StopaNezaposlenost = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    BankovniRacun = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    NazivBanke = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Aktivan = table.Column<bool>(type: "INTEGER", nullable: false),
                    VanRadnogOdnosa = table.Column<bool>(type: "INTEGER", nullable: false),
                    LicniOslobodjenje = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ProcenatPovracajaPoreza = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ProcenatPovracajaDoprinosa = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    OlaksicaVaziDo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Operativni = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DatumUnosa = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumIzmene = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Radnici", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Radnici_MestaTroska_MestoTroskaId",
                        column: x => x.MestoTroskaId,
                        principalTable: "MestaTroska",
                        principalColumn: "MestoTroskaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Radnici_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlanjaListica",
                columns: table => new
                {
                    SlanjeListicaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojRadnika = table.Column<int>(type: "INTEGER", nullable: false),
                    ImeRadnika = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Ishod = table.Column<int>(type: "INTEGER", nullable: false),
                    ZasticenLozinkom = table.Column<bool>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    KorisnikId = table.Column<int>(type: "INTEGER", nullable: true),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Vreme = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlanjaListica", x => x.SlanjeListicaId);
                });

            migrationBuilder.CreateTable(
                name: "VrstePrimanja",
                columns: table => new
                {
                    VrstaPrimanjaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Svp = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Oporezivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UlaziUOsnovicuDoprinosa = table.Column<bool>(type: "INTEGER", nullable: false),
                    NeoporeziviLimit = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Konto = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NaTeretFonda = table.Column<bool>(type: "INTEGER", nullable: false),
                    VecIsplacenoVanObracuna = table.Column<bool>(type: "INTEGER", nullable: false),
                    Redosled = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktivna = table.Column<bool>(type: "INTEGER", nullable: false),
                    JeSistemska = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VrstePrimanja", x => x.VrstaPrimanjaId);
                });

            migrationBuilder.CreateTable(
                name: "VrsteUgovora",
                columns: table => new
                {
                    VrstaUgovoraId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Ovp = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    NormiraniTroskoviProcenat = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StopaPoreza = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StopaPioPrimalac = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StopaZdravstvoPrimalac = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StopaNezaposlenostPrimalac = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StopaPioIsplatilac = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StopaZdravstvoIsplatilac = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StopaNezaposlenostIsplatilac = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Konto = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SifraPlacanja = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Redosled = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktivna = table.Column<bool>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VrsteUgovora", x => x.VrstaUgovoraId);
                });

            migrationBuilder.CreateTable(
                name: "OlaksicaMfp",
                columns: table => new
                {
                    OlaksicaMfpId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PoreskaOlaksicaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Oznaka = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Izvor = table.Column<int>(type: "INTEGER", nullable: false),
                    FiksnaVrednost = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OlaksicaMfp", x => x.OlaksicaMfpId);
                    table.ForeignKey(
                        name: "FK_OlaksicaMfp_PoreskeOlaksice_PoreskaOlaksicaId",
                        column: x => x.PoreskaOlaksicaId,
                        principalTable: "PoreskeOlaksice",
                        principalColumn: "PoreskaOlaksicaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoprinosiPoslodavca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RadnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    Zar1 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar2 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar3 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar4 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar5 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar6 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar7 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar8 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Zar9 = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoprinosiPoslodavca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoprinosiPoslodavca_Radnici_RadnikId",
                        column: x => x.RadnikId,
                        principalTable: "Radnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Krediti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RadnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    UkupanIznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    MesecnaRata = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    OstatakDuga = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BrojRata = table.Column<int>(type: "INTEGER", nullable: false),
                    PlateneRate = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumPocetka = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumZavrsetka = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Aktivan = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrimalacNaziv = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PrimalacRacun = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    ModelPozivaNaBroj = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PozivNaBroj = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    Tip = table.Column<int>(type: "INTEGER", nullable: false),
                    RedosledNaplate = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Krediti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Krediti_Radnici_RadnikId",
                        column: x => x.RadnikId,
                        principalTable: "Radnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RadniSati",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RadnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    IsplataId = table.Column<int>(type: "INTEGER", nullable: true),
                    RedovniSati = table.Column<int>(type: "INTEGER", nullable: false),
                    BolovanjeSati = table.Column<int>(type: "INTEGER", nullable: false),
                    PrekovremeneSati = table.Column<int>(type: "INTEGER", nullable: false),
                    GodisnjiOdmorSati = table.Column<int>(type: "INTEGER", nullable: false),
                    DrzavniPraznikSati = table.Column<int>(type: "INTEGER", nullable: false),
                    NocniSati = table.Column<int>(type: "INTEGER", nullable: false),
                    SmenskiSati = table.Column<int>(type: "INTEGER", nullable: false),
                    RadPraznikomSati = table.Column<int>(type: "INTEGER", nullable: false),
                    NocniRadPraznikomSati = table.Column<int>(type: "INTEGER", nullable: false),
                    PlacenoOdsustvoSati = table.Column<int>(type: "INTEGER", nullable: false),
                    Stimulacija = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    RadNedeljomSati = table.Column<int>(type: "INTEGER", nullable: false),
                    PlacenoZakonskiSati = table.Column<int>(type: "INTEGER", nullable: false),
                    BolovanjePreko60Sati = table.Column<int>(type: "INTEGER", nullable: false),
                    PorodiljskoOdsustvoSati = table.Column<int>(type: "INTEGER", nullable: false),
                    Bolovanje100Sati = table.Column<int>(type: "INTEGER", nullable: false),
                    TopliObrokDani = table.Column<int>(type: "INTEGER", nullable: false),
                    RegresIznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Prosek = table.Column<decimal>(type: "decimal(14,4)", nullable: false),
                    Varijabila = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadniSati", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadniSati_Isplate_IsplataId",
                        column: x => x.IsplataId,
                        principalTable: "Isplate",
                        principalColumn: "IsplataId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadniSati_Radnici_RadnikId",
                        column: x => x.RadnikId,
                        principalTable: "Radnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Samodoprinosi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RadnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samodoprinosi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Samodoprinosi_Radnici_RadnikId",
                        column: x => x.RadnikId,
                        principalTable: "Radnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnetaPrimanja",
                columns: table => new
                {
                    UnetoPrimanjeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RadnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    IsplataId = table.Column<int>(type: "INTEGER", nullable: true),
                    VrstaPrimanjaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnetaPrimanja", x => x.UnetoPrimanjeId);
                    table.ForeignKey(
                        name: "FK_UnetaPrimanja_Isplate_IsplataId",
                        column: x => x.IsplataId,
                        principalTable: "Isplate",
                        principalColumn: "IsplataId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnetaPrimanja_Radnici_RadnikId",
                        column: x => x.RadnikId,
                        principalTable: "Radnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnetaPrimanja_VrstePrimanja_VrstaPrimanjaId",
                        column: x => x.VrstaPrimanjaId,
                        principalTable: "VrstePrimanja",
                        principalColumn: "VrstaPrimanjaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SabloniUgovora",
                columns: table => new
                {
                    SablonUgovoraId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    VrstaUgovoraId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Redosled = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktivan = table.Column<bool>(type: "INTEGER", nullable: false),
                    JeSistemski = table.Column<bool>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SabloniUgovora", x => x.SablonUgovoraId);
                    table.ForeignKey(
                        name: "FK_SabloniUgovora_VrsteUgovora_VrstaUgovoraId",
                        column: x => x.VrstaUgovoraId,
                        principalTable: "VrsteUgovora",
                        principalColumn: "VrstaUgovoraId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Ugovori",
                columns: table => new
                {
                    UgovorId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VrstaUgovoraId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojRadnika = table.Column<int>(type: "INTEGER", nullable: false),
                    TipPrimaoca = table.Column<int>(type: "INTEGER", nullable: false),
                    Broj = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Predmet = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DatumZakljucenja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumOd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DatumDo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UgovorenIznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    IznosJeNeto = table.Column<bool>(type: "INTEGER", nullable: false),
                    Aktivan = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    DatumTeksta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DatumUnosa = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ugovori", x => x.UgovorId);
                    table.ForeignKey(
                        name: "FK_Ugovori_VrsteUgovora_VrstaUgovoraId",
                        column: x => x.VrstaUgovoraId,
                        principalTable: "VrsteUgovora",
                        principalColumn: "VrstaUgovoraId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObracuniPlata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RadnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Godina = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesec = table.Column<int>(type: "INTEGER", nullable: false),
                    IsplataId = table.Column<int>(type: "INTEGER", nullable: true),
                    Zakljucan = table.Column<bool>(type: "INTEGER", nullable: false),
                    UgovorId = table.Column<int>(type: "INTEGER", nullable: true),
                    OsnovicaDoprinosa = table.Column<decimal>(type: "decimal(14,2)", nullable: true),
                    Storniran = table.Column<bool>(type: "INTEGER", nullable: false),
                    DatumStorniranja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RazlogStorniranja = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Verzija = table.Column<int>(type: "INTEGER", nullable: false),
                    OlaksicaOznaka = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    OlaksicaPorez = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    OlaksicaDoprinosi = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    OlaksicaUmanjujeUplatu = table.Column<bool>(type: "INTEGER", nullable: false),
                    BrutoZarada = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BrutoBolovanje = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BrutoNaknade = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BrutoStimulacija = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BrutoMinuliRad = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoZar = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoNerd = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoGOd = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoTo = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoReg = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Neto = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoBol = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoB100 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoPlac = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoPlZ = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoDrza = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoNocni = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoVezba = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoPrek = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoTer = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    KorDod = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    KorDod1 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Kumul = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoNede = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosPioRadnik = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosZdravstvoRadnik = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosNezaposlenostRadnik = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosPioPoslodavac = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosZdravstvoPoslodavac = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DoprinosNezaposlenostPoslodavac = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PorezNaDohodak = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PoreskaOsnovica = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    LicniOdbitak = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    KreditObustava = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Samodoprinosi = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    OstaliOdbici = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoIsplata = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    RedovniSati = table.Column<int>(type: "INTEGER", nullable: false),
                    BolovanjeSati = table.Column<int>(type: "INTEGER", nullable: false),
                    PrekovremeneSati = table.Column<int>(type: "INTEGER", nullable: false),
                    GodisnjioOdmorSati = table.Column<int>(type: "INTEGER", nullable: false),
                    DrzavniPraznikSati = table.Column<int>(type: "INTEGER", nullable: false),
                    NocniSati = table.Column<int>(type: "INTEGER", nullable: false),
                    SmenskiSati = table.Column<int>(type: "INTEGER", nullable: false),
                    RadPraznikomSati = table.Column<int>(type: "INTEGER", nullable: false),
                    NocniRadPraznikomSati = table.Column<int>(type: "INTEGER", nullable: false),
                    PlacenoOdsustvoSati = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumObracuna = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Prosek = table.Column<decimal>(type: "decimal(14,4)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Koeficijent = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    MinuliRadGodine = table.Column<int>(type: "INTEGER", nullable: false),
                    Kategorija = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BrojRadneJedinice = table.Column<int>(type: "INTEGER", nullable: false),
                    UkupnoRadnihSatiLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    FondSatiMesecni = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    CenaSataRedovan = table.Column<decimal>(type: "decimal(14,5)", nullable: false),
                    CenaSataMinuliRad = table.Column<decimal>(type: "decimal(14,5)", nullable: false),
                    DodaciLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DodatakNaM1 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DodatakNaM2 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DodatakNaM3 = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BrutoOsnovica = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    TopliObrokIznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BrutoPioOsnovica = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoNaknadeLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Operativni = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Oznaka = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NedeljaSati = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    BolovanjePreko60SatiLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PorodiljskoOdsustvoSatiLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PlacenoOdsustvoSatiLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PlacenoZakonskiSatiLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Bolovanje100SatiLegacy = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    MinimalnaPlataOsnovica = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    SifraSamodoprinosa1 = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraSamodoprinosa2 = table.Column<int>(type: "INTEGER", nullable: false),
                    PosebanPorez = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoPorez = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetoBezPoreza = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Varijabila = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObracuniPlata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObracuniPlata_Isplate_IsplataId",
                        column: x => x.IsplataId,
                        principalTable: "Isplate",
                        principalColumn: "IsplataId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObracuniPlata_Radnici_RadnikId",
                        column: x => x.RadnikId,
                        principalTable: "Radnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObracuniPlata_Ugovori_UgovorId",
                        column: x => x.UgovorId,
                        principalTable: "Ugovori",
                        principalColumn: "UgovorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObracunStavke",
                columns: table => new
                {
                    ObracunStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObracunPlateId = table.Column<int>(type: "INTEGER", nullable: false),
                    VrstaPrimanjaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sati = table.Column<int>(type: "INTEGER", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    OporeziviDeo = table.Column<decimal>(type: "decimal(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObracunStavke", x => x.ObracunStavkaId);
                    table.ForeignKey(
                        name: "FK_ObracunStavke_ObracuniPlata_ObracunPlateId",
                        column: x => x.ObracunPlateId,
                        principalTable: "ObracuniPlata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObracunStavke_VrstePrimanja_VrstaPrimanjaId",
                        column: x => x.VrstaPrimanjaId,
                        principalTable: "VrstePrimanja",
                        principalColumn: "VrstaPrimanjaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bolovanja_BrojRadnika_Godina_Mesec_DatumOd",
                table: "Bolovanja",
                columns: new[] { "BrojRadnika", "Godina", "Mesec", "DatumOd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bolovanja_Godina_Mesec_BrojRadnika",
                table: "Bolovanja",
                columns: new[] { "Godina", "Mesec", "BrojRadnika" });

            migrationBuilder.CreateIndex(
                name: "IX_DoprinosiPoslodavca_RadnikId_Godina_Mesec",
                table: "DoprinosiPoslodavca",
                columns: new[] { "RadnikId", "Godina", "Mesec" });

            migrationBuilder.CreateIndex(
                name: "IX_Isplate_Godina_Mesec_RedniBroj",
                table: "Isplate",
                columns: new[] { "Godina", "Mesec", "RedniBroj" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KontaKnjizenja_Kljuc",
                table: "KontaKnjizenja",
                column: "Kljuc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Krediti_RadnikId",
                table: "Krediti",
                column: "RadnikId");

            migrationBuilder.CreateIndex(
                name: "IX_ObracunAuditi_Godina_Mesec_Vreme",
                table: "ObracunAuditi",
                columns: new[] { "Godina", "Mesec", "Vreme" });

            migrationBuilder.CreateIndex(
                name: "IX_ObracuniPlata_IsplataId",
                table: "ObracuniPlata",
                column: "IsplataId");

            migrationBuilder.CreateIndex(
                name: "IX_ObracuniPlata_RadnikId_Godina_Mesec",
                table: "ObracuniPlata",
                columns: new[] { "RadnikId", "Godina", "Mesec" });

            migrationBuilder.CreateIndex(
                name: "IX_ObracuniPlata_UgovorId",
                table: "ObracuniPlata",
                column: "UgovorId");

            migrationBuilder.CreateIndex(
                name: "IX_ObracunStavke_ObracunPlateId_VrstaPrimanjaId",
                table: "ObracunStavke",
                columns: new[] { "ObracunPlateId", "VrstaPrimanjaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObracunStavke_VrstaPrimanjaId",
                table: "ObracunStavke",
                column: "VrstaPrimanjaId");

            migrationBuilder.CreateIndex(
                name: "IX_ObracunVerzije_Godina_Mesec_BrojRadnika_Verzija",
                table: "ObracunVerzije",
                columns: new[] { "Godina", "Mesec", "BrojRadnika", "Verzija" });

            migrationBuilder.CreateIndex(
                name: "IX_OlaksicaMfp_PoreskaOlaksicaId_Oznaka",
                table: "OlaksicaMfp",
                columns: new[] { "PoreskaOlaksicaId", "Oznaka" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoreskeOlaksice_Sifra",
                table: "PoreskeOlaksice",
                column: "Sifra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PppPdPrijave_Godina_Mesec_RedniBroj",
                table: "PppPdPrijave",
                columns: new[] { "Godina", "Mesec", "RedniBroj" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Praznici_Datum",
                table: "Praznici",
                column: "Datum",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Radnici_BrojRadnika",
                table: "Radnici",
                column: "BrojRadnika");

            migrationBuilder.CreateIndex(
                name: "IX_Radnici_BrojRadnika_Godina_Mesec",
                table: "Radnici",
                columns: new[] { "BrojRadnika", "Godina", "Mesec" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Radnici_Godina_Mesec",
                table: "Radnici",
                columns: new[] { "Godina", "Mesec" });

            migrationBuilder.CreateIndex(
                name: "IX_Radnici_Jmbg",
                table: "Radnici",
                column: "Jmbg");

            migrationBuilder.CreateIndex(
                name: "IX_Radnici_MestoTroskaId",
                table: "Radnici",
                column: "MestoTroskaId");

            migrationBuilder.CreateIndex(
                name: "IX_Radnici_PartnerId",
                table: "Radnici",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RadniSati_IsplataId",
                table: "RadniSati",
                column: "IsplataId");

            migrationBuilder.CreateIndex(
                name: "IX_RadniSati_RadnikId_Godina_Mesec_IsplataId",
                table: "RadniSati",
                columns: new[] { "RadnikId", "Godina", "Mesec", "IsplataId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SabloniUgovora_Sifra",
                table: "SabloniUgovora",
                column: "Sifra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SabloniUgovora_VrstaUgovoraId",
                table: "SabloniUgovora",
                column: "VrstaUgovoraId");

            migrationBuilder.CreateIndex(
                name: "IX_Samodoprinosi_RadnikId",
                table: "Samodoprinosi",
                column: "RadnikId");

            migrationBuilder.CreateIndex(
                name: "IX_SlanjaListica_Godina_Mesec_BrojRadnika",
                table: "SlanjaListica",
                columns: new[] { "Godina", "Mesec", "BrojRadnika" });

            migrationBuilder.CreateIndex(
                name: "IX_Ugovori_BrojRadnika",
                table: "Ugovori",
                column: "BrojRadnika");

            migrationBuilder.CreateIndex(
                name: "IX_Ugovori_VrstaUgovoraId",
                table: "Ugovori",
                column: "VrstaUgovoraId");

            migrationBuilder.CreateIndex(
                name: "IX_UnetaPrimanja_IsplataId",
                table: "UnetaPrimanja",
                column: "IsplataId");

            migrationBuilder.CreateIndex(
                name: "IX_UnetaPrimanja_RadnikId_Godina_Mesec_VrstaPrimanjaId_IsplataId",
                table: "UnetaPrimanja",
                columns: new[] { "RadnikId", "Godina", "Mesec", "VrstaPrimanjaId", "IsplataId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnetaPrimanja_VrstaPrimanjaId",
                table: "UnetaPrimanja",
                column: "VrstaPrimanjaId");

            migrationBuilder.CreateIndex(
                name: "IX_VrstePrimanja_Sifra",
                table: "VrstePrimanja",
                column: "Sifra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VrsteUgovora_Sifra",
                table: "VrsteUgovora",
                column: "Sifra",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Banke");

            migrationBuilder.DropTable(
                name: "Bolovanja");

            migrationBuilder.DropTable(
                name: "Doprinosi");

            migrationBuilder.DropTable(
                name: "DoprinosiPoslodavca");

            migrationBuilder.DropTable(
                name: "Kategorije");

            migrationBuilder.DropTable(
                name: "KontaKnjizenja");

            migrationBuilder.DropTable(
                name: "Krediti");

            migrationBuilder.DropTable(
                name: "Normativi");

            migrationBuilder.DropTable(
                name: "ObracunAuditi");

            migrationBuilder.DropTable(
                name: "ObracunStavke");

            migrationBuilder.DropTable(
                name: "ObracunVerzije");

            migrationBuilder.DropTable(
                name: "OlaksicaMfp");

            migrationBuilder.DropTable(
                name: "PlatniRazredi");

            migrationBuilder.DropTable(
                name: "PoreskeStope");

            migrationBuilder.DropTable(
                name: "Porezi");

            migrationBuilder.DropTable(
                name: "PppPdPrijave");

            migrationBuilder.DropTable(
                name: "Praznici");

            migrationBuilder.DropTable(
                name: "RadniSati");

            migrationBuilder.DropTable(
                name: "SabloniUgovora");

            migrationBuilder.DropTable(
                name: "Samodoprinosi");

            migrationBuilder.DropTable(
                name: "SlanjaListica");

            migrationBuilder.DropTable(
                name: "UnetaPrimanja");

            migrationBuilder.DropTable(
                name: "ObracuniPlata");

            migrationBuilder.DropTable(
                name: "PoreskeOlaksice");

            migrationBuilder.DropTable(
                name: "VrstePrimanja");

            migrationBuilder.DropTable(
                name: "Isplate");

            migrationBuilder.DropTable(
                name: "Radnici");

            migrationBuilder.DropTable(
                name: "Ugovori");

            migrationBuilder.DropTable(
                name: "VrsteUgovora");
        }
    }
}
