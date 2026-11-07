using Microsoft.AspNetCore.Identity;

namespace TemplateApp.SqlRepository.Entities;

/// <summary>
/// A user account (ASP.NET Core Identity). Add profile fields here; they become columns of AspNetUsers.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string? DisplayName { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
