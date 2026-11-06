using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RazorSolution.Wizard.Core;

namespace RazorSolution.Wizard;

/// <summary>
/// Multi-step New Project wizard: Projects → Database (with Test connection) → API &amp; security → Front end → Summary.
/// Steps, fields, labels, choices and defaults all come from the template (see TemplateMetadata / WizardSteps.json).
/// </summary>
public sealed class WizardWindow : Window
{
    private readonly WizardState _state;
    private readonly string _outputDirectory;
    private readonly ConnectionTester _tester;
    private readonly SolutionGenerator _generator;

    private readonly StackPanel _stepList = new() { Margin = new Thickness(0, 12, 0, 0) };
    private readonly TextBlock _title = Ui.Text(string.Empty, 20, FontWeights.SemiBold);
    private readonly TextBlock _subtitle = Ui.Text(string.Empty, 12, brush: Ui.Muted, margin: new Thickness(0, 4, 0, 14));
    private readonly ContentControl _page = new();
    private readonly TextBlock _status = Ui.Text(string.Empty, 12, brush: Ui.Muted);
    private readonly Button _back = Ui.Button("< Back");
    private readonly Button _next = Ui.Button("Next >", primary: true);
    private readonly Button _cancel = Ui.Button("Cancel");

    private readonly Dictionary<string, FrameworkElement> _fieldRows = new(StringComparer.Ordinal);
    private int _stepIndex;
    private bool _busy;

    public WizardWindow(WizardState state, string outputDirectory)
    {
        _state = state;
        _outputDirectory = outputDirectory;
        _tester = new ConnectionTester(ExtensionPaths.ConnectionTester);
        _generator = new SolutionGenerator(ExtensionPaths.TemplatePackage);

        Title = $"Razor Pages Solution – {state.SolutionName}";
        Width = 940;
        Height = 680;
        MinWidth = 780;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 12;
        Background = Brushes.White;

        Content = BuildLayout();
        _back.Click += (_, _) => Move(-1);
        _next.Click += async (_, _) => await NextOrCreateAsync();
        _cancel.Click += (_, _) => { if (!_busy) { DialogResult = false; } };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && !_busy) { DialogResult = false; } };
        _state.Changed += (_, _) => RefreshVisibility();

        ShowStep(0);
    }

    /// <summary>The folder the solution was generated into (a temporary folder; the caller moves it).</summary>
    public string? GeneratedDirectory { get; private set; }

    private List<StepInfo> Steps => _state.VisibleSteps.ToList();

    private UIElement BuildLayout()
    {
        var root = new DockPanel();

        // Bottom bar: status on the left, Back / Next / Cancel on the right.
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(_back);
        buttons.Children.Add(_next);
        buttons.Children.Add(_cancel);
        var bottom = new DockPanel { Margin = new Thickness(16, 10, 16, 12), LastChildFill = true };
        DockPanel.SetDock(buttons, Dock.Right);
        bottom.Children.Add(buttons);
        _status.VerticalAlignment = VerticalAlignment.Center;
        bottom.Children.Add(_status);
        var bottomBorder = new Border { BorderBrush = Ui.Border, BorderThickness = new Thickness(0, 1, 0, 0), Child = bottom };
        DockPanel.SetDock(bottomBorder, Dock.Bottom);
        root.Children.Add(bottomBorder);

        // Left: step list.
        var sidebar = new StackPanel { Margin = new Thickness(16) };
        sidebar.Children.Add(Ui.Text("Razor Pages Solution", 14, FontWeights.SemiBold));
        sidebar.Children.Add(Ui.Text(_state.SolutionName, 11, brush: Ui.Muted, margin: new Thickness(0, 2, 0, 0)));
        sidebar.Children.Add(_stepList);
        var sidebarBorder = new Border { Width = 210, Background = Ui.Sidebar, BorderBrush = Ui.Border, BorderThickness = new Thickness(0, 0, 1, 0), Child = sidebar };
        DockPanel.SetDock(sidebarBorder, Dock.Left);
        root.Children.Add(sidebarBorder);

        // Right: title, subtitle, page.
        var main = new DockPanel { Margin = new Thickness(24, 18, 24, 6) };
        var header = new StackPanel();
        header.Children.Add(_title);
        header.Children.Add(_subtitle);
        DockPanel.SetDock(header, Dock.Top);
        main.Children.Add(header);
        main.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _page });
        root.Children.Add(main);

        return root;
    }

    private void ShowStep(int index)
    {
        var steps = Steps;
        _stepIndex = Math.Max(0, Math.Min(index, steps.Count - 1));
        var step = steps[_stepIndex];

        _title.Text = step.Title;
        _subtitle.Text = step.Subtitle;
        _status.Text = string.Empty;
        _fieldRows.Clear();
        _page.Content = step.Id == "summary" ? BuildSummary() : BuildFieldsPage(step);

        _back.IsEnabled = _stepIndex > 0;
        _next.Content = _stepIndex == steps.Count - 1 ? "Create" : "Next >";
        RefreshStepList();
        RefreshVisibility();
    }

    private void Move(int delta)
    {
        if (!_busy)
        {
            ShowStep(_stepIndex + delta);
        }
    }

    private async Task NextOrCreateAsync()
    {
        if (_busy)
        {
            return;
        }

        if (_stepIndex < Steps.Count - 1)
        {
            Move(1);
            return;
        }

        var problems = _state.Validate();
        if (problems.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, problems), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await CreateAsync();
    }

    private async Task CreateAsync()
    {
        SetBusy(true, "Creating the solution…");
        var temp = Path.Combine(Path.GetTempPath(), "RazorSolutionWizard", Guid.NewGuid().ToString("N"), _state.SolutionName);
        var progress = new Progress<string>(message => _status.Text = message);

        try
        {
            var result = await _generator.GenerateAsync(_state, temp, progress);
            if (result.ExitCode == 0)
            {
                GeneratedDirectory = temp;
                DialogResult = true;
                return;
            }

            ShowError("dotnet new failed", (result.Output + Environment.NewLine + result.Error).Trim());
        }
        catch (Exception ex)
        {
            ShowError("The solution could not be created", ex.ToString());
        }
        finally
        {
            SetBusy(false, _status.Text);
        }
    }

    private void SetBusy(bool busy, string status)
    {
        _busy = busy;
        _status.Text = status;
        _back.IsEnabled = !busy && _stepIndex > 0;
        _next.IsEnabled = !busy;
        _cancel.IsEnabled = !busy;
        Cursor = busy ? Cursors.Wait : null;
    }

    private void ShowError(string title, string detail)
    {
        _status.Text = title;
        _status.Foreground = Ui.Error;
        var box = new TextBox
        {
            Text = detail,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            Height = 220,
        };
        var panel = new StackPanel();
        panel.Children.Add(Ui.Text(title, 13, FontWeights.SemiBold, Ui.Error, new Thickness(0, 0, 0, 6)));
        panel.Children.Add(box);
        _page.Content = panel;
    }

    // ------------------------------------------------------------------ step list

    private void RefreshStepList()
    {
        _stepList.Children.Clear();
        var steps = Steps;
        for (var i = 0; i < steps.Count; i++)
        {
            var current = i == _stepIndex;
            var row = new Border
            {
                Background = current ? Ui.AccentLight : Brushes.Transparent,
                BorderBrush = current ? Ui.Accent : Brushes.Transparent,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(8, 6, 6, 6),
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = Cursors.Hand,
                Child = Ui.Text($"{i + 1}.  {steps[i].Title}", 12, current ? FontWeights.SemiBold : FontWeights.Normal, current ? Ui.Accent : Brushes.Black),
            };
            var target = i;
            row.MouseLeftButtonUp += (_, _) => { if (!_busy) { ShowStep(target); } };
            _stepList.Children.Add(row);
        }
    }

    // ------------------------------------------------------------------ fields

    private UIElement BuildFieldsPage(StepInfo step)
    {
        var panel = new StackPanel { MaxWidth = 620, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var id in step.Fields)
        {
            var row = BuildField(_state.Metadata.Fields[id]);
            _fieldRows[id] = row;
            panel.Children.Add(row);
        }

        if (step.TestConnection)
        {
            panel.Children.Add(BuildConnectionTest());
        }

        return panel;
    }

    private FrameworkElement BuildField(FieldInfo field)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

        if (field.Kind == FieldKind.Bool)
        {
            var check = new CheckBox { IsChecked = _state.GetBool(field.Id), VerticalContentAlignment = VerticalAlignment.Center };
            var label = new StackPanel { Orientation = Orientation.Horizontal };
            label.Children.Add(new TextBlock { Text = field.Label, VerticalAlignment = VerticalAlignment.Center });
            label.Children.Add(Ui.Info(field.Description));
            check.Content = label;
            check.Checked += (_, _) => _state.SetBool(field.Id, true);
            check.Unchecked += (_, _) => _state.SetBool(field.Id, false);
            row.Children.Add(check);
            return row;
        }

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        header.Children.Add(new TextBlock { Text = field.Label });
        header.Children.Add(Ui.Info(field.Description));
        row.Children.Add(header);

        if (field.Kind == FieldKind.Choice)
        {
            var combo = new ComboBox
            {
                ItemsSource = field.Choices,
                DisplayMemberPath = nameof(FieldChoice.Display),
                SelectedValuePath = nameof(FieldChoice.Value),
                SelectedValue = _state[field.Id],
                Height = 26,
                MaxDropDownHeight = 420,
            };
            combo.SelectionChanged += (_, _) => _state[field.Id] = combo.SelectedValue as string ?? field.DefaultValue;
            row.Children.Add(combo);
        }
        else if (_state.Metadata.PasswordFields.Contains(field.Id))
        {
            var password = new PasswordBox { Password = _state[field.Id], Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            password.PasswordChanged += (_, _) => _state[field.Id] = password.Password;
            row.Children.Add(password);
        }
        else
        {
            var text = new TextBox { Text = _state[field.Id], Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            text.TextChanged += (_, _) => _state[field.Id] = text.Text;
            row.Children.Add(text);
        }

        row.Children.Add(Ui.Text(field.Description, 11, brush: Ui.Muted, margin: new Thickness(0, 3, 0, 0)));
        return row;
    }

    private void RefreshVisibility()
    {
        foreach (var pair in _fieldRows)
        {
            pair.Value.Visibility = _state.IsFieldVisible(pair.Key) ? Visibility.Visible : Visibility.Collapsed;
        }

        _connectionPreview?.SetValue(TextBlock.TextProperty, ConnectionStrings.Mask(ConnectionStrings.Build(_state)));

        // Ticking/unticking projects can add or remove later steps.
        if (_stepList.Children.Count != Steps.Count)
        {
            RefreshStepList();
        }
    }

    // ------------------------------------------------------------------ test connection

    private TextBlock? _connectionPreview;

    private UIElement BuildConnectionTest()
    {
        var box = new Border
        {
            BorderBrush = Ui.Border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 6, 0, 0),
            Background = Ui.Sidebar,
        };
        var panel = new StackPanel();
        panel.Children.Add(Ui.Text("Connection string the API will use", 12, FontWeights.SemiBold));
        _connectionPreview = Ui.Text(ConnectionStrings.Mask(ConnectionStrings.Build(_state)), 11, brush: Ui.Muted, margin: new Thickness(0, 4, 0, 10));
        _connectionPreview.FontFamily = new FontFamily("Consolas");
        panel.Children.Add(_connectionPreview);

        var bar = new StackPanel { Orientation = Orientation.Horizontal };
        var button = Ui.Button("Test connection", width: 130);
        button.Margin = new Thickness(0);
        var result = Ui.Text(string.Empty, 12, margin: new Thickness(12, 0, 0, 0));
        result.VerticalAlignment = VerticalAlignment.Center;
        result.MaxWidth = 420;
        bar.Children.Add(button);
        bar.Children.Add(result);
        panel.Children.Add(bar);

        var detail = Ui.Text(string.Empty, 11, brush: Ui.Muted, margin: new Thickness(0, 6, 0, 0));
        panel.Children.Add(detail);

        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            result.Foreground = Ui.Muted;
            result.Text = "Testing…";
            detail.Text = string.Empty;
            try
            {
                var outcome = await _tester.TestAsync(_state);
                result.Foreground = outcome.Success ? Ui.Success : Ui.Error;
                result.Text = (outcome.Success ? "✔ " : "✖ ") + outcome.Message;
                detail.Text = outcome.Detail ?? string.Empty;
            }
            catch (Exception ex)
            {
                result.Foreground = Ui.Error;
                result.Text = "✖ The test could not run.";
                detail.Text = ex.Message;
            }
            finally
            {
                button.IsEnabled = true;
            }
        };

        box.Child = panel;
        return box;
    }

    // ------------------------------------------------------------------ summary

    private UIElement BuildSummary()
    {
        var panel = new StackPanel { MaxWidth = 660, HorizontalAlignment = HorizontalAlignment.Left };

        var projects = new List<string>();
        if (_state.GetBool("IncludeInternal")) projects.Add($"{_state.SolutionName}.Internal");
        if (_state.GetBool("IncludeExternal")) projects.Add($"{_state.SolutionName}.External");
        if (_state.CreatesApi) projects.Add($"{_state.SolutionName}.API");
        if (_state.GetBool("IncludeServices")) projects.Add($"{_state.SolutionName}.Services");
        if (_state.UsesDatabase) projects.Add($"{_state.SolutionName}.SqlRepository");
        if (_state.GetBool("IncludeTests") && _state.GetBool("IncludeServices")) projects.Add($"{_state.SolutionName}.Tests");

        AddSummaryLine(panel, "Location", _outputDirectory);
        AddSummaryLine(panel, "Framework", Display("Framework"));
        AddSummaryLine(panel, "Projects", string.Join(", ", projects));
        if (_state.UsesDatabase)
        {
            AddSummaryLine(panel, "Database", ConnectionStrings.Mask(ConnectionStrings.Build(_state)));
        }

        if (_state.GetBool("IncludeInternal") || _state.GetBool("IncludeExternal"))
        {
            AddSummaryLine(panel, "Front end", $"{Display("JQueryVersion")}, {Display("JQueryUIVersion")} ({Display("JQueryUITheme")}), {Display("BootstrapVersion")} ({Display("BootstrapTheme")}), {Display("FontAwesomeVersion")}");
        }

        var changed = _state.ChangedValues.Select(p => _state.Metadata.Fields[p.Key]).Where(f => f.Kind == FieldKind.Bool).Select(f => $"{(_state.GetBool(f.Id) ? "+" : "−")} {f.Label}");
        AddSummaryLine(panel, "Changed options", string.Join(Environment.NewLine, changed.DefaultIfEmpty("(defaults)")));

        panel.Children.Add(Ui.Text("Command", 12, FontWeights.SemiBold, margin: new Thickness(0, 12, 0, 4)));
        var command = new TextBox
        {
            Text = DotnetNewCommand.Display(_state, _outputDirectory),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            Padding = new Thickness(6),
        };
        panel.Children.Add(command);
        var copy = Ui.Button("Copy command", width: 120);
        copy.Margin = new Thickness(0, 6, 0, 0);
        copy.HorizontalAlignment = HorizontalAlignment.Left;
        copy.Click += (_, _) => Clipboard.SetText(command.Text);
        panel.Children.Add(copy);

        return panel;
    }

    private string Display(string id)
    {
        var field = _state.Metadata.Fields[id];
        return field.Choices.FirstOrDefault(c => c.Value == _state[id])?.Display ?? _state[id];
    }

    private static void AddSummaryLine(Panel panel, string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var l = Ui.Text(label, 12, FontWeights.SemiBold, Ui.Muted);
        var v = Ui.Text(value, 12);
        Grid.SetColumn(v, 1);
        grid.Children.Add(l);
        grid.Children.Add(v);
        panel.Children.Add(grid);
    }
}
