using System.Net.Http.Json;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

public class StatusApiClient : IStatusApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<StatusApiClient> _logger;

    public StatusApiClient(HttpClient http, ILogger<StatusApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ApiStatus?> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiStatus>("api/status", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "TemplateApp.API is not reachable at {BaseAddress}", _http.BaseAddress);
            return null;
        }
    }
}
