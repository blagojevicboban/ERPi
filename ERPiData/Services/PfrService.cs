using System;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Finansije;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Servis za e-Fiskalizaciju (PFR/LPFR/VPFR) — orkestrira <see cref="PfrApiClient"/> nad
/// <see cref="PfrRacun"/> zapisima, čita PFR podešavanja iz aktivne <see cref="Models.Core.Firma"/>.
/// Portovan iz ERPiFinansijeData, prilagođen novom <see cref="ErpiDbContext"/>. ERPi-jev
/// <see cref="PfrRacun"/> je namerno pojednostavljen (nema stavke po artiklu kao izvorni
/// FiskalniRacunLog), pa se PFR zahtev gradi kao jedna stavka koja nosi ukupan iznos računa —
/// dovoljno za izdavanje fiskalnog računa, nedovoljno za PDV razrez po stopi po stavci
/// (vidi PLAN_NASTAVKA.md).
/// </summary>
public class PfrService
{
    private readonly ErpiDbContext _db;

    public PfrService(ErpiDbContext db)
    {
        _db = db;
    }

    private static PfrPostavke PostavkeIzFirme(Models.Core.Firma firma) => new()
    {
        PfrUrl = string.IsNullOrWhiteSpace(firma.PfrUrl) ? "http://localhost:8443" : firma.PfrUrl,
        PacKod = string.IsNullOrWhiteSpace(firma.PfrPacKod) ? "123456" : firma.PfrPacKod,
        Kasir = string.IsNullOrWhiteSpace(firma.PfrKasirName) ? "Glavni Kasir" : firma.PfrKasirName,
        SimulatorMod = firma.PfrSimulatorMod
    };

    public async Task<(bool Success, string Message)> TestPfrKonekcijuAsync()
    {
        var firma = await _db.Firme.AsNoTracking().FirstOrDefaultAsync();
        if (firma == null)
            return (false, "Podaci o vašoj firmi nisu pronađeni u bazi.");

        var client = new PfrApiClient();
        return await client.TestirajPfrKonekcijuAsync(PostavkeIzFirme(firma));
    }

    public async Task<(bool Success, string Message)> FiskalizujRacunAsync(int pfrRacunId)
    {
        var racun = await _db.PfrRacuni.FirstOrDefaultAsync(r => r.PfrRacunId == pfrRacunId);
        if (racun == null)
            return (false, "Račun nije pronađen u bazi.");

        var firma = await _db.Firme.AsNoTracking().FirstOrDefaultAsync();
        if (firma == null)
            return (false, "Podaci o vašoj firmi nisu pronađeni u bazi.");

        var postavke = PostavkeIzFirme(firma);

        var (invoiceType, transactionType) = racun.TipRacuna switch
        {
            "PrometRefunkcija" => ("Refund", "Refund"),
            _ => ("Normal", "Sale")
        };

        var zahtev = new PfrZahtev
        {
            InvoiceType = invoiceType,
            TransactionType = transactionType,
            Cashier = postavke.Kasir,
            Items =
            {
                new PfrZahtevStavka
                {
                    Name = string.IsNullOrWhiteSpace(racun.Napomena) ? racun.BrojRacuna : racun.Napomena,
                    Quantity = 1,
                    UnitPrice = racun.Iznos,
                    TotalAmount = racun.Iznos
                }
            },
            Payment =
            {
                new PfrZahtevPlacanje { Amount = racun.Iznos, PaymentType = "Cash" }
            }
        };

        var client = new PfrApiClient();
        var (success, simulacija, message, odgovor) = await client.FiskalizujRacunAsync(zahtev, postavke);

        if (success && odgovor != null)
        {
            racun.PfrBroj = odgovor.InvoiceNumber;
            racun.QrKodUrl = string.IsNullOrWhiteSpace(odgovor.VerificationUrl) ? null : odgovor.VerificationUrl;
            racun.Status = simulacija ? "Simulacija" : "Fiskalizovan";
            racun.Napomena = message;

            await _db.SaveChangesAsync();
            return (true, message);
        }

        racun.Status = "Greška";
        racun.Napomena = message;
        await _db.SaveChangesAsync();

        return (false, message);
    }

    /// <summary>
    /// Fiskalizuje Račun-otpremnicu (maloprodaja fizičkom licu) — za razliku od
    /// <see cref="FiskalizujRacunAsync"/> (koja gradi jednu lump-sum stavku za samostalni
    /// <see cref="PfrRacun"/>), ovde se PFR zahtev gradi sa PRAVOM stavkom po redu fakture
    /// (Artikal ili usluga), jer <see cref="RacunOtpremnica"/> već ima pravu listu stavki.
    /// Rezultat se upisuje u RacunOtpremnica.FiskalniBroj/FiskalniQrKod/FiskalniDatum.
    /// Isti PfrApiClient kao i za samostalne PfrRacun zapise — nema duplog HTTP klijenta.
    /// </summary>
    public async Task<(bool Success, string Message)> FiskalizujRacunOtpremnicuAsync(int racunOtpremnicaId)
    {
        var racun = await _db.RacuniOtpremnice
            .Include(r => r.Stavke).ThenInclude(s => s.Artikal)
            .Include(r => r.Partner)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);

        if (racun == null)
            return (false, "Račun nije pronađen u bazi.");

        if (racun.Stavke.Count == 0)
            return (false, "Račun nema nijednu stavku.");

        var firma = await _db.Firme.AsNoTracking().FirstOrDefaultAsync();
        if (firma == null)
            return (false, "Podaci o vašoj firmi nisu pronađeni u bazi.");

        var postavke = PostavkeIzFirme(firma);

        var zahtev = new PfrZahtev
        {
            InvoiceType = "Normal",
            TransactionType = "Sale",
            Cashier = postavke.Kasir,
        };
        foreach (var s in racun.Stavke)
        {
            zahtev.Items.Add(new PfrZahtevStavka
            {
                Name = s.Artikal?.Naziv ?? s.OpisUsluge ?? "Stavka",
                Quantity = s.Kolicina,
                UnitPrice = s.ProdajnaCena,
                TotalAmount = s.Ukupno
            });
        }
        zahtev.Payment.Add(new PfrZahtevPlacanje { Amount = racun.UkupnoZaUplatu, PaymentType = "Cash" });

        var client = new PfrApiClient();
        var (success, simulacija, message, odgovor) = await client.FiskalizujRacunAsync(zahtev, postavke);

        if (success && odgovor != null)
        {
            racun.FiskalniBroj = odgovor.InvoiceNumber;
            racun.FiskalniQrKod = string.IsNullOrWhiteSpace(odgovor.VerificationUrl) ? null : odgovor.VerificationUrl;
            racun.FiskalniDatum = DateTime.Now;

            await _db.SaveChangesAsync();
            return (true, simulacija ? $"[SIMULACIJA] {message}" : message);
        }

        return (false, message);
    }
}
