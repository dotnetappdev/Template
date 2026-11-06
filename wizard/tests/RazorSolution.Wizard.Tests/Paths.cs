namespace RazorSolution.Wizard.Tests;

internal static class Paths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string TemplateFolder => Path.Combine(RepoRoot, "templates", "RazorSolution");

    public static string TesterDll
    {
        get
        {
            var bin = Path.Combine(RepoRoot, "wizard", "src", "RazorSolution.ConnectionTester", "bin");
            return Directory.EnumerateFiles(bin, "RazorSolution.ConnectionTester.dll", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .First();
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DotNetAppDev.RazorSolution.Templates.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
