using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.API.Controllers;

/// <summary>
/// The signed-in user's profile, used by the sites right after sign-in to build their cookie.
/// Sign-in itself (register, login, refresh, two-factor, change password) is MapIdentityApi under /api/account.
/// </summary>
[ApiController]
[Authorize]
[Route("api/account")]
[Produces("application/json")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;

    public AccountController(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public record MeResponse(string Id, string Email, string? DisplayName, IReadOnlyList<string> Roles, bool TwoFactorEnabled, int PasskeyCount);

    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

#if (UsePasskeys)
        var passkeys = (await _users.GetPasskeysAsync(user)).Count;
#else
        var passkeys = 0;
#endif
        return new MeResponse(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.DisplayName,
            (await _users.GetRolesAsync(user)).ToList(),
            await _users.GetTwoFactorEnabledAsync(user),
            passkeys);
    }
}
