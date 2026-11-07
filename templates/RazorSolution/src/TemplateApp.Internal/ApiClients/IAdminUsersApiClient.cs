using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

/// <summary>User and role administration through TemplateApp.API (/api/admin, Admin role only).</summary>
public interface IAdminUsersApiClient
{
    Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<AdminUser?> GetUserAsync(string id, CancellationToken cancellationToken = default);

    Task<ApiResult> CreateAsync(CreateUserInput input, CancellationToken cancellationToken = default);

    Task<ApiResult> SetRolesAsync(string id, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    Task<ApiResult> LockAsync(string id, CancellationToken cancellationToken = default);

    Task<ApiResult> UnlockAsync(string id, CancellationToken cancellationToken = default);

    Task<ApiResult> ResetTwoFactorAsync(string id, CancellationToken cancellationToken = default);
#if (UsePasskeys)

    Task<ApiResult> RemovePasskeysAsync(string id, CancellationToken cancellationToken = default);
#endif

    Task<ApiResult> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
