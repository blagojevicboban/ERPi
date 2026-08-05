using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class DodajPoreskeTarife : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoreskeTarife",
                columns: table => new
                {
                    PoreskaTarifaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TarifniBroj = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PorezProcenat = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    PosebanPorezProcenat = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    PorezUCeni = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoreskeTarife", x => x.PoreskaTarifaId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoreskeTarife");
        }
    }
}
