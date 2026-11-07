using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TemplateApp.Internal.ApiClients;
using TemplateApp.Internal.ApiClients.Models;

namespace TemplateApp.Internal.Auth;

/// <summary>
/// The user's session on this site: an encrypted cookie holding the user's claims plus the API's
/// access/refresh tokens. The site never sees the user database; the API (ASP.NET Core Identity) does.
/// </summary>
public interface IUserSession
{
    Task SignInAsync(AccessTokenResponse tokens, bool persistent, CancellationToken cancellationToken = default);

    /// <summary>The access token for API calls, refreshed when needed; null when nobody is signed in.</summary>
    Task<string?> GetAccessTokenAsync();

    Task SignOutAsync();
}

public class UserSession : IUserSession
{
    private const string RefreshedTokenKey = "UserSession.AccessToken";
    private readonly IHttpContextAccessor _httpContext;
    private readonly IAccountApiClient _account;
    private readonly ILogger<UserSession> _logger;

    public UserSession(IHttpContextAccessor httpContext, IAccountApiClient account, ILogger<UserSession> logger)
    {
        _httpContext = httpContext;
        _account = account;
        _logger = logger;
    }

    private HttpContext Context => _httpContext.HttpContext ?? throw new InvalidOperationException("No HTTP request.");

    public async Task SignInAsync(AccessTokenResponse tokens, bool persistent, CancellationToken cancellationToken = default)
    {
        var me = await _account.GetMeAsync(tokens.AccessToken, cancellationToken)
            ?? throw new InvalidOperationException("The API accepted the sign-in but did not return the user.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, me.Id),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(me.DisplayName) ? me.Email : me.DisplayName),
            new(ClaimTypes.Email, me.Email),
        };
        claims.AddRange(me.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role));
        var properties = new AuthenticationProperties { IsPersistent = persistent };
        StoreTokens(properties, tokens);

        await Context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (Context.Items.TryGetValue(RefreshedTokenKey, out var cached) && cached is string refreshedToken)
        {
            return refreshedToken;
        }

        var result = await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Properties is null)
        {
            return null;
        }

        var accessToken = result.Properties.GetTokenValue("access_token");
        var expiresAt = DateTimeOffset.TryParse(result.Properties.GetTokenValue("expires_at"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var e) ? e : DateTimeOffset.MinValue;
        if (accessToken is not null && expiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return accessToken;
        }

        // Expired: swap the refresh token for new tokens and update the cookie.
        var refreshToken = result.Properties.GetTokenValue("refresh_token");
        var tokens = refreshToken is null ? null : await _account.RefreshAsync(refreshToken);
        if (tokens is null)
        {
            _logger.LogInformation("The API session ended (refresh failed); signing the user out.");
            await SignOutAsync();
            return null;
        }

        StoreTokens(result.Properties, tokens);
        if (!Context.Response.HasStarted)
        {
            await Context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal!, result.Properties);
        }

        Context.Items[RefreshedTokenKey] = tokens.AccessToken;
        return tokens.AccessToken;
    }

    public Task SignOutAsync() => Context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    private static void StoreTokens(AuthenticationProperties properties, AccessTokenResponse tokens) =>
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = tokens.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = tokens.RefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn).ToString("o", CultureInfo.InvariantCulture) },
        ]);
}
