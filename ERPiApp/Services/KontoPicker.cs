using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ERPiData.Models.Core;

namespace ERPiApp.Services;

/// <summary>
/// Puni i pretražuje padajuću listu konta partnera (dobavljač / kupac) u dokumentima.
/// Portovan iz ERPiFinansijeApp, prilagođen za novu strukturu.
/// </summary>
public static class KontoPicker
{
    public static class Grupe
    {
        public const string DobavljaciNoviZakon = "435";
        public const string DobavljaciStariZakon = "220";
        public const string KupciNoviZakon = "204";
        public const string KupciStariZakon = "120";
    }

    public static string OdrediPrefiks(IEnumerable<Konto> konta, string noviZakon, string stariZakon)
    {
        var lista = konta as IList<Konto> ?? konta.ToList();
        if (lista.Any(k => k.BrojKonta.StartsWith(noviZakon, System.StringComparison.Ordinal))) return noviZakon;
        if (lista.Any(k => k.BrojKonta.StartsWith(stariZakon, System.StringComparison.Ordinal))) return stariZakon;
        return noviZakon;
    }

    public static void PoveziDobavljace(ComboBox combo, IEnumerable<Konto> konta)
        => Poveži(combo, konta, OdrediPrefiks(konta, Grupe.DobavljaciNoviZakon, Grupe.DobavljaciStariZakon));

    public static void PoveziKupce(ComboBox combo, IEnumerable<Konto> konta)
        => Poveži(combo, konta, OdrediPrefiks(konta, Grupe.KupciNoviZakon, Grupe.KupciStariZakon));

    public static void Poveži(ComboBox combo, IEnumerable<Konto> konta, string prefiks)
    {
        var svi = konta
            .Where(k => string.IsNullOrEmpty(prefiks) || k.BrojKonta.StartsWith(prefiks, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k.BrojKonta)
            .ToList();

        combo.IsEditable = true;
        combo.IsTextSearchEnabled = false;
        combo.StaysOpenOnEdit = true;
        combo.DisplayMemberPath = nameof(Konto.Prikaz);
        combo.SelectedValuePath = nameof(Konto.BrojKonta);
        combo.ItemsSource = svi;
        _izvori[combo] = svi;

        if (combo.Template?.FindName("PART_EditableTextBox", combo) is TextBox)
        {
            ZakačiPretragu(combo);
        }
        else
        {
            combo.Loaded += (_, _) => ZakačiPretragu(combo);
        }
    }

    private static readonly Dictionary<ComboBox, List<Konto>> _izvori = new();

    private static void ZakačiPretragu(ComboBox combo)
    {
        if (combo.Template?.FindName("PART_EditableTextBox", combo) is not TextBox tb) return;

        tb.TextChanged -= NaPromenuTeksta;
        tb.TextChanged += NaPromenuTeksta;
    }

    private static void NaPromenuTeksta(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (tb.TemplatedParent is not ComboBox combo) return;
        if (!_izvori.TryGetValue(combo, out var svi)) return;

        string upit = tb.Text?.Trim() ?? "";
        int caret = tb.CaretIndex;

        combo.ItemsSource = string.IsNullOrEmpty(upit)
            ? svi
            : svi.Where(k =>
                    k.BrojKonta.Contains(upit, System.StringComparison.OrdinalIgnoreCase) ||
                    k.NazivKonta.Contains(upit, System.StringComparison.OrdinalIgnoreCase))
                 .ToList();

        if (tb.Text != upit)
        {
            tb.Text = upit;
            tb.CaretIndex = caret;
        }

        combo.IsDropDownOpen = combo.Items.Count > 0;
    }

    public static string? IzabraniKonto(ComboBox combo)
    {
        if (combo.SelectedItem is Konto k) return k.BrojKonta;

        string tekst = combo.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(tekst)) return null;

        int crta = tekst.IndexOf(" - ", System.StringComparison.Ordinal);
        return crta > 0 ? tekst[..crta] : tekst;
    }

    public static void PostaviKonto(ComboBox combo, string? brojKonta)
    {
        if (string.IsNullOrWhiteSpace(brojKonta)) return;

        if (_izvori.TryGetValue(combo, out var svi) &&
            svi.FirstOrDefault(k => k.BrojKonta == brojKonta) is { } pogodak)
        {
            combo.SelectedItem = pogodak;
        }
        else
        {
            combo.Text = brojKonta;
        }
    }
}
