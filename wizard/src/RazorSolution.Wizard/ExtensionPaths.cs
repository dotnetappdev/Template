using System.IO;

namespace RazorSolution.Wizard;

/// <summary>Files that ship inside the VSIX next to this assembly.</summary>
internal static class ExtensionPaths
{
    public static string Folder { get; } = Path.GetDirectoryName(typeof(ExtensionPaths).Assembly.Location) ?? ".";

    /// <summary>Out-of-process "Test connection" helper (net8.0, run with dotnet).</summary>
    public static string ConnectionTester => Path.Combine(Folder, "Tools", "ConnectionTester", "RazorSolution.ConnectionTester.dll");

    /// <summary>The template package, installed on first use if "dotnet new razorsln" is not available yet.</summary>
    public static string TemplatePackage => Path.Combine(Folder, "Templates", "DotNetAppDev.RazorSolution.Templates.nupkg");
}
