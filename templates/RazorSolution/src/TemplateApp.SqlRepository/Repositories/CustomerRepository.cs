using Microsoft.EntityFrameworkCore;
using TemplateApp.SqlRepository.Entities;
using TemplateApp.SqlRepository.Interfaces;

namespace TemplateApp.SqlRepository.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public override async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Customer>> SearchAsync(string term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return await GetAllAsync(cancellationToken);
        }

        term = term.Trim();

        return await DbSet.AsNoTracking()
            .Where(c => c.FirstName.Contains(term)
                     || c.LastName.Contains(term)
                     || c.Email.Contains(term)
                     || (c.Company != null && c.Company.Contains(term)))
            .OrderBy(c => c.LastName)
            .ToListAsync(cancellationToken);
    }
}
