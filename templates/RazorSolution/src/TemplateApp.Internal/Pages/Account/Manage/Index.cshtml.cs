using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAccountManageApiClient _manage;

    public IndexModel(IAccountManageApiClient manage)
    {
        _manage = manage;
    }

    public MeResponse? Me { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Me = await _manage.GetMeAsync(cancellationToken);
    }
}
