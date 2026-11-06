using Microsoft.Extensions.Options;

namespace TemplateApp.External.ApiClients;

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

        services.AddApiClient<IStatusApiClient, StatusApiClient>();
        services.AddApiClient<IContactApiClient, ContactApiClient>();

        return services;
    }

    private static IHttpClientBuilder AddApiClient<TClient, TImplementation>(this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
    }
}
