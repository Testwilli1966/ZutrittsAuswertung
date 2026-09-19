using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZutrittsAuswertung.Data;
using ZutrittsAuswertung.Models;

namespace ZutrittsAuswertung.Pages;

public class IndexModel : PageModel
{
    private readonly AuswertungsRepository _repository;

    public IndexModel(AuswertungsRepository repository)
    {
        _repository = repository;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? Von { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Bis { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Suche { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "alle";

    public IReadOnlyList<AccessEventView> Ergebnisse { get; private set; } = Array.Empty<AccessEventView>();

    public int AnzahlErlaubt { get; private set; }
    public int AnzahlVerweigert { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Ohne Angabe: heutiger Tag als Standardzeitraum
        var von = Von ?? DateTime.Today;
        var bis = Bis ?? DateTime.Today.AddDays(1).AddSeconds(-1);

        Von ??= von;
        Bis ??= bis;

        Ergebnisse = await _repository.SucheAsync(von, bis, Suche, Status, ct: ct);
        AnzahlErlaubt = Ergebnisse.Count(e => e.Allowed);
        AnzahlVerweigert = Ergebnisse.Count(e => !e.Allowed);
    }
}
