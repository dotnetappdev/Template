using TemplateApp.External.ApiClients.Models;

namespace TemplateApp.External.ApiClients;

public interface IStatusApiClient
{
    /// <summary>GET api/status. Returns null when the API cannot be reached.</summary>
    Task<ApiStatus?> GetAsync(CancellationToken cancellationToken = default);
}
