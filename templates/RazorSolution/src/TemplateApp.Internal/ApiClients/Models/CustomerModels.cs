using System.ComponentModel.DataAnnotations;

namespace TemplateApp.Internal.ApiClients.Models;

/// <summary>A customer as returned by GET api/customers.</summary>
public record CustomerModel(int Id, string FirstName, string LastName, string Email, string? Company, bool IsActive)
{
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>Body for POST/PUT api/customers. The attributes drive server and jQuery validation.</summary>
public class SaveCustomerModel
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
