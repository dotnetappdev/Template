using Microsoft.AspNetCore.Identity;
#if (UsePasskeys)
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif
using TemplateApp.SqlRepository;
using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.API.Identity;

/// <summary>
/// ASP.NET Core Identity lives in the API: users, roles, lockout, two-factor and passkeys are stored by
/// ApplicationDbContext. The Internal/External sites sign users in through the endpoints below and keep the
/// returned bearer tokens in their own cookie (they never touch the database).
/// </summary>
public static class IdentitySetup
{
    public const string AdminRole = "Admin";

    /// <summary>Needed (or Admin) to sign in to the Internal site. External customers get no role.</summary>
    public const string StaffRole = "Staff";

    public static IServiceCollection AddApiIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization();

        services.AddIdentityApiEndpoints<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = false; // Set to true once an IEmailSender<ApplicationUser> sends real email.
#if (UsePasskeys)
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3; // adds the passkeys table
#endif
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
#if (UsePasskeys)

        // Passkeys: the browser talks WebAuthn with the *sites*, so the API must accept their origins.
        // Identity:Passkeys:ServerDomain is the relying-party ID: the sites' shared domain (for example contoso.com),
        // or "localhost" in development. Identity:Passkeys:AllowedOrigins lists the sites' https origins.
        var passkeys = configuration.GetSection("Identity:Passkeys");
        var allowedOrigins = passkeys.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        services.Configure<IdentityPasskeyOptions>(options =>
        {
            options.ServerDomain = passkeys["ServerDomain"] is { Length: > 0 } domain ? domain : null;
            options.ValidateOrigin = context => ValueTask.FromResult(
                !context.CrossOrigin && allowedOrigins.Contains(context.Origin, StringComparer.OrdinalIgnoreCase));
        });
        services.TryAddScoped<IPasskeyHandler<ApplicationUser>, PasskeyHandler<ApplicationUser>>();
#endif

        return services;
    }

    /// <summary>
    /// Maps /api/account/register, /login, /refresh, /manage/info, /manage/2fa, /forgotPassword, ... (MapIdentityApi).
    /// </summary>
    public static void MapApiIdentity(this WebApplication app)
    {
        app.MapGroup("/api/account").MapIdentityApi<ApplicationUser>().WithTags("Account");
    }
}
