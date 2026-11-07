using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.External.ApiClients;
using TemplateApp.External.ApiClients.Models;

namespace TemplateApp.External.Pages.Account.Manage;

/// <summary>
/// Passkeys (WebAuthn) through the API. The API's attestation state is kept in TempData between the two
/// steps, so it never reaches the browser.
/// </summary>
[Authorize]
public class PasskeysModel : PageModel
{
    private const string StateKey = "Passkeys.AttestationState";
    private readonly IAccountManageApiClient _manage;

    public PasskeysModel(IAccountManageApiClient manage)
    {
        _manage = manage;
    }

    public IReadOnlyList<PasskeyItem> Passkeys { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Passkeys = await _manage.GetPasskeysAsync(cancellationToken);
    }

    /// <summary>Step 1 (fetch): WebAuthn creation options for navigator.credentials.create().</summary>
    public async Task<IActionResult> OnPostCreationOptionsAsync(CancellationToken cancellationToken)
    {
        var options = await _manage.GetPasskeyCreationOptionsAsync(cancellationToken);
        TempData[StateKey] = options.State;
        return Content(options.OptionsJson, "application/json");
    }

    /// <summary>Step 2 (form post): the new credential.</summary>
    public async Task<IActionResult> OnPostAddAsync(string credentialJson, string? name, CancellationToken cancellationToken)
    {
        if (TempData[StateKey] is not string state || string.IsNullOrEmpty(credentialJson))
        {
            TempData["Message"] = "Adding the passkey took too long. Try again.";
            return RedirectToPage();
        }

        var result = await _manage.AddPasskeyAsync(credentialJson, state, name, cancellationToken);
        TempData["Message"] = result.Succeeded ? "Your passkey was added." : result.Error ?? "The passkey could not be added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _manage.DeletePasskeyAsync(id, cancellationToken);
        TempData["Message"] = result.Succeeded ? "The passkey was removed." : result.Error;
        return RedirectToPage();
    }
}
