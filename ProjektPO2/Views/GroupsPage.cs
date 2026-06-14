using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class GroupsPage : UserControl
{
    private readonly MainWindow _host;

    public GroupsPage(MainWindow host)
    {
        _host   = host;
        Content = Build();
    }

    private UIElement Build()
    {
        var groups      = DbRepository.GetGroups();
        var allStudents = DbRepository.GetStudents();
        var students    = allStudents.ToDictionary(s => s.Id);
        var tests       = DbRepository.GetTestsForTeacher(_host.CurrentUser.Id);

        var wrap = new WrapPanel();
        foreach (var g in groups)
            wrap.Children.Add(GroupCard(g, students, allStudents, tests));
        return wrap;
    }

    private Border GroupCard(Group g, System.Collections.Generic.Dictionary<int, User> students,
                             System.Collections.Generic.List<User> allStudents,
                             System.Collections.Generic.List<Test> tests)
    {
        var sp = new StackPanel();

        // nagłówek karty
        var head = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headLeft = new StackPanel { Orientation = Orientation.Horizontal };
        headLeft.Children.Add(Ui.IconChip("🗂", "#7C3AED", 44));
        var ht = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        ht.Children.Add(Ui.T(g.Name, 17, FontWeights.Bold));
        ht.Children.Add(Ui.T(g.Description, 12.5, null, "Muted", true));
        headLeft.Children.Add(ht);
        Grid.SetColumn(headLeft, 0);

        var manageBtn = Ui.Btn("+ Zarządzaj", "SecondaryButton");
        manageBtn.Height   = 34;
        manageBtn.FontSize = 12.5;
        manageBtn.VerticalAlignment = VerticalAlignment.Center;
        manageBtn.Click += (_, _) =>
        {
            var win = new GroupMembersWindow(g, allStudents) { Owner = _host };
            if (win.ShowDialog() == true)
                _host.ShowGroups();
        };
        Grid.SetColumn(manageBtn, 1);

        head.Children.Add(headLeft);
        head.Children.Add(manageBtn);
        sp.Children.Add(head);

        sp.Children.Add(new Border { Height = 1, Background = Ui.Br("Border"), Margin = new Thickness(0, 0, 0, 16) });

        // lista uczniów
        sp.Children.Add(new TextBlock { Text = $"UCZNIOWIE ({g.StudentIds.Count})", FontSize = 12.5, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 0, 0, 10), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        if (g.StudentIds.Count == 0)
        {
            sp.Children.Add(Ui.T("Brak uczniów — kliknij „+ Zarządzaj\".", 13, null, "Muted"));
        }
        else
        {
            var chips = new WrapPanel();
            foreach (var sid in g.StudentIds)
            {
                if (!students.TryGetValue(sid, out var st)) continue;
                var chip = new Border { Background = Ui.Br("Surface2"), BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(100), Padding = new Thickness(8, 6, 12, 6), Margin = new Thickness(0, 0, 8, 8) };
                var row  = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(TeacherDashboardPage.Avatar(st.Name, 26));
                row.Children.Add(new TextBlock { Text = st.Name, FontSize = 13.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Foreground = Ui.Br("Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
                chip.Child = row;
                chips.Children.Add(chip);
            }
            sp.Children.Add(chips);
        }

        // przypisane testy
        var assigned = tests.Where(t => t.GroupIds.Contains(g.Id)).ToList();
        sp.Children.Add(new TextBlock { Text = $"PRZYPISANE TESTY ({assigned.Count})", FontSize = 12.5, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 14, 0, 10), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        if (assigned.Count == 0)
            sp.Children.Add(Ui.T("Brak przypisanych testów", 13, null, "Muted"));
        else
        {
            var tw = new WrapPanel();
            foreach (var t in assigned) { var b = Ui.Badge(t.Title, "blue", true); b.Margin = new Thickness(0, 0, 8, 8); tw.Children.Add(b); }
            sp.Children.Add(tw);
        }

        return new Border { Style = Ui.St("Card"), Padding = new Thickness(20), Child = sp, Width = 380, Margin = new Thickness(0, 0, 18, 18) };
    }
}
