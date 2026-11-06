using TemplateApp.Services.Interfaces;
using TemplateApp.Services.Models;
using TemplateApp.SqlRepository.Entities;
using TemplateApp.SqlRepository.Interfaces;

namespace TemplateApp.Services;

/// <summary>
/// Business logic for customers. Talks to the database only through <see cref="ICustomerRepository"/>.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;

    public CustomerService(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await _customers.GetAllAsync(cancellationToken)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string? term, CancellationToken cancellationToken = default)
        => (await _customers.SearchAsync(term ?? string.Empty, cancellationToken)).Select(ToDto).ToList();

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        if (await _customers.ExistsAsync(c => c.Email == email, cancellationToken))
        {
            throw new InvalidOperationException($"A customer with the email '{email}' already exists.");
        }

        var customer = new Customer();
        Apply(customer, request);

        await _customers.AddAsync(customer, cancellationToken);
        await _customers.SaveChangesAsync(cancellationToken);

        return ToDto(customer);
    }

    public async Task<bool> UpdateAsync(int id, SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return false;
        }

        Apply(customer, request);
        _customers.Update(customer);
        await _customers.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return false;
        }

        _customers.Remove(customer);
        await _customers.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(Customer customer, SaveCustomerRequest request)
    {
        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = request.Email.Trim();
        customer.Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim();
        customer.IsActive = request.IsActive;
    }

    private static CustomerDto ToDto(Customer c)
        => new(c.Id, c.FirstName, c.LastName, c.Email, c.Company, c.IsActive);
}
