using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.External.ApiClients;

namespace TemplateApp.External.Pages.Account.Manage;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly IAccountManageApiClient _manage;

    public ChangePasswordModel(IAccountManageApiClient manage)
    {
        _manage = manage;
    }

    [BindProperty]
    public ChangePasswordInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _manage.ChangePasswordAsync(Input.OldPassword, Input.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The password could not be changed.");
            return Page();
        }

        TempData["Message"] = "Your password was changed.";
        return RedirectToPage("Index");
    }

    public class ChangePasswordInput
    {
        [Required, DataType(DataType.Password), Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password), Display(Name = "Confirm new password"), Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
