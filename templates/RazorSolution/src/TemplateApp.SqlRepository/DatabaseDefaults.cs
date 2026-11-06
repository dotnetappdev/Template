namespace TemplateApp.SqlRepository;

/// <summary>
/// Defaults chosen when the solution was created from the template.
/// </summary>
public static class DatabaseDefaults
{
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>
    /// The development connection string (also in each web project's appsettings.json).
    /// Keep real passwords in user-secrets or a key vault, not in source control.
    /// </summary>
#if (HasCustomConnectionString)
    public const string DevelopmentConnectionString = "__CUSTOM_CONNECTION_STRING__";
#elseif (UseSqlite)
    public const string DevelopmentConnectionString = "Data Source=__DB_NAME__.db";
#elseif (UsePostgreSql)
    public const string DevelopmentConnectionString = "Host=__DB_SERVER__;Port=5432;Database=__DB_NAME__;Username=__DB_USER__;Password=__DB_PASSWORD__";
#elseif (UseAzureSql && UseIntegratedAuth)
    public const string DevelopmentConnectionString = "Server=tcp:__DB_SERVER__,1433;Database=__DB_NAME__;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
#elseif (UseAzureSql)
    public const string DevelopmentConnectionString = "Server=tcp:__DB_SERVER__,1433;Database=__DB_NAME__;User ID=__DB_USER__;Password=__DB_PASSWORD__;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
#elseif (UseIntegratedAuth)
    public const string DevelopmentConnectionString = "Server=__DB_SERVER__;Database=__DB_NAME__;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
#else
    public const string DevelopmentConnectionString = "Server=__DB_SERVER__;Database=__DB_NAME__;User Id=__DB_USER__;Password=__DB_PASSWORD__;MultipleActiveResultSets=true;TrustServerCertificate=True";
#endif
}
