using Newtonsoft.Json.Linq;
using RazorSolution.Wizard.Core;

namespace RazorSolution.Wizard.Tests;

/// <summary>
/// Runs the real "dotnet new" (in a private template hive) through SolutionGenerator and checks that
/// the connection string the wizard tests is exactly the one the template writes into the API.
/// </summary>
public class GeneratedSolutionTests : IDisposable
{
    private readonly string _work = Directory.CreateTempSubdirectory("wizard-tests-").FullName;
    private readonly TemplateMetadata _metadata = TemplateMetadata.LoadEmbedded();

    public void Dispose()
    {
        try { Directory.Delete(_work, recursive: true); } catch (IOException) { }
    }

    public static TheoryData<string, string[]> Cases => new()
    {
        { "Default", Array.Empty<string>() },
        { "SqlLogin", new[] { "DbAuthentication=SqlLogin", "DbServer=SQL01\\APPS", "DbUser=app", "DbPassword=P@ss\"w\\rd" } },
        { "Azure", new[] { "SqlServerVersion=AzureSql", "DbServer=contoso.database.windows.net" } },
        { "AzureLogin", new[] { "SqlServerVersion=AzureSql", "DbAuthentication=SqlLogin", "DbName=Portal" } },
        { "Postgres", new[] { "DatabaseProvider=PostgreSQL", "DbUser=portal", "DbPassword=x" } },
        { "Sqlite", new[] { "DatabaseProvider=Sqlite" } },
        { "Custom", new[] { "ConnectionString=Server=.\\SQLEXPRESS;Database=Custom;Trusted_Connection=True" } },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Wizard_connection_string_matches_the_generated_api_settings(string name, string[] settings)
    {
        var hive = Path.Combine(_work, "hive");
        var generator = new SolutionGenerator(bundledTemplatePackage: null)
        {
            ExtraNewArguments = new[] { "--debug:custom-hive", hive },
        };
        var install = await ProcessRunner.RunDotnetAsync(new[] { "new", "install", Paths.TemplateFolder, "--debug:custom-hive", hive }, null, TimeSpan.FromMinutes(2));
        Assert.True(install.ExitCode == 0, install.Output + install.Error);

        var state = new WizardState(_metadata, "Acme." + name);
        foreach (var setting in settings)
        {
            var eq = setting.IndexOf('=');
            state[setting[..eq]] = setting[(eq + 1)..];
        }

        // Only the data layer is needed for this check.
        state.SetBool("IncludeInternal", false);
        state.SetBool("IncludeExternal", false);
        state.SetBool("IncludeTests", false);
        state.SetBool("skipRestore", true);

        var output = Path.Combine(_work, name);
        var result = await generator.GenerateAsync(state, output);
        Assert.True(result.ExitCode == 0, result.Output + result.Error);

        var settingsFile = Path.Combine(output, "src", $"Acme.{name}.API", "appsettings.json");
        var generated = (string)JObject.Parse(File.ReadAllText(settingsFile))["ConnectionStrings"]!["DefaultConnection"]!;

        Assert.Equal(generated, ConnectionStrings.Build(state));
    }
}
