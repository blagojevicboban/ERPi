using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class PopraviRobnoFkVezama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SifraArtikla",
                table: "UlazStavke");

            migrationBuilder.DropColumn(
                name: "SifraMagacina",
                table: "UlazNalozi");

            migrationBuilder.DropColumn(
                name: "SifraArtikla",
                table: "TrebovanjeStavke");

            migrationBuilder.DropColumn(
                name: "SifraMagacina",
                table: "TrebovanjeNalozi");

            migrationBuilder.DropColumn(
                name: "SifraArtikla",
                table: "PrimopredajaStavke");

            migrationBuilder.DropColumn(
                name: "SifraMagacinaDaje",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropColumn(
                name: "SifraMagacinaPrima",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropColumn(
                name: "SifraArtikla",
                table: "MaloprodajneKalkulacijeStavke");

            migrationBuilder.DropColumn(
                name: "SifraDobavljaca",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropColumn(
                name: "SifraMagacinaDaje",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropColumn(
                name: "SifraMagacinaPrima",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.RenameColumn(
                name: "SifraProdavnice",
                table: "MaloprodajneKalkulacije",
                newName: "MagacinIdPrima");

            migrationBuilder.AddColumn<int>(
                name: "MaterijalId",
                table: "UlazStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MagacinId",
                table: "UlazNalozi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaterijalId",
                table: "TrebovanjeStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MagacinId",
                table: "TrebovanjeNalozi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaterijalId",
                table: "PrimopredajaStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MagacinIdDaje",
                table: "PrimopredajaNalozi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MagacinIdPrima",
                table: "PrimopredajaNalozi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArtikalId",
                table: "MaloprodajneKalkulacijeStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DobavljacId",
                table: "MaloprodajneKalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MagacinIdDaje",
                table: "MaloprodajneKalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UlazStavke_MaterijalId",
                table: "UlazStavke",
                column: "MaterijalId");

            migrationBuilder.CreateIndex(
                name: "IX_UlazNalozi_MagacinId",
                table: "UlazNalozi",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_TrebovanjeStavke_MaterijalId",
                table: "TrebovanjeStavke",
                column: "MaterijalId");

            migrationBuilder.CreateIndex(
                name: "IX_TrebovanjeNalozi_MagacinId",
                table: "TrebovanjeNalozi",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaStavke_MaterijalId",
                table: "PrimopredajaStavke",
                column: "MaterijalId");

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaNalozi_MagacinIdDaje",
                table: "PrimopredajaNalozi",
                column: "MagacinIdDaje");

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaNalozi_MagacinIdPrima",
                table: "PrimopredajaNalozi",
                column: "MagacinIdPrima");

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacijeStavke_ArtikalId",
                table: "MaloprodajneKalkulacijeStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacije_DobavljacId",
                table: "MaloprodajneKalkulacije",
                column: "DobavljacId");

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacije_MagacinIdDaje",
                table: "MaloprodajneKalkulacije",
                column: "MagacinIdDaje");

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacije_MagacinIdPrima",
                table: "MaloprodajneKalkulacije",
                column: "MagacinIdPrima");

            migrationBuilder.AddForeignKey(
                name: "FK_MaloprodajneKalkulacije_Magacini_MagacinIdDaje",
                table: "MaloprodajneKalkulacije",
                column: "MagacinIdDaje",
                principalTable: "Magacini",
                principalColumn: "MagacinId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaloprodajneKalkulacije_Magacini_MagacinIdPrima",
                table: "MaloprodajneKalkulacije",
                column: "MagacinIdPrima",
                principalTable: "Magacini",
                principalColumn: "MagacinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaloprodajneKalkulacije_Partneri_DobavljacId",
                table: "MaloprodajneKalkulacije",
                column: "DobavljacId",
                principalTable: "Partneri",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaloprodajneKalkulacijeStavke_Artikli_ArtikalId",
                table: "MaloprodajneKalkulacijeStavke",
                column: "ArtikalId",
                principalTable: "Artikli",
                principalColumn: "ArtikalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrimopredajaNalozi_Magacini_MagacinIdDaje",
                table: "PrimopredajaNalozi",
                column: "MagacinIdDaje",
                principalTable: "Magacini",
                principalColumn: "MagacinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrimopredajaNalozi_Magacini_MagacinIdPrima",
                table: "PrimopredajaNalozi",
                column: "MagacinIdPrima",
                principalTable: "Magacini",
                principalColumn: "MagacinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrimopredajaStavke_Materijali_MaterijalId",
                table: "PrimopredajaStavke",
                column: "MaterijalId",
                principalTable: "Materijali",
                principalColumn: "MaterijalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrebovanjeNalozi_Magacini_MagacinId",
                table: "TrebovanjeNalozi",
                column: "MagacinId",
                principalTable: "Magacini",
                principalColumn: "MagacinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrebovanjeStavke_Materijali_MaterijalId",
                table: "TrebovanjeStavke",
                column: "MaterijalId",
                principalTable: "Materijali",
                principalColumn: "MaterijalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UlazNalozi_Magacini_MagacinId",
                table: "UlazNalozi",
                column: "MagacinId",
                principalTable: "Magacini",
                principalColumn: "MagacinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UlazStavke_Materijali_MaterijalId",
                table: "UlazStavke",
                column: "MaterijalId",
                principalTable: "Materijali",
                principalColumn: "MaterijalId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaloprodajneKalkulacije_Magacini_MagacinIdDaje",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropForeignKey(
                name: "FK_MaloprodajneKalkulacije_Magacini_MagacinIdPrima",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropForeignKey(
                name: "FK_MaloprodajneKalkulacije_Partneri_DobavljacId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropForeignKey(
                name: "FK_MaloprodajneKalkulacijeStavke_Artikli_ArtikalId",
                table: "MaloprodajneKalkulacijeStavke");

            migrationBuilder.DropForeignKey(
                name: "FK_PrimopredajaNalozi_Magacini_MagacinIdDaje",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropForeignKey(
                name: "FK_PrimopredajaNalozi_Magacini_MagacinIdPrima",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropForeignKey(
                name: "FK_PrimopredajaStavke_Materijali_MaterijalId",
                table: "PrimopredajaStavke");

            migrationBuilder.DropForeignKey(
                name: "FK_TrebovanjeNalozi_Magacini_MagacinId",
                table: "TrebovanjeNalozi");

            migrationBuilder.DropForeignKey(
                name: "FK_TrebovanjeStavke_Materijali_MaterijalId",
                table: "TrebovanjeStavke");

            migrationBuilder.DropForeignKey(
                name: "FK_UlazNalozi_Magacini_MagacinId",
                table: "UlazNalozi");

            migrationBuilder.DropForeignKey(
                name: "FK_UlazStavke_Materijali_MaterijalId",
                table: "UlazStavke");

            migrationBuilder.DropIndex(
                name: "IX_UlazStavke_MaterijalId",
                table: "UlazStavke");

            migrationBuilder.DropIndex(
                name: "IX_UlazNalozi_MagacinId",
                table: "UlazNalozi");

            migrationBuilder.DropIndex(
                name: "IX_TrebovanjeStavke_MaterijalId",
                table: "TrebovanjeStavke");

            migrationBuilder.DropIndex(
                name: "IX_TrebovanjeNalozi_MagacinId",
                table: "TrebovanjeNalozi");

            migrationBuilder.DropIndex(
                name: "IX_PrimopredajaStavke_MaterijalId",
                table: "PrimopredajaStavke");

            migrationBuilder.DropIndex(
                name: "IX_PrimopredajaNalozi_MagacinIdDaje",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropIndex(
                name: "IX_PrimopredajaNalozi_MagacinIdPrima",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropIndex(
                name: "IX_MaloprodajneKalkulacijeStavke_ArtikalId",
                table: "MaloprodajneKalkulacijeStavke");

            migrationBuilder.DropIndex(
                name: "IX_MaloprodajneKalkulacije_DobavljacId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_MaloprodajneKalkulacije_MagacinIdDaje",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_MaloprodajneKalkulacije_MagacinIdPrima",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropColumn(
                name: "MaterijalId",
                table: "UlazStavke");

            migrationBuilder.DropColumn(
                name: "MagacinId",
                table: "UlazNalozi");

            migrationBuilder.DropColumn(
                name: "MaterijalId",
                table: "TrebovanjeStavke");

            migrationBuilder.DropColumn(
                name: "MagacinId",
                table: "TrebovanjeNalozi");

            migrationBuilder.DropColumn(
                name: "MaterijalId",
                table: "PrimopredajaStavke");

            migrationBuilder.DropColumn(
                name: "MagacinIdDaje",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropColumn(
                name: "MagacinIdPrima",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropColumn(
                name: "ArtikalId",
                table: "MaloprodajneKalkulacijeStavke");

            migrationBuilder.DropColumn(
                name: "DobavljacId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropColumn(
                name: "MagacinIdDaje",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.RenameColumn(
                name: "MagacinIdPrima",
                table: "MaloprodajneKalkulacije",
                newName: "SifraProdavnice");

            migrationBuilder.AddColumn<string>(
                name: "SifraArtikla",
                table: "UlazStavke",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraMagacina",
                table: "UlazNalozi",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraArtikla",
                table: "TrebovanjeStavke",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraMagacina",
                table: "TrebovanjeNalozi",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraArtikla",
                table: "PrimopredajaStavke",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraMagacinaDaje",
                table: "PrimopredajaNalozi",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraMagacinaPrima",
                table: "PrimopredajaNalozi",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraArtikla",
                table: "MaloprodajneKalkulacijeStavke",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifraDobavljaca",
                table: "MaloprodajneKalkulacije",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SifraMagacinaDaje",
                table: "MaloprodajneKalkulacije",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SifraMagacinaPrima",
                table: "MaloprodajneKalkulacije",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }
    }
}
