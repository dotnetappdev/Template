using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages.Admin.Users;

/// <summary>User administration (Admin role, see the "/Admin" folder convention in Program.cs).</summary>
public class IndexModel : PageModel
{
    private readonly IAdminUsersApiClient _admin;

    public IndexModel(IAdminUsersApiClient admin)
    {
        _admin = admin;
    }

    public IReadOnlyList<AdminUser> Users { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Users = await _admin.GetUsersAsync(cancellationToken);
    }
}
