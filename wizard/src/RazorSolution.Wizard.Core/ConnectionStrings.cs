using System;

namespace RazorSolution.Wizard.Core;

/// <summary>
/// Builds the development connection string exactly as the template writes it to the API's appsettings.json
/// (see templates/RazorSolution/src/TemplateApp.API/appsettings.json), so "Test connection" tests what will be generated.
/// </summary>
public static class ConnectionStrings
{
    public static string Provider(WizardState state) => state["DatabaseProvider"];

    public static string Build(WizardState state)
    {
        var custom = state["ConnectionString"].Trim();
        if (custom.Length > 0)
        {
            return custom;
        }

        var provider = state["DatabaseProvider"];
        var azure = provider == "SqlServer" && state["SqlServerVersion"] == "AzureSql";
        var integrated = state["DbAuthentication"] == "Integrated";

        var server = Or(state["DbServer"], provider == "SqlServer"
            ? (azure ? "your-server.database.windows.net" : @"(localdb)\MSSQLLocalDB")
            : "localhost");
        var database = Or(state["DbName"], state.SolutionName);
        var user = Or(state["DbUser"], provider == "PostgreSQL" ? "postgres" : "sa");
        var password = Or(state["DbPassword"], "Your_password123");

        switch (provider)
        {
            case "Sqlite":
                return $"Data Source={database}.db";
            case "PostgreSQL":
                return $"Host={server};Port=5432;Database={database};Username={user};Password={password}";
        }

        if (azure)
        {
            return integrated
                ? $"Server=tcp:{server},1433;Database={database};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30"
                : $"Server=tcp:{server},1433;Database={database};User ID={user};Password={password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
        }

        return integrated
            ? $"Server={server};Database={database};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
            : $"Server={server};Database={database};User Id={user};Password={password};MultipleActiveResultSets=true;TrustServerCertificate=True";
    }

    /// <summary>The connection string with the password replaced by ****, for display.</summary>
    public static string Mask(string connectionString)
    {
        var parts = connectionString.Split(';');
        for (var i = 0; i < parts.Length; i++)
        {
            var eq = parts[i].IndexOf('=');
            if (eq > 0)
            {
                var key = parts[i].Substring(0, eq).Trim();
                if (key.Equals("Password", StringComparison.OrdinalIgnoreCase) || key.Equals("Pwd", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = parts[i].Substring(0, eq + 1) + "****";
                }
            }
        }

        return string.Join(";", parts);
    }

    private static string Or(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
