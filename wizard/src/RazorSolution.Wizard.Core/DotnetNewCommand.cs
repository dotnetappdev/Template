using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorSolution.Wizard.Core;

/// <summary>Builds the dotnet new command for the wizard's choices.</summary>
public static class DotnetNewCommand
{
    /// <summary>Arguments after "dotnet": new razorsln -n Name -o Dir --switch value ...</summary>
    public static IReadOnlyList<string> Arguments(WizardState state, string outputDirectory, bool hidePasswords = false)
    {
        var args = new List<string> { "new", state.Metadata.ShortName, "-n", state.SolutionName, "-o", outputDirectory };

        foreach (var pair in state.ChangedValues)
        {
            var field = state.Metadata.Fields[pair.Key];
            args.Add(field.CliName);
            args.Add(hidePasswords && state.Metadata.PasswordFields.Contains(field.Id) && pair.Value.Length > 0 ? "****" : pair.Value);
        }

        return args;
    }

    /// <summary>A copy-and-paste command for the summary page (passwords hidden).</summary>
    public static string Display(WizardState state, string outputDirectory) =>
        "dotnet " + string.Join(" ", Arguments(state, outputDirectory, hidePasswords: true).Select(Quote));

    /// <summary>Joins arguments into one Windows command line (ProcessStartInfo.Arguments on .NET Framework).</summary>
    public static string ToCommandLine(IEnumerable<string> arguments) => string.Join(" ", arguments.Select(Quote));

    /// <summary>Quotes one argument using the rules of CommandLineToArgvW / the .NET runtime.</summary>
    public static string Quote(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
        {
            return argument;
        }

        var sb = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            if (c == '"')
            {
                sb.Append('\\', backslashes * 2 + 1).Append('"');
            }
            else
            {
                sb.Append('\\', backslashes).Append(c);
            }

            backslashes = 0;
        }

        sb.Append('\\', backslashes * 2).Append('"');
        return sb.ToString();
    }
}
