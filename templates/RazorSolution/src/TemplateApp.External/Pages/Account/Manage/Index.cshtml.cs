using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.External.ApiClients;
using TemplateApp.External.ApiClients.Models;

namespace TemplateApp.External.Pages.Account.Manage;

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
