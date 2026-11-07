using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
#if (UseSharedSignIn)
using Microsoft.AspNetCore.DataProtection;
#endif

namespace TemplateApp.External.Auth;

public static class SiteAuthentication
{
    public const string AdminPolicy = "Admin";

    /// <summary>Roles allowed into the Internal (staff) site. Customers who register on External have neither.</summary>
    public static readonly string[] StaffRoles = ["Staff", "Admin"];

    /// <summary>
    /// Cookie sign-in backed by ASP.NET Core Identity in TemplateApp.API (see IUserSession).
    /// </summary>
    /// <param name="staffOnly">Every page needs a signed-in user with the Staff or Admin role (the Internal site).</param>
    public static IServiceCollection AddSiteAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment, bool staffOnly)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserSession, UserSession>();
#if (UseSharedSignIn)

        // Single sign-on between Internal and External: both sites use the same cookie name and the same
        // Data Protection keys, so a sign-in cookie issued by one site is accepted by the other.
        // Production: point Authentication:SharedKeysFolder at storage both sites share (or use
        // PersistKeysToAzureBlobStorage / a database), and set Authentication:CookieDomain (for example .example.com).
        var keysFolder = configuration["Authentication:SharedKeysFolder"];
        if (string.IsNullOrWhiteSpace(keysFolder))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Single sign-on needs Authentication:SharedKeysFolder, a folder both sites can read and write.");
            }

            keysFolder = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", ".keys"));
        }

        services.AddDataProtection()
            .SetApplicationName("TemplateApp")
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));
#endif

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
#if (UseSharedSignIn)
                options.Cookie.Name = "TemplateApp.Auth"; // shared by Internal and External
                if (configuration["Authentication:CookieDomain"] is { Length: > 0 } domain)
                {
                    options.Cookie.Domain = domain;
                }
#else
                options.Cookie.Name = "TemplateApp.External.Auth"; // this site only
#endif
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
            if (staffOnly)
            {
                // Every page needs a signed-in staff member unless it says [AllowAnonymous] (the Account pages do).
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireRole(StaffRoles)
                    .Build();
            }
        });

        return services;
    }
}
