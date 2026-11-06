using System.ComponentModel.DataAnnotations;

namespace TemplateApp.API.Models;

/// <summary>
/// A message sent from the External site's Contact page.
/// </summary>
public class ContactRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PreferredDate { get; set; }

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}
