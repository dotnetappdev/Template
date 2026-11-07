using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages.Account.Manage;

/// <summary>
/// Two-factor authentication with an authenticator app (TOTP) and recovery codes, through the API's
/// /api/account/manage/2fa endpoint.
/// </summary>
[Authorize]
public class TwoFactorModel : PageModel
{
    private const string Issuer = "TemplateApp";
    private readonly IAccountManageApiClient _manage;

    public TwoFactorModel(IAccountManageApiClient manage)
    {
        _manage = manage;
    }

    public TwoFactorStatus? Status { get; private set; }

    public string[]? RecoveryCodes { get; private set; }

    public string FormattedKey { get; private set; } = string.Empty;

    public string AuthenticatorUri { get; private set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(await _manage.TwoFactorAsync(cancellationToken: cancellationToken));

    public async Task<IActionResult> OnPostEnableAsync(string code, CancellationToken cancellationToken)
    {
        var status = await _manage.TwoFactorAsync(enable: true, code: code, cancellationToken: cancellationToken);
        if (status is null || !status.IsTwoFactorEnabled)
        {
            TempData["Message"] = "That code was not valid. Try the next code from your app.";
            return RedirectToPage();
        }

        // New recovery codes come back when two-factor is first turned on.
        if (status.RecoveryCodes is not { Length: > 0 })
        {
            status = await _manage.TwoFactorAsync(resetRecoveryCodes: true, cancellationToken: cancellationToken);
        }

        await LoadAsync(status);
        return Page();
    }

    public async Task<IActionResult> OnPostResetRecoveryCodesAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(await _manage.TwoFactorAsync(resetRecoveryCodes: true, cancellationToken: cancellationToken));
        return Page();
    }

    public async Task<IActionResult> OnPostResetKeyAsync(CancellationToken cancellationToken)
    {
        await _manage.TwoFactorAsync(resetSharedKey: true, cancellationToken: cancellationToken);
        TempData["Message"] = "The authenticator key was reset. Set up your app again to turn two-factor back on.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisableAsync(CancellationToken cancellationToken)
    {
        await _manage.TwoFactorAsync(enable: false, cancellationToken: cancellationToken);
        TempData["Message"] = "Two-factor authentication is off.";
        return RedirectToPage();
    }

    private Task LoadAsync(TwoFactorStatus? status)
    {
        Status = status;
        RecoveryCodes = status?.RecoveryCodes;
        if (status is not null)
        {
            var key = status.SharedKey;
            FormattedKey = string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4).Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4)))).ToLowerInvariant();
            var account = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "user";
            AuthenticatorUri = $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(account)}?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6";
        }

        return Task.CompletedTask;
    }
}
