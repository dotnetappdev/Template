using System;
using System.Collections.Generic;
using System.Linq;

namespace RazorSolution.Wizard.Core;

/// <summary>
/// The choices made in the wizard, keyed by template symbol id, plus the visibility rules for steps and fields.
/// </summary>
public sealed class WizardState
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public WizardState(TemplateMetadata metadata, string solutionName)
    {
        Metadata = metadata;
        SolutionName = solutionName;
        foreach (var field in metadata.Fields.Values)
        {
            _values[field.Id] = field.DefaultValue;
        }
    }

    public TemplateMetadata Metadata { get; }

    public string SolutionName { get; }

    public event EventHandler<string>? Changed;

    public string this[string id]
    {
        get => _values.TryGetValue(id, out var value) ? value : string.Empty;
        set
        {
            value ??= string.Empty;
            if (_values.TryGetValue(id, out var current) && current == value)
            {
                return;
            }

            _values[id] = value;
            Changed?.Invoke(this, id);
        }
    }

    public bool GetBool(string id) => string.Equals(this[id], "true", StringComparison.OrdinalIgnoreCase);

    public void SetBool(string id, bool value) => this[id] = value ? "true" : "false";

    /// <summary>Mirrors the template's computed symbol UseSqlRepository.</summary>
    public bool UsesDatabase => GetBool("IncludeSqlRepository") || GetBool("IncludeServices") || UsesIdentity;

    /// <summary>Mirrors the template's computed symbol UseIdentity (ASP.NET Core Identity needs the data layer).</summary>
    public bool UsesIdentity =>
        (GetBool("IncludeInternal") && this["InternalAuthentication"] == "Identity") ||
        (GetBool("IncludeExternal") && this["ExternalAuthentication"] == "Identity");

    /// <summary>Mirrors the template's computed symbol UseApi.</summary>
    public bool CreatesApi => GetBool("IncludeApi") || GetBool("IncludeInternal") || GetBool("IncludeExternal");

    public bool IsStepVisible(StepInfo step) => step.When.Count == 0 || step.When.Any(c => c.IsMatch(id => this[id]));

    public bool IsFieldVisible(string fieldId) =>
        !Metadata.FieldRules.TryGetValue(fieldId, out var rules) || rules.Count == 0 || rules.Any(c => c.IsMatch(id => this[id]));

    public IEnumerable<StepInfo> VisibleSteps => Metadata.Steps.Where(IsStepVisible);

    /// <summary>Values that differ from the template defaults (what goes on the dotnet new command line).</summary>
    public IEnumerable<KeyValuePair<string, string>> ChangedValues =>
        Metadata.Fields.Values
            .Where(f => !string.Equals(this[f.Id], f.DefaultValue, StringComparison.Ordinal))
            .Select(f => new KeyValuePair<string, string>(f.Id, this[f.Id]));

    /// <summary>Checks the values the CLI would reject. Returns a list of problems (empty when valid).</summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(SolutionName))
        {
            problems.Add("The solution needs a name.");
        }

        if (!GetBool("IncludeInternal") && !GetBool("IncludeExternal") && !CreatesApi && !UsesDatabase)
        {
            problems.Add("Select at least one project.");
        }

        foreach (var field in Metadata.Fields.Values.Where(f => f.Kind == FieldKind.Choice))
        {
            if (!field.Choices.Any(c => c.Value == this[field.Id]))
            {
                problems.Add($"'{this[field.Id]}' is not a valid value for {field.Label}.");
            }
        }

        return problems;
    }
}
