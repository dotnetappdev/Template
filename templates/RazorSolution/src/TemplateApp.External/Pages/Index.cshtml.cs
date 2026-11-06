using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplateApp.External.ApiClients;
using TemplateApp.External.ApiClients.Models;

namespace TemplateApp.External.Pages;

public class IndexModel : PageModel
{
    private readonly IStatusApiClient _status;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(IStatusApiClient status, IWebHostEnvironment environment)
    {
        _status = status;
        _environment = environment;
    }

    /// <summary>The API status banner is a development aid; it is hidden outside Development.</summary>
    public bool ShowApiStatus => _environment.IsDevelopment();

    public ApiStatus? ApiStatus { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (ShowApiStatus)
        {
            ApiStatus = await _status.GetAsync(cancellationToken);
        }
    }
}
