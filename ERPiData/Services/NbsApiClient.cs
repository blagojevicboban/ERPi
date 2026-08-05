using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using ERPiData.Models.Finansije;

namespace ERPiData.Services;

public class NbsApiClient
{
    private readonly HttpClient _httpClient;
    private const string BazaUrl = "https://webappcenter.nbs.rs/ExchangeRateWebApp/ExchangeRate/IndexByDate";

    public NbsApiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<List<KursnaListaStavka>> PreuzmiKursnuListuAsync(DateTime datum)
    {
        try
        {
            var srednjiRedovi = await PreuzmiTabeluAsync(datum, listTypeId: 3);
            if (srednjiRedovi.Count == 0) return new List<KursnaListaStavka>();

            var kupProdRedovi = await PreuzmiTabeluAsync(datum, listTypeId: 1);
            var kupProdPoValuti = kupProdRedovi
                .Where(r => r.Count >= 6 && !string.IsNullOrWhiteSpace(r[0]))
                .GroupBy(r => r[0].ToUpperInvariant())
                .ToDictionary(g => g.Key, g => (Kupovni: ParsirajDecimal(g.First()[4]), Prodajni: ParsirajDecimal(g.First()[5])));

            var rezultati = new List<KursnaListaStavka>();
            foreach (var red in srednjiRedovi)
            {
                if (red.Count < 5 || string.IsNullOrWhiteSpace(red[0])) continue;

                string valuta = red[0].ToUpperInvariant();
                int jedinica = int.TryParse(red[3], out int j) ? j : 1;
                decimal srednji = ParsirajDecimal(red[4]);
                var (kupovni, prodajni) = kupProdPoValuti.TryGetValue(valuta, out var kp) ? kp : (srednji, srednji);

                rezultati.Add(new KursnaListaStavka
                {
                    Datum = datum.Date,
                    ValutaOznaka = valuta,
                    NazivValute = red[2],
                    Jedinica = jedinica,
                    SrednjiKurs = srednji,
                    KupovniKurs = kupovni,
                    ProdavniKurs = prodajni
                });
            }

            return rezultati;
        }
        catch
        {
            return new List<KursnaListaStavka>();
        }
    }

    private async Task<List<List<string>>> PreuzmiTabeluAsync(DateTime datum, int listTypeId)
    {
        string url = $"{BazaUrl}?isSearchExecuted=true&Date={datum:dd.MM.yyyy}&ExchangeRateListTypeID={listTypeId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", ".AspNetCore.Culture=c=sr-Latn|uic=sr-Latn");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<List<string>>();

        string html = await response.Content.ReadAsStringAsync();
        var tbody = Regex.Match(html, "<tbody>(.*?)</tbody>", RegexOptions.Singleline);
        if (!tbody.Success) return new List<List<string>>();

        var redovi = new List<List<string>>();
        foreach (Match red in Regex.Matches(tbody.Groups[1].Value, "<tr>(.*?)</tr>", RegexOptions.Singleline))
        {
            var celije = Regex.Matches(red.Groups[1].Value, "<td>(.*?)</td>", RegexOptions.Singleline)
                .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value).Trim())
                .ToList();
            if (celije.Count > 0) redovi.Add(celije);
        }

        return redovi;
    }

    private static decimal ParsirajDecimal(string vrednost)
        => decimal.TryParse(vrednost.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d) ? d : 0m;

    public async Task<(bool Success, string Message, string? TekuciRacun, string StatusBlokade)> ProveriTekuciRacunPartneraAsync(string pibIliMb)
    {
        if (string.IsNullOrWhiteSpace(pibIliMb))
            return (false, "PIB ili matični broj nije unet.", null, "Nepoznato");

        string ociscen = pibIliMb.Trim();

        try
        {
            string url = $"https://www.nbs.rs/rir_service/rir.xml?pib={ociscen}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string xml = await response.Content.ReadAsStringAsync();
                var xdoc = XDocument.Parse(xml);
                var racunElem = xdoc.Descendants("Racun").FirstOrDefault();
                string tekuci = racunElem?.Element("BrojRacuna")?.Value ?? "";
                string blokada = xdoc.Descendants("Status").FirstOrDefault()?.Value ?? "AKTIVAN";

                return (true, "Uspešna verifikacija iz Registra računa NBS.", string.IsNullOrWhiteSpace(tekuci) ? null : tekuci, blokada);
            }

            return (false, $"NBS registar je vratio status {(int)response.StatusCode} ({response.StatusCode}). Partner nije verifikovan.", null, "Nepoznato");
        }
        catch (Exception ex)
        {
            return (false, $"Registar računa NBS nije dostupan: {ex.Message}", null, "Nepoznato");
        }
    }
}
