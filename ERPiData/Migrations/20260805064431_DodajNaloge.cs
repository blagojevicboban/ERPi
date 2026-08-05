using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajNaloge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nalozi",
                columns: table => new
                {
                    NalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumNaloga = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VrstaNaloga = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumKnjizenja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IzvorModula = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    IzvorId = table.Column<int>(type: "INTEGER", nullable: true),
                    UkupnoDuguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoPotrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nalozi", x => x.NalogId);
                });

            migrationBuilder.CreateTable(
                name: "StavkeNaloga",
                columns: table => new
                {
                    StavkaNalogaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    KontoId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojDokumenta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DatumDokumenta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValutaDospela = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Duguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Potrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    MestoTroskaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Valuta = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    KursValute = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Osnovica = table.Column<decimal>(type: "decimal(18, 2)", nullable: true),
                    StopaPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StavkeNaloga", x => x.StavkaNalogaId);
                    table.ForeignKey(
                        name: "FK_StavkeNaloga_Konta_KontoId",
                        column: x => x.KontoId,
                        principalTable: "Konta",
                        principalColumn: "KontoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StavkeNaloga_MestaTroska_MestoTroskaId",
                        column: x => x.MestoTroskaId,
                        principalTable: "MestaTroska",
                        principalColumn: "MestoTroskaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StavkeNaloga_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StavkeNaloga_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StavkeNaloga_KontoId",
                table: "StavkeNaloga",
                column: "KontoId");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeNaloga_MestoTroskaId",
                table: "StavkeNaloga",
                column: "MestoTroskaId");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeNaloga_NalogId",
                table: "StavkeNaloga",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeNaloga_PartnerId",
                table: "StavkeNaloga",
                column: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StavkeNaloga");

            migrationBuilder.DropTable(
                name: "Nalozi");
        }
    }
}
