using TemplateApp.External.ApiClients.Models;

namespace TemplateApp.External.ApiClients;

/// <summary>
/// Contact requests, through TemplateApp.API (api/contact-requests).
/// </summary>
public interface IContactApiClient
{
    Task<ApiResult> SendAsync(ContactRequestModel request, CancellationToken cancellationToken = default);
}
