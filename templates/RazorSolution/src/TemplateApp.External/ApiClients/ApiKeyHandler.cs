using Microsoft.Extensions.Options;

namespace TemplateApp.External.ApiClients;

/// <summary>
/// Adds the X-Api-Key header to every request sent to TemplateApp.API.
/// </summary>
public class ApiKeyHandler : DelegatingHandler
{
    private readonly IOptions<ApiOptions> _options;

    public ApiKeyHandler(IOptions<ApiOptions> options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove("X-Api-Key");
        request.Headers.Add("X-Api-Key", _options.Value.ApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
