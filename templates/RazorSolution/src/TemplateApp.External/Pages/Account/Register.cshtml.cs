using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.External.ApiClients;
using TemplateApp.External.Auth;

namespace TemplateApp.External.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly IAccountApiClient _account;
    private readonly IUserSession _session;

    public RegisterModel(IAccountApiClient account, IUserSession session)
    {
        _account = account;
        _session = session;
    }

    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var registered = await _account.RegisterAsync(Input.Email, Input.Password, cancellationToken);
        if (!registered.Succeeded)
        {
            ModelState.AddModelError(string.Empty, registered.Error ?? "The account could not be created.");
            return Page();
        }

        // Sign straight in with the new account.
        var login = await _account.LoginAsync(Input.Email, Input.Password, null, null, cancellationToken);
        if (!login.Succeeded)
        {
            TempData["Message"] = "Your account was created. Sign in to continue.";
            return RedirectToPage("Login", new { ReturnUrl });
        }

        await _session.SignInAsync(login.Tokens!, persistent: false, cancellationToken);
        TempData["Message"] = "Welcome! Your account is ready.";
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/"));
    }

    public class RegisterInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password), Display(Name = "Confirm password"), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
