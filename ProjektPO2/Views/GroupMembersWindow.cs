using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class GroupMembersWindow : Window
{
    private readonly Group _group;
    private readonly List<(User student, CheckBox cb)> _rows = new();

    public GroupMembersWindow(Group group, List<User> allStudents)
    {
        _group = group;
        Title  = $"Zarządzaj grupą: {group.Name}";
        Width  = 500; Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode  = ResizeMode.NoResize;
        Background  = (Brush)Application.Current.FindResource("Bg");
        FontFamily  = (FontFamily)Application.Current.FindResource("UiFont");

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // ---- nagłówek ----
        var header = new StackPanel { Margin = new Thickness(24, 22, 24, 16) };
        header.Children.Add(Ui.T($"Zarządzaj grupą: {group.Name}", 18, FontWeights.Bold));
        header.Children.Add(Ui.T("Zaznacz uczniów należących do tej grupy.", 13, null, "Muted"));
        header.Children.Add(new Border { Height = 1, Background = (Brush)Application.Current.FindResource("Border"), Margin = new Thickness(0, 16, 0, 0) });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // ---- lista uczniów ----
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(24, 0, 24, 0) };
        var list   = new StackPanel();

        foreach (var student in allStudents.OrderBy(s => s.Name))
        {
            bool inGroup = student.GroupNames.Contains(group.Name);

            var cb = new CheckBox { IsChecked = inGroup, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };

            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(cb, 0);

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            nameRow.Children.Add(TeacherDashboardPage.Avatar(student.Name, 30));
            nameRow.Children.Add(new TextBlock
            {
                Text = student.Name, FontSize = 14, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0),
                Foreground = (Brush)Application.Current.FindResource("Text"),
                FontFamily = (FontFamily)Application.Current.FindResource("UiFont")
            });
            Grid.SetColumn(nameRow, 1);

            rowGrid.Children.Add(cb);
            rowGrid.Children.Add(nameRow);

            if (student.GroupNames.Count > 0)
            {
                var badgePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                foreach (var grpName in student.GroupNames)
                {
                    string tone = grpName == group.Name ? "green" : "gray";
                    var badge = Ui.Badge(grpName, tone);
                    badge.Margin = new Thickness(0, 0, 4, 0);
                    badgePanel.Children.Add(badge);
                }
                Grid.SetColumn(badgePanel, 2);
                rowGrid.Children.Add(badgePanel);
            }

            var row = new Border
            {
                Background      = (Brush)Application.Current.FindResource("Surface"),
                BorderBrush     = (Brush)Application.Current.FindResource("Border"),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(10),
                Padding         = new Thickness(14, 12, 14, 12),
                Margin          = new Thickness(0, 0, 0, 8),
                Child           = rowGrid,
                Cursor          = Cursors.Hand
            };
            row.MouseLeftButtonUp += (_, _) => cb.IsChecked = !cb.IsChecked;

            _rows.Add((student, cb));
            list.Children.Add(row);
        }

        scroll.Content = list;
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        // ---- stopka ----
        var footer = new Border
        {
            BorderBrush     = (Brush)Application.Current.FindResource("Border"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding         = new Thickness(24, 14, 24, 14),
            Background      = (Brush)Application.Current.FindResource("Surface")
        };
        var footerRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancelBtn = Ui.Btn("Anuluj", "SecondaryButton", (_, _) => { DialogResult = false; Close(); });
        cancelBtn.Margin = new Thickness(0, 0, 12, 0);
        var saveBtn = Ui.Btn("Zapisz  ✓", "PrimaryButton", (_, _) => Save());
        footerRow.Children.Add(cancelBtn);
        footerRow.Children.Add(saveBtn);
        footer.Child = footerRow;
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        Content = root;
    }

    private void Save()
    {
        bool ok = Safe.Run(() =>
        {
            foreach (var (student, cb) in _rows)
            {
                bool shouldBeIn = cb.IsChecked == true;
                bool isIn       = student.GroupNames.Contains(_group.Name);
                if (shouldBeIn && !isIn)
                    DbRepository.AddStudentToGroup(student.Id, _group.Id);
                else if (!shouldBeIn && isIn)
                    DbRepository.RemoveStudentFromGroup(student.Id, _group.Id);
            }
        }, "zapisać składu grupy");
        if (!ok) return;
        DialogResult = true;
        Close();
    }
}
