using System.Net.Http.Json;
using TemplateApp.External.ApiClients.Models;

namespace TemplateApp.External.ApiClients;

public class ContactApiClient : IContactApiClient
{
    private readonly HttpClient _http;

    public ContactApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult> SendAsync(ContactRequestModel request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/contact-requests", request, cancellationToken);

        return response.IsSuccessStatusCode
            ? ApiResult.Success()
            : ApiResult.Failure($"The API returned {(int)response.StatusCode} {response.ReasonPhrase}.");
    }
}
