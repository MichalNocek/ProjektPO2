using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class StudentTestsPage : UserControl
{
    private readonly MainWindow _host;

    public StudentTestsPage(MainWindow host, bool historyOnly)
    {
        _host = host;
        var tests = DbRepository.GetTestsForStudent(host.CurrentUser);
        var pairs = new List<(Test t, Result? r, int used)>();
        foreach (var t in tests)
            pairs.Add((t,
                DbRepository.GetBestResult(t.Id, host.CurrentUser.Id),
                DbRepository.CountAttempts(t.Id, host.CurrentUser.Id)));

        Content = historyOnly ? BuildHistory(pairs) : BuildDashboard(pairs);
    }

    private UIElement BuildDashboard(List<(Test t, Result? r, int used)> pairs)
    {
        var root = new StackPanel();
        var todo = pairs.FindAll(p => p.r == null);
        var done = pairs.FindAll(p => p.r != null);

        if (todo.Count > 0)
        {
            root.Children.Add(SectionHeader("Do zrobienia", todo.Count, "blue"));
            root.Children.Add(CardGrid(todo));
        }
        if (done.Count > 0)
        {
            var h = SectionHeader("Ukończone", null, "gray");
            h.Margin = new Thickness(0, todo.Count > 0 ? 26 : 0, 0, 14);
            root.Children.Add(h); root.Children.Add(CardGrid(done));
        }
        if (pairs.Count == 0)
            root.Children.Add(Ui.T("Nie masz przydzielonych testów.", 14, null, "Muted"));
        return root;
    }

    private FrameworkElement SectionHeader(string title, int? count, string tone)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
        sp.Children.Add(Ui.T(title, 15, FontWeights.Bold));
        if (count.HasValue) { var b = Ui.Badge(count.Value.ToString(), tone); b.Margin = new Thickness(10, 0, 0, 0); sp.Children.Add(b); }
        return sp;
    }

    private WrapPanel CardGrid(List<(Test t, Result? r, int used)> items)
    {
        var wrap = new WrapPanel();
        foreach (var (t, r, used) in items) wrap.Children.Add(TestCard(t, r, used));
        return wrap;
    }

    private Border TestCard(Test t, Result? r, int used)
    {
        // Ponowne podejście tylko gdy zostały podejścia I test jest wciąż aktywny.
        // Po dezaktywacji uczeń widzi wynik, ale nie wykorzysta pozostałych podejść.
        bool canRetake = used < t.MaxAttempts && t.IsActive;
        var stripe = new Border { Height = 6, Background = Ui.Hex(t.ColorHex), CornerRadius = new CornerRadius(18, 18, 0, 0) };
        var body   = new StackPanel { Margin = new Thickness(18, 16, 18, 18) };

        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var subj = Ui.HStack(0, new Border { Width = 7, Height = 7, CornerRadius = new CornerRadius(4), Background = Ui.Hex(t.ColorHex), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 7, 0) }, Ui.T(t.Subject, 12, FontWeights.Bold, "Muted"));
        subj.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(subj, 0);
        UIElement badge = r != null
            ? (r.Pending ? Ui.Badge("W ocenie", "amber", true) : Ui.Badge(r.Pct + "%", r.Passed ? "green" : "red", true))
            : Ui.Badge("Nowy", "blue");
        Grid.SetColumn(badge, 1);
        topRow.Children.Add(subj); topRow.Children.Add(badge);
        body.Children.Add(topRow);
        body.Children.Add(new TextBlock { Text = t.Title, FontSize = 17, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Br("Text"), Margin = new Thickness(0, 12, 0, 12), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });

        string attemptsLabel = !t.IsActive          ? "Test zamknięty"
                             : used >= t.MaxAttempts ? "Limit wyczerpany"
                             :                         $"{used}/{t.MaxAttempts} podejść";
        var meta = Ui.HStack(16, MetaItem("📝", t.Questions.Count + " pytań"), MetaItem("⏱", t.TimeLimit + " min"), MetaItem("🔁", attemptsLabel));
        body.Children.Add(meta);

        var foot = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        foot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        foot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var info = Ui.T(r != null ? $"{r.Score}/{r.MaxScore} pkt" : "Nierozwiązany", 13, FontWeights.SemiBold, "Muted");
        info.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(info, 0);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
        if (r != null)
        {
            var view = Ui.Btn("👁  Wynik", "SecondaryButton", (_, _) => _host.ShowResult(t, r));
            view.Height = 34; view.FontSize = 13; btnRow.Children.Add(view);
            if (canRetake)
            {
                var retake = Ui.Btn("🔁  Ponów", "PrimaryButton", (_, _) => _host.StartQuiz(t));
                retake.Height = 34; retake.FontSize = 13; retake.Margin = new Thickness(8, 0, 0, 0);
                btnRow.Children.Add(retake);
            }
        }
        else
        {
            var start = Ui.Btn("▶  Rozpocznij", "PrimaryButton", (_, _) => _host.StartQuiz(t));
            start.Height = 34; start.FontSize = 13; btnRow.Children.Add(start);
        }
        Grid.SetColumn(btnRow, 1);
        foot.Children.Add(info); foot.Children.Add(btnRow); body.Children.Add(foot);

        var inner = new StackPanel();
        inner.Children.Add(stripe); inner.Children.Add(body);
        var card = new Border { Style = Ui.St("Card"), Width = 320, Margin = new Thickness(0, 0, 18, 18), Child = inner, Cursor = Cursors.Hand };

        // Kliknięcie karty: rozwiązany → podgląd wyniku; nierozwiązany → start testu.
        card.MouseLeftButtonUp += (_, _) =>
        {
            if (r != null) _host.ShowResult(t, r);
            else _host.StartQuiz(t);
        };
        return card;
    }

    private FrameworkElement MetaItem(string emoji, string text)
        => Ui.HStack(0,
            new TextBlock { Text = emoji, FontSize = 13, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center },
            Ui.T(text, 13, FontWeights.SemiBold, "Muted"));

    private UIElement BuildHistory(List<(Test t, Result? r, int used)> pairs)
    {
        var done = pairs.FindAll(p => p.r != null);
        var root = new StackPanel();
        if (done.Count == 0) { root.Children.Add(Ui.T("Nie masz jeszcze ukończonych testów.", 14, null, "Muted")); return root; }
        foreach (var (t, r, _) in done)
        {
            var grid = new Grid { Margin = new Thickness(22, 18, 22, 18) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var chip = Ui.IconChip("📄", t.ColorHex, 46); Grid.SetColumn(chip, 0);
            var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };
            texts.Children.Add(Ui.T(t.Title, 16, FontWeights.Bold));
            texts.Children.Add(Ui.T($"{t.Subject} · {r!.DateTaken:yyyy-MM-dd HH:mm}", 13, null, "Muted"));
            Grid.SetColumn(texts, 1);

            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var scoreBox = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
            scoreBox.Children.Add(new TextBlock { Text = r.Pct + "%", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Ui.Br(r.Pending ? "Warning" : r.Passed ? "Success" : "Danger"), HorizontalAlignment = HorizontalAlignment.Right, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
            scoreBox.Children.Add(new TextBlock { Text = $"{r.Score}/{r.MaxScore} pkt", FontSize = 12, Foreground = Ui.Br("Muted"), HorizontalAlignment = HorizontalAlignment.Right, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
            right.Children.Add(scoreBox);
            right.Children.Add(r.Pending ? Ui.Badge("W ocenie", "amber", true) : Ui.Badge(r.Passed ? "Zaliczony" : "Niezaliczony", r.Passed ? "green" : "red", true));
            Grid.SetColumn(right, 2);

            grid.Children.Add(chip); grid.Children.Add(texts); grid.Children.Add(right);
            var card = new Border { Style = Ui.St("Card"), Child = grid, Margin = new Thickness(0, 0, 0, 12), Cursor = Cursors.Hand };
            card.MouseLeftButtonUp += (_, _) => _host.ShowResult(t, r!);
            root.Children.Add(card);
        }
        return root;
    }
}
