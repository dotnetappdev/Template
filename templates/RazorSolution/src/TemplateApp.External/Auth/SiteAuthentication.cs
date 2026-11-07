using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace TemplateApp.External.Auth;

public static class SiteAuthentication
{
    public const string AdminPolicy = "Admin";

    /// <summary>
    /// Cookie sign-in backed by ASP.NET Core Identity in TemplateApp.API (see IUserSession).
    /// </summary>
    public static IServiceCollection AddSiteAuthentication(this IServiceCollection services, bool requireSignedInUser)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserSession, UserSession>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "TemplateApp.External.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole("Admin"));
            if (requireSignedInUser)
            {
                // Every page needs a signed-in user unless it says [AllowAnonymous] (the Account pages do).
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            }
        });

        return services;
    }
}
