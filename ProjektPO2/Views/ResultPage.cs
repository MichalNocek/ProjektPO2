using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class ResultPage : UserControl
{
    private readonly MainWindow _host;
    private readonly Test _test;
    private readonly Result _result;

    public ResultPage(MainWindow host, Test test, Result result)
    {
        _host = host; _test = test; _result = result;
        Build();
    }

    private void Build()
    {
        var dock = new DockPanel { Background = Ui.Br("Bg") };

        var topGrid = new Grid { Background = Ui.Br("Surface") };
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var back = Ui.Btn("‹  Moje testy", "SecondaryButton", (_, _) => _host.ShowStudentTests());
        back.Margin = new Thickness(32, 14, 0, 14); Grid.SetColumn(back, 0);
        var ttl = Ui.T(_test.Title + " · wynik", 15.5, FontWeights.Bold);
        ttl.HorizontalAlignment = HorizontalAlignment.Center; ttl.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(ttl, 1);
        topGrid.Children.Add(back); topGrid.Children.Add(ttl);
        var topBorder = new Border { Child = topGrid, BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 0, 0, 1) };
        topBorder.SetValue(DockPanel.DockProperty, Dock.Top); dock.Children.Add(topBorder);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var body   = new StackPanel { MaxWidth = 820, Margin = new Thickness(24, 32, 24, 60) };
        if (_result.Cheated) body.Children.Add(CheatBanner());
        body.Children.Add(HeroCard());
        var reviewHeader = Ui.T("Przegląd odpowiedzi", 16, FontWeights.Bold); reviewHeader.Margin = new Thickness(0, 24, 0, 14);
        body.Children.Add(reviewHeader);
        foreach (var q in _test.Questions) body.Children.Add(ReviewCard(q));
        var backBtn = Ui.Btn("Wróć do moich testów", "PrimaryButton", (_, _) => _host.ShowStudentTests());
        backBtn.Height = 50; backBtn.FontSize = 16; backBtn.HorizontalAlignment = HorizontalAlignment.Center; backBtn.Margin = new Thickness(0, 28, 0, 0);
        body.Children.Add(backBtn);
        scroll.Content = body; dock.Children.Add(scroll);
        Content = dock;
    }

    private Border CheatBanner()
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = "⛔", FontSize = 24, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) });
        var t = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        t.Children.Add(new TextBlock { Text = "Test oznaczony jako próba oszustwa", FontWeight = FontWeights.Bold, FontSize = 15.5, Foreground = Ui.Br("Danger"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        t.Children.Add(new TextBlock { Text = "Wykryto dwukrotne opuszczenie okna testu. Wynik został wyzerowany, a zdarzenie zgłoszone nauczycielowi.", FontSize = 13.5, Foreground = Ui.Br("Text2"), TextWrapping = TextWrapping.Wrap, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        sp.Children.Add(t);
        return new Border { Background = Ui.Br("DangerSoft"), BorderBrush = Ui.Br("Danger"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 16), Child = sp };
    }

    private Border HeroCard()
    {
        bool pending = _result.Pending, passed = _result.Passed;
        string colorKey = pending ? "Warning" : passed ? "Success" : "Danger";
        var grid = new Grid { Margin = new Thickness(12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var ring = MakeRing(_result.Pct, (SolidColorBrush)Ui.Br(colorKey), _result.Score, _result.MaxScore);
        Grid.SetColumn(ring, 0);
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(34, 0, 0, 0) };
        info.Children.Add(pending ? Ui.Badge("Oczekuje na ocenę nauczyciela", "amber", true) : Ui.Badge(passed ? "Zaliczony" : "Niezaliczony", passed ? "green" : "red", true));
        info.Children.Add(new TextBlock { Text = pending ? "Wynik wstępny" : passed ? "Dobra robota!" : "Trudniejszy temat", FontSize = 26, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 12, 0, 12), Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        var meta = new StackPanel { Orientation = Orientation.Horizontal };
        meta.Children.Add(MetaItem($"🎯 Próg: {_test.PassThreshold}%"));
        meta.Children.Add(MetaItem($"⏱ Czas: {_result.TimeUsed / 60}m {_result.TimeUsed % 60}s"));
        meta.Children.Add(MetaItem($"🏅 {_result.Score}/{_result.MaxScore} pkt"));
        info.Children.Add(meta); Grid.SetColumn(info, 1);
        grid.Children.Add(ring); grid.Children.Add(info);
        return Ui.Card(grid, 22);
    }

    private FrameworkElement MetaItem(string text)
        => new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 0, 22, 0), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };

    private Grid MakeRing(int pct, SolidColorBrush color, int score, int max)
    {
        const double size = 156, cx = 78, cy = 78, r = 66, th = 13;
        var grid = new Grid { Width = size, Height = size };
        grid.Children.Add(new Ellipse { Width = r * 2, Height = r * 2, Stroke = Ui.Br("Surface3"), StrokeThickness = th, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        double f = Math.Max(0, Math.Min(1, pct / 100.0));
        if (f >= 0.999)
            grid.Children.Add(new Ellipse { Width = r * 2, Height = r * 2, Stroke = color, StrokeThickness = th, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        else if (f > 0)
        {
            double theta = 2 * Math.PI * f;
            var start = new Point(cx, cy - r);
            var end   = new Point(cx + r * Math.Sin(theta), cy - r * Math.Cos(theta));
            var fig = new PathFigure { StartPoint = start };
            fig.Segments.Add(new ArcSegment(end, new Size(r, r), 0, f > 0.5, SweepDirection.Clockwise, true));
            var geo = new PathGeometry(); geo.Figures.Add(fig);
            grid.Children.Add(new Path { Data = geo, Stroke = color, StrokeThickness = th, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        }
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        center.Children.Add(new TextBlock { Text = pct + "%", FontSize = 38, FontWeight = FontWeights.Bold, Foreground = color, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        center.Children.Add(new TextBlock { Text = $"{score} / {max} pkt", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Muted"), HorizontalAlignment = HorizontalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        grid.Children.Add(center);
        return grid;
    }

    private Border ReviewCard(Question q)
    {
        _result.Answers.TryGetValue(q.Id, out var ans);
        var g = GradingService.Grade(q, ans, _result.Manual);
        string markKey   = g.pending ? "Warning" : g.ok ? "Success" : g.partial ? "Warning" : "Danger";
        string markSoft  = g.pending ? "WarningSoft" : g.ok ? "SuccessSoft" : g.partial ? "WarningSoft" : "DangerSoft";
        string markGlyph = g.pending ? "⏳" : g.ok ? "✓" : g.partial ? "⚑" : "✕";

        var stack = new StackPanel();
        var head = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var mark = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(8), Background = Ui.Br(markSoft), Margin = new Thickness(0, 0, 13, 0), VerticalAlignment = VerticalAlignment.Top };
        mark.Child = new TextBlock { Text = markGlyph, Foreground = Ui.Br(markKey), FontWeight = FontWeights.Bold, FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(mark, 0);
        var headTexts = new StackPanel();
        headTexts.Children.Add(Ui.T(g.pending ? $"— / {q.Points} pkt · oczekuje na ocenę" : $"{g.pts}/{q.Points} pkt", 12, FontWeights.Bold, "Muted"));
        headTexts.Children.Add(new TextBlock { Text = q.Text, FontSize = 16, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Br("Text"), Margin = new Thickness(0, 4, 0, 0), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        Grid.SetColumn(headTexts, 1);
        head.Children.Add(mark); head.Children.Add(headTexts);
        stack.Children.Add(head);

        if (q.Type == QuestionType.Open)
        {
            stack.Children.Add(ReviewLine("Twoja odp.: " + (GradingService.AsText(ans).Length > 0 ? GradingService.AsText(ans) : "— brak —"), g.pending ? "neutral" : g.ok ? "correct" : g.partial ? "neutral" : "wrong"));
            if (g.pending) stack.Children.Add(ReviewLine("⏳  Odpowiedź sprawdzi nauczyciel", "neutral"));
            else           stack.Children.Add(ReviewLine("Wzorcowa: " + q.Sample, "correct"));
        }
        else
        {
            var chosen = GradingService.AsIndices(ans);
            for (int i = 0; i < q.Options.Count; i++)
            {
                bool isCorrect = q.Correct.Contains(i), picked = chosen.Contains(i);
                string tone = isCorrect ? "correct" : picked ? "wrong" : "neutral";
                stack.Children.Add(ReviewLine($"{(char)('A' + i)}.  {q.Options[i]}{(picked ? "   (Twój wybór)" : "")}", tone));
            }
        }

        return new Border { Style = Ui.St("Card"), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 12), Child = stack };
    }

    private Border ReviewLine(string text, string tone)
    {
        (string bg, string fg) = tone switch { "correct" => ("SuccessSoft", "Success"), "wrong" => ("DangerSoft", "Danger"), _ => ("Surface2", "Text2") };
        return new Border { Background = Ui.Br(bg), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 11, 14, 11), Margin = new Thickness(0, 0, 0, 8), Child = new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Br(fg), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") } };
    }
}
