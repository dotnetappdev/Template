using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.API.Controllers;

/// <summary>
/// Passkeys (WebAuthn) through the API. The site asks for options, keeps the returned State (it never goes
/// to the browser), runs navigator.credentials in the browser, then sends the credential + State back here.
/// Uses ASP.NET Core Identity's IPasskeyHandler (.NET 10+).
/// </summary>
[ApiController]
[Route("api/account/passkeys")]
[Produces("application/json")]
public class PasskeysController : ControllerBase
{
    private readonly IPasskeyHandler<ApplicationUser> _passkeys;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public PasskeysController(IPasskeyHandler<ApplicationUser> passkeys, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _passkeys = passkeys;
        _users = users;
        _signIn = signIn;
    }

    public record OptionsResponse(string OptionsJson, string State);

    public record CompleteRequest(string CredentialJson, string State, string? Name);

    public record RequestOptionsRequest(string? Email);

    public record PasskeyResponse(string Id, string Name, DateTimeOffset CreatedAt);

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PasskeyResponse>>> List()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var passkeys = await _users.GetPasskeysAsync(user);
        return Ok(passkeys.Select(p => new PasskeyResponse(WebEncoders.Base64UrlEncode(p.CredentialId), p.Name ?? "Passkey", p.CreatedAt)));
    }

    /// <summary>Step 1 of adding a passkey: WebAuthn creation options for the signed-in user.</summary>
    [Authorize]
    [HttpPost("creation-options")]
    public async Task<ActionResult<OptionsResponse>> CreationOptions()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var entity = new PasskeyUserEntity
        {
            Id = await _users.GetUserIdAsync(user),
            Name = await _users.GetUserNameAsync(user) ?? user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? user.Email ?? string.Empty,
        };
        var options = await _passkeys.MakeCreationOptionsAsync(entity, HttpContext);
        return new OptionsResponse(options.CreationOptionsJson, options.AttestationState ?? string.Empty);
    }

    /// <summary>Step 2 of adding a passkey: verify the browser's credential and store it.</summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add(CompleteRequest request)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _passkeys.PerformAttestationAsync(new PasskeyAttestationContext
        {
            HttpContext = HttpContext,
            CredentialJson = request.CredentialJson,
            AttestationState = request.State,
        });
        if (!result.Succeeded || result.UserEntity.Id != user.Id)
        {
            return Problem(title: "The passkey could not be added", detail: result.Failure?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        result.Passkey.Name = string.IsNullOrWhiteSpace(request.Name) ? "Passkey" : request.Name.Trim();
        var saved = await _users.AddOrUpdatePasskeyAsync(user, result.Passkey);
        return saved.Succeeded ? NoContent() : Problem(detail: string.Join("; ", saved.Errors.Select(e => e.Description)), statusCode: 400);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _users.RemovePasskeyAsync(user, WebEncoders.Base64UrlDecode(id));
        return result.Succeeded ? NoContent() : NotFound();
    }

    /// <summary>Step 1 of signing in with a passkey. Email is optional (discoverable passkeys need none).</summary>
    [AllowAnonymous]
    [HttpPost("request-options")]
    public async Task<ActionResult<OptionsResponse>> RequestOptions(RequestOptionsRequest request)
    {
        var user = string.IsNullOrWhiteSpace(request.Email) ? null : await _users.FindByEmailAsync(request.Email);
        var options = await _passkeys.MakeRequestOptionsAsync(user, HttpContext);
        return new OptionsResponse(options.RequestOptionsJson, options.AssertionState ?? string.Empty);
    }

    /// <summary>
    /// Step 2 of signing in with a passkey. On success the response is the same bearer-token JSON as
    /// /api/account/login (tokenType, accessToken, expiresIn, refreshToken).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(CompleteRequest request)
    {
        var result = await _passkeys.PerformAssertionAsync(new PasskeyAssertionContext
        {
            HttpContext = HttpContext,
            CredentialJson = request.CredentialJson,
            AssertionState = request.State,
        });
        if (!result.Succeeded)
        {
            return Problem(title: "Passkey sign-in failed", detail: result.Failure?.Message, statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = result.User;
        if (await _users.IsLockedOutAsync(user) || !await _signIn.CanSignInAsync(user))
        {
            return Problem(title: "Passkey sign-in failed", detail: "LockedOut", statusCode: StatusCodes.Status401Unauthorized);
        }

        // Keep the signature counter and backup flags current.
        await _users.AddOrUpdatePasskeyAsync(user, result.Passkey);

        // The bearer-token scheme writes the token response itself, exactly like MapIdentityApi's /login.
        _signIn.AuthenticationScheme = IdentityConstants.BearerScheme;
        await _signIn.SignInAsync(user, isPersistent: false, authenticationMethod: "passkey");
        return new EmptyResult();
    }
}
