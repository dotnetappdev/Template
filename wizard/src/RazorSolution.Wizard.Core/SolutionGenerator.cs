using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RazorSolution.Wizard.Core;

/// <summary>
/// Creates the solution with "dotnet new razorsln". If the template is not installed yet,
/// it installs the template package that ships inside the extension first.
/// </summary>
public sealed class SolutionGenerator
{
    private readonly string? _bundledTemplatePackage;

    public SolutionGenerator(string? bundledTemplatePackage)
    {
        _bundledTemplatePackage = bundledTemplatePackage;
    }

    /// <summary>Extra arguments for every dotnet new call (for example --debug:custom-hive in tests).</summary>
    public string[] ExtraNewArguments { get; set; } = Array.Empty<string>();

    public async Task<ProcessResult> GenerateAsync(WizardState state, string outputDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var timeout = TimeSpan.FromMinutes(5);
        var args = DotnetNewCommand.Arguments(state, outputDirectory).Concat(ExtraNewArguments).ToArray();

        progress?.Report("Running " + DotnetNewCommand.Display(state, outputDirectory));
        var result = await ProcessRunner.RunDotnetAsync(args, null, timeout, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0 && LooksLikeTemplateNotInstalled(result) && _bundledTemplatePackage != null && File.Exists(_bundledTemplatePackage))
        {
            progress?.Report("Installing the template package that ships with the extension…");
            var install = await ProcessRunner.RunDotnetAsync(new[] { "new", "install", _bundledTemplatePackage, "--force" }.Concat(ExtraNewArguments), null, timeout, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (install.ExitCode != 0)
            {
                return install;
            }

            progress?.Report("Running " + DotnetNewCommand.Display(state, outputDirectory));
            result = await ProcessRunner.RunDotnetAsync(args, null, timeout, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private static bool LooksLikeTemplateNotInstalled(ProcessResult result)
    {
        // dotnet new exit code 103 = template not found (https://aka.ms/templating-exit-codes).
        var text = result.Output + result.Error;
        return result.ExitCode == 103
            || text.IndexOf("No templates found", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("No templates or subcommands found", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
