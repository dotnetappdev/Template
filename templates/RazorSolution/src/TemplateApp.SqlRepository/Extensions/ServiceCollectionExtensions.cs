using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplateApp.SqlRepository.Interfaces;
using TemplateApp.SqlRepository.Repositories;

namespace TemplateApp.SqlRepository.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ApplicationDbContext"/> and every repository.
    /// Call it from Program.cs: <c>builder.Services.AddSqlRepository(builder.Configuration);</c>
    /// </summary>
    public static IServiceCollection AddSqlRepository(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseDefaults.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseDefaults.ConnectionStringName}' was not found. Add it to appsettings.json or user-secrets.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseConfiguredDatabase(connectionString));

        // Repositories: one interface per repository so consumers can be unit tested with fakes.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        return services;
    }

    /// <summary>
    /// Configures the database provider chosen when the solution was created.
    /// </summary>
    public static DbContextOptionsBuilder UseConfiguredDatabase(this DbContextOptionsBuilder options, string connectionString)
    {
#if (UseSqlServer)
        return options.UseSqlServer(connectionString, sql =>
        {
            // Targets the SQL Server version picked in the template (compatibility level __SQL_COMPAT_LEVEL__).
            sql.UseCompatibilityLevel(__SQL_COMPAT_LEVEL__);
            sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
#if (UseAzureSql)
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
#endif
        });
#elseif (UsePostgreSql)
        return options.UseNpgsql(connectionString, npgsql =>
        {
            // Targets the PostgreSQL version picked in the template.
            npgsql.SetPostgresVersion(__POSTGRES_MAJOR__, 0);
            npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        });
#else
        return options.UseSqlite(connectionString, sqlite =>
            sqlite.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
#endif
    }
}
