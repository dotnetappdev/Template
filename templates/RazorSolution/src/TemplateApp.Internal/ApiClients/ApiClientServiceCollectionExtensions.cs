using Microsoft.Extensions.Options;

namespace TemplateApp.Internal.ApiClients;

public static class ApiClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers one typed HttpClient per API client, all pointing at Api:BaseUrl.
    /// Add new clients here: services.AddApiClient&lt;IOrderApiClient, OrderApiClient&gt;();
    /// </summary>
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiOptions>()
            .Bind(configuration.GetSection(ApiOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Api:BaseUrl must be an absolute URL.")
            .ValidateOnStart();
#if (UseApiKey)
        services.AddTransient<ApiKeyHandler>();
#endif

        services.AddApiClient<IStatusApiClient, StatusApiClient>();
#if (UseSqlRepository)
        services.AddApiClient<ICustomerApiClient, CustomerApiClient>();
#endif

        return services;
    }

    private static IHttpClientBuilder AddApiClient<TClient, TImplementation>(this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
#if (UseApiKey)

        // Sends the X-Api-Key header the API expects.
        builder.AddHttpMessageHandler<ApiKeyHandler>();
#endif
#if (IncludeResilience)

        // Retries, timeouts and a circuit breaker (Microsoft.Extensions.Http.Resilience / Polly).
        builder.AddStandardResilienceHandler();
#endif

        return builder;
    }
}
