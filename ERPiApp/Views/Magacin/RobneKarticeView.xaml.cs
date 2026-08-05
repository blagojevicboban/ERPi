using System;
using System.Linq;
using System.Windows.Controls;
using ERPiData;
using ERPiData.Models.Magacin;
using Microsoft.EntityFrameworkCore;

namespace ERPiApp.Views.Magacin;

/// <summary>
/// Robna kartica (pojedinačna analitička kartica po artiklu) — master-detail: magacin + lista
/// artikala levo, hronologija promena po izabranom artiklu desno. Čita <see cref="MaterijalnaKartica"/>
/// tabelu (knjigovodstveno zajednička za Robno i Materijalno, vidi <see cref="RobniBrutoBilansService"/>),
/// ali ISKLJUČIVO nad <see cref="Artikal"/> šifarnikom — Materijalna strana ima svoj pandan
/// (planirani <c>MaterijalneKarticeView</c>, još nije portovan, vidi PLAN_NASTAVKA.md §3g/§3i).
/// Namerno ne poziva <c>MaterijalnaKarticaService</c> (Materijal-specifičan servis) da se izbegne
/// mešanje Robno/Materijalno slojeva — upit ide direktno nad deljenom tabelom.
/// </summary>
public partial class RobneKarticeView : UserControl
{
    private readonly ErpiDbContext _db;

    public RobneKarticeView(ErpiDbContext db)
    {
        InitializeComponent();
        _db = db;
        // Postavlja se posle InitializeComponent I posle dodele _db — Checked handler
        // (ChkSamoSaKarticom_Changed) čita _db, vidi PLAN_NASTAVKA.md §2 (IsChecked gotcha).
        ChkSamoSaKarticom.IsChecked = true;
        UcitajMagacine();
    }

    private void UcitajMagacine()
    {
        var magacini = _db.Magacini.AsNoTracking().OrderBy(m => m.SifraMagacina).ToList();
        CmbMagacin.ItemsSource = magacini;
        if (magacini.Count > 0) CmbMagacin.SelectedIndex = 0;
    }

    private void UcitajArtikle()
    {
        if (CmbMagacin.SelectedItem is not ERPiData.Models.Magacin.Magacin magacin)
        {
            LstArtikli.ItemsSource = null;
            return;
        }

        var upit = _db.Artikli.AsNoTracking().AsQueryable();

        var pretraga = TxtPretragaArtikla.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(pretraga))
        {
            upit = upit.Where(a => a.SifraArtikla.Contains(pretraga) || a.Naziv.Contains(pretraga));
        }

        var artikli = upit.OrderBy(a => a.Naziv).ToList();

        if (ChkSamoSaKarticom.IsChecked == true)
        {
            var sifreSaKarticom = _db.MaterijalneKartice
                .Where(k => k.SifraMagacina == magacin.SifraMagacina)
                .Select(k => k.SifraArtikla)
                .Distinct()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            artikli = artikli.Where(a => sifreSaKarticom.Contains(a.SifraArtikla)).ToList();
        }

        LstArtikli.ItemsSource = artikli;
    }

    private void UcitajKarticu()
    {
        if (CmbMagacin.SelectedItem is not ERPiData.Models.Magacin.Magacin magacin || LstArtikli.SelectedItem is not Artikal artikal)
        {
            TxtNaslovArtikla.Text = "Izaberite magacin i artikal sa leve strane";
            TxtStanjeArtikla.Text = " ";
            DgKartica.ItemsSource = null;
            TxtSumaUlaz.Text = "0,00";
            TxtSumaIzlaz.Text = "0,00";
            TxtSumaSaldo.Text = "0,00";
            return;
        }

        var kartice = _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == magacin.SifraMagacina && k.SifraArtikla == artikal.SifraArtikla)
            .OrderBy(k => k.DatumPromene)
            .ThenBy(k => k.RedniBroj)
            .ToList();

        DgKartica.ItemsSource = kartice;

        TxtNaslovArtikla.Text = $"{artikal.Prikaz} — {magacin.NazivMagacina}";
        var poslednja = kartice.LastOrDefault();
        TxtStanjeArtikla.Text = poslednja == null
            ? "Nema prometa na kartici."
            : $"Trenutno stanje: {poslednja.Stanje:N2} {artikal.JedinicaMere}, saldo: {poslednja.Saldo:N2} RSD";

        TxtSumaUlaz.Text = kartice.Sum(k => k.Ulaz).ToString("N2");
        TxtSumaIzlaz.Text = kartice.Sum(k => k.Izlaz).ToString("N2");
        TxtSumaSaldo.Text = (poslednja?.Saldo ?? 0m).ToString("N2");
    }

    private void CmbMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UcitajArtikle();
        UcitajKarticu();
    }

    private void TxtPretragaArtikla_TextChanged(object sender, TextChangedEventArgs e) => UcitajArtikle();

    private void ChkSamoSaKarticom_Changed(object sender, System.Windows.RoutedEventArgs e) => UcitajArtikle();

    private void LstArtikli_SelectionChanged(object sender, SelectionChangedEventArgs e) => UcitajKarticu();
}
