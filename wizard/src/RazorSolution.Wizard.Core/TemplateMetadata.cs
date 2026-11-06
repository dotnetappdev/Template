using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RazorSolution.Wizard.Core;

public enum FieldKind
{
    Bool,
    Choice,
    Text,
}

public sealed class FieldChoice
{
    public FieldChoice(string value, string display)
    {
        Value = value;
        Display = display;
    }

    public string Value { get; }

    public string Display { get; }

    public override string ToString() => Display;
}

/// <summary>A template parameter as Visual Studio and the CLI see it.</summary>
public sealed class FieldInfo
{
    public FieldInfo(string id, FieldKind kind, string label, string description, string defaultValue, IReadOnlyList<FieldChoice> choices, string cliName)
    {
        Id = id;
        Kind = kind;
        Label = label;
        Description = description;
        DefaultValue = defaultValue;
        Choices = choices;
        CliName = cliName;
    }

    public string Id { get; }

    public FieldKind Kind { get; }

    public string Label { get; }

    public string Description { get; }

    public string DefaultValue { get; }

    public IReadOnlyList<FieldChoice> Choices { get; }

    /// <summary>The dotnet new switch, for example "--bootstrap" or "-f".</summary>
    public string CliName { get; }
}

public sealed class StepInfo
{
    public StepInfo(string id, string title, string subtitle, IReadOnlyList<string> fields, IReadOnlyList<Condition> when, bool testConnection)
    {
        Id = id;
        Title = title;
        Subtitle = subtitle;
        Fields = fields;
        When = when;
        TestConnection = testConnection;
    }

    public string Id { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public IReadOnlyList<string> Fields { get; }

    /// <summary>The step is shown when any condition matches (or there are none).</summary>
    public IReadOnlyList<Condition> When { get; }

    public bool TestConnection { get; }
}

/// <summary>All keys must match one of their allowed values.</summary>
public sealed class Condition
{
    public Condition(IReadOnlyDictionary<string, string[]> requirements)
    {
        Requirements = requirements;
    }

    public IReadOnlyDictionary<string, string[]> Requirements { get; }

    public bool IsMatch(Func<string, string> valueOf) =>
        Requirements.All(r => r.Value.Contains(valueOf(r.Key), StringComparer.OrdinalIgnoreCase));
}

/// <summary>
/// Reads the template's template.json, ide.host.json and dotnetcli.host.json (embedded at build time)
/// plus WizardSteps.json, so the wizard always offers exactly what the template supports.
/// </summary>
public sealed class TemplateMetadata
{
    private TemplateMetadata(
        string templateName,
        string shortName,
        string identity,
        IReadOnlyDictionary<string, FieldInfo> fields,
        IReadOnlyList<StepInfo> steps,
        IReadOnlyDictionary<string, IReadOnlyList<Condition>> fieldRules,
        IReadOnlyCollection<string> passwordFields,
        TileHints tiles)
    {
        TemplateName = templateName;
        ShortName = shortName;
        Identity = identity;
        Fields = fields;
        Steps = steps;
        FieldRules = fieldRules;
        PasswordFields = passwordFields;
        Tiles = tiles;
    }

    /// <summary>How each option is drawn as a tile (icons, badges, swatches, groups).</summary>
    public TileHints Tiles { get; }

    public string TemplateName { get; }

    public string ShortName { get; }

    public string Identity { get; }

    public IReadOnlyDictionary<string, FieldInfo> Fields { get; }

    public IReadOnlyList<StepInfo> Steps { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<Condition>> FieldRules { get; }

    public IReadOnlyCollection<string> PasswordFields { get; }

    public static TemplateMetadata LoadEmbedded()
    {
        var assembly = typeof(TemplateMetadata).Assembly;

        string Read(string name)
        {
            using var stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Embedded resource '{name}' is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return Parse(Read("template.json"), Read("ide.host.json"), Read("dotnetcli.host.json"), Read("WizardSteps.json"));
    }

    public static TemplateMetadata Parse(string templateJson, string ideHostJson, string cliHostJson, string stepsJson)
    {
        var template = JObject.Parse(templateJson);
        var ide = JObject.Parse(ideHostJson);
        var cli = JObject.Parse(cliHostJson);
        var stepsDoc = JObject.Parse(stepsJson);

        var symbols = (JObject)template["symbols"]!;
        var ideInfo = ((JArray?)ide["symbolInfo"] ?? new JArray())
            .OfType<JObject>()
            .Where(o => o["id"] != null)
            .ToDictionary(o => (string)o["id"]!, o => o, StringComparer.Ordinal);
        var cliInfo = (JObject?)cli["symbolInfo"] ?? new JObject();

        var fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        foreach (var property in symbols.Properties())
        {
            var symbol = (JObject)property.Value;
            if ((string?)symbol["type"] != "parameter")
            {
                continue;
            }

            var id = property.Name;
            ideInfo.TryGetValue(id, out var ideSymbol);

            var datatype = ((string?)symbol["datatype"] ?? (string?)symbol["dataType"] ?? "text").ToLowerInvariant();
            var kind = datatype switch
            {
                "bool" or "boolean" => FieldKind.Bool,
                "choice" => FieldKind.Choice,
                _ => FieldKind.Text,
            };

            var label = (string?)ideSymbol?["name"]?["text"] ?? (string?)symbol["displayName"] ?? id;
            var description = (string?)ideSymbol?["description"]?["text"] ?? (string?)symbol["description"] ?? string.Empty;
            var defaultValue = (string?)symbol["defaultValue"] ?? (kind == FieldKind.Bool ? "false" : string.Empty);

            var choices = ((JArray?)symbol["choices"] ?? new JArray())
                .OfType<JObject>()
                .Select(c => new FieldChoice((string)c["choice"]!, (string?)c["displayName"] ?? (string?)c["description"] ?? (string)c["choice"]!))
                .ToList();

            var cliSymbol = (JObject?)cliInfo[id];
            var longName = (string?)cliSymbol?["longName"];
            var cliName = longName != null ? "--" + longName : "--" + id;
            if (id == "Framework")
            {
                cliName = "--framework";
            }

            fields[id] = new FieldInfo(id, kind, label, description, defaultValue, choices, cliName);
        }

        var steps = ((JArray)stepsDoc["steps"]!).OfType<JObject>().Select(s => new StepInfo(
            (string)s["id"]!,
            (string)s["title"]!,
            (string?)s["subtitle"] ?? string.Empty,
            ((JArray?)s["fields"] ?? new JArray()).Select(f => (string)f!).Where(fields.ContainsKey).ToList(),
            ParseConditions((JArray?)s["when"]),
            (bool?)s["testConnection"] ?? false)).ToList();

        var rules = new Dictionary<string, IReadOnlyList<Condition>>(StringComparer.Ordinal);
        if (stepsDoc["fieldRules"] is JObject fieldRules)
        {
            foreach (var rule in fieldRules.Properties())
            {
                rules[rule.Name] = ParseConditions((JArray)rule.Value);
            }
        }

        var passwords = ((JArray?)stepsDoc["passwordFields"] ?? new JArray()).Select(p => (string)p!).ToList();

        return new TemplateMetadata(
            (string?)template["name"] ?? "Razor Pages Solution",
            (string?)template["shortName"] ?? "razorsln",
            (string?)template["identity"] ?? string.Empty,
            fields,
            steps,
            rules,
            passwords,
            TileHints.Parse(stepsDoc["tiles"] as JObject));
    }

    private static IReadOnlyList<Condition> ParseConditions(JArray? array) =>
        (array ?? new JArray()).OfType<JObject>()
            .Select(o => new Condition(o.Properties().ToDictionary(
                p => p.Name,
                p => ((JArray)p.Value).Select(v => (string)v!).ToArray(),
                StringComparer.Ordinal)))
            .ToList();
}
