using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ERPiMigration.Legacy;

public class LegacyKonto
{
    public int KontoId { get; set; }
    public string BrojKonta { get; set; } = string.Empty;
    public string NazivKonta { get; set; } = string.Empty;
    public bool IsSintetika { get; set; }
    public string VrstaKonta { get; set; } = string.Empty;
}

public class LegacyPartner
{
    public int PartnerId { get; set; }
    public string SifraPartnera { get; set; } = string.Empty;
    public string Naziv { get; set; } = string.Empty;
    public string? Pib { get; set; }
    public string? MaticniBroj { get; set; }
    public string? Adresa { get; set; }
    public string? PttIMesto { get; set; }
    public string? Telefon { get; set; }
    public string? ZiroRacun { get; set; }
}

public class LegacyMagacin
{
    public int MagacinId { get; set; }
    public string SifraMagacina { get; set; } = string.Empty;
    public string NazivMagacina { get; set; } = string.Empty;
    public string VrstaMagacina { get; set; } = string.Empty;
    public string? OdgovornoLice { get; set; }
}

public class LegacyArtikal
{
    public int ArtikalId { get; set; }
    public string SifraArtikla { get; set; } = string.Empty;
    public string Naziv { get; set; } = string.Empty;
    public string JedinicaMere { get; set; } = "kom";
    public string? Barkod { get; set; }
    public decimal NabavnaCena { get; set; }
    public decimal ProdajnaCena { get; set; }
}

public class LegacyNalog
{
    public int NalogId { get; set; }
    public int BrojNaloga { get; set; }
    public DateTime DatumNaloga { get; set; }
    public string VrstaNaloga { get; set; } = string.Empty;
    public string? Opis { get; set; }
    public List<LegacyStavkaNaloga> Stavke { get; set; } = new();
}

public class LegacyStavkaNaloga
{
    public int StavkaId { get; set; }
    public int NalogId { get; set; }
    public string KontoBroj { get; set; } = string.Empty;
    public string? PartnerSifra { get; set; }
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public string? OpisStavke { get; set; }
    public DateTime? DatumDokumenta { get; set; }
    public string? BrojDokumenta { get; set; }
}

public class LegacyAccountingDbContext : DbContext
{
    public DbSet<LegacyKonto> Konta => Set<LegacyKonto>();
    public DbSet<LegacyPartner> Partneri => Set<LegacyPartner>();
    public DbSet<LegacyMagacin> Magacini => Set<LegacyMagacin>();
    public DbSet<LegacyArtikal> Artikli => Set<LegacyArtikal>();
    public DbSet<LegacyNalog> Nalozi => Set<LegacyNalog>();
    public DbSet<LegacyStavkaNaloga> StavkeNaloga => Set<LegacyStavkaNaloga>();

    public LegacyAccountingDbContext(DbContextOptions<LegacyAccountingDbContext> options) : base(options)
    {
    }

    public static LegacyAccountingDbContext Create(string dbPath)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LegacyAccountingDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new LegacyAccountingDbContext(optionsBuilder.Options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<LegacyKonto>().ToTable("Konta");
        modelBuilder.Entity<LegacyPartner>().ToTable("Partneri");
        modelBuilder.Entity<LegacyMagacin>().ToTable("Magacini");
        modelBuilder.Entity<LegacyArtikal>().ToTable("Artikli");
        modelBuilder.Entity<LegacyNalog>().ToTable("Nalozi");
        modelBuilder.Entity<LegacyStavkaNaloga>().ToTable("StavkeNaloga");
    }
}
