using RazorSolution.Wizard.Core;

namespace RazorSolution.Wizard.Tests;

/// <summary>Runs the real out-of-process tester, as the Visual Studio wizard does.</summary>
public class ConnectionTesterTests
{
    private readonly TemplateMetadata _metadata = TemplateMetadata.LoadEmbedded();

    [Fact]
    public async Task Sqlite_reports_that_the_database_will_be_created()
    {
        var state = new WizardState(_metadata, "Acme.Portal") { ["DatabaseProvider"] = "Sqlite" };

        var result = await new ConnectionTester(Paths.TesterDll).TestAsync(state);

        Assert.True(result.Success, result.Message + " " + result.Detail);
        Assert.Contains("Acme.Portal.db", result.Message);
    }

    [Fact]
    public async Task Unreachable_postgres_server_fails_with_a_helpful_message()
    {
        var state = new WizardState(_metadata, "Acme.Portal")
        {
            ["DatabaseProvider"] = "PostgreSQL",
            ["ConnectionString"] = "Host=127.0.0.1;Port=1;Database=x;Username=u;Password=p",
        };

        var result = await new ConnectionTester(Paths.TesterDll).TestAsync(state, timeoutSeconds: 3);

        Assert.False(result.Success);
        Assert.Contains("not found or refused", result.Message);
    }

    [Fact]
    public async Task Invalid_connection_string_is_reported()
    {
        var state = new WizardState(_metadata, "Acme.Portal") { ["ConnectionString"] = "not a connection string" };

        var result = await new ConnectionTester(Paths.TesterDll).TestAsync(state, timeoutSeconds: 3);

        Assert.False(result.Success);
        Assert.Equal("The connection string is not valid.", result.Message);
    }

    [Fact]
    public async Task Missing_tester_is_reported()
    {
        var result = await new ConnectionTester("/nope/tester.dll").TestAsync(new WizardState(_metadata, "Acme"));
        Assert.False(result.Success);
    }

    /// <summary>Set WIZARD_TEST_POSTGRES to a reachable server's connection string to run this.</summary>
    [Fact]
    public async Task Real_postgres_server_connects_when_configured()
    {
        var cs = Environment.GetEnvironmentVariable("WIZARD_TEST_POSTGRES");
        if (string.IsNullOrEmpty(cs))
        {
            return;
        }

        var state = new WizardState(_metadata, "Acme.Portal") { ["DatabaseProvider"] = "PostgreSQL", ["ConnectionString"] = cs };
        var result = await new ConnectionTester(Paths.TesterDll).TestAsync(state);

        Assert.True(result.Success, result.Message + " " + result.Detail);
    }
}
