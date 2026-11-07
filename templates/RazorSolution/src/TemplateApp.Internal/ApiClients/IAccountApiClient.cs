using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

/// <summary>
/// Sign-in calls to TemplateApp.API (ASP.NET Core Identity). These run before the user has a session,
/// so this client does not attach the user's token.
/// </summary>
public interface IAccountApiClient
{
    Task<LoginResult> LoginAsync(string email, string password, string? twoFactorCode, string? recoveryCode, CancellationToken cancellationToken = default);

    Task<ApiResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AccessTokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<MeResponse?> GetMeAsync(string accessToken, CancellationToken cancellationToken = default);
#if (UsePasskeys)

    Task<PasskeyOptions> GetPasskeyRequestOptionsAsync(string? email, CancellationToken cancellationToken = default);

    Task<LoginResult> PasskeyLoginAsync(string credentialJson, string state, CancellationToken cancellationToken = default);
#endif
}

/// <summary>
/// The signed-in user's own account (password, two-factor, passkeys). Sends the user's token.
/// </summary>
public interface IAccountManageApiClient
{
    Task<MeResponse?> GetMeAsync(CancellationToken cancellationToken = default);

    Task<ApiResult> ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default);
#if (UseTwoFactor)

    /// <summary>Status, shared key and (when changed) new recovery codes. All arguments null = just read the status.</summary>
    Task<TwoFactorStatus?> TwoFactorAsync(bool? enable = null, string? code = null, bool resetSharedKey = false, bool resetRecoveryCodes = false, CancellationToken cancellationToken = default);
#endif
#if (UsePasskeys)

    Task<IReadOnlyList<PasskeyItem>> GetPasskeysAsync(CancellationToken cancellationToken = default);

    Task<PasskeyOptions> GetPasskeyCreationOptionsAsync(CancellationToken cancellationToken = default);

    Task<ApiResult> AddPasskeyAsync(string credentialJson, string state, string? name, CancellationToken cancellationToken = default);

    Task<ApiResult> DeletePasskeyAsync(string id, CancellationToken cancellationToken = default);
#endif
}
