using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using RazorSolution.Wizard.Core;

namespace RazorSolution.Wizard;

/// <summary>
/// Big selectable tiles (like a web template builder): radio tiles for choices, toggle tiles for features.
/// The look is one ToggleButton style (parsed from XAML at run time, so no XAML compilation is needed).
/// </summary>
internal static class Tiles
{
    private const string StyleXaml = @"
<Style TargetType='ToggleButton' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
  <Setter Property='Cursor' Value='Hand' />
  <Setter Property='Background' Value='#FFFFFF' />
  <Setter Property='BorderBrush' Value='#DDDDDD' />
  <Setter Property='Margin' Value='0,0,10,10' />
  <Setter Property='HorizontalContentAlignment' Value='Stretch' />
  <Setter Property='Template'>
    <Setter.Value>
      <ControlTemplate TargetType='ToggleButton'>
        <Border x:Name='Tile' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Background='{TemplateBinding Background}'
                BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='2' CornerRadius='8' Padding='{TemplateBinding Padding}'>
          <Grid>
            <ContentPresenter HorizontalAlignment='Stretch' VerticalAlignment='Center' />
            <Border x:Name='Check' Width='18' Height='18' CornerRadius='9' Background='#5C2D91'
                    HorizontalAlignment='Right' VerticalAlignment='Top' Visibility='Collapsed'>
              <TextBlock Text='&#x2713;' Foreground='White' FontSize='11' FontWeight='Bold' HorizontalAlignment='Center' VerticalAlignment='Center' />
            </Border>
          </Grid>
        </Border>
        <ControlTemplate.Triggers>
          <Trigger Property='IsMouseOver' Value='True'>
            <Setter TargetName='Tile' Property='BorderBrush' Value='#B49AD6' />
            <Setter TargetName='Tile' Property='Background' Value='#FBF9FE' />
          </Trigger>
          <Trigger Property='IsChecked' Value='True'>
            <Setter TargetName='Tile' Property='BorderBrush' Value='#5C2D91' />
            <Setter TargetName='Tile' Property='Background' Value='#F4EEFB' />
            <Setter TargetName='Check' Property='Visibility' Value='Visible' />
          </Trigger>
          <Trigger Property='IsKeyboardFocused' Value='True'>
            <Setter TargetName='Tile' Property='BorderBrush' Value='#0F6CBD' />
          </Trigger>
          <Trigger Property='IsEnabled' Value='False'>
            <Setter Property='Opacity' Value='0.45' />
          </Trigger>
        </ControlTemplate.Triggers>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>";

    private static readonly Lazy<Style> TileStyle = new(() => (Style)XamlReader.Parse(StyleXaml));
    private static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");

    /// <summary>One radio tile per choice of a choice field.</summary>
    public static FrameworkElement ChoiceTiles(FieldInfo field, WizardState state, TileLayout layout)
    {
        var panel = new WrapPanel();
        foreach (var choice in field.Choices)
        {
            var hint = state.Metadata.Tiles.HintFor(field.Id, choice.Value);
            var tile = new RadioButton
            {
                GroupName = "tile-" + field.Id,
                Style = TileStyle.Value,
                IsChecked = state[field.Id] == choice.Value,
                ToolTip = hint?.Note ?? field.Description,
                Content = layout switch
                {
                    TileLayout.Large => LargeContent(choice.Display, hint?.Note, hint?.Badge, hint?.Icon, hint?.Recommended ?? false),
                    TileLayout.Swatch => SwatchContent(choice.Display, hint),
                    _ => CompactContent(choice.Display, hint?.Badge),
                },
            };
            Size(tile, layout);
            var value = choice.Value;
            tile.Checked += (_, _) => state[field.Id] = value;
            panel.Children.Add(tile);
        }

        return panel;
    }

    /// <summary>A toggle tile for a bool field (a feature or project).</summary>
    public static ToggleButton FeatureTile(FieldInfo field, WizardState state)
    {
        var tiles = state.Metadata.Tiles;
        var tile = new ToggleButton
        {
            Style = TileStyle.Value,
            IsChecked = state.GetBool(field.Id),
            ToolTip = field.Description,
            Content = LargeContent(tiles.ShortLabel(field.Id) ?? field.Label, PackageOf(field.Label), null, tiles.IconFor(field.Id), false),
        };
        Size(tile, TileLayout.Large);
        tile.Checked += (_, _) => state.SetBool(field.Id, true);
        tile.Unchecked += (_, _) => state.SetBool(field.Id, false);
        return tile;
    }

    private static void Size(Control tile, TileLayout layout)
    {
        switch (layout)
        {
            case TileLayout.Large:
                tile.Width = 236;
                tile.MinHeight = 72;
                tile.Padding = new Thickness(12, 10, 10, 10);
                break;
            case TileLayout.Swatch:
                tile.Width = 112;
                tile.Padding = new Thickness(8);
                break;
            default:
                tile.Width = 136;
                tile.MinHeight = 52;
                tile.Padding = new Thickness(10, 8, 10, 8);
                break;
        }
    }

    /// <summary>"Add DataTables (datatables.net 2.3.8)" → "datatables.net 2.3.8"; otherwise the label.</summary>
    private static string PackageOf(string label)
    {
        var open = label.LastIndexOf('(');
        return open > 0 && label.EndsWith(")", StringComparison.Ordinal) ? label.Substring(open + 1, label.Length - open - 2) : label;
    }

    private static UIElement LargeContent(string title, string? note, string? badge, TileIcon? icon, bool recommended)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var circle = new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(19),
            Background = icon?.Color != null ? Ui.Brush(icon.Color) : Ui.Accent,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = icon?.GlyphText ?? Monogram(title),
                FontFamily = icon?.GlyphText != null ? IconFont : new FontFamily("Segoe UI"),
                FontSize = icon?.GlyphText != null ? 17 : 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        grid.Children.Add(circle);

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
        var titleRow = new WrapPanel();
        titleRow.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 6, 0) });
        if (badge != null)
        {
            titleRow.Children.Add(Badge(badge, recommended));
        }

        text.Children.Add(titleRow);
        if (!string.IsNullOrEmpty(note) && note != title)
        {
            text.Children.Add(new TextBlock { Text = note, FontSize = 11, Foreground = Ui.Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
        }

        if (recommended)
        {
            text.Children.Add(new TextBlock { Text = "Recommended", FontSize = 10, Foreground = Ui.Success, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 0) });
        }

        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }

    private static UIElement CompactContent(string title, string? badge)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        if (badge != null)
        {
            var b = Badge(badge, false);
            b.Margin = new Thickness(0, 4, 0, 0);
            b.HorizontalAlignment = HorizontalAlignment.Left;
            panel.Children.Add(b);
        }

        return panel;
    }

    private static UIElement SwatchContent(string title, ChoiceHint? hint)
    {
        var panel = new StackPanel();
        var colors = hint?.Swatch.Count > 0 ? hint.Swatch : new[] { "#E0E0E0" };
        var bar = new Grid { Height = 34, Margin = new Thickness(0, 0, 0, 6), ClipToBounds = true };
        for (var i = 0; i < colors.Count; i++)
        {
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(i == 0 ? 2 : 1, GridUnitType.Star) });
            var swatch = new Border { Background = Ui.Brush(colors[i]), BorderBrush = Ui.Border, BorderThickness = new Thickness(0.5) };
            Grid.SetColumn(swatch, i);
            bar.Children.Add(swatch);
        }

        panel.Children.Add(new Border { CornerRadius = new CornerRadius(4), ClipToBounds = true, Child = bar });
        panel.Children.Add(new TextBlock
        {
            Text = title.Replace("Bootswatch ", string.Empty),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        return panel;
    }

    private static Border Badge(string text, bool strong) => new()
    {
        Background = strong ? Ui.Success : Ui.AccentLight,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(6, 1, 6, 1),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = text, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = strong ? Brushes.White : Ui.Accent },
    };

    private static string Monogram(string title) =>
        new string(title.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])).ToArray());
}
