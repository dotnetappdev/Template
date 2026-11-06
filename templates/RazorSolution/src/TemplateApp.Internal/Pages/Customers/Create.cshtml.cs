using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
#if (UseServices)
using TemplateApp.Services.Interfaces;
using TemplateApp.Services.Models;
#else
using TemplateApp.SqlRepository.Entities;
using TemplateApp.SqlRepository.Interfaces;
#endif

namespace TemplateApp.Internal.Pages.Customers;

public class CreateModel : PageModel
{
#if (UseServices)
    private readonly ICustomerService _customers;

    public CreateModel(ICustomerService customers)
    {
        _customers = customers;
    }
#else
    private readonly ICustomerRepository _customers;

    public CreateModel(ICustomerRepository customers)
    {
        _customers = customers;
    }
#endif

    [BindProperty]
    public CustomerInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

#if (UseServices)
        try
        {
            await _customers.CreateAsync(
                new SaveCustomerRequest(Input.FirstName, Input.LastName, Input.Email, Input.Company, Input.IsActive),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Input.Email", ex.Message);
            return Page();
        }
#else
        if (await _customers.ExistsAsync(c => c.Email == Input.Email, cancellationToken))
        {
            ModelState.AddModelError("Input.Email", $"A customer with the email '{Input.Email}' already exists.");
            return Page();
        }

        await _customers.AddAsync(new Customer
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            Email = Input.Email,
            Company = Input.Company,
            IsActive = Input.IsActive,
        }, cancellationToken);
        await _customers.SaveChangesAsync(cancellationToken);
#endif

        TempData["Message"] = $"Customer {Input.FirstName} {Input.LastName} was created.";
        return RedirectToPage("Index");
    }

    public class CustomerInput
    {
        [Required, StringLength(100), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Company { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
