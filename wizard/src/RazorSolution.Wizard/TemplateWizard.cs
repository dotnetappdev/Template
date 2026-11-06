using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using RazorSolution.Wizard.Core;

namespace RazorSolution.Wizard;

/// <summary>
/// Visual Studio calls this when "Razor Pages Solution (Wizard)" is picked in New Project.
/// It shows the multi-step <see cref="WizardWindow"/>, creates the solution with "dotnet new razorsln"
/// into a temporary folder, cancels Visual Studio's own (placeholder) project creation, then moves the
/// solution to the folder chosen in "Configure your new project" and opens it.
/// </summary>
public sealed class TemplateWizard : IWizard
{
    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        var dte = (DTE)automationObject;

        var solutionName = Value(replacementsDictionary, "$specifiedsolutionname$")
            ?? Value(replacementsDictionary, "$projectname$")
            ?? "MySolution";
        var target = Value(replacementsDictionary, "$solutiondirectory$")
            ?? Value(replacementsDictionary, "$destinationdirectory$")
            ?? throw new WizardBackoutException("Visual Studio did not supply a destination folder.");

        var state = new WizardState(TemplateMetadata.LoadEmbedded(), solutionName);
        var window = new WizardWindow(state, target);
        try
        {
            new WindowInteropHelper(window).Owner = dte.MainWindow.HWnd;
        }
        catch (Exception)
        {
            // Owner is cosmetic; carry on without it.
        }

        if (window.ShowDialog() != true || window.GeneratedDirectory is null)
        {
            throw new WizardCancelledException("The wizard was cancelled.");
        }

        var generated = window.GeneratedDirectory;

        // Visual Studio cleans up the folder it prepared for the placeholder project after we cancel,
        // so move the real solution in once it has finished (when the UI is idle again).
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            try
            {
                MoveContents(generated, target);
                var solution = Path.Combine(target, solutionName + ".sln");
                if (File.Exists(solution))
                {
                    dte.Solution.Open(solution);
                }
                else
                {
                    MessageBox.Show($"The solution was created in {target}.", "Razor Pages Solution", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The solution was created in {generated} but could not be moved to {target}:{Environment.NewLine}{ex.Message}",
                    "Razor Pages Solution", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }));

        // Stop Visual Studio from creating the placeholder project; the real solution is opened above.
        throw new WizardCancelledException("Created by the Razor Pages Solution wizard.");
    }

    public void ProjectFinishedGenerating(Project project)
    {
    }

    public void ProjectItemFinishedGenerating(ProjectItem projectItem)
    {
    }

    public bool ShouldAddProjectItem(string filePath) => false;

    public void BeforeOpeningFile(ProjectItem projectItem)
    {
    }

    public void RunFinished()
    {
    }

    private static string? Value(Dictionary<string, string> replacements, string key) =>
        replacements.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static void MoveContents(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.GetDirectories(source))
        {
            var destination = Path.Combine(target, Path.GetFileName(directory));
            if (Directory.Exists(destination))
            {
                MoveContents(directory, destination);
            }
            else
            {
                Directory.Move(directory, destination);
            }
        }

        foreach (var file in Directory.GetFiles(source))
        {
            var destination = Path.Combine(target, Path.GetFileName(file));
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Move(file, destination);
        }
    }
}
