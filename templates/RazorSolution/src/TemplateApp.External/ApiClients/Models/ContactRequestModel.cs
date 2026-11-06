using System.ComponentModel.DataAnnotations;

namespace TemplateApp.External.ApiClients.Models;

/// <summary>Body for POST api/contact-requests. The attributes drive server and jQuery validation.</summary>
public class ContactRequestModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Topic { get; set; }

    [Display(Name = "Preferred date")]
    [StringLength(20)]
    public string? PreferredDate { get; set; }

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}
