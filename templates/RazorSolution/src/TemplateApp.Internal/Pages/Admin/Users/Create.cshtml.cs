using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly IAdminUsersApiClient _admin;

    public CreateModel(IAdminUsersApiClient admin)
    {
        _admin = admin;
    }

    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public IReadOnlyList<string> AllRoles { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        AllRoles = await _admin.GetRolesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        AllRoles = await _admin.GetRolesAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _admin.CreateAsync(Input, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The user could not be created.");
            return Page();
        }

        TempData["Message"] = $"{Input.Email} was created.";
        return RedirectToPage("Index");
    }
}
