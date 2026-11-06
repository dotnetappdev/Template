namespace TemplateApp.External.ApiClients;

/// <summary>
/// Settings for calling TemplateApp.API (section "Api" in appsettings.json).
/// </summary>
public class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>The API's base address, for example https://localhost:7001/.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
