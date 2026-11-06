using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RazorSolution.Wizard;

/// <summary>Small helpers so the WPF UI can be written in code (no XAML).</summary>
internal static class Ui
{
    public static readonly Brush Accent = Brush("#5C2D91");
    public static readonly Brush AccentLight = Brush("#EFE8F7");
    public static readonly Brush Sidebar = Brush("#F3F3F3");
    public static readonly Brush Border = Brush("#D6D6D6");
    public static readonly Brush Muted = Brush("#616161");
    public static readonly Brush Success = Brush("#107C10");
    public static readonly Brush Error = Brush("#C42B1C");

    public static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    public static TextBlock Text(string text, double size = 12, FontWeight? weight = null, Brush? brush = null, Thickness? margin = null) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = weight ?? FontWeights.Normal,
        Foreground = brush ?? Brushes.Black,
        TextWrapping = TextWrapping.Wrap,
        Margin = margin ?? new Thickness(0),
    };

    public static Button Button(string content, bool primary = false, double width = 96)
    {
        var button = new Button
        {
            Content = content,
            MinWidth = width,
            Height = 28,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(12, 0, 12, 0),
        };
        if (primary)
        {
            button.Background = Accent;
            button.Foreground = Brushes.White;
            button.BorderBrush = Accent;
            button.IsDefault = true;
        }

        return button;
    }

    /// <summary>A small round "i" that shows the option's description as a tooltip, like the VS template dialog.</summary>
    public static Border Info(string tooltip) => new()
    {
        Width = 14,
        Height = 14,
        CornerRadius = new CornerRadius(7),
        BorderBrush = Accent,
        BorderThickness = new Thickness(1),
        Margin = new Thickness(6, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
        ToolTip = tooltip,
        Child = new TextBlock
        {
            Text = "i",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Accent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };
}
