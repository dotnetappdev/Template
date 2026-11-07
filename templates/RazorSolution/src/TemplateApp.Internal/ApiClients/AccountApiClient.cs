using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

public class AccountApiClient : IAccountApiClient
{
    private readonly HttpClient _http;

    public AccountApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, string? twoFactorCode, string? recoveryCode, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/login", new
        {
            email,
            password,
            twoFactorCode = string.IsNullOrWhiteSpace(twoFactorCode) ? null : twoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty),
            twoFactorRecoveryCode = string.IsNullOrWhiteSpace(recoveryCode) ? null : recoveryCode.Replace(" ", string.Empty),
        }, cancellationToken);
        return await ApiErrors.ToLoginResultAsync(response, cancellationToken);
    }

    public async Task<ApiResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/register", new { email, password }, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Success() : ApiResult.Failure(await ApiErrors.ReadAsync(response, cancellationToken));
    }

    public async Task<AccessTokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/refresh", new { refreshToken }, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken) : null;
    }

    public async Task<MeResponse?> GetMeAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/account/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MeResponse>(cancellationToken) : null;
    }
#if (UsePasskeys)

    public async Task<PasskeyOptions> GetPasskeyRequestOptionsAsync(string? email, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/passkeys/request-options", new { email }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PasskeyOptions>(cancellationToken))!;
    }

    public async Task<LoginResult> PasskeyLoginAsync(string credentialJson, string state, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/passkeys/login", new { credentialJson, state }, cancellationToken);
        return await ApiErrors.ToLoginResultAsync(response, cancellationToken);
    }
#endif
}

public class AccountManageApiClient : IAccountManageApiClient
{
    private readonly HttpClient _http;

    public AccountManageApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<MeResponse?> GetMeAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/account/me", cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MeResponse>(cancellationToken) : null;
    }

    public async Task<ApiResult> ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/manage/info", new { oldPassword, newPassword }, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Success() : ApiResult.Failure(await ApiErrors.ReadAsync(response, cancellationToken));
    }
#if (UseTwoFactor)

    public async Task<TwoFactorStatus?> TwoFactorAsync(bool? enable = null, string? code = null, bool resetSharedKey = false, bool resetRecoveryCodes = false, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/manage/2fa", new
        {
            enable,
            twoFactorCode = code?.Replace(" ", string.Empty).Replace("-", string.Empty),
            resetSharedKey,
            resetRecoveryCodes,
        }, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<TwoFactorStatus>(cancellationToken) : null;
    }
#endif
#if (UsePasskeys)

    public async Task<IReadOnlyList<PasskeyItem>> GetPasskeysAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<List<PasskeyItem>>("api/account/passkeys", cancellationToken) ?? [];

    public async Task<PasskeyOptions> GetPasskeyCreationOptionsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync("api/account/passkeys/creation-options", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PasskeyOptions>(cancellationToken))!;
    }

    public async Task<ApiResult> AddPasskeyAsync(string credentialJson, string state, string? name, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/account/passkeys", new { credentialJson, state, name }, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Success() : ApiResult.Failure(await ApiErrors.ReadAsync(response, cancellationToken));
    }

    public async Task<ApiResult> DeletePasskeyAsync(string id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"api/account/passkeys/{Uri.EscapeDataString(id)}", cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Success() : ApiResult.Failure("The passkey could not be removed.");
    }
#endif
}

/// <summary>Reads Identity's problem details ("RequiresTwoFactor", "LockedOut", validation errors) into plain messages.</summary>
internal static class ApiErrors
{
    public static async Task<LoginResult> ToLoginResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return new LoginResult(true, false, false, await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken), null);
        }

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            response.EnsureSuccessStatusCode();
        }

        var detail = await ReadAsync(response, cancellationToken);
        return detail switch
        {
            var d when d.Contains("RequiresTwoFactor", StringComparison.OrdinalIgnoreCase) => new LoginResult(false, true, false, null, null),
            var d when d.Contains("LockedOut", StringComparison.OrdinalIgnoreCase) => new LoginResult(false, false, true, null, "This account is locked. Try again later or ask an administrator."),
            var d when d.Contains("NotAllowed", StringComparison.OrdinalIgnoreCase) => new LoginResult(false, false, false, null, "This account cannot sign in yet (for example, the email address is not confirmed)."),
            _ => new LoginResult(false, false, false, null, "The email address or password is not correct."),
        };
    }

    public static async Task<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                return string.Join(" ", errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(v => v.GetString())));
            }

            if (root.TryGetProperty("detail", out var detail) && detail.GetString() is { Length: > 0 } text)
            {
                return text;
            }

            if (root.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? response.ReasonPhrase ?? "The request failed.";
            }
        }
        catch (JsonException)
        {
        }

        return response.ReasonPhrase ?? "The request failed.";
    }
}
