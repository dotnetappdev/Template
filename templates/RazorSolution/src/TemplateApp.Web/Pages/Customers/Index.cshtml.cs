using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
#if (UseServices)
using TemplateApp.Services.Interfaces;
using TemplateApp.Services.Models;
#else
using TemplateApp.SqlRepository.Entities;
using TemplateApp.SqlRepository.Interfaces;
#endif

namespace TemplateApp.Web.Pages.Customers;

public class IndexModel : PageModel
{
#if (UseServices)
    private readonly ICustomerService _customers;

    public IndexModel(ICustomerService customers)
    {
        _customers = customers;
    }

    public IReadOnlyList<CustomerDto> Customers { get; private set; } = [];
#else
    private readonly ICustomerRepository _customers;

    public IndexModel(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public IReadOnlyList<Customer> Customers { get; private set; } = [];
#endif

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Customers = await _customers.SearchAsync(Search ?? string.Empty, cancellationToken);
    }

    /// <summary>
    /// JSON for the jQuery UI autocomplete: GET /Customers?handler=Autocomplete&amp;term=...
    /// </summary>
    public async Task<JsonResult> OnGetAutocompleteAsync(string? term, CancellationToken cancellationToken)
    {
        var matches = await _customers.SearchAsync(term ?? string.Empty, cancellationToken);

        return new JsonResult(matches
            .Take(10)
            .Select(c => new { label = $"{c.FullName} ({c.Email})", value = c.FullName }));
    }
}
