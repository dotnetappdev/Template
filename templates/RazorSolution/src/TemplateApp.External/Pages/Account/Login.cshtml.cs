using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.External.ApiClients;
using TemplateApp.External.ApiClients.Models;
using TemplateApp.External.Auth;

namespace TemplateApp.External.Pages.Account;

/// <summary>
/// Signs in through the API (ASP.NET Core Identity): password, then the authenticator or a recovery code
/// when two-factor is on, or a passkey.
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private const string PasskeyStateKey = "Login.PasskeyState";
    private readonly IAccountApiClient _account;
    private readonly IUserSession _session;
    private readonly ITimeLimitedDataProtector _protector;

    public LoginModel(IAccountApiClient account, IUserSession session, IDataProtectionProvider dataProtection)
    {
        _account = account;
        _session = session;
        _protector = dataProtection.CreateProtector("TemplateApp.External.Login.TwoFactor").ToTimeLimitedDataProtector();
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>The email and password, encrypted for 5 minutes, while the two-factor code is asked for.</summary>
    [BindProperty]
    public string? PendingLogin { get; set; }

    [BindProperty]
    public string? TwoFactorCode { get; set; }

    [BindProperty]
    public string? RecoveryCode { get; set; }

    [BindProperty]
    public bool UseRecoveryCode { get; set; }

    public bool RequiresTwoFactor { get; private set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        string email, password;
        var secondStep = !string.IsNullOrEmpty(PendingLogin);
        if (secondStep)
        {
            try
            {
                var parts = _protector.Unprotect(PendingLogin!).Split('\n', 2);
                (email, password) = (parts[0], parts[1]);
                Input.Email = email;
            }
            catch (CryptographicException)
            {
                PendingLogin = null;
                ErrorMessage = "That took too long. Enter your password again.";
                return Page();
            }

            // Switching between "authenticator code" and "recovery code" just redraws the form.
            if (string.IsNullOrWhiteSpace(TwoFactorCode) && string.IsNullOrWhiteSpace(RecoveryCode))
            {
                RequiresTwoFactor = true;
                ModelState.Clear();
                return Page();
            }
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            (email, password) = (Input.Email, Input.Password);
        }

        LoginResult result;
        try
        {
            result = await _account.LoginAsync(email, password, UseRecoveryCode ? null : TwoFactorCode, UseRecoveryCode ? RecoveryCode : null, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Sign-in is unavailable right now (TemplateApp.API is not reachable).";
            return Page();
        }

        if (result.Succeeded)
        {
            await _session.SignInAsync(result.Tokens!, Input.RememberMe, cancellationToken);
            return LocalRedirect(SafeReturnUrl);
        }

        if (result.RequiresTwoFactor || (secondStep && !result.LockedOut))
        {
            // Ask for (or re-ask for) the second factor without asking for the password again.
            RequiresTwoFactor = true;
            PendingLogin = _protector.Protect(email + "\n" + password, TimeSpan.FromMinutes(5));
            ErrorMessage = secondStep ? "That code is not valid. Try again." : null;
            TwoFactorCode = RecoveryCode = null;
            ModelState.Clear();
            return Page();
        }

        ErrorMessage = result.Error;
        return Page();
    }
#if (UsePasskeys)

    /// <summary>Step 1 of passkey sign-in (fetch): WebAuthn request options. The API's state stays in TempData.</summary>
    public async Task<IActionResult> OnPostPasskeyOptionsAsync([FromBody] PasskeyOptionsRequest? request, CancellationToken cancellationToken)
    {
        var options = await _account.GetPasskeyRequestOptionsAsync(request?.Email, cancellationToken);
        TempData[PasskeyStateKey] = options.State;
        return Content(options.OptionsJson, "application/json");
    }

    /// <summary>Step 2 of passkey sign-in (form post): the credential from navigator.credentials.get().</summary>
    public async Task<IActionResult> OnPostPasskeyAsync(string credentialJson, CancellationToken cancellationToken)
    {
        if (TempData[PasskeyStateKey] is not string state || string.IsNullOrEmpty(credentialJson))
        {
            ErrorMessage = "The passkey sign-in expired. Try again.";
            return Page();
        }

        var result = await _account.PasskeyLoginAsync(credentialJson, state, cancellationToken);
        if (!result.Succeeded)
        {
            ErrorMessage = result.LockedOut ? result.Error : "That passkey was not accepted.";
            return Page();
        }

        await _session.SignInAsync(result.Tokens!, persistent: false, cancellationToken);
        return LocalRedirect(SafeReturnUrl);
    }

    public record PasskeyOptionsRequest(string? Email);
#endif

    private string SafeReturnUrl => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/");
}
