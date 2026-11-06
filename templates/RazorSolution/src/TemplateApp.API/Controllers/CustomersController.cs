using Microsoft.AspNetCore.Mvc;
#if (UseServices)
using TemplateApp.Services.Interfaces;
using TemplateApp.Services.Models;
#else
using TemplateApp.SqlRepository.Entities;
using TemplateApp.SqlRepository.Interfaces;
#endif

namespace TemplateApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
#if (UseServices)
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> Get([FromQuery] string? search, CancellationToken cancellationToken)
        => Ok(await _customers.SearchAsync(search, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create(SaveCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _customers.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Duplicate customer", Detail = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, SaveCustomerRequest request, CancellationToken cancellationToken)
        => await _customers.UpdateAsync(id, request, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => await _customers.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
#else
    private readonly ICustomerRepository _customers;

    public CustomersController(ICustomerRepository customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Customer>>> Get([FromQuery] string? search, CancellationToken cancellationToken)
        => Ok(await _customers.SearchAsync(search ?? string.Empty, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Customer>> GetById(int id, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Customer>> Create(Customer customer, CancellationToken cancellationToken)
    {
        if (await _customers.ExistsAsync(c => c.Email == customer.Email, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate customer",
                Detail = $"A customer with the email '{customer.Email}' already exists.",
            });
        }

        await _customers.AddAsync(customer, cancellationToken);
        await _customers.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        _customers.Remove(customer);
        await _customers.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
#endif
}
