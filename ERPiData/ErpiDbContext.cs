using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;

namespace ERPiData;

/// <summary>
/// Objedinjeni DbContext za ceo ERP sistem — jedna baza po firmi (Faza 1). Trenutno sadrži
/// samo zajednička matična polja (Core schema); Finansije/Magacin, Osnovna sredstva i Zarade
/// dolaze u svojim fazama (3, 4, 5) kao dodatni DbSet-ovi i migracije nad istim kontekstom.
/// </summary>
public class ErpiDbContext : DbContext
{
    public ErpiDbContext(DbContextOptions<ErpiDbContext> options) : base(options) { }

    public DbSet<Firma> Firme => Set<Firma>();
    public DbSet<Korisnik> Korisnici => Set<Korisnik>();
    public DbSet<Partner> Partneri => Set<Partner>();
    public DbSet<Konto> Konta => Set<Konto>();
    public DbSet<MestoTroska> MestaTroska => Set<MestoTroska>();

    public DbSet<Nalog> Nalozi => Set<Nalog>();
    public DbSet<StavkaNaloga> StavkeNaloga => Set<StavkaNaloga>();
    public DbSet<ZatvaranjeStavke> ZatvaranjaStavki => Set<ZatvaranjeStavke>();

    /// <summary>
    /// Kreira DbContext nad zadatom SQLite bazom (jedna baza po firmi) i primenjuje EF Core
    /// migracije — kreira bazu od nule (uključujući seed admin naloga) ako ne postoji.
    /// </summary>
    public static ErpiDbContext Create(string dbPath)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ErpiDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        var ctx = new ErpiDbContext(optionsBuilder.Options);
        ctx.Database.Migrate();
        return ctx;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Firma>()
            .HasIndex(f => f.Sifra)
            .IsUnique();

        modelBuilder.Entity<Korisnik>()
            .HasIndex(k => k.KorisnickoIme)
            .IsUnique();

        modelBuilder.Entity<Partner>()
            .HasIndex(p => p.SifraPartnera)
            .IsUnique();

        modelBuilder.Entity<Konto>()
            .HasIndex(k => k.BrojKonta)
            .IsUnique();

        modelBuilder.Entity<MestoTroska>()
            .HasIndex(m => m.Sifra)
            .IsUnique();

        modelBuilder.Entity<Nalog>()
            .HasMany(n => n.Stavke)
            .WithOne(s => s.Nalog)
            .HasForeignKey(s => s.NalogId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, ne Cascade: konto/partner/mesto troška korišćeno u knjiženju se ne sme
        // obrisati ispod naloga koji na njega ukazuje (i istorijski nalozi moraju ostati čitljivi).
        modelBuilder.Entity<StavkaNaloga>()
            .HasOne(s => s.Konto)
            .WithMany()
            .HasForeignKey(s => s.KontoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StavkaNaloga>()
            .HasOne(s => s.Partner)
            .WithMany()
            .HasForeignKey(s => s.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StavkaNaloga>()
            .HasOne(s => s.MestoTroska)
            .WithMany()
            .HasForeignKey(s => s.MestoTroskaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict na obe strane: stavka koja je već zatvorena (delimično ili potpuno) se ne
        // sme obrisati bez prethodnog otkazivanja zatvaranja — inače bi "koliko je zatvoreno"
        // tiho izgubilo osnovu iz koje se računa.
        modelBuilder.Entity<ZatvaranjeStavke>()
            .HasOne(z => z.StavkaDuguje)
            .WithMany()
            .HasForeignKey(z => z.StavkaDugujeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ZatvaranjeStavke>()
            .HasOne(z => z.StavkaPotrazuje)
            .WithMany()
            .HasForeignKey(z => z.StavkaPotrazujeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ZatvaranjeStavke>()
            .HasOne(z => z.Korisnik)
            .WithMany()
            .HasForeignKey(z => z.KorisnikId)
            .OnDelete(DeleteBehavior.Restrict);

        // Podrazumevani administratorski nalog — isti obrazac (i ista lozinka "admin123") kao
        // u ERPiFinansije/ERPiZarade, da alati za pokretanje/testiranje rade bez izmene.
        // LoginWindow primorava promenu ove lozinke pre prvog stvarnog korišćenja (Faza 2).
        modelBuilder.Entity<Korisnik>().HasData(new Korisnik
        {
            KorisnikId = 1,
            KorisnickoIme = "admin",
            ImeIPrezime = "Administrator",
            LozinkaHash = "PBKDF2$100000$CnYWiALqycqWTueq6ayEvQ==$hvm9e8z3e+KVeRsego3azOuoTp3q64deikPgUB9/D4o=",
            Uloga = UlogaKorisnika.Administrator,
            IsActive = true
        });

        base.OnModelCreating(modelBuilder);
    }

    private const int PasswordSaltSize = 16;
    private const int PasswordHashSize = 32;
    private const int PasswordIterations = 100_000;

    public static string HashPassword(string password)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(PasswordSaltSize);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password, salt, PasswordIterations, System.Security.Cryptography.HashAlgorithmName.SHA256, PasswordHashSize);
        return $"PBKDF2${PasswordIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash) || !storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
            return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, expected.Length);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
