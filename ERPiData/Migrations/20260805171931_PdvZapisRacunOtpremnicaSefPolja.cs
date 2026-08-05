using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class PdvZapisRacunOtpremnicaSefPolja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PdvZapisi_Nalozi_NalogId",
                table: "PdvZapisi");

            migrationBuilder.DropForeignKey(
                name: "FK_PdvZapisi_Partneri_PartnerId",
                table: "PdvZapisi");

            migrationBuilder.DropIndex(
                name: "IX_PdvZapisi_NalogId",
                table: "PdvZapisi");

            migrationBuilder.DropIndex(
                name: "IX_PdvZapisi_PartnerId",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "IznosPdv",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "NalogId",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "Napomena",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "Osnovica",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "StopaPdv",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "Ukupno",
                table: "PdvZapisi");

            migrationBuilder.RenameColumn(
                name: "PartnerId",
                table: "PdvZapisi",
                newName: "IzvornoDokumentId");

            migrationBuilder.RenameColumn(
                name: "DatumPoreskogDogadjaja",
                table: "PdvZapisi",
                newName: "UkupnaNaknadaSaPdv");

            migrationBuilder.RenameColumn(
                name: "DatumDokumenta",
                table: "PdvZapisi",
                newName: "Pdv20");

            // NAPOMENA: FiskalniBroj/FiskalniDatum/FiskalniQrKod namerno NISU dodati ovde iako ih
            // model traži — te tri kolone su na realnim produkcionim bazama (ARHIBEL, PSSS PIROT)
            // već ranije dodate ručnim ALTER TABLE-om iz ErpiDbContext.EnsureDbSchemaUpdated (bez
            // prave EF migracije, vidi komentar tamo), pa bi AddColumn ovde pukao na "duplicate
            // column name" na tim bazama. EnsureDbSchemaUpdated i dalje idempotentno pokriva njih
            // troje (proverava PRAGMA table_info pre ALTER-a) — ne dupliraj tu logiku ovde.

            migrationBuilder.AddColumn<DateTime>(
                name: "SefDatumSlanja",
                table: "RacuniOtpremnice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SefId",
                table: "RacuniOtpremnice",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SefPoruka",
                table: "RacuniOtpremnice",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SefStatus",
                table: "RacuniOtpremnice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TipKnjige",
                table: "PdvZapisi",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumKnjizenja",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumRacuna",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "OslobodjenPromet",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Osnovica10",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Osnovica20",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PartnerNaziv",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PartnerPib",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Pdv10",
                table: "PdvZapisi",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RedniBroj",
                table: "PdvZapisi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DokumentiPrilozi",
                columns: table => new
                {
                    DokumentPrilogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    RacunOtpremnicaId = table.Column<int>(type: "INTEGER", nullable: true),
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivFajla = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    TipDokumenta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PutanjaFajla = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    VelicinaBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DatumPriloga = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentiPrilozi", x => x.DokumentPrilogId);
                    table.ForeignKey(
                        name: "FK_DokumentiPrilozi_Kalkulacije_KalkulacijaId",
                        column: x => x.KalkulacijaId,
                        principalTable: "Kalkulacije",
                        principalColumn: "KalkulacijaId");
                    table.ForeignKey(
                        name: "FK_DokumentiPrilozi_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                    table.ForeignKey(
                        name: "FK_DokumentiPrilozi_RacuniOtpremnice_RacunOtpremnicaId",
                        column: x => x.RacunOtpremnicaId,
                        principalTable: "RacuniOtpremnice",
                        principalColumn: "RacunOtpremnicaId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DokumentiPrilozi_KalkulacijaId",
                table: "DokumentiPrilozi",
                column: "KalkulacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentiPrilozi_NalogId",
                table: "DokumentiPrilozi",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentiPrilozi_RacunOtpremnicaId",
                table: "DokumentiPrilozi",
                column: "RacunOtpremnicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DokumentiPrilozi");

            migrationBuilder.DropColumn(
                name: "SefDatumSlanja",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "SefId",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "SefPoruka",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "SefStatus",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "DatumKnjizenja",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "DatumRacuna",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "OslobodjenPromet",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "Osnovica10",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "Osnovica20",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "PartnerNaziv",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "PartnerPib",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "Pdv10",
                table: "PdvZapisi");

            migrationBuilder.DropColumn(
                name: "RedniBroj",
                table: "PdvZapisi");

            migrationBuilder.RenameColumn(
                name: "UkupnaNaknadaSaPdv",
                table: "PdvZapisi",
                newName: "DatumPoreskogDogadjaja");

            migrationBuilder.RenameColumn(
                name: "Pdv20",
                table: "PdvZapisi",
                newName: "DatumDokumenta");

            migrationBuilder.RenameColumn(
                name: "IzvornoDokumentId",
                table: "PdvZapisi",
                newName: "PartnerId");

            migrationBuilder.AlterColumn<string>(
                name: "TipKnjige",
                table: "PdvZapisi",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<decimal>(
                name: "IznosPdv",
                table: "PdvZapisi",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NalogId",
                table: "PdvZapisi",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Napomena",
                table: "PdvZapisi",
                type: "TEXT",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Osnovica",
                table: "PdvZapisi",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StopaPdv",
                table: "PdvZapisi",
                type: "decimal(5, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Ukupno",
                table: "PdvZapisi",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_PdvZapisi_NalogId",
                table: "PdvZapisi",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PdvZapisi_PartnerId",
                table: "PdvZapisi",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PdvZapisi_Nalozi_NalogId",
                table: "PdvZapisi",
                column: "NalogId",
                principalTable: "Nalozi",
                principalColumn: "NalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_PdvZapisi_Partneri_PartnerId",
                table: "PdvZapisi",
                column: "PartnerId",
                principalTable: "Partneri",
                principalColumn: "PartnerId");
        }
    }
}
