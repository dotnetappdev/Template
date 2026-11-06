namespace TemplateApp.Services.Models;

/// <summary>
/// What the web sites and API see. Entities stay inside the services and repository layers.
/// </summary>
public record CustomerDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    bool IsActive)
{
    public string FullName => $"{FirstName} {LastName}";
}

public record SaveCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    bool IsActive = true);
