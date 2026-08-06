using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiData.Migrations
{
    /// <inheritdoc />
    public partial class PromeniPodrazumevanuLozinkuNaAdmin : Migration
    {
        private const string StaraLozinkaHash = "PBKDF2$100000$CnYWiALqycqWTueq6ayEvQ==$hvm9e8z3e+KVeRsego3azOuoTp3q64deikPgUB9/D4o=";
        private const string NovaLozinkaHash = "PBKDF2$100000$Q5qwVnp/FEFudiM3Pm6TDw==$aQ2LbPzbt/jHw+gAvQKPs3d2WbbeakD5KY5JR9Qx33E=";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Namerno NIJE migrationBuilder.UpdateData (bezuslovni UPDATE po KorisnikId) — to bi
            // vratilo lozinku na podrazumevanu i za firme gde je admin nalog već promenio svoju
            // pravu lozinku. Uslov na trenutni hash osigurava da se dira SAMO nalog koji i dalje
            // ima staru podrazumevanu (admin123) lozinku.
            migrationBuilder.Sql(
                $"UPDATE Korisnici SET LozinkaHash = '{NovaLozinkaHash}' " +
                $"WHERE KorisnikId = 1 AND LozinkaHash = '{StaraLozinkaHash}';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"UPDATE Korisnici SET LozinkaHash = '{StaraLozinkaHash}' " +
                $"WHERE KorisnikId = 1 AND LozinkaHash = '{NovaLozinkaHash}';");
        }
    }
}
