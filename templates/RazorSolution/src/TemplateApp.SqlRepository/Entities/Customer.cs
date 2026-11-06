namespace TemplateApp.SqlRepository.Entities;

/// <summary>
/// Sample entity. Replace it with your own domain entities.
/// </summary>
public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Company { get; set; }

    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}
