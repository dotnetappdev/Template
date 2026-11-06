using RazorSolution.Wizard.Core;

namespace RazorSolution.Wizard.Tests;

public class MetadataTests
{
    private readonly TemplateMetadata _metadata = TemplateMetadata.LoadEmbedded();

    [Fact]
    public void Reads_the_template_parameters_with_their_cli_names()
    {
        Assert.Equal("razorsln", _metadata.ShortName);
        Assert.Equal("--framework", _metadata.Fields["Framework"].CliName);
        Assert.Equal("--bootstrap-theme", _metadata.Fields["BootstrapTheme"].CliName);
        Assert.Equal("--internal-auth", _metadata.Fields["InternalAuthentication"].CliName);
        Assert.Contains(_metadata.Fields["BootstrapVersion"].Choices, c => c.Value == "5.3.8" && c.Display == "Bootstrap 5.3.8");
        Assert.Contains(_metadata.Fields["SqlServerVersion"].Choices, c => c.Value == "AzureSql");
        Assert.Equal(FieldKind.Bool, _metadata.Fields["IncludeApi"].Kind);
        Assert.Equal(FieldKind.Text, _metadata.Fields["DbServer"].Kind);
    }

    [Fact]
    public void Every_step_field_is_a_template_parameter_and_every_visible_option_is_on_a_step()
    {
        var stepFields = _metadata.Steps.SelectMany(s => s.Fields).ToList();
        Assert.All(stepFields, f => Assert.True(_metadata.Fields.ContainsKey(f), f));

        // Everything Visual Studio shows on its single page must also be somewhere in the wizard.
        var ideHost = File.ReadAllText(Path.Combine(Paths.TemplateFolder, ".template.config", "ide.host.json"));
        var visible = Newtonsoft.Json.Linq.JObject.Parse(ideHost)["symbolInfo"]!
            .Where(s => (bool?)s["isVisible"] == true)
            .Select(s => (string)s["id"]!);
        Assert.All(visible, id => Assert.Contains(id, stepFields));
    }

    [Fact]
    public void Database_step_is_hidden_when_there_is_no_data_layer()
    {
        var state = new WizardState(_metadata, "Acme.Portal");
        var database = _metadata.Steps.Single(s => s.Id == "database");
        Assert.True(state.IsStepVisible(database));

        state.SetBool("IncludeSqlRepository", false);
        state.SetBool("IncludeServices", false);
        Assert.False(state.IsStepVisible(database));
    }

    [Theory]
    [InlineData("SqlServer", "Integrated", false)]
    [InlineData("SqlServer", "SqlLogin", true)]
    [InlineData("PostgreSQL", "Integrated", true)]
    [InlineData("Sqlite", "SqlLogin", false)]
    public void User_and_password_only_show_when_they_are_used(string provider, string auth, bool visible)
    {
        var state = new WizardState(_metadata, "Acme.Portal");
        state["DatabaseProvider"] = provider;
        state["DbAuthentication"] = auth;

        Assert.Equal(visible, state.IsFieldVisible("DbUser"));
        Assert.Equal(visible, state.IsFieldVisible("DbPassword"));
        Assert.Equal(provider == "SqlServer", state.IsFieldVisible("SqlServerVersion"));
    }

    [Fact]
    public void Entra_fields_only_show_for_entra_sign_in()
    {
        var state = new WizardState(_metadata, "Acme.Portal");
        Assert.False(state.IsFieldVisible("EntraTenantId"));
        state["InternalAuthentication"] = "EntraId";
        Assert.True(state.IsFieldVisible("EntraTenantId"));
        state.SetBool("IncludeInternal", false);
        Assert.False(state.IsFieldVisible("EntraTenantId"));
    }
}
