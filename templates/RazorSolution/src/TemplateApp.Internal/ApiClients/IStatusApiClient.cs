using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

public interface IStatusApiClient
{
    /// <summary>GET api/status. Returns null when the API cannot be reached.</summary>
    Task<ApiStatus?> GetAsync(CancellationToken cancellationToken = default);
}
