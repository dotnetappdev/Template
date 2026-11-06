using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplateApp.Services.Interfaces;
using TemplateApp.SqlRepository.Extensions;

namespace TemplateApp.Services.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services layer and the SqlRepository layer under it.
    /// Call it from Program.cs: <c>builder.Services.AddApplicationServices(builder.Configuration);</c>
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSqlRepository(configuration);

        services.AddScoped<ICustomerService, CustomerService>();

        return services;
    }
}
