using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ERPiData.Models.Magacin;

namespace ERPiApp.Services;

/// <summary>
/// Puni i pretražuje padajuću listu artikala i robe u dokumentima (kalkulacije, fakture, otpremnice, nivelacije).
/// Traži se po šifri artikla, nazivu i barkodu u realnom vremenu.
/// </summary>
public static class ArtikalPicker
{
    public static void Poveži(ComboBox combo, IEnumerable<Artikal> artikli)
    {
        var svi = artikli.OrderBy(a => a.Naziv).ToList();

        combo.IsEditable = true;
        combo.IsTextSearchEnabled = false;
        combo.StaysOpenOnEdit = true;
        combo.DisplayMemberPath = nameof(Artikal.Prikaz);
        combo.SelectedValuePath = nameof(Artikal.ArtikalId);
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

    private static readonly Dictionary<ComboBox, List<Artikal>> _izvori = new();

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
            : svi.Where(a =>
                    a.Naziv.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
                    a.SifraArtikla.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
                    (a.Barkod != null && a.Barkod.Contains(upit, StringComparison.OrdinalIgnoreCase)))
                 .ToList();

        if (tb.Text != upit)
        {
            tb.Text = upit;
            tb.CaretIndex = caret;
        }

        combo.IsDropDownOpen = combo.Items.Count > 0;
    }

    public static Artikal? IzabraniArtikal(ComboBox combo) => combo.SelectedItem as Artikal;

    public static void PostaviArtikal(ComboBox combo, int? artikalId)
    {
        if (artikalId == null) return;

        if (_izvori.TryGetValue(combo, out var svi) &&
            svi.FirstOrDefault(a => a.ArtikalId == artikalId.Value) is { } pogodak)
        {
            combo.SelectedItem = pogodak;
        }
    }
}
