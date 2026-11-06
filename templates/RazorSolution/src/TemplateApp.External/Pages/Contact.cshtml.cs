using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.External.ApiClients;
using TemplateApp.External.ApiClients.Models;

namespace TemplateApp.External.Pages;

public class ContactModel : PageModel
{
    private readonly IContactApiClient _contact;

    public ContactModel(IContactApiClient contact)
    {
        _contact = contact;
    }

    [BindProperty]
    public ContactRequestModel Input { get; set; } = new();

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
            result = await _contact.SendAsync(Input, cancellationToken);
        }
        catch (HttpRequestException)
        {
            result = ApiResult.Failure("Your message could not be sent right now. Please try again later.");
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Your message could not be sent.");
            return Page();
        }

        TempData["Message"] = "Thanks, your message has been sent.";
        return RedirectToPage("/Index");
    }
}
