using Microsoft.AspNetCore.Identity;
using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.API.Identity;

/// <summary>
/// Development helper: creates the Admin and Staff roles and, while there are no users yet, the admin account from
/// Identity:SeedAdmin (appsettings.Development.json) so you can sign in to the Internal site straight away.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedIdentityAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IdentitySeeder));

        try
        {
            foreach (var role in new[] { IdentitySetup.AdminRole, IdentitySetup.StaffRole })
            {
                if (!await roles.RoleExistsAsync(role))
                {
                    await roles.CreateAsync(new IdentityRole(role));
                }
            }

            var email = configuration["Identity:SeedAdmin:Email"];
            var password = configuration["Identity:SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || users.Users.Any())
            {
                return;
            }

            var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Administrator" };
            var result = await users.CreateAsync(admin, password);
            if (result.Succeeded)
            {
                await users.AddToRoleAsync(admin, IdentitySetup.AdminRole);
                logger.LogInformation("Created the development admin account {Email}.", email);
            }
            else
            {
                logger.LogWarning("Could not create the admin account: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not seed Identity. Is the database available?");
        }
    }
}
