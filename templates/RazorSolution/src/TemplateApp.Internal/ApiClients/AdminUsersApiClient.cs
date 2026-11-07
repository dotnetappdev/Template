using System.Net;
using System.Net.Http.Json;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

public class AdminUsersApiClient : IAdminUsersApiClient
{
    private readonly HttpClient _http;

    public AdminUsersApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<List<string>>("api/admin/roles", cancellationToken) ?? [];

    public async Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<List<AdminUser>>("api/admin/users", cancellationToken) ?? [];

    public async Task<AdminUser?> GetUserAsync(string id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/admin/users/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminUser>(cancellationToken);
    }

    public Task<ApiResult> CreateAsync(CreateUserInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/admin/users", new { input.Email, input.Password, input.DisplayName, input.Roles }, cancellationToken);

    public Task<ApiResult> SetRolesAsync(string id, IEnumerable<string> roles, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/users/{Uri.EscapeDataString(id)}/roles", new { roles }, cancellationToken);

    public Task<ApiResult> LockAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/admin/users/{Uri.EscapeDataString(id)}/lock", null, cancellationToken);

    public Task<ApiResult> UnlockAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/admin/users/{Uri.EscapeDataString(id)}/unlock", null, cancellationToken);

    public Task<ApiResult> ResetTwoFactorAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/admin/users/{Uri.EscapeDataString(id)}/reset-2fa", null, cancellationToken);
#if (UsePasskeys)

    public Task<ApiResult> RemovePasskeysAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/admin/users/{Uri.EscapeDataString(id)}/passkeys", null, cancellationToken);
#endif

    public Task<ApiResult> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/admin/users/{Uri.EscapeDataString(id)}", null, cancellationToken);

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        using var response = await _http.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Success() : ApiResult.Failure(await ApiErrors.ReadAsync(response, cancellationToken));
    }
}
