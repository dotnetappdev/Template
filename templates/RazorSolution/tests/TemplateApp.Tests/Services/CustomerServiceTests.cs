using System.Linq.Expressions;
using TemplateApp.Services;
using TemplateApp.Services.Models;
using TemplateApp.SqlRepository.Entities;
using TemplateApp.SqlRepository.Interfaces;

namespace TemplateApp.Tests.Services;

/// <summary>
/// The services depend on repository interfaces, so they can be tested without a database:
/// ICustomerRepository is replaced with an NSubstitute fake.
/// </summary>
public class CustomerServiceTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_repository);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_the_customer_does_not_exist()
    {
        _repository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await _service.GetByIdAsync(42);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_maps_the_entity_to_a_dto()
    {
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = 1, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Ada Lovelace", result.FullName);
        Assert.Equal("ada@example.com", result.Email);
    }

    [Fact]
    public async Task CreateAsync_trims_values_adds_and_saves()
    {
        _repository.ExistsAsync(Arg.Any<Expression<Func<Customer, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.CreateAsync(new SaveCustomerRequest("  Grace ", "Hopper", " grace@example.com ", "  ", true));

        Assert.Equal("Grace", result.FirstName);
        Assert.Equal("grace@example.com", result.Email);
        Assert.Null(result.Company);
        await _repository.Received(1).AddAsync(Arg.Is<Customer>(c => c.Email == "grace@example.com"), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_email()
    {
        _repository.ExistsAsync(Arg.Any<Expression<Func<Customer, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(new SaveCustomerRequest("Ada", "Lovelace", "ada@example.com", null)));

        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_returns_false_when_the_customer_does_not_exist()
    {
        _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        Assert.False(await _service.DeleteAsync(7));
        _repository.DidNotReceive().Remove(Arg.Any<Customer>());
    }
}
