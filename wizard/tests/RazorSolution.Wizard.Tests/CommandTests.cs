using RazorSolution.Wizard.Core;

namespace RazorSolution.Wizard.Tests;

public class CommandTests
{
    private readonly TemplateMetadata _metadata = TemplateMetadata.LoadEmbedded();

    [Fact]
    public void Only_changed_values_go_on_the_command_line()
    {
        var state = new WizardState(_metadata, "Acme.Portal");
        state["Framework"] = "net10.0";
        state["BootstrapTheme"] = "flatly";
        state.SetBool("IncludeExternal", false);

        var args = DotnetNewCommand.Arguments(state, @"C:\src\Acme.Portal");

        Assert.Equal(new[] { "new", "razorsln", "-n", "Acme.Portal", "-o", @"C:\src\Acme.Portal" }, args.Take(6));
        Assert.Equal(new[] { "--framework", "net10.0", "--external", "false", "--bootstrap-theme", "flatly" }, args.Skip(6));
    }

    [Fact]
    public void Display_hides_the_password()
    {
        var state = new WizardState(_metadata, "Acme.Portal");
        state["DbPassword"] = "S3cret!";

        var display = DotnetNewCommand.Display(state, "out");

        Assert.DoesNotContain("S3cret!", display);
        Assert.Contains("--db-password ****", display);
        Assert.Contains("S3cret!", DotnetNewCommand.Arguments(state, "out"));
    }

    [Theory]
    [InlineData("simple", "simple")]
    [InlineData("two words", "\"two words\"")]
    [InlineData(@"C:\My Projects\", "\"C:\\My Projects\\\\\"")]
    [InlineData("P@ss\"w\\rd", "\"P@ss\\\"w\\rd\"")]
    [InlineData("", "\"\"")]
    public void Quote_follows_windows_command_line_rules(string input, string expected)
    {
        Assert.Equal(expected, DotnetNewCommand.Quote(input));
    }

    [Fact]
    public void Validate_flags_an_empty_selection()
    {
        var state = new WizardState(_metadata, "Acme.Portal");
        foreach (var id in new[] { "IncludeInternal", "IncludeExternal", "IncludeApi", "IncludeServices", "IncludeSqlRepository" })
        {
            state.SetBool(id, false);
        }

        Assert.Contains("Select at least one project.", state.Validate());
    }

    [Fact]
    public void Mask_hides_passwords()
    {
        Assert.Equal("Host=h;Username=u;Password=****", ConnectionStrings.Mask("Host=h;Username=u;Password=abc"));
    }
}
