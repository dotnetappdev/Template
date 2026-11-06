using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RazorSolution.Wizard.Core;

public sealed class ProcessResult
{
    public ProcessResult(int exitCode, string output, string error, bool timedOut)
    {
        ExitCode = exitCode;
        Output = output;
        Error = error;
        TimedOut = timedOut;
    }

    public int ExitCode { get; }

    public string Output { get; }

    public string Error { get; }

    public bool TimedOut { get; }
}

/// <summary>Runs "dotnet ..." without a window, with optional stdin, and returns its output.</summary>
public static class ProcessRunner
{
    public static string DotnetPath { get; set; } = "dotnet";

    public static Task<ProcessResult> RunDotnetAsync(IEnumerable<string> arguments, string? standardInput, TimeSpan timeout, string? workingDirectory = null, CancellationToken cancellationToken = default) =>
        RunAsync(DotnetPath, DotnetNewCommand.ToCommandLine(arguments), standardInput, timeout, workingDirectory, cancellationToken);

    public static async Task<ProcessResult> RunAsync(string fileName, string arguments, string? standardInput, TimeSpan timeout, string? workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (workingDirectory != null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        // No first-run banners or telemetry prompts in the captured output.
        startInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";
        startInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.EnvironmentVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var output = new StringBuilder();
        var error = new StringBuilder();
        var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputDataReceived += (_, e) => { if (e.Data != null) { lock (output) { output.AppendLine(e.Data); } } };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) { lock (error) { error.AppendLine(e.Data); } } };
        process.Exited += (_, _) => exited.TrySetResult(true);

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new ProcessResult(-1, string.Empty, $"Could not start '{fileName}': {ex.Message}. Is the .NET SDK installed and on the PATH?", false);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (standardInput != null)
        {
            await process.StandardInput.WriteAsync(standardInput).ConfigureAwait(false);
        }

        process.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        var cancelled = new TaskCompletionSource<bool>();
        using (timeoutCts.Token.Register(() => cancelled.TrySetResult(true)))
        {
            var finished = await Task.WhenAny(exited.Task, cancelled.Task).ConfigureAwait(false);
            if (finished != exited.Task)
            {
                try { process.Kill(); } catch (InvalidOperationException) { }
                return new ProcessResult(-1, output.ToString(), error.ToString(), timedOut: true);
            }
        }

        process.WaitForExit(); // flush the async output readers
        return new ProcessResult(process.ExitCode, output.ToString(), error.ToString(), false);
    }
}
