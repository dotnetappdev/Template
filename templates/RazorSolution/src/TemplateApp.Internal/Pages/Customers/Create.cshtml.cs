using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages.Customers;

public class CreateModel : PageModel
{
    private readonly ICustomerApiClient _customers;

    public CreateModel(ICustomerApiClient customers)
    {
        _customers = customers;
    }

    [BindProperty]
    public SaveCustomerModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        ApiResult result;
        try
        {
            result = await _customers.CreateAsync(Input, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "The customer could not be saved because TemplateApp.API is not reachable.");
            return Page();
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The customer could not be saved.");
            return Page();
        }

        TempData["Message"] = $"Customer {Input.FirstName} {Input.LastName} was created.";
        return RedirectToPage("Index");
    }
}
