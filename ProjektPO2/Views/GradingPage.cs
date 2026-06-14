using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class GradingPage : UserControl
{
    private readonly MainWindow _host;

    public GradingPage(MainWindow host)
    {
        _host = host;
        Build();
    }

    private void Build()
    {
        var subs = DbRepository.PendingSubmissions(_host.CurrentUser.Id);
        var root = new StackPanel();

        if (subs.Count == 0)
        {
            var empty = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 70, 0, 0) };
            empty.Children.Add(new TextBlock { Text = "✓", FontSize = 40, Foreground = Ui.Br("Success"), HorizontalAlignment = HorizontalAlignment.Center });
            empty.Children.Add(new TextBlock { Text = "Wszystko ocenione!", FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Text"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 4), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
            empty.Children.Add(new TextBlock { Text = "Brak odpowiedzi otwartych czekających na ocenę.", FontSize = 14, Foreground = Ui.Br("Muted"), HorizontalAlignment = HorizontalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
            root.Children.Add(empty);
            Content = root;
            return;
        }

        var banner = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        banner.Children.Add(Ui.IconChip("📝", "#2563EB", 44));
        var bt = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) };
        bt.Children.Add(new TextBlock { Text = $"{subs.Count} {(subs.Count == 1 ? "zgłoszenie czeka" : "zgłoszeń czeka")} na ocenę", FontWeight = FontWeights.Bold, FontSize = 15, Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        bt.Children.Add(new TextBlock { Text = "Sprawdź odpowiedzi otwarte i przyznaj punkty. Wynik ucznia zaktualizuje się automatycznie.", FontSize = 13.5, Foreground = Ui.Br("Muted"), TextWrapping = TextWrapping.Wrap, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        banner.Children.Add(bt);
        root.Children.Add(new Border { Background = Ui.Br("PrimarySoft"), BorderBrush = Ui.Br("PrimarySoft2"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(18), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 16), Child = banner });

        foreach (var s in subs)
            root.Children.Add(new GradingCard(s.result, s.test, s.student, s.openQs, () => _host.ShowGrading()));

        Content = root;
    }
}

public class GradingCard : Border
{
    private readonly Result _result;
    private readonly Test _test;
    private readonly List<Question> _openQs;
    private readonly System.Action _onSaved;
    private readonly Dictionary<int, int?> _pts = new();
    private Button _saveBtn = null!;
    private TextBlock _hint = null!;

    public GradingCard(Result result, Test test, User student, List<Question> openQs, System.Action onSaved)
    {
        _result = result; _test = test; _openQs = openQs; _onSaved = onSaved;
        foreach (var q in openQs)
            _pts[q.Id] = (_result.Manual != null && _result.Manual.TryGetValue(q.Id, out var v)) ? v : (int?)null;

        Style = Ui.St("Card"); Padding = new Thickness(22); Margin = new Thickness(0, 0, 0, 16);
        var sp = new StackPanel();

        var head = new Grid();
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var who = new StackPanel { Orientation = Orientation.Horizontal };
        who.Children.Add(TeacherDashboardPage.Avatar(student.Name, 42));
        var whoT = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        whoT.Children.Add(new TextBlock { Text = student.Name, FontWeight = FontWeights.Bold, FontSize = 15.5, Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        whoT.Children.Add(new TextBlock { Text = $"Grupa {student.GroupName} · oddano {result.DateTaken:yyyy-MM-dd HH:mm}", FontSize = 12.5, Foreground = Ui.Br("Muted"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        who.Children.Add(whoT); Grid.SetColumn(who, 0);
        var badge = Ui.Badge(test.Title, "blue", true); badge.HorizontalAlignment = HorizontalAlignment.Right; badge.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(badge, 1);
        head.Children.Add(who); head.Children.Add(badge);
        sp.Children.Add(head);

        foreach (var q in _openQs) sp.Children.Add(QuestionBlock(q));

        var foot = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        foot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        foot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _hint = new TextBlock { FontSize = 13, Foreground = Ui.Br("Muted"), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        Grid.SetColumn(_hint, 0);
        _saveBtn = Ui.Btn("✓  Zapisz ocenę", "PrimaryButton", (_, _) => Save());
        Grid.SetColumn(_saveBtn, 1);
        foot.Children.Add(_hint); foot.Children.Add(_saveBtn);
        sp.Children.Add(new Border { BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 16, 0, 0), Margin = new Thickness(0, 16, 0, 0), Child = foot });

        Child = sp;
        UpdateState();
    }

    private FrameworkElement QuestionBlock(Question q)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        sp.Children.Add(new TextBlock { Text = q.Text, FontWeight = FontWeights.Bold, FontSize = 15.5, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Br("Text"), Margin = new Thickness(0, 0, 0, 10), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        sp.Children.Add(new TextBlock { Text = "ODPOWIEDŹ UCZNIA", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 0, 0, 6), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        _result.Answers.TryGetValue(q.Id, out var ans);
        string txt = GradingService.AsText(ans);
        sp.Children.Add(new Border { Background = Ui.Br("Surface2"), BorderBrush = Ui.Br("Border2"), BorderThickness = new Thickness(1.5), CornerRadius = new CornerRadius(12), Padding = new Thickness(15, 13, 15, 13), Child = new TextBlock { Text = string.IsNullOrWhiteSpace(txt) ? "— brak odpowiedzi —" : txt, FontSize = 15, FontWeight = FontWeights.Medium, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Br(string.IsNullOrWhiteSpace(txt) ? "Muted" : "Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") } });
        var sug = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        sug.Children.Add(new TextBlock { Text = "✓", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Success"), Margin = new Thickness(0, 0, 7, 0) });
        sug.Children.Add(new TextBlock { Text = $"Sugerowana odpowiedź: {(string.IsNullOrEmpty(q.Sample) ? "—" : q.Sample)}", FontSize = 13, Foreground = Ui.Br("Muted"), TextWrapping = TextWrapping.Wrap, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        sp.Children.Add(sug);

        var ptsRow = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        ptsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ptsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ptsRow.Children.Add(new TextBlock { Text = $"Przyznaj punkty (0–{q.Points}):", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Text2"), VerticalAlignment = VerticalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });

        var inputRow = new StackPanel { Orientation = Orientation.Horizontal };
        var box = new TextBox
        {
            Width = 70, Height = 42, FontSize = 14,
            Text = _pts[q.Id]?.ToString() ?? "",
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        box.TextChanged += (_, _) =>
        {
            // Bez cichego ucinania — przechowujemy wpisaną wartość i walidujemy zakres.
            _pts[q.Id] = int.TryParse(box.Text, out var v) ? v : (int?)null;
            bool valid = _pts[q.Id] is int p && p >= 0 && p <= q.Points;
            box.BorderBrush = (valid || box.Text.Length == 0) ? Ui.Br("Border2") : Ui.Br("Danger");
            UpdateState();
        };
        var maxLbl = Ui.T($"/ {q.Points} pkt", 13.5, FontWeights.SemiBold, "Muted");
        maxLbl.VerticalAlignment = VerticalAlignment.Center; maxLbl.Margin = new Thickness(8, 0, 0, 0);
        inputRow.Children.Add(box); inputRow.Children.Add(maxLbl);
        Grid.SetColumn(inputRow, 1); ptsRow.Children.Add(inputRow);
        sp.Children.Add(ptsRow);

        return new Border { BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 16, 0, 0), Child = sp };
    }

    private void UpdateState()
    {
        bool allValid = _openQs.All(q => _pts[q.Id] is int p && p >= 0 && p <= q.Points);
        _saveBtn.IsEnabled = allValid;
        _hint.Text = allValid
            ? "Po zapisaniu uczeń zobaczy ostateczny wynik."
            : "Wpisz punkty z zakresu 0–max dla każdego pytania, aby zapisać.";
    }

    private void Save()
    {
        _result.Manual ??= new();
        foreach (var q in _openQs)
            if (_pts[q.Id] is int v) _result.Manual[q.Id] = v;
        GradingService.Recompute(_test, _result);
        if (!Safe.Run(() => DbRepository.UpdateGrading(_result), "zapisać oceny")) return;
        _onSaved();
    }
}
