using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Pages;

public class IndexModel : PageModel
{
    private readonly IStatusApiClient _status;

    public IndexModel(IStatusApiClient status)
    {
        _status = status;
    }

    public ApiStatus? ApiStatus { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ApiStatus = await _status.GetAsync(cancellationToken);
    }
}
