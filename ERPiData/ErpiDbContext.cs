using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ERPiData.Models.Core;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using ERPiData.Models.Zarade;
using ERPiData.Seeds.Zarade;

namespace ERPiData;

/// <summary>
/// Objedinjeni DbContext za ceo ERP sistem — jedna baza po firmi (Faza 1). Trenutno sadrži
/// Core, Finansije, Magacin i Zarade šeme i migracije nad istim kontekstom.
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
    public DbSet<KamatnaStopa> KamatneStope => Set<KamatnaStopa>();

    public DbSet<BlagajnickiNalog> BlagajnickiNalozi => Set<BlagajnickiNalog>();
    public DbSet<PutniNalog> PutniNalozi => Set<PutniNalog>();
    public DbSet<PutniNalogTrosakStavka> PutniNaloziTroskoviStavke => Set<PutniNalogTrosakStavka>();
    public DbSet<NeoporeziviIznosDnevnice> NeoporeziviIznosiDnevnice => Set<NeoporeziviIznosDnevnice>();
    public DbSet<Kompenzacija> Kompenzacije => Set<Kompenzacija>();
    public DbSet<KompenzacijaStavka> KompenzacijeStavke => Set<KompenzacijaStavka>();
    public DbSet<KursnaListaStavka> KursneListeStavke => Set<KursnaListaStavka>();

    public DbSet<Magacin> Magacini => Set<Magacin>();
    public DbSet<Artikal> Artikli => Set<Artikal>();
    public DbSet<Kalkulacija> Kalkulacije => Set<Kalkulacija>();
    public DbSet<StavkaKalkulacije> StavkeKalkulacije => Set<StavkaKalkulacije>();
    public DbSet<MaloprodajnaKalkulacija> MaloprodajneKalkulacije => Set<MaloprodajnaKalkulacija>();
    public DbSet<MaloprodajnaKalkulacijaStavka> MaloprodajneKalkulacijeStavke => Set<MaloprodajnaKalkulacijaStavka>();
    public DbSet<UvoznaKalkulacija> UvozneKalkulacije => Set<UvoznaKalkulacija>();
    public DbSet<UvoznaStavka> UvozneStavke => Set<UvoznaStavka>();
    public DbSet<NivelacijaCena> NivelacijeCena => Set<NivelacijaCena>();
    public DbSet<NivelacijaStavka> NivelacijeStavke => Set<NivelacijaStavka>();
    public DbSet<Materijal> Materijali => Set<Materijal>();
    public DbSet<MaterijalnaKartica> MaterijalneKartice => Set<MaterijalnaKartica>();
    public DbSet<TrebovanjeNalog> TrebovanjeNalozi => Set<TrebovanjeNalog>();
    public DbSet<TrebovanjeStavka> TrebovanjeStavke => Set<TrebovanjeStavka>();
    public DbSet<UlazNalog> UlazNalozi => Set<UlazNalog>();
    public DbSet<UlazStavka> UlazStavke => Set<UlazStavka>();
    public DbSet<PrimopredajaNalog> PrimopredajaNalozi => Set<PrimopredajaNalog>();
    public DbSet<PrimopredajaStavka> PrimopredajaStavke => Set<PrimopredajaStavka>();
    public DbSet<PdvZapis> PdvZapisi => Set<PdvZapis>();
    public DbSet<DokumentPrilog> DokumentiPrilozi => Set<DokumentPrilog>();
    public DbSet<SefDokument> SefDokumenti => Set<SefDokument>();
    public DbSet<PfrRacun> PfrRacuni => Set<PfrRacun>();
    public DbSet<RacunOtpremnica> RacuniOtpremnice => Set<RacunOtpremnica>();
    public DbSet<RacunOtpremnicaStavka> RacunOtpremnicaStavke => Set<RacunOtpremnicaStavka>();

    // ── Zarade ────────────────────────────────────────────────────────
    public DbSet<Radnik> Radnici => Set<Radnik>();
    public DbSet<ObracunPlate> ObracuniPlata => Set<ObracunPlate>();
    public DbSet<Kredit> Krediti => Set<Kredit>();
    public DbSet<RadniSat> RadniSati => Set<RadniSat>();
    public DbSet<PoreznaStopa> PoreskeStope => Set<PoreznaStopa>();
    public DbSet<Kategorija> Kategorije => Set<Kategorija>();
    public DbSet<Samodoprinosi> Samodoprinosi => Set<Samodoprinosi>();
    public DbSet<Normativ> Normativi => Set<Normativ>();
    public DbSet<Porezi> Porezi => Set<Porezi>();
    public DbSet<Doprinos> Doprinosi => Set<Doprinos>();
    public DbSet<PlatniRazred> PlatniRazredi => Set<PlatniRazred>();
    public DbSet<DoprinosiPoslodavca> DoprinosiPoslodavca => Set<DoprinosiPoslodavca>();
    public DbSet<Banka> Banke => Set<Banka>();
    public DbSet<PppPdPrijava> PppPdPrijave => Set<PppPdPrijava>();
    public DbSet<ObracunAudit> ObracunAuditi => Set<ObracunAudit>();
    public DbSet<ObracunVerzija> ObracunVerzije => Set<ObracunVerzija>();
    public DbSet<SlanjeListica> SlanjaListica => Set<SlanjeListica>();
    public DbSet<Praznik> Praznici => Set<Praznik>();
    public DbSet<VrstaPrimanja> VrstePrimanja => Set<VrstaPrimanja>();
    public DbSet<ObracunStavka> ObracunStavke => Set<ObracunStavka>();
    public DbSet<UnetoPrimanje> UnetaPrimanja => Set<UnetoPrimanje>();
    public DbSet<PoreskaOlaksica> PoreskeOlaksice => Set<PoreskaOlaksica>();
    public DbSet<OlaksicaMfp> OlaksicaMfpDeklaracije => Set<OlaksicaMfp>();
    public DbSet<Isplata> Isplate => Set<Isplata>();
    public DbSet<VrstaUgovora> VrsteUgovora => Set<VrstaUgovora>();
    public DbSet<Ugovor> Ugovori => Set<Ugovor>();
    public DbSet<SablonUgovora> SabloniUgovora => Set<SablonUgovora>();
    public DbSet<KontoKnjizenja> KontaKnjizenja => Set<KontoKnjizenja>();
    public DbSet<Bolovanje> Bolovanja => Set<Bolovanje>();

    /// <summary>
    /// Kreira DbContext nad zadatom SQLite bazom (jedna baza po firmi) i primenjuje EF Core
    /// migracije — kreira bazu od nule (uključujući seed admin naloga) ako ne postoji.
    /// </summary>
    public static ErpiDbContext Create(string dbPath)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ErpiDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        var ctx = new ErpiDbContext(optionsBuilder.Options);
        try
        {
            ctx.Database.Migrate();
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("already exists"))
        {
            // Baza je kreirana sa EnsureCreated ili uvozom pa tabela već postoji.
            // NAPOMENA: uslov je namerno sveden samo na poruku "already exists" — širi uslov
            // `ex.SqliteErrorCode == 1` (SQLITE_ERROR, generički kod koji pokriva skoro svaku
            // SQL grešku) je ranije nečujno gutao STVARNE greške migracije (npr. "baza nema
            // kolonu X posle dodavanja migracije") kad baza nema urednu __EFMigrationsHistory
            // istoriju (npr. kreirana van EF migracija) — uhvaćeno pri uvozu u pravu produkcionu
            // firmu (PSSS PIROT): DopuniDoprinosiPoslodavcaKolone migracija se tiho "primenila"
            // a zapravo nikad nije, sve dok insert nije pukao na "no column named B601".
        }
        catch (System.Exception ex) when (ex.Message.Contains("already exists"))
        {
            // Fallback za obvijene izuzetke
        }

        EnsureDbSchemaUpdated(ctx);
        return ctx;
    }

    public static void EnsureDbSchemaUpdated(ErpiDbContext ctx)
    {
        try
        {
            var connection = ctx.Database.GetDbConnection();
            bool wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen) connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                // 1. Provera RacuniOtpremnice
                cmd.CommandText = "PRAGMA table_info(RacuniOtpremnice);";
                var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingColumns.Add(reader.GetString(1));
                    }
                }

                if (existingColumns.Count > 0)
                {
                    EnsureColumn(connection, "RacuniOtpremnice", "FiskalniBroj", "TEXT NULL", existingColumns);
                    EnsureColumn(connection, "RacuniOtpremnice", "FiskalniQrKod", "TEXT NULL", existingColumns);
                    EnsureColumn(connection, "RacuniOtpremnice", "FiskalniDatum", "TEXT NULL", existingColumns);
                    EnsureColumn(connection, "RacuniOtpremnice", "BrojOtpremnice", "TEXT NULL", existingColumns);
                    EnsureColumn(connection, "RacuniOtpremnice", "KontoKupcaId", "INTEGER NULL", existingColumns);
                    EnsureColumn(connection, "RacuniOtpremnice", "RokPlacanjaDana", "INTEGER NOT NULL DEFAULT 15", existingColumns);
                    EnsureColumn(connection, "RacuniOtpremnice", "NacinPlacanja", "TEXT NULL", existingColumns);
                }

                // 2. Provera StavkeNaloga
                cmd.CommandText = "PRAGMA table_info(StavkeNaloga);";
                existingColumns.Clear();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingColumns.Add(reader.GetString(1));
                    }
                }

                if (existingColumns.Count > 0)
                {
                    EnsureColumn(connection, "StavkeNaloga", "Osnovica", "TEXT NULL", existingColumns);
                    EnsureColumn(connection, "StavkeNaloga", "StopaPdv", "TEXT NULL", existingColumns);
                }
            }

            if (!wasOpen) connection.Close();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri sinhronizaciji šeme baze: {ex.Message}");
        }
    }

    private static void EnsureColumn(System.Data.Common.DbConnection conn, string table, string column, string columnDef, HashSet<string> existing)
    {
        if (!existing.Contains(column))
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {columnDef};";
            alterCmd.ExecuteNonQuery();
        }
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

        modelBuilder.Entity<Magacin>()
            .HasIndex(m => m.SifraMagacina)
            .IsUnique();

        modelBuilder.Entity<Artikal>()
            .HasIndex(a => a.SifraArtikla)
            .IsUnique();

        modelBuilder.Entity<Kalkulacija>()
            .HasMany(k => k.Stavke)
            .WithOne(s => s.Kalkulacija)
            .HasForeignKey(s => s.KalkulacijaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Kalkulacija>()
            .HasOne(k => k.Magacin)
            .WithMany()
            .HasForeignKey(k => k.MagacinId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Kalkulacija>()
            .HasOne(k => k.Partner)
            .WithMany()
            .HasForeignKey(k => k.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StavkaKalkulacije>()
            .HasOne(s => s.Artikal)
            .WithMany()
            .HasForeignKey(s => s.ArtikalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RacunOtpremnica>()
            .HasMany(r => r.Stavke)
            .WithOne(s => s.RacunOtpremnica)
            .HasForeignKey(s => s.RacunOtpremnicaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RacunOtpremnicaStavka>()
            .HasOne(s => s.Artikal)
            .WithMany()
            .HasForeignKey(s => s.ArtikalId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Podrazumevani administratorski nalog
        modelBuilder.Entity<Korisnik>().HasData(new Korisnik
        {
            KorisnikId = 1,
            KorisnickoIme = "admin",
            ImeIPrezime = "Administrator",
            LozinkaHash = "PBKDF2$100000$CnYWiALqycqWTueq6ayEvQ==$hvm9e8z3e+KVeRsego3azOuoTp3q64deikPgUB9/D4o=",
            Uloga = UlogaKorisnika.Administrator,
            IsActive = true
        });

        // ── Zarade Relacije i Indeksi ───────────────────────────────────
        modelBuilder.Entity<Radnik>()
            .HasOne(r => r.Partner)
            .WithMany()
            .HasForeignKey(r => r.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Radnik>()
            .HasOne(r => r.MestoTroska)
            .WithMany()
            .HasForeignKey(r => r.MestoTroskaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ObracunPlate>()
            .HasOne(o => o.Radnik)
            .WithMany(r => r.Obracuni)
            .HasForeignKey(o => o.RadnikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Kredit>()
            .HasOne(k => k.Radnik)
            .WithMany(r => r.Krediti)
            .HasForeignKey(k => k.RadnikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RadniSat>()
            .HasOne(rs => rs.Radnik)
            .WithMany(r => r.RadniSati)
            .HasForeignKey(rs => rs.RadnikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Radnik>()
            .HasIndex(r => new { r.BrojRadnika, r.Godina, r.Mesec })
            .IsUnique();

        modelBuilder.Entity<Radnik>()
            .HasIndex(r => new { r.Godina, r.Mesec });

        modelBuilder.Entity<Radnik>()
            .HasIndex(r => r.Jmbg);

        modelBuilder.Entity<Radnik>()
            .HasIndex(r => r.BrojRadnika);

        modelBuilder.Entity<ObracunPlate>()
            .HasIndex(o => new { o.RadnikId, o.Godina, o.Mesec });

        modelBuilder.Entity<RadniSat>()
            .HasIndex(rs => new { rs.RadnikId, rs.Godina, rs.Mesec, rs.IsplataId })
            .IsUnique();

        modelBuilder.Entity<RadniSat>()
            .HasOne(rs => rs.Isplata)
            .WithMany()
            .HasForeignKey(rs => rs.IsplataId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoprinosiPoslodavca>()
            .HasOne(dp => dp.Radnik)
            .WithMany()
            .HasForeignKey(dp => dp.RadnikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoprinosiPoslodavca>()
            .HasIndex(dp => new { dp.RadnikId, dp.Godina, dp.Mesec });

        modelBuilder.Entity<PppPdPrijava>()
            .HasIndex(p => new { p.Godina, p.Mesec, p.RedniBroj })
            .IsUnique();

        modelBuilder.Entity<ObracunAudit>()
            .HasIndex(a => new { a.Godina, a.Mesec, a.Vreme });

        modelBuilder.Entity<ObracunVerzija>()
            .HasIndex(v => new { v.Godina, v.Mesec, v.BrojRadnika, v.Verzija });

        modelBuilder.Entity<SlanjeListica>()
            .HasIndex(s => new { s.Godina, s.Mesec, s.BrojRadnika });

        modelBuilder.Entity<Praznik>()
            .HasIndex(p => p.Datum)
            .IsUnique();

        modelBuilder.Entity<VrstaPrimanja>()
            .HasIndex(v => v.Sifra)
            .IsUnique();

        modelBuilder.Entity<ObracunStavka>()
            .HasOne(s => s.Obracun)
            .WithMany(o => o.Stavke)
            .HasForeignKey(s => s.ObracunPlateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ObracunStavka>()
            .HasOne(s => s.VrstaPrimanja)
            .WithMany(v => v.Stavke)
            .HasForeignKey(s => s.VrstaPrimanjaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ObracunStavka>()
            .HasIndex(s => new { s.ObracunPlateId, s.VrstaPrimanjaId })
            .IsUnique();

        modelBuilder.Entity<UnetoPrimanje>()
            .HasIndex(p => new { p.RadnikId, p.Godina, p.Mesec, p.VrstaPrimanjaId, p.IsplataId })
            .IsUnique();

        modelBuilder.Entity<UnetoPrimanje>()
            .HasOne(p => p.Radnik)
            .WithMany()
            .HasForeignKey(p => p.RadnikId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UnetoPrimanje>()
            .HasOne(p => p.VrstaPrimanja)
            .WithMany()
            .HasForeignKey(p => p.VrstaPrimanjaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UnetoPrimanje>()
            .HasOne(p => p.Isplata)
            .WithMany()
            .HasForeignKey(p => p.IsplataId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PoreskaOlaksica>()
            .HasIndex(o => o.Sifra)
            .IsUnique();

        modelBuilder.Entity<OlaksicaMfp>()
            .HasOne(m => m.Olaksica)
            .WithMany(o => o.MfpDeklaracije)
            .HasForeignKey(m => m.PoreskaOlaksicaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OlaksicaMfp>()
            .HasIndex(m => new { m.PoreskaOlaksicaId, m.Oznaka })
            .IsUnique();

        modelBuilder.Entity<ObracunPlate>()
            .HasOne(o => o.Isplata)
            .WithMany(i => i.Obracuni)
            .HasForeignKey(o => o.IsplataId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Isplata>()
            .HasIndex(i => new { i.Godina, i.Mesec, i.RedniBroj })
            .IsUnique();

        modelBuilder.Entity<VrstaUgovora>()
            .HasIndex(v => v.Sifra)
            .IsUnique();

        modelBuilder.Entity<Ugovor>()
            .HasOne(u => u.VrstaUgovora)
            .WithMany(v => v.Ugovori)
            .HasForeignKey(u => u.VrstaUgovoraId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ugovor>()
            .HasIndex(u => u.BrojRadnika);

        modelBuilder.Entity<ObracunPlate>()
            .HasOne(o => o.Ugovor)
            .WithMany(u => u.Obracuni)
            .HasForeignKey(o => o.UgovorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SablonUgovora>()
            .HasIndex(s => s.Sifra)
            .IsUnique();

        modelBuilder.Entity<SablonUgovora>()
            .HasOne(s => s.VrstaUgovora)
            .WithMany()
            .HasForeignKey(s => s.VrstaUgovoraId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KontoKnjizenja>()
            .HasIndex(k => k.Kljuc)
            .IsUnique();

        modelBuilder.Entity<Bolovanje>()
            .HasIndex(b => new { b.Godina, b.Mesec, b.BrojRadnika });

        modelBuilder.Entity<Bolovanje>()
            .HasIndex(b => new { b.BrojRadnika, b.Godina, b.Mesec, b.DatumOd })
            .IsUnique();

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
