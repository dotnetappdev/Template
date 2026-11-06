using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RazorSolution.Wizard.Core;

public sealed class ConnectionTestResult
{
    public ConnectionTestResult(bool success, string message, string? detail)
    {
        Success = success;
        Message = message;
        Detail = detail;
    }

    public bool Success { get; }

    public string Message { get; }

    public string? Detail { get; }
}

/// <summary>
/// Runs RazorSolution.ConnectionTester.dll (shipped next to the wizard) in its own process,
/// so SQL Server / PostgreSQL drivers are never loaded into Visual Studio.
/// </summary>
public sealed class ConnectionTester
{
    private readonly string _testerDll;

    public ConnectionTester(string testerDll)
    {
        _testerDll = testerDll;
    }

    public async Task<ConnectionTestResult> TestAsync(WizardState state, int timeoutSeconds = 10, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_testerDll))
        {
            return new ConnectionTestResult(false, "The connection tester is missing from the extension.", _testerDll);
        }

        var provider = ConnectionStrings.Provider(state);
        var connectionString = ConnectionStrings.Build(state);
        var args = new[] { _testerDll, "--provider", provider, "--timeout", timeoutSeconds.ToString(), "--base-dir", Path.GetTempPath() };

        var result = await ProcessRunner.RunDotnetAsync(args, connectionString, TimeSpan.FromSeconds(timeoutSeconds * 2 + 15), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.TimedOut)
        {
            return new ConnectionTestResult(false, "The connection test did not finish in time.", null);
        }

        var json = result.Output.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith("{", StringComparison.Ordinal));
        if (json == null)
        {
            return new ConnectionTestResult(false, "The connection tester did not answer.", (result.Error + result.Output).Trim());
        }

        var doc = JObject.Parse(json);
        return new ConnectionTestResult((bool?)doc["success"] ?? false, (string?)doc["message"] ?? "Unknown result.", (string?)doc["detail"]);
    }
}
