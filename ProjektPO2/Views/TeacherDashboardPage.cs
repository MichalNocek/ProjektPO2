using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class TeacherDashboardPage : UserControl
{
    private readonly MainWindow _host;

    public TeacherDashboardPage(MainWindow host)
    {
        _host = host;
        var tests    = DbRepository.GetTestsForTeacher(host.CurrentUser.Id);
        var students = DbRepository.GetStudents();
        var groups   = DbRepository.GetGroups();
        // Wyniki ograniczone do testów tego nauczyciela.
        var testIds  = tests.Select(t => t.Id).ToHashSet();
        var results  = DbRepository.GetAllResults().Where(r => testIds.Contains(r.TestId)).ToList();
        // Średnia liczona z najlepszego podejścia każdego ucznia w każdym teście.
        var bestResults = DbRepository.BestPerStudentPerTest(results);
        int avg      = bestResults.Count > 0 ? (int)bestResults.Average(r => r.Pct) : 0;

        var root = new StackPanel();

        var statRow = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 22) };
        statRow.Children.Add(StatCard("📄", "#2563EB", tests.Count.ToString(),    "Testy"));
        statRow.Children.Add(StatCard("👥", "#7C3AED", students.Count.ToString(), "Uczniowie"));
        statRow.Children.Add(StatCard("🗂", "#0D9488", groups.Count.ToString(),   "Grupy"));
        statRow.Children.Add(StatCard("🏆", "#15A34A", avg + "%",                 "Średni wynik"));
        root.Children.Add(statRow);

        var cols = new Grid();
        cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
        cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1,   GridUnitType.Star) });
        var left  = TestsCard(tests, results); left.Margin  = new Thickness(0, 0, 11, 0); Grid.SetColumn(left,  0);
        var right = RecentCard(results);       right.Margin = new Thickness(11, 0, 0, 0); Grid.SetColumn(right, 1);
        cols.Children.Add(left); cols.Children.Add(right);
        root.Children.Add(cols);

        Content = root;
    }

    private Border StatCard(string emoji, string color, string val, string label)
    {
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(Ui.IconChip(emoji, color, 44));
        sp.Children.Add(new TextBlock
        {
            Text = val, FontSize = 30, FontWeight = FontWeights.Bold,
            Foreground = Ui.Br("Text"), Margin = new Thickness(0, 12, 0, 0),
            FontFamily = (FontFamily)Application.Current.FindResource("UiFont")
        });
        sp.Children.Add(Ui.T(label, 13.5, FontWeights.SemiBold, "Muted"));
        return new Border { Style = Ui.St("Card"), Child = sp, Margin = new Thickness(0, 0, 16, 0) };
    }

    private Border TestsCard(List<Test> tests, List<Result> results)
    {
        var sp = new StackPanel();
        sp.Children.Add(new Border { Padding = new Thickness(22, 18, 22, 14), Child = Ui.T("Twoje testy", 15, FontWeights.Bold) });
        foreach (var t in tests)
        {
            int attempts = results.Count(r => r.TestId == t.Id);
            var row = new Grid { Margin = new Thickness(22, 14, 22, 14) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var chip = Ui.IconChip("📄", t.ColorHex, 40, 11);
            Grid.SetColumn(chip, 0);

            var tx = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) };
            tx.Children.Add(Ui.T(t.Title, 15, FontWeights.Bold));
            tx.Children.Add(Ui.T($"{t.Subject} · {t.Questions.Count} pytań", 12.5, null, "Muted"));
            Grid.SetColumn(tx, 1);

            var statBtn = Ui.Btn("Statystyki", "SecondaryButton", (_, _) => _host.ShowStats(t));
            statBtn.Height = 34; statBtn.FontSize = 13; statBtn.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(statBtn, 2);

            row.Children.Add(chip); row.Children.Add(tx); row.Children.Add(statBtn);
            var b = new Border
            {
                Child = row, BorderBrush = Ui.Br("Border"),
                BorderThickness = new Thickness(0, 0, 0, 1), Cursor = Cursors.Hand
            };
            b.MouseLeftButtonUp += (_, _) => _host.ShowEditor(t);
            sp.Children.Add(b);
        }
        return new Border { Style = Ui.St("Card"), Child = sp, VerticalAlignment = VerticalAlignment.Top };
    }

    private Border RecentCard(List<Result> results)
    {
        var sp = new StackPanel();
        sp.Children.Add(new Border { Padding = new Thickness(22, 18, 22, 10), Child = Ui.T("Ostatnie wyniki", 15, FontWeights.Bold) });
        var students = DbRepository.GetStudents().ToDictionary(s => s.Id);
        var tests    = DbRepository.GetTestsForTeacher(_host.CurrentUser.Id).ToDictionary(t => t.Id);
        foreach (var r in results.Take(7))
        {
            if (!students.TryGetValue(r.StudentId, out var st)) continue;
            var row = new Grid { Margin = new Thickness(22, 11, 22, 11) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var av = Avatar(st.Name, 34);
            Grid.SetColumn(av, 0);

            var tx = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            tx.Children.Add(Ui.T(st.Name, 13.5, FontWeights.Bold));
            tx.Children.Add(Ui.T(tests.TryGetValue(r.TestId, out var tt) ? tt.Title : "", 12, null, "Muted"));
            Grid.SetColumn(tx, 1);

            var badge = r.Cheated ? Ui.Badge("⛔ Oszukiwał", "red", true) : Ui.Badge(r.Pct + "%", r.Passed ? "green" : "red");
            Grid.SetColumn(badge, 2);

            row.Children.Add(av); row.Children.Add(tx); row.Children.Add(badge);
            sp.Children.Add(row);
        }
        return new Border { Style = Ui.St("Card"), Child = sp, VerticalAlignment = VerticalAlignment.Top };
    }

    public static Border Avatar(string name, double size)
    {
        var parts = name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        string ini = (parts.Length > 0 ? parts[0].Substring(0, 1) : "")
                   + (parts.Length > 1 ? parts[1].Substring(0, 1) : "");
        return new Border
        {
            Width = size, Height = size, CornerRadius = new CornerRadius(size / 2),
            Background = MainWindow.AvatarColor(name), VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = ini.ToUpper(), Foreground = Brushes.White, FontWeight = FontWeights.Bold,
                FontSize = size * 0.36, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }
}
