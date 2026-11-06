using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if (SeedSampleData)
using TemplateApp.SqlRepository.Entities;
#endif

namespace TemplateApp.SqlRepository.Extensions;

public static class DatabaseInitializer
{
    /// <summary>
    /// Development helper: creates the database (or applies migrations once you have added some)
    /// and (optionally) seeds sample data. Called from Program.cs only in the Development environment.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        try
        {
            if (context.Database.GetMigrations().Any())
            {
                await context.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                // No migrations yet: create the schema straight from the model.
                // After "dotnet ef migrations add InitialCreate" this switches to MigrateAsync automatically
                // (drop the development database first).
                await context.Database.EnsureCreatedAsync(cancellationToken);
            }

#if (SeedSampleData)
            await SeedAsync(context, cancellationToken);
#endif
        }
        catch (Exception ex)
        {
            // Log and carry on so the site still starts (for example when the database server is not running yet).
            logger.LogError(ex, "An error occurred while initialising the database. Check the DefaultConnection connection string.");
        }
    }

#if (SeedSampleData)
    private static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Customers.AddRange(
            new Customer { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Company = "Analytical Engines" },
            new Customer { FirstName = "Alan", LastName = "Turing", Email = "alan@example.com", Company = "Bletchley Park" },
            new Customer { FirstName = "Grace", LastName = "Hopper", Email = "grace@example.com", Company = "US Navy" },
            new Customer { FirstName = "Linus", LastName = "Torvalds", Email = "linus@example.com", Company = "Linux Foundation", IsActive = false });

        await context.SaveChangesAsync(cancellationToken);
    }
#endif
}
