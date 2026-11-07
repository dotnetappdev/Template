using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly IAdminUsersApiClient _admin;

    public EditModel(IAdminUsersApiClient admin)
    {
        _admin = admin;
    }

    public AdminUser? UserDetails { get; private set; }

    public IReadOnlyList<string> AllRoles { get; private set; } = [];

    public async Task OnGetAsync(string id, CancellationToken cancellationToken)
    {
        UserDetails = await _admin.GetUserAsync(id, cancellationToken);
        AllRoles = await _admin.GetRolesAsync(cancellationToken);
    }

    public Task<IActionResult> OnPostRolesAsync(string id, string[] roles, CancellationToken cancellationToken) =>
        Run(id, _admin.SetRolesAsync(id, roles, cancellationToken), "Roles saved.");

    public Task<IActionResult> OnPostLockAsync(string id, CancellationToken cancellationToken) =>
        Run(id, _admin.LockAsync(id, cancellationToken), "The user is locked out.");

    public Task<IActionResult> OnPostUnlockAsync(string id, CancellationToken cancellationToken) =>
        Run(id, _admin.UnlockAsync(id, cancellationToken), "The user is unlocked.");

    public Task<IActionResult> OnPostResetTwoFactorAsync(string id, CancellationToken cancellationToken) =>
        Run(id, _admin.ResetTwoFactorAsync(id, cancellationToken), "Two-factor authentication was reset.");
#if (UsePasskeys)

    public Task<IActionResult> OnPostRemovePasskeysAsync(string id, CancellationToken cancellationToken) =>
        Run(id, _admin.RemovePasskeysAsync(id, cancellationToken), "All passkeys were removed.");
#endif

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _admin.DeleteAsync(id, cancellationToken);
        TempData["Message"] = result.Succeeded ? "The user was deleted." : result.Error;
        return result.Succeeded ? RedirectToPage("Index") : RedirectToPage(new { id });
    }

    private async Task<IActionResult> Run(string id, Task<ApiResult> action, string success)
    {
        var result = await action;
        TempData["Message"] = result.Succeeded ? success : result.Error;
        return RedirectToPage(new { id });
    }
}
