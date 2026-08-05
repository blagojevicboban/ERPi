using System;
using System.Linq;
using System.Threading.Tasks;
using ERPiData.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace ERPiData.Services;

/// <summary>
/// Pretvara "sintetičkog" partnera (PartnerId=0, izveden direktno iz kontnog plana) u pravi red u tabeli Partneri,
/// i tom prilikom povezuje (backfill) sve dosadašnje stavke naloga tog konta koje još nemaju PartnerId.
/// Portovan iz ERPiFinansijeData, prilagođen za novi <see cref="ErpiDbContext"/> sa pravim FK vezama.
/// </summary>
public class PartnerPromocijaService
{
    private readonly ErpiDbContext _db;

    public PartnerPromocijaService(ErpiDbContext db)
    {
        _db = db;
    }

    public async Task<Partner> SacuvajPartneraAsync(int partnerId, string? brojKontaZaPromociju, Partner podaci)
    {
        Partner partner;

        if (partnerId > 0)
        {
            partner = await _db.Partneri.FirstOrDefaultAsync(p => p.PartnerId == partnerId)
                ?? throw new InvalidOperationException($"Partner sa ID {partnerId} ne postoji.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(brojKontaZaPromociju))
                throw new InvalidOperationException("Nedostaje konto za promociju sintetičkog partnera.");

            partner = await _db.Partneri.FirstOrDefaultAsync(p => p.SifraPartnera == brojKontaZaPromociju)
                ?? new Partner { SifraPartnera = brojKontaZaPromociju };

            if (partner.PartnerId == 0)
            {
                _db.Partneri.Add(partner);
            }
        }

        partner.Naziv = podaci.Naziv;
        partner.Adresa = podaci.Adresa;
        partner.PttIMesto = podaci.PttIMesto;
        partner.Pib = podaci.Pib;
        partner.MaticniBroj = podaci.MaticniBroj;
        partner.Telefon = podaci.Telefon;
        partner.ZiroRacun = podaci.ZiroRacun;
        partner.KontoPartnera = podaci.KontoPartnera;

        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(brojKontaZaPromociju))
        {
            var konto = await _db.Konta.FirstOrDefaultAsync(k => k.BrojKonta == brojKontaZaPromociju);
            if (konto != null)
            {
                await _db.StavkeNaloga
                    .Where(s => s.PartnerId == null && s.KontoId == konto.KontoId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.PartnerId, partner.PartnerId));
            }
        }

        return partner;
    }
}
