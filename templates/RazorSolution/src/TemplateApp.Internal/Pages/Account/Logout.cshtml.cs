using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.Auth;

namespace TemplateApp.Internal.Pages.Account;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly IUserSession _session;

    public LogoutModel(IUserSession session)
    {
        _session = session;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _session.SignOutAsync();
        TempData["Message"] = "You are signed out.";
        return RedirectToPage("/Account/Login");
    }
}
