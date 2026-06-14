using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class StatsPage : UserControl
{
    private readonly MainWindow _host;

    public StatsPage(MainWindow host, Test? selected)
    {
        _host   = host;
        Content = selected == null ? BuildPicker() : BuildStats(selected);
    }

    private UIElement BuildPicker()
    {
        var root = new StackPanel();
        root.Children.Add(new TextBlock { Text = "Wybierz test, aby zobaczyć statystyki:", FontSize = 14, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 0, 0, 16), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        var wrap    = new WrapPanel();
        var results = DbRepository.GetAllResults();
        foreach (var t in DbRepository.GetTestsForTeacher(_host.CurrentUser.Id))
        {
            int attempts = results.Count(r => r.TestId == t.Id);
            var sp = new StackPanel();
            sp.Children.Add(new Border { Height = 6, Background = Ui.Hex(t.ColorHex), CornerRadius = new CornerRadius(18, 18, 0, 0) });
            var body = new StackPanel { Margin = new Thickness(18, 16, 18, 18) };
            body.Children.Add(Ui.HStack(0,
                new Border { Width = 7, Height = 7, CornerRadius = new CornerRadius(4), Background = Ui.Hex(t.ColorHex), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 7, 0) },
                Ui.T(t.Subject, 12, FontWeights.Bold, "Muted")));
            body.Children.Add(new TextBlock { Text = t.Title, FontSize = 17, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 14), Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
            var foot = new Grid();
            foot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            foot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var info = Ui.T(attempts + " podejść", 13, FontWeights.SemiBold, "Muted"); info.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(info, 0);
            var btn = Ui.Btn("📊  Otwórz", "SecondaryButton", (_, _) => _host.ShowStats(t)); btn.Height = 34; btn.FontSize = 13; Grid.SetColumn(btn, 1);
            foot.Children.Add(info); foot.Children.Add(btn);
            body.Children.Add(foot); sp.Children.Add(body);
            var card = new Border { Style = Ui.St("Card"), Width = 320, Margin = new Thickness(0, 0, 18, 18), Child = sp, Cursor = Cursors.Hand };
            card.MouseLeftButtonUp += (_, _) => _host.ShowStats(t);
            wrap.Children.Add(card);
        }
        root.Children.Add(wrap);
        return root;
    }

    private UIElement BuildStats(Test test)
    {
        // Liczy się najlepsze podejście każdego ucznia (jeden wiersz na ucznia).
        var allResults = DbRepository.GetResultsForTest(test.Id);
        var results    = DbRepository.BestPerStudent(allResults);
        var students   = DbRepository.GetStudents().ToDictionary(s => s.Id);
        // Uczniowie, którzy oszukiwali w KTÓRYMKOLWIEK podejściu (znacznik widać,
        // nawet jeśli najlepsze podejście było uczciwe).
        var everCheated = allResults.Where(r => r.Cheated).Select(r => r.StudentId).ToHashSet();
        int passRate = results.Count > 0 ? (int)Math.Round(results.Count(r => r.Passed) * 100.0 / results.Count) : 0;
        int avg      = results.Count > 0 ? (int)Math.Round(results.Average(r => r.Pct)) : 0;

        var qStats  = test.Questions.Select(q => (q, pct: GradingService.Correctness(test, q, results))).ToList();
        var hardest = qStats.OrderBy(x => x.pct).FirstOrDefault();

        var root = new StackPanel();
        var back = Ui.Btn("‹  Wszystkie testy", "SecondaryButton", (_, _) => _host.ShowStats());
        back.HorizontalAlignment = HorizontalAlignment.Left; back.Margin = new Thickness(0, 0, 0, 22);
        root.Children.Add(back);

        var top = new Grid { Margin = new Thickness(0, 0, 0, 22) };
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

        var donutCard = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        donutCard.Children.Add(MakeDonut(passRate, (SolidColorBrush)Ui.Br("Success"), "zdawalność"));
        donutCard.Children.Add(new TextBlock { Text = $"{results.Count(r => r.Passed)}/{results.Count} zaliczyło", FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = Ui.Br("Muted"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        var c1 = Ui.Card(donutCard, 20); c1.Margin = new Thickness(0, 0, 16, 0); Grid.SetColumn(c1, 0);

        var attCard = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        attCard.Children.Add(Ui.IconChip("👥", "#2563EB", 44));
        var ac = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) };
        ac.Children.Add(new TextBlock { Text = results.Count.ToString(), FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        ac.Children.Add(Ui.T($"Uczniów · {allResults.Count} podejść · śr. {avg}%", 13.5, FontWeights.SemiBold, "Muted"));
        attCard.Children.Add(ac);
        var c2 = Ui.Card(attCard, 20); c2.Margin = new Thickness(0, 0, 16, 0); Grid.SetColumn(c2, 1);

        var hardCard = new StackPanel { Orientation = Orientation.Horizontal };
        hardCard.Children.Add(Ui.IconChip("⚡", "#D97706", 44));
        var hc = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) };
        hc.Children.Add(Ui.T("Najtrudniejsze pytanie", 13, FontWeights.Bold, "Warning"));
        hc.Children.Add(new TextBlock { Text = hardest.q != null ? hardest.q.Text : "—", FontSize = 14.5, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        if (hardest.q != null) hc.Children.Add(Ui.T($"tylko {hardest.pct}% poprawnych odpowiedzi", 12.5, null, "Muted"));
        hardCard.Children.Add(hc);
        var c3 = Ui.Card(hardCard, 20); Grid.SetColumn(c3, 2);

        top.Children.Add(c1); top.Children.Add(c2); top.Children.Add(c3);
        root.Children.Add(top);

        var barsSp = new StackPanel();
        barsSp.Children.Add(Ui.T("Poprawność wg pytań", 15, FontWeights.Bold));
        barsSp.Children.Add(new TextBlock { Text = "Średni procent zdobytych punktów dla każdego pytania.", FontSize = 13, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 4, 0, 10), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        int qi = 1;
        foreach (var (q, pct) in qStats) barsSp.Children.Add(BarRow($"{qi++}. {q.Text}", pct));
        var barsCard = Ui.Card(barsSp, 22); barsCard.Margin = new Thickness(0, 0, 0, 22);
        root.Children.Add(barsCard);

        var tableSp = new StackPanel();
        var tableHead = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        tableHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tableHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var tableTitle = new TextBlock { Text = "Wyniki uczniów", FontSize = 15, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        Grid.SetColumn(tableTitle, 0);
        var exportBtn = Ui.Btn("⬇  Eksportuj CSV", "SecondaryButton", (_, _) => ExportCsv(test, results, students, everCheated));
        exportBtn.Height = 34; exportBtn.FontSize = 13; exportBtn.IsEnabled = results.Count > 0;
        Grid.SetColumn(exportBtn, 1);
        tableHead.Children.Add(tableTitle); tableHead.Children.Add(exportBtn);
        tableSp.Children.Add(tableHead);
        // Tabela pokazuje WSZYSTKIE podejścia (każde jako osobny wiersz), z oznaczeniem
        // najlepszego u uczniów, którzy podchodzili więcej niż raz.
        var bestIds      = results.Select(br => br.Id).ToHashSet();
        var attemptCount = allResults.GroupBy(br => br.StudentId).ToDictionary(g => g.Key, g => g.Count());

        tableSp.Children.Add(StudentRow(true, null, null, test, false));
        if (allResults.Count == 0)
            tableSp.Children.Add(new TextBlock { Text = "Brak podejść do tego testu.", FontSize = 14, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 16, 0, 8), HorizontalAlignment = HorizontalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        foreach (var r in allResults
                     .Where(x => students.ContainsKey(x.StudentId))
                     .OrderBy(x => students[x.StudentId].Name)
                     .ThenBy(x => x.DateTaken))
        {
            var st = students[r.StudentId];
            bool bestMark = attemptCount[r.StudentId] > 1 && bestIds.Contains(r.Id);
            tableSp.Children.Add(StudentRow(false, st, r, test, bestMark));
        }
        root.Children.Add(Ui.Card(tableSp, 22));

        return root;
    }

    private void ExportCsv(Test test, List<Result> results, Dictionary<int, User> students, HashSet<int> cheatedEver)
    {
        // Bezpieczna domyślna nazwa pliku (bez znaków niedozwolonych w ścieżce)
        string safeTitle = string.Join("_", test.Title.Split(System.IO.Path.GetInvalidFileNameChars()));
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName  = $"wyniki_{safeTitle}.csv",
            Filter    = "Plik CSV (*.csv)|*.csv",
            DefaultExt = ".csv"
        };
        if (dlg.ShowDialog() != true) return;

        if (Safe.Run(() => CsvExporter.ExportTestResults(dlg.FileName, test, results, students, cheatedEver), "wyeksportować wyników do CSV"))
            MessageBox.Show($"Zapisano {results.Count} wyników do pliku:\n{dlg.FileName}",
                "Eksport zakończony", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static readonly GridLength[] Cols =
    {
        new(1.8, GridUnitType.Star), new(1, GridUnitType.Star), new(1, GridUnitType.Star),
        new(1, GridUnitType.Star), new(1, GridUnitType.Star), new(1.2, GridUnitType.Star),
        new(1.2, GridUnitType.Star)
    };

    private FrameworkElement StudentRow(bool header, User? st, Result? r, Test test, bool isBest)
    {
        var grid = new Grid { Margin = new Thickness(0, header ? 0 : 12, 0, header ? 12 : 0) };
        foreach (var w in Cols) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = w });
        UIElement Cell(int col, UIElement el) { Grid.SetColumn(el, col); return el; }
        if (header)
        {
            string[] hs = { "UCZEŃ", "GRUPA", "WYNIK", "PROCENT", "CZAS", "STATUS", "OCENA" };
            for (int i = 0; i < hs.Length; i++)
                grid.Children.Add(Cell(i, new TextBlock { Text = hs[i], FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Muted2"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") }));
            return new Border { Child = grid, BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 0, 0, 2) };
        }
        var nameCell = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        nameCell.Children.Add(TeacherDashboardPage.Avatar(st!.Name, 30));
        nameCell.Children.Add(new TextBlock { Text = st.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        if (isBest)
        {
            var bestBadge = Ui.Badge("★", "green");
            bestBadge.Margin = new Thickness(8, 0, 0, 0);
            bestBadge.ToolTip = "Najlepsze podejście — liczone do oceny i statystyk";
            nameCell.Children.Add(bestBadge);
        }
        grid.Children.Add(Cell(0, nameCell));
        grid.Children.Add(Cell(1, Ui.T(st.GroupName, 14, null, "Muted")));
        grid.Children.Add(Cell(2, Ui.T($"{r!.Score}/{r.MaxScore} pkt", 14, FontWeights.Bold)));
        grid.Children.Add(Cell(3, new TextBlock { Text = r.Pct + "%", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Ui.Br(r.Passed ? "Success" : "Danger"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") }));
        grid.Children.Add(Cell(4, Ui.T($"{r.TimeUsed / 60}m {r.TimeUsed % 60}s", 14, null, "Muted")));
        // Jeden, czysty status na wiersz — każde podejście ma swój własny wiersz,
        // więc próba oszustwa jest widoczna na swoim wierszu (bez doklejania do innych).
        UIElement statusBadge = r!.Cheated
            ? Ui.Badge("⛔ Oszukiwał", "red", true)
            : r.Pending
                ? Ui.Badge("Oczekuje na ocenę", "amber", true)
                : Ui.Badge(r.Passed ? "Zaliczony" : "Niezaliczony", r.Passed ? "green" : "red", true);
        grid.Children.Add(Cell(5, statusBadge));

        var editBtn = Ui.Btn("Edytuj ocenę", "SecondaryButton", (_, _) =>
        {
            var win = new GradeEditWindow(test, r!, st!) { Owner = _host };
            if (win.ShowDialog() == true) _host.ShowStats(test);
        });
        editBtn.Height = 30; editBtn.FontSize = 12; editBtn.HorizontalAlignment = HorizontalAlignment.Left;
        grid.Children.Add(Cell(6, editBtn));

        foreach (var child in grid.Children) if (child is FrameworkElement fe) fe.VerticalAlignment = VerticalAlignment.Center;
        return new Border { Child = grid, BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 2, 0, 2) };
    }

    private FrameworkElement BarRow(string label, int pct)
    {
        string colorKey = pct >= 70 ? "Success" : pct >= 45 ? "Warning" : "Danger";
        var grid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        var lbl = new TextBlock { Text = label, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.Br("Text2"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        Grid.SetColumn(lbl, 0);
        var track = new Grid { Height = 26, Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
        track.Children.Add(new Border { Background = Ui.Br("Surface3"), CornerRadius = new CornerRadius(8) });
        var fill = new Border { Background = Ui.Br(colorKey), CornerRadius = new CornerRadius(8), HorizontalAlignment = HorizontalAlignment.Left };
        fill.Loaded += (_, _) => fill.Width = track.ActualWidth * pct / 100.0;
        track.Children.Add(fill); Grid.SetColumn(track, 1);
        var val = new TextBlock { Text = pct + "%", FontSize = 13.5, FontWeight = FontWeights.Bold, Foreground = Ui.Br(colorKey), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        Grid.SetColumn(val, 2);
        grid.Children.Add(lbl); grid.Children.Add(track); grid.Children.Add(val);
        return grid;
    }

    private Grid MakeDonut(int pct, SolidColorBrush color, string sub)
    {
        const double size = 130, c = 65, r = 53, th = 11;
        var grid = new Grid { Width = size, Height = size };
        grid.Children.Add(new Ellipse { Width = r * 2, Height = r * 2, Stroke = Ui.Br("Surface3"), StrokeThickness = th, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        double f = Math.Max(0, Math.Min(1, pct / 100.0));
        if (f >= 0.999)
            grid.Children.Add(new Ellipse { Width = r * 2, Height = r * 2, Stroke = color, StrokeThickness = th, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        else if (f > 0)
        {
            double theta = 2 * Math.PI * f;
            var start = new Point(c, c - r);
            var end   = new Point(c + r * Math.Sin(theta), c - r * Math.Cos(theta));
            var fig   = new PathFigure { StartPoint = start };
            fig.Segments.Add(new ArcSegment(end, new Size(r, r), 0, f > 0.5, SweepDirection.Clockwise, true));
            var geo = new PathGeometry(); geo.Figures.Add(fig);
            grid.Children.Add(new Path { Data = geo, Stroke = color, StrokeThickness = th, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        }
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        center.Children.Add(new TextBlock { Text = pct + "%", FontSize = 28, FontWeight = FontWeights.Bold, Foreground = color, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        center.Children.Add(new TextBlock { Text = sub, FontSize = 12, Foreground = Ui.Br("Muted"), HorizontalAlignment = HorizontalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        grid.Children.Add(center);
        return grid;
    }
}
