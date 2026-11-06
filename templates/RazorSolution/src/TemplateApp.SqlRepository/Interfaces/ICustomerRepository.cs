using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.SqlRepository.Interfaces;

/// <summary>
/// Customer-specific queries go here. Generic CRUD comes from <see cref="IRepository{TEntity}"/>.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> SearchAsync(string term, CancellationToken cancellationToken = default);
}
