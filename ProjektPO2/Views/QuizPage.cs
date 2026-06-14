using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class QuizPage : UserControl
{
    private readonly MainWindow _host;
    private readonly Test _test;
    private readonly Dictionary<int, object> _answers = new();
    private int _idx;
    private int _secondsLeft;
    private DispatcherTimer? _timer;

    // Anti-cheat: liczy ile razy uczeń opuścił okno testu (np. alt-tab do ChatGPT/przeglądarki)
    private int _violations;
    private bool _finished;            // test zakończony — ignoruj dalsze zdarzenia
    private bool _ignoreDeactivation;  // własne okna dialogowe nie liczą się jako opuszczenie

    private TextBlock _timerText   = null!;
    private Border    _timerBox    = null!;
    private TextBlock _counter     = null!;
    private TextBlock _answeredText = null!;
    private Border    _progressFill = null!;
    private Grid      _progressTrack = null!;
    private StackPanel _questionHost = null!;
    private StackPanel _dotsHost    = null!;
    private Button _prevBtn = null!, _nextBtn = null!;

    public QuizPage(MainWindow host, Test test)
    {
        _host = host; _test = test;
        _secondsLeft = test.TimeLimit * 60;
        Build();
        RenderQuestion();
        StartTimer();

        // Nadzór nad oknem: gdy aplikacja straci fokus podczas testu = podejrzenie oszustwa
        _host.Deactivated += OnHostDeactivated;
        // Gdy strona opuszcza drzewo wizualne (np. wylogowanie w trakcie testu) — zatrzymaj
        // timer i odepnij zdarzenia, żeby po czasie nie odpalił się „duch" zapisu wyniku
        // na już zamkniętym oknie.
        Unloaded += (_, _) =>
        {
            _finished = true;
            _timer?.Stop();
            _host.Deactivated -= OnHostDeactivated;
        };
    }

    // ---------- ANTI-CHEAT ----------
    private void OnHostDeactivated(object? sender, EventArgs e)
    {
        if (_ignoreDeactivation || _finished) return;

        _violations++;
        if (_violations == 1)
        {
            Msg("⚠ Wykryto opuszczenie okna testu!\n\n" +
                "Nie przełączaj się na inne aplikacje (np. przeglądarkę czy ChatGPT) podczas testu. " +
                "Kolejne opuszczenie okna spowoduje automatyczne zakończenie testu z wynikiem 0% " +
                "i zgłoszenie próby oszustwa nauczycielowi.",
                "Ostrzeżenie", MessageBoxImage.Warning);
        }
        else
        {
            FailForCheating();
        }
    }

    private void FailForCheating()
    {
        if (_finished) return;
        _finished = true;
        _timer?.Stop();
        _host.Deactivated -= OnHostDeactivated;

        int timeUsed = _test.TimeLimit * 60 - Math.Max(0, _secondsLeft);
        var result = new Result
        {
            TestId    = _test.Id,
            StudentId = _host.CurrentUser.Id,
            Answers   = _answers,
            Manual    = new(),
            Score     = 0,
            MaxScore  = _test.MaxPoints,
            Pct       = 0,
            Passed    = false,
            Pending   = false,
            Cheated   = true,
            TimeUsed  = timeUsed,
            DateTaken = DateTime.Now
        };
        Safe.Run(() => DbRepository.AddAttempt(result), "zapisać wyniku testu");

        Msg("Test został przerwany.\n\n" +
            "Wykryto dwukrotne opuszczenie okna testu (przełączenie na inną aplikację). " +
            "Test został oznaczony jako próba oszustwa i zgłoszony nauczycielowi. Wynik: 0%.",
            "Test przerwany — wykryto oszustwo", MessageBoxImage.Stop);

        _host.RefreshNav();
        _host.ShowResult(_test, result);
    }

    // Pokazuje okno dialogowe nie licząc go jako opuszczenie okna testu.
    private MessageBoxResult Msg(string text, string title, MessageBoxImage icon,
                                 MessageBoxButton btn = MessageBoxButton.OK)
    {
        _ignoreDeactivation = true;
        try { return MessageBox.Show(text, title, btn, icon); }
        finally { _ignoreDeactivation = false; }
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => { _secondsLeft--; UpdateTimer(); if (_secondsLeft <= 0) Finish(); };
        _timer.Start();
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        _timerText.Text = $"{_secondsLeft / 60:00}:{_secondsLeft % 60:00}";
        bool warn = _secondsLeft <= 60;
        _timerBox.Background = warn ? Ui.Br("DangerSoft") : Ui.Br("Surface2");
        _timerText.Foreground = warn ? Ui.Br("Danger") : Ui.Br("Text");
    }

    private void Build()
    {
        var dock = new DockPanel { Background = Ui.Br("Bg") };

        // Top bar
        var top = new Grid { Background = Ui.Br("Surface") };
        top.SetValue(DockPanel.DockProperty, Dock.Top);
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(32, 16, 0, 16) };
        var mark = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(9), Background = Ui.Br("Primary"), Margin = new Thickness(0, 0, 12, 0) };
        mark.Child = new TextBlock { Text = "🎓", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titles.Children.Add(Ui.T(_test.Title, 15.5, FontWeights.Bold));
        titles.Children.Add(Ui.T(_test.Subject, 12.5, null, "Muted"));
        titleRow.Children.Add(mark); titleRow.Children.Add(titles);
        Grid.SetColumn(titleRow, 0);
        _timerText = new TextBlock { Text = "00:00", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        _timerBox  = new Border { CornerRadius = new CornerRadius(10), BorderBrush = Ui.Br("Border2"), BorderThickness = new Thickness(1.5), Padding = new Thickness(16, 0, 16, 0), Height = 42, VerticalAlignment = VerticalAlignment.Center, Child = Ui.HStack(9, new TextBlock { Text = "⏱", FontSize = 15, VerticalAlignment = VerticalAlignment.Center }, _timerText) };
        var exitBtn = Ui.Btn("Przerwij", "SecondaryButton", (_, _) => ConfirmExit());
        var rightRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 32, 16) };
        rightRow.Children.Add(_timerBox); exitBtn.Margin = new Thickness(16, 0, 0, 0); rightRow.Children.Add(exitBtn);
        Grid.SetColumn(rightRow, 1);
        top.Children.Add(titleRow); top.Children.Add(rightRow);
        dock.Children.Add(new Border { Child = top, BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 0, 0, 1) } .Also(b => b.SetValue(DockPanel.DockProperty, Dock.Top)));

        // Progress
        var progWrap = new Border { Background = Ui.Br("Surface"), BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 0, 0, 1) };
        progWrap.SetValue(DockPanel.DockProperty, Dock.Top);
        var progInner = new Grid { MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 16) };
        progInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        progInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _counter = Ui.T("", 13, FontWeights.Bold, "Muted"); _counter.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(_counter, 0);
        _progressTrack = new Grid { Height = 8, Margin = new Thickness(16, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        var trackBg = new Border { Background = Ui.Br("Surface3"), CornerRadius = new CornerRadius(100) };
        _progressFill = new Border { Background = Ui.Br("Primary"), CornerRadius = new CornerRadius(100), HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
        _progressTrack.Children.Add(trackBg); _progressTrack.Children.Add(_progressFill); Grid.SetColumn(_progressTrack, 1);
        _answeredText = Ui.T("", 13, FontWeights.Bold, "Muted"); _answeredText.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(_answeredText, 2);
        progInner.Children.Add(_counter); progInner.Children.Add(_progressTrack); progInner.Children.Add(_answeredText);
        progWrap.Child = progInner; dock.Children.Add(progWrap);

        // Bottom nav
        var navBorder = new Border { Background = Ui.Br("Surface"), BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 1, 0, 0) };
        navBorder.SetValue(DockPanel.DockProperty, Dock.Bottom);
        var navInner = new Grid { MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 16) };
        navInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        navInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        navInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _prevBtn = Ui.Btn("‹  Wstecz", "SecondaryButton", (_, _) => { if (_idx > 0) { _idx--; RenderQuestion(); } });
        Grid.SetColumn(_prevBtn, 0);
        _dotsHost = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_dotsHost, 1);
        _nextBtn = Ui.Btn("Dalej  ›", "PrimaryButton", (_, _) => NextOrFinish());
        Grid.SetColumn(_nextBtn, 2);
        navInner.Children.Add(_prevBtn); navInner.Children.Add(_dotsHost); navInner.Children.Add(_nextBtn);
        navBorder.Child = navInner; dock.Children.Add(navBorder);

        // Question area
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _questionHost = new StackPanel { MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(32, 38, 32, 38) };
        scroll.Content = _questionHost; dock.Children.Add(scroll);
        Content = dock;
    }

    private void NextOrFinish()
    {
        if (_idx < _test.Questions.Count - 1) { _idx++; RenderQuestion(); }
        else ConfirmFinish();
    }

    private int AnsweredCount()
    {
        int n = 0;
        foreach (var q in _test.Questions)
        {
            if (!_answers.TryGetValue(q.Id, out var a)) continue;
            if (a is List<int> li && li.Count > 0) n++;
            else if (a is string s && s.Trim().Length > 0) n++;
        }
        return n;
    }

    private void RenderQuestion()
    {
        var q = _test.Questions[_idx];
        int total = _test.Questions.Count;
        _counter.Text      = $"Pytanie {_idx + 1} / {total}";
        _answeredText.Text = $"{AnsweredCount()} udzielonych";

        _progressTrack.Dispatcher.BeginInvoke(new Action(() =>
            _progressFill.Width = _progressTrack.ActualWidth * (_idx + 1) / total), DispatcherPriority.Loaded);

        _prevBtn.IsEnabled = _idx > 0;
        _nextBtn.Content   = _idx < total - 1 ? "Dalej  ›" : "Zakończ  ⚑";

        _dotsHost.Children.Clear();
        for (int i = 0; i < total; i++)
        {
            int captured = i;
            var qq = _test.Questions[i];
            bool answered = _answers.TryGetValue(qq.Id, out var a) &&
                            ((a is List<int> li && li.Count > 0) || (a is string s && s.Trim().Length > 0));
            string bg = i == _idx ? "Primary" : answered ? "PrimarySoft2" : "Surface3";
            string fg = i == _idx ? "Surface"  : answered ? "Primary"     : "Muted";
            var dot = new Border { Width = 30, Height = 30, CornerRadius = new CornerRadius(8), Background = Ui.Br(bg), Margin = new Thickness(3, 0, 3, 0), Cursor = Cursors.Hand, Child = new TextBlock { Text = (i + 1).ToString(), FontSize = 12.5, FontWeight = FontWeights.Bold, Foreground = Ui.Br(fg), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            dot.MouseLeftButtonUp += (_, _) => { _idx = captured; RenderQuestion(); };
            _dotsHost.Children.Add(dot);
        }

        _questionHost.Children.Clear();
        string typeLabel = q.Type switch { QuestionType.Single => "Jedna odpowiedź", QuestionType.Multi => "Wiele odpowiedzi", _ => "Odpowiedź otwarta" };
        _questionHost.Children.Add(Ui.T($"{typeLabel.ToUpper()} · {q.Points} PKT", 12, FontWeights.Bold, "Primary"));
        _questionHost.Children.Add(new TextBlock { Text = q.Text, FontSize = 24, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Br("Text"), Margin = new Thickness(0, 14, 0, 24), LineHeight = 33, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });

        if (q.Type == QuestionType.Open)
        {
            var tb = new TextBox { Text = _answers.TryGetValue(q.Id, out var v) ? GradingService.AsText(v) : "", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 130, FontSize = 16, VerticalContentAlignment = VerticalAlignment.Top };
            tb.TextChanged += (_, _) => { _answers[q.Id] = tb.Text; _answeredText.Text = $"{AnsweredCount()} udzielonych"; };
            _questionHost.Children.Add(tb);
        }
        else
        {
            if (q.Type == QuestionType.Multi)
                _questionHost.Children.Add(new TextBlock { Text = "✓ Zaznacz wszystkie poprawne odpowiedzi", FontSize = 13, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, -16, 0, 16), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });

            var chosen = _answers.TryGetValue(q.Id, out var cv) ? GradingService.AsIndices(cv) : new List<int>();
            for (int i = 0; i < q.Options.Count; i++)
                _questionHost.Children.Add(OptionRow(q, i, chosen.Contains(i)));
        }
    }

    private Border OptionRow(Question q, int i, bool selected)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        UIElement marker;
        if (q.Type == QuestionType.Single)
            marker = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(9), Margin = new Thickness(0, 0, 15, 0), Background = selected ? Ui.Br("Primary") : Ui.Br("Surface3"), Child = new TextBlock { Text = ((char)('A' + i)).ToString(), FontWeight = FontWeights.Bold, FontSize = 15, Foreground = selected ? Brushes.White : Ui.Br("Muted2"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        else
            marker = new Border { Width = 24, Height = 24, CornerRadius = new CornerRadius(7), Margin = new Thickness(0, 0, 15, 0), BorderThickness = new Thickness(2), BorderBrush = selected ? Ui.Br("Primary") : Ui.Br("Border2"), Background = selected ? Ui.Br("Primary") : Brushes.Transparent, Child = selected ? new TextBlock { Text = "✓", Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } : null };

        Grid.SetColumn((UIElement)marker, 0);
        var text = new TextBlock { Text = q.Options[i], FontSize = 15.5, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        Grid.SetColumn(text, 1);
        grid.Children.Add(marker); grid.Children.Add(text);

        var border = new Border { CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1.5), BorderBrush = selected ? Ui.Br("Primary") : Ui.Br("Border2"), Background = selected ? Ui.Br("PrimarySoft") : Ui.Br("Surface"), Padding = new Thickness(18, 16, 18, 16), Margin = new Thickness(0, 0, 0, 12), Child = grid, Cursor = Cursors.Hand };
        border.MouseLeftButtonUp += (_, _) => ToggleAnswer(q, i);
        return border;
    }

    private void ToggleAnswer(Question q, int i)
    {
        if (q.Type == QuestionType.Single)
            _answers[q.Id] = new List<int> { i };
        else
        {
            var list = _answers.TryGetValue(q.Id, out var v) ? GradingService.AsIndices(v) : new List<int>();
            if (list.Contains(i)) list.Remove(i); else list.Add(i);
            _answers[q.Id] = list;
        }
        RenderQuestion();
    }

    private void ConfirmExit()
    {
        var r = Msg("Przerwać test? Odpowiedzi nie zostaną zapisane.", "TestPortal", MessageBoxImage.Question, MessageBoxButton.YesNo);
        if (r == MessageBoxResult.Yes)
        {
            _finished = true;
            _timer?.Stop();
            _host.Deactivated -= OnHostDeactivated;
            _host.ShowStudentTests();
        }
    }

    private void ConfirmFinish()
    {
        // Zbierz numery pytań bez udzielonej odpowiedzi (do pytań można wrócić
        // przyciskiem „Wstecz" lub kropkami, więc ostrzegamy dopiero przy oddaniu).
        var unanswered = new List<int>();
        for (int i = 0; i < _test.Questions.Count; i++)
        {
            var q = _test.Questions[i];
            bool answered = _answers.TryGetValue(q.Id, out var a) &&
                            ((a is List<int> li && li.Count > 0) || (a is string s && s.Trim().Length > 0));
            if (!answered) unanswered.Add(i + 1);
        }

        string text = unanswered.Count == 0
            ? "Zakończyć i oddać test? Odpowiedziałeś na wszystkie pytania."
            : $"Nie odpowiedziałeś na {(unanswered.Count == 1 ? "pytanie" : "pytania")}: {string.Join(", ", unanswered)}.\n\n" +
              "Czy na pewno chcesz oddać test? Pytań bez odpowiedzi nie da się już uzupełnić po oddaniu.";

        var r = Msg(text, "Zakończ test", MessageBoxImage.Question, MessageBoxButton.YesNo);
        if (r == MessageBoxResult.Yes) Finish();
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        _timer?.Stop();
        _host.Deactivated -= OnHostDeactivated;

        int timeUsed = _test.TimeLimit * 60 - Math.Max(0, _secondsLeft);
        var result   = GradingService.NewResult(_test, _host.CurrentUser.Id, _answers, timeUsed);
        if (!Safe.Run(() => DbRepository.AddAttempt(result), "zapisać wyniku testu")) return;
        _host.RefreshNav();
        _host.ShowResult(_test, result);
    }
}

internal static class BorderExt
{
    public static Border Also(this Border b, Action<Border> action) { action(b); return b; }
}
