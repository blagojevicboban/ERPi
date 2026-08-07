using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajZavisneTroskoviUKalkulacije : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Iznos",
                table: "StavkeKalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NabavnaVrednost",
                table: "StavkeKalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorezIznos",
                table: "StavkeKalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorezProcenat",
                table: "StavkeKalkulacije",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProdajnaVrednost",
                table: "StavkeKalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProdajnaVrednostBezPoreza",
                table: "StavkeKalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RazlikaIznos",
                table: "StavkeKalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RazlikaProcenat",
                table: "StavkeKalkulacije",
                type: "decimal(18, 6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RedniBroj",
                table: "StavkeKalkulacije",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "StaraCena",
                table: "StavkeKalkulacije",
                type: "decimal(18, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Troskovi",
                table: "StavkeKalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BrojOtpremnice",
                table: "Kalkulacije",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrojRacuna",
                table: "Kalkulacije",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumOtpremnice",
                table: "Kalkulacije",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumRacuna",
                table: "Kalkulacije",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKnjizen",
                table: "Kalkulacije",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "KontoDobavljacaId",
                table: "Kalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarzaProcenat",
                table: "Kalkulacije",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NabavnaVrednost",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NalogId",
                table: "Kalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OstaliTroskovi",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PoreskaStopaProcenat",
                table: "Kalkulacije",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Porez",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProdajnaVrednost",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Razlika",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SvegaNabavno",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SvegaTroskovi",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportniTroskovi",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportnoOsiguranje",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TroskoviUskladistenja",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtovarIstovar",
                table: "Kalkulacije",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Kalkulacije_KontoDobavljacaId",
                table: "Kalkulacije",
                column: "KontoDobavljacaId");

            migrationBuilder.CreateIndex(
                name: "IX_Kalkulacije_NalogId",
                table: "Kalkulacije",
                column: "NalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kalkulacije_Konta_KontoDobavljacaId",
                table: "Kalkulacije",
                column: "KontoDobavljacaId",
                principalTable: "Konta",
                principalColumn: "KontoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kalkulacije_Nalozi_NalogId",
                table: "Kalkulacije",
                column: "NalogId",
                principalTable: "Nalozi",
                principalColumn: "NalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kalkulacije_Konta_KontoDobavljacaId",
                table: "Kalkulacije");

            migrationBuilder.DropForeignKey(
                name: "FK_Kalkulacije_Nalozi_NalogId",
                table: "Kalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_Kalkulacije_KontoDobavljacaId",
                table: "Kalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_Kalkulacije_NalogId",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "Iznos",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "NabavnaVrednost",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "PorezIznos",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "PorezProcenat",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "ProdajnaVrednost",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "ProdajnaVrednostBezPoreza",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "RazlikaIznos",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "RazlikaProcenat",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "RedniBroj",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "StaraCena",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "Troskovi",
                table: "StavkeKalkulacije");

            migrationBuilder.DropColumn(
                name: "BrojOtpremnice",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "BrojRacuna",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "DatumOtpremnice",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "DatumRacuna",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "IsKnjizen",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "KontoDobavljacaId",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "MarzaProcenat",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "NabavnaVrednost",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "NalogId",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "OstaliTroskovi",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "PoreskaStopaProcenat",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "Porez",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "ProdajnaVrednost",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "Razlika",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "SvegaNabavno",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "SvegaTroskovi",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "TransportniTroskovi",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "TransportnoOsiguranje",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "TroskoviUskladistenja",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "UtovarIstovar",
                table: "Kalkulacije");
        }
    }
}
