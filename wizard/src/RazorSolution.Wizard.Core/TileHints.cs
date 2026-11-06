using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RazorSolution.Wizard.Core;

public enum TileLayout
{
    /// <summary>Checkbox / drop-down style rows (text fields, or choices with no tile hint).</summary>
    None,

    /// <summary>Big tiles with an icon, a title and a note.</summary>
    Large,

    /// <summary>Small tiles, for example version numbers.</summary>
    Compact,

    /// <summary>Theme tiles with a color preview.</summary>
    Swatch,
}

public sealed class TileIcon
{
    public TileIcon(string? fontAwesome, string? glyph, string? color)
    {
        FontAwesome = fontAwesome;
        Glyph = glyph;
        Color = color;
    }

    /// <summary>Font Awesome 6 class, used by the web configurator.</summary>
    public string? FontAwesome { get; }

    /// <summary>Segoe MDL2 / Fluent Icons code point (hex), used by the WPF wizard.</summary>
    public string? Glyph { get; }

    public string? Color { get; }

    public string? GlyphText => Glyph is { Length: > 0 } ? char.ConvertFromUtf32(Convert.ToInt32(Glyph, 16)) : null;
}

public sealed class ChoiceHint
{
    public ChoiceHint(string? badge, string? note, bool recommended, TileIcon? icon, IReadOnlyList<string> swatch)
    {
        Badge = badge;
        Note = note;
        Recommended = recommended;
        Icon = icon;
        Swatch = swatch;
    }

    public string? Badge { get; }

    public string? Note { get; }

    public bool Recommended { get; }

    public TileIcon? Icon { get; }

    /// <summary>Colors that preview a theme (for example primary, background, secondary).</summary>
    public IReadOnlyList<string> Swatch { get; }
}

/// <summary>The "tiles" section of WizardSteps.json: how the wizard and the web configurator draw each option.</summary>
public sealed class TileHints
{
    private readonly Dictionary<string, TileLayout> _layouts;
    private readonly Dictionary<string, Dictionary<string, ChoiceHint>> _choices;
    private readonly Dictionary<string, TileIcon> _icons;
    private readonly Dictionary<string, string> _shortLabels;
    private readonly Dictionary<string, IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>>> _groups;

    private TileHints(
        Dictionary<string, TileLayout> layouts,
        Dictionary<string, Dictionary<string, ChoiceHint>> choices,
        Dictionary<string, TileIcon> icons,
        Dictionary<string, string> shortLabels,
        Dictionary<string, IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>>> groups)
    {
        _layouts = layouts;
        _choices = choices;
        _icons = icons;
        _shortLabels = shortLabels;
        _groups = groups;
    }

    public static TileHints Empty { get; } = new(new(), new(), new(), new(), new());

    public TileLayout LayoutOf(string fieldId) => _layouts.TryGetValue(fieldId, out var layout) ? layout : TileLayout.None;

    public ChoiceHint? HintFor(string fieldId, string choice) =>
        _choices.TryGetValue(fieldId, out var map) && map.TryGetValue(choice, out var hint) ? hint : null;

    public TileIcon? IconFor(string fieldId) => _icons.TryGetValue(fieldId, out var icon) ? icon : null;

    public string? ShortLabel(string fieldId) => _shortLabels.TryGetValue(fieldId, out var label) ? label : null;

    /// <summary>Toggle-tile groups for a step: (group title, bool field ids).</summary>
    public IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> GroupsFor(string stepId) =>
        _groups.TryGetValue(stepId, out var groups) ? groups : Array.Empty<KeyValuePair<string, IReadOnlyList<string>>>();

    public static TileHints Parse(JObject? tiles)
    {
        if (tiles == null)
        {
            return Empty;
        }

        var layouts = new Dictionary<string, TileLayout>(StringComparer.Ordinal);
        foreach (var p in ((JObject?)tiles["fields"])?.Properties() ?? Enumerable.Empty<JProperty>())
        {
            layouts[p.Name] = ((string?)p.Value) switch
            {
                "large" => TileLayout.Large,
                "compact" => TileLayout.Compact,
                "swatch" => TileLayout.Swatch,
                _ => TileLayout.None,
            };
        }

        var choices = new Dictionary<string, Dictionary<string, ChoiceHint>>(StringComparer.Ordinal);
        foreach (var field in ((JObject?)tiles["choices"])?.Properties() ?? Enumerable.Empty<JProperty>())
        {
            var map = new Dictionary<string, ChoiceHint>(StringComparer.Ordinal);
            foreach (var choice in ((JObject)field.Value).Properties())
            {
                var o = (JObject)choice.Value;
                map[choice.Name] = new ChoiceHint(
                    (string?)o["badge"],
                    (string?)o["note"],
                    (bool?)o["recommended"] ?? false,
                    ParseIcon(o["icon"] as JObject),
                    ((JArray?)o["swatch"])?.Select(c => (string)c!).ToList() ?? new List<string>());
            }

            choices[field.Name] = map;
        }

        var icons = (((JObject?)tiles["icons"])?.Properties() ?? Enumerable.Empty<JProperty>())
            .ToDictionary(p => p.Name, p => ParseIcon((JObject)p.Value)!, StringComparer.Ordinal);

        var shortLabels = (((JObject?)tiles["shortLabels"])?.Properties() ?? Enumerable.Empty<JProperty>())
            .ToDictionary(p => p.Name, p => (string)p.Value!, StringComparer.Ordinal);

        var groups = new Dictionary<string, IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>>>(StringComparer.Ordinal);
        foreach (var step in ((JObject?)tiles["groups"])?.Properties() ?? Enumerable.Empty<JProperty>())
        {
            groups[step.Name] = ((JArray)step.Value).OfType<JArray>()
                .Select(g => new KeyValuePair<string, IReadOnlyList<string>>((string)g[0]!, ((JArray)g[1]).Select(f => (string)f!).ToList()))
                .ToList();
        }

        return new TileHints(layouts, choices, icons, shortLabels, groups);
    }

    private static TileIcon? ParseIcon(JObject? o) =>
        o == null ? null : new TileIcon((string?)o["fa"], (string?)o["glyph"], (string?)o["color"]);
}
