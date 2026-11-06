using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

public class CustomerApiClient : ICustomerApiClient
{
    private const string Resource = "api/customers";
    private readonly HttpClient _http;

    public CustomerApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<CustomerModel>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(search) ? Resource : $"{Resource}?search={Uri.EscapeDataString(search)}";
        return await _http.GetFromJsonAsync<List<CustomerModel>>(url, cancellationToken) ?? [];
    }

    public async Task<CustomerModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"{Resource}/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerModel>(cancellationToken);
    }

    public async Task<ApiResult> CreateAsync(SaveCustomerModel customer, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(Resource, customer, cancellationToken);
        return await ToResultAsync(response, cancellationToken);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"{Resource}/{id}", cancellationToken);
        return await ToResultAsync(response, cancellationToken);
    }

    private static async Task<ApiResult> ToResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return ApiResult.Success();
        }

        // The API returns RFC 7807 problem details for 4xx responses (for example 409 for a duplicate email).
        if ((int)response.StatusCode is >= 400 and < 500)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            return ApiResult.Failure(problem?.Detail ?? problem?.Title ?? response.ReasonPhrase ?? "The request was rejected.");
        }

        response.EnsureSuccessStatusCode();
        return ApiResult.Success();
    }
}
