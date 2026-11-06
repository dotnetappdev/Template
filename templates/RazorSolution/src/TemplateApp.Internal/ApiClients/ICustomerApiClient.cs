using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.ApiClients;

/// <summary>
/// Customers, through TemplateApp.API (api/customers). Pages depend on this interface only.
/// </summary>
public interface ICustomerApiClient
{
    Task<IReadOnlyList<CustomerModel>> SearchAsync(string? search, CancellationToken cancellationToken = default);

    Task<CustomerModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ApiResult> CreateAsync(SaveCustomerModel customer, CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
