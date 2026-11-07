using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateApp.API.Identity;
using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.API.Controllers;

/// <summary>
/// User and role administration for the Internal site's admin pages. Admin role only.
/// </summary>
[ApiController]
[Authorize(Roles = IdentitySetup.AdminRole)]
[Route("api/admin")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public AdminUsersController(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    public record UserResponse(
        string Id, string Email, string? DisplayName, IReadOnlyList<string> Roles, bool EmailConfirmed,
        bool TwoFactorEnabled, bool LockedOut, DateTimeOffset? LockoutEnd, int AccessFailedCount, int PasskeyCount, DateTime CreatedUtc);

    public record CreateUserRequest(string Email, string Password, string? DisplayName, IReadOnlyList<string>? Roles);

    public record RolesRequest(IReadOnlyList<string> Roles);

    [HttpGet("roles")]
    public async Task<IReadOnlyList<string>> Roles() =>
        await _roles.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync();

    [HttpGet("users")]
    public async Task<IReadOnlyList<UserResponse>> Users()
    {
        var users = await _users.Users.OrderBy(u => u.Email).ToListAsync();
        var list = new List<UserResponse>();
        foreach (var user in users)
        {
            list.Add(await ToResponseAsync(user));
        }

        return list;
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserResponse>> GetUser(string id) =>
        await _users.FindByIdAsync(id) is { } user ? await ToResponseAsync(user) : NotFound();

    [HttpPost("users")]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true, DisplayName = request.DisplayName };
        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return ValidationErrors(created);
        }

        if (request.Roles is { Count: > 0 })
        {
            await _users.AddToRolesAsync(user, request.Roles);
        }

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, await ToResponseAsync(user));
    }

    [HttpPut("users/{id}/roles")]
    public async Task<IActionResult> SetRoles(string id, RolesRequest request)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (IsSelf(user) && !request.Roles.Contains(IdentitySetup.AdminRole))
        {
            return Problem(detail: "You cannot remove your own Admin role.", statusCode: 400);
        }

        var current = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, current.Except(request.Roles));
        await _users.AddToRolesAsync(user, request.Roles.Except(current));
        return NoContent();
    }

    [HttpPost("users/{id}/lock")]
    public async Task<IActionResult> Lock(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (IsSelf(user))
        {
            return Problem(detail: "You cannot lock your own account.", statusCode: 400);
        }

        await _users.SetLockoutEnabledAsync(user, true);
        await _users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await _users.UpdateSecurityStampAsync(user); // signs the user out on their next token refresh
        return NoContent();
    }

    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _users.SetLockoutEndDateAsync(user, null);
        await _users.ResetAccessFailedCountAsync(user);
        return NoContent();
    }

    /// <summary>Turns two-factor off and forgets the authenticator key (the user sets it up again).</summary>
    [HttpPost("users/{id}/reset-2fa")]
    public async Task<IActionResult> ResetTwoFactor(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _users.SetTwoFactorEnabledAsync(user, false);
        await _users.ResetAuthenticatorKeyAsync(user);
        return NoContent();
    }
#if (UsePasskeys)

    [HttpDelete("users/{id}/passkeys")]
    public async Task<IActionResult> RemovePasskeys(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        foreach (var passkey in await _users.GetPasskeysAsync(user))
        {
            await _users.RemovePasskeyAsync(user, passkey.CredentialId);
        }

        return NoContent();
    }
#endif

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (IsSelf(user))
        {
            return Problem(detail: "You cannot delete your own account.", statusCode: 400);
        }

        await _users.DeleteAsync(user);
        return NoContent();
    }

    private bool IsSelf(ApplicationUser user) => user.Id == _users.GetUserId(User);

    private ActionResult ValidationErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(ModelState);
    }

    private async Task<UserResponse> ToResponseAsync(ApplicationUser user)
    {
#if (UsePasskeys)
        var passkeys = (await _users.GetPasskeysAsync(user)).Count;
#else
        var passkeys = 0;
#endif
        var lockoutEnd = await _users.GetLockoutEndDateAsync(user);
        return new UserResponse(
            user.Id, user.Email ?? string.Empty, user.DisplayName, (await _users.GetRolesAsync(user)).ToList(), user.EmailConfirmed,
            user.TwoFactorEnabled, lockoutEnd > DateTimeOffset.UtcNow, lockoutEnd, user.AccessFailedCount, passkeys, user.CreatedUtc);
    }
}
