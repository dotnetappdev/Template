using System.ComponentModel.DataAnnotations;

namespace TemplateApp.Internal.ApiClients.Models;

/// <summary>The bearer tokens returned by the API's /api/account/login, /refresh and passkey login.</summary>
public record AccessTokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);

public record LoginResult(bool Succeeded, bool RequiresTwoFactor, bool LockedOut, AccessTokenResponse? Tokens, string? Error);

/// <summary>GET /api/account/me.</summary>
public record MeResponse(string Id, string Email, string? DisplayName, IReadOnlyList<string> Roles, bool TwoFactorEnabled, int PasskeyCount);

/// <summary>POST /api/account/manage/2fa.</summary>
public record TwoFactorStatus(string SharedKey, int RecoveryCodesLeft, string[]? RecoveryCodes, bool IsTwoFactorEnabled, bool IsMachineRemembered);

/// <summary>WebAuthn options for the browser plus the API's state (kept by the site, never sent to the browser).</summary>
public record PasskeyOptions(string OptionsJson, string State);

public record PasskeyItem(string Id, string Name, DateTimeOffset CreatedAt);

public class LoginInput
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }
}
