using System.ComponentModel.DataAnnotations;

namespace TemplateApp.Internal.ApiClients.Models;

public record AdminUser(
    string Id, string Email, string? DisplayName, IReadOnlyList<string> Roles, bool EmailConfirmed,
    bool TwoFactorEnabled, bool LockedOut, DateTimeOffset? LockoutEnd, int AccessFailedCount, int PasskeyCount, DateTime CreatedUtc);

public class CreateUserInput
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Display name"), StringLength(100)]
    public string? DisplayName { get; set; }

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Temporary password")]
    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}
