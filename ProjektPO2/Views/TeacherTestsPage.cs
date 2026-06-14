using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class TeacherTestsPage : UserControl
{
    private readonly MainWindow _host;

    public TeacherTestsPage(MainWindow host)
    {
        _host = host;
        var tests   = DbRepository.GetTestsForTeacher(host.CurrentUser.Id);
        var groups  = DbRepository.GetGroups();
        var results = DbRepository.GetAllResults();

        var table = new StackPanel();
        table.Children.Add(MakeRow(true, "NAZWA TESTU", "PRZEDMIOT", "PYTANIA", "CZAS", "GRUPY", "PODEJŚCIA", ""));

        foreach (var t in tests)
        {
            var groupNames = t.GroupIds
                .Select(id => groups.FirstOrDefault(g => g.Id == id)?.Name)
                .Where(n => n != null)
                .ToList();
            int attempts = results.Count(r => r.TestId == t.Id);

            var nameCell = new Grid { VerticalAlignment = VerticalAlignment.Center };
            nameCell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            nameCell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var nameDot = new Border
            {
                Width = 8, Height = 8, CornerRadius = new CornerRadius(3),
                Background = Ui.Hex(t.ColorHex), Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameDot, 0);
            var nameTitle = new TextBlock
            {
                Text = t.Title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Text"),
                TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0),
                FontFamily = (FontFamily)Application.Current.FindResource("UiFont")
            };
            Grid.SetColumn(nameTitle, 1);
            nameCell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var statusBadge = Ui.Badge(t.IsActive ? "● Aktywny" : "○ Nieaktywny", t.IsActive ? "green" : "gray");
            statusBadge.Cursor = Cursors.Hand;
            statusBadge.VerticalAlignment = VerticalAlignment.Center;
            statusBadge.ToolTip = t.IsActive ? "Kliknij, aby ukryć test przed uczniami" : "Kliknij, aby udostępnić test uczniom";
            statusBadge.MouseLeftButtonUp += (_, _) => ToggleActive(t);
            Grid.SetColumn(statusBadge, 2);
            nameCell.Children.Add(nameDot); nameCell.Children.Add(nameTitle); nameCell.Children.Add(statusBadge);

            var groupsCell = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var gn in groupNames) { var b = Ui.Badge(gn!, "gray"); b.Margin = new Thickness(0, 0, 6, 0); groupsCell.Children.Add(b); }

            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            var statBtn = Ui.Btn("Statystyki", "SecondaryButton", (_, _) => _host.ShowStats(t));
            statBtn.Height = 34; statBtn.FontSize = 13; statBtn.Margin = new Thickness(0, 0, 6, 0);
            var editBtn = Ui.Btn("Edytuj", "SecondaryButton", (_, _) => _host.ShowEditor(t));
            editBtn.Height = 34; editBtn.FontSize = 13; editBtn.Margin = new Thickness(0, 0, 6, 0);
            var dupBtn = Ui.Btn("Duplikuj", "SecondaryButton", (_, _) => DuplicateTest(t));
            dupBtn.Height = 34; dupBtn.FontSize = 13; dupBtn.Margin = new Thickness(0, 0, 6, 0);
            var delBtn = Ui.Btn("Usuń", "SecondaryButton", (_, _) => DeleteTest(t));
            delBtn.Height = 34; delBtn.FontSize = 13;
            actions.Children.Add(statBtn); actions.Children.Add(editBtn); actions.Children.Add(dupBtn); actions.Children.Add(delBtn);

            table.Children.Add(MakeRowCells(false,
                nameCell,
                new TextBlock { Text = t.Subject, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Ui.Br("Muted"), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") },
                Ui.T(t.Questions.Count.ToString(), 14),
                Ui.T(t.TimeLimit + " min", 14),
                groupsCell,
                Ui.Badge(attempts.ToString(), "blue"),
                actions));
        }

        Content = new Border { Style = Ui.St("Card"), Child = table, Padding = new Thickness(0, 6, 0, 6), VerticalAlignment = VerticalAlignment.Top };
    }

    private void ToggleActive(Test t)
    {
        if (!Safe.Run(() => DbRepository.SetTestActive(t.Id, !t.IsActive), "zmienić widoczności testu")) return;
        _host.RefreshNav();
        _host.ShowTeacherTests();
    }

    private void DuplicateTest(Test t)
    {
        // Kopia testu jako nowy test (Id = 0) należący do bieżącego nauczyciela, bez podejść.
        var copy = new Test
        {
            Title         = t.Title + " (kopia)",
            Subject       = t.Subject,
            TimeLimit     = t.TimeLimit,
            PassThreshold = t.PassThreshold,
            MaxAttempts   = t.MaxAttempts,
            ColorHex      = t.ColorHex,
            GroupIds      = new List<int>(t.GroupIds),
            Questions     = t.Questions.Select(q => new Question
            {
                Type    = q.Type,
                Text    = q.Text,
                Points  = q.Points,
                Options = new List<string>(q.Options),
                Correct = new List<int>(q.Correct),
                Sample  = q.Sample
            }).ToList()
        };
        if (!Safe.Run(() => DbRepository.SaveTest(copy, _host.CurrentUser.Id), "zduplikować testu")) return;
        _host.RefreshNav();
        _host.ShowTeacherTests();
    }

    private void DeleteTest(Test t)
    {
        var r = MessageBox.Show(
            $"Usunąć test „{t.Title}\"?\n\nWszystkie podejścia i wyniki uczniów do tego testu zostaną trwale usunięte. Tej operacji nie można cofnąć.",
            "Usuń test", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (r != MessageBoxResult.Yes) return;
        if (!Safe.Run(() => DbRepository.DeleteTest(t.Id), "usunąć testu")) return;
        _host.RefreshNav();
        _host.ShowTeacherTests();
    }

    private static readonly GridLength[] Widths =
    {
        new(2, GridUnitType.Star), new(1.2, GridUnitType.Star), new(70), new(80),
        new(1.3, GridUnitType.Star), new(90), new(330)
    };

    private Grid MakeRow(bool header, params string[] cells)
    {
        var elems = cells.Select(c => header
            ? (UIElement)new TextBlock { Text = c, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Muted2"), VerticalAlignment = VerticalAlignment.Center, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") }
            : Ui.T(c, 14)).ToArray();
        return MakeRowCells(header, elems);
    }

    private Grid MakeRowCells(bool header, params UIElement[] cells)
    {
        var grid = new Grid { Margin = new Thickness(16, header ? 14 : 13, 16, header ? 14 : 13) };
        for (int i = 0; i < Widths.Length; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = Widths[i] });
        for (int i = 0; i < cells.Length && i < Widths.Length; i++)
        {
            var el = cells[i];
            if (el is FrameworkElement fe) fe.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(el, i);
            grid.Children.Add(el);
        }
        var border = new Border { Child = grid, BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 0, 0, 1) };
        var hostGrid = new Grid();
        hostGrid.Children.Add(border);
        return hostGrid;
    }
}
