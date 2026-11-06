using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages.Customers;

public class IndexModel : PageModel
{
    private readonly ICustomerApiClient _customers;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ICustomerApiClient customers, ILogger<IndexModel> logger)
    {
        _customers = customers;
        _logger = logger;
    }

    public IReadOnlyList<CustomerModel> Customers { get; private set; } = [];

    public string? ApiError { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Customers = await _customers.SearchAsync(Search, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not load customers from the API.");
            ApiError = "Customers could not be loaded because TemplateApp.API is not reachable.";
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _customers.DeleteAsync(id, cancellationToken);
        TempData["Message"] = result.Succeeded ? "The customer was deleted." : result.Error;
        return RedirectToPage();
    }

    /// <summary>
    /// JSON for the jQuery UI autocomplete: GET /Customers?handler=Autocomplete&amp;term=...
    /// </summary>
    public async Task<JsonResult> OnGetAutocompleteAsync(string? term, CancellationToken cancellationToken)
    {
        var matches = await _customers.SearchAsync(term, cancellationToken);

        return new JsonResult(matches
            .Take(10)
            .Select(c => new { label = $"{c.FullName} ({c.Email})", value = c.FullName }));
    }
}
