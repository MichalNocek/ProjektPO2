using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjektPO2.Services;
using ProjektPO2.Views;

namespace ProjektPO2;

public partial class MainWindow : Window
{
    public User CurrentUser { get; }
    private readonly Dictionary<string, Button> _navButtons = new();
    private string _activeKey = "";

    public MainWindow(User user)
    {
        InitializeComponent();
        CurrentUser = user;

        UserInitials.Text = user.Initials;
        UserAvatar.Background = AvatarColor(user.Name);
        UserName.Text = user.Name;
        UserRole.Text = user.IsTeacher ? "Nauczyciel" : "Uczeń · " + user.GroupName;

        BuildNav();

        if (user.IsTeacher) ShowTeacherDashboard();
        else ShowStudentTests();
    }

    // ---------- NAWIGACJA ----------
    private void BuildNav()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();

        if (CurrentUser.IsTeacher)
        {
            AddSection("NAUCZYCIEL");
            AddNav("dashboard", "▦",  "Pulpit",      null,                                    ShowTeacherDashboard);
            AddNav("tests",     "📄", "Testy",        DbRepository.GetTestsForTeacher(CurrentUser.Id).Count, ShowTeacherTests);
            int pending = DbRepository.PendingSubmissions(CurrentUser.Id).Count;
            AddNav("review",    "✓",  "Ocenianie",   pending > 0 ? pending : (int?)null,       ShowGrading);
            AddNav("groups",    "🗂", "Grupy",        DbRepository.GetGroups().Count,            ShowGroups);
            AddNav("stats",     "📊", "Statystyki",   null,                                    () => ShowStats());
        }
        else
        {
            AddSection("UCZEŃ");
            var tests = DbRepository.GetTestsForStudent(CurrentUser);
            int done  = 0;
            foreach (var t in tests)
                if (DbRepository.GetBestResult(t.Id, CurrentUser.Id) != null) done++;
            AddNav("tests",   "📖", "Moje testy", tests.Count, ShowStudentTests);
            AddNav("results", "🏆", "Wyniki",      done,        ShowStudentResults);
        }
    }

    private void AddSection(string text)
    {
        NavPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 11, FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("Muted2"),
            Margin = new Thickness(12, 14, 0, 6)
        });
    }

    private void AddNav(string key, string emoji, string label, int? count, System.Action action)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var ico = new TextBlock { Text = emoji, FontSize = 16, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        Grid.SetColumn(ico, 0);
        var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(lbl, 1);
        grid.Children.Add(ico);
        grid.Children.Add(lbl);

        if (count.HasValue)
        {
            var badge = new Border
            {
                Background = (Brush)FindResource("Surface3"),
                CornerRadius = new CornerRadius(100), Padding = new Thickness(8, 1, 8, 1),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock
            {
                Text = count.Value.ToString(), FontSize = 12,
                FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("Muted")
            };
            Grid.SetColumn(badge, 2);
            grid.Children.Add(badge);
        }

        var btn = new Button { Style = (Style)FindResource("NavButton"), Content = grid };
        btn.Click += (_, _) => action();
        _navButtons[key] = btn;
        NavPanel.Children.Add(btn);
    }

    private void SetActive(string key)
    {
        _activeKey = key;
        foreach (var (k, b) in _navButtons)
        {
            bool active = k == key;
            b.Background = active ? (Brush)FindResource("PrimarySoft") : System.Windows.Media.Brushes.Transparent;
            b.Foreground  = active ? (Brush)FindResource("Primary")     : (Brush)FindResource("Muted");
        }
    }

    // ---------- USTAWIANIE STRONY ----------
    private void SetPage(string title, string sub, UserControl content, object? actions = null)
    {
        ShowChrome(true);
        PageTitle.Text = title;
        PageSub.Text   = sub;
        TopBarActions.Content = actions;
        ContentArea.Content   = content;
        ContentScroll.ScrollToTop();
    }

    private void ShowChrome(bool visible)
    {
        Sidebar.Visibility    = visible ? Visibility.Visible : Visibility.Collapsed;
        SidebarCol.Width      = visible ? new GridLength(256) : new GridLength(0);
        TopBar.Visibility     = visible ? Visibility.Visible : Visibility.Collapsed;
        ContentScroll.Padding = visible ? new Thickness(36, 8, 36, 36) : new Thickness(0);
        ContentScroll.VerticalScrollBarVisibility = visible ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    }

    private void ShowFullScreen(UserControl content)
    {
        ShowChrome(false);
        TopBarActions.Content = null;
        ContentArea.Content   = content;
    }

    // ---------- EKRANY UCZNIA ----------
    public void ShowStudentTests()
    {
        SetActive("tests");
        SetPage("Moje testy", "Testy przydzielone do Twojej grupy " + CurrentUser.GroupName,
            new StudentTestsPage(this, false));
    }

    public void ShowStudentResults()
    {
        SetActive("results");
        SetPage("Wyniki", "Twoje ukończone testy i oceny",
            new StudentTestsPage(this, true));
    }

    public void StartQuiz(Test test)   => ShowFullScreen(new QuizPage(this, test));
    public void ShowResult(Test test, Result result) => ShowFullScreen(new ResultPage(this, test, result));

    // ---------- EKRANY NAUCZYCIELA ----------
    public void ShowTeacherDashboard()
    {
        SetActive("dashboard");
        SetPage("Pulpit", "Przegląd Twoich testów i wyników uczniów", new TeacherDashboardPage(this));
    }

    public void ShowTeacherTests()
    {
        SetActive("tests");
        var btn = new Button { Content = "+  Nowy test", Style = (Style)FindResource("PrimaryButton") };
        btn.Click += (_, _) => ShowEditor(null);
        SetPage("Testy", "Twórz i edytuj testy dla swoich grup", new TeacherTestsPage(this), btn);
    }

    public void ShowEditor(Test? test)
    {
        SetActive("tests");
        SetPage(test == null ? "Nowy test" : "Edytuj test", "Skonfiguruj test i jego pytania",
            new TestEditorPage(this, test));
    }

    public void ShowGroups()
    {
        SetActive("groups");
        var btn = new Button { Content = "+  Nowa grupa", Style = (Style)FindResource("PrimaryButton") };
        btn.Click += (_, _) =>
        {
            var win = new AddGroupWindow { Owner = this };
            if (win.ShowDialog() == true) ShowGroups();
        };
        SetPage("Grupy", "Grupy uczniów i przypisane testy", new GroupsPage(this), btn);
    }

    public void ShowGrading()
    {
        BuildNav();
        SetActive("review");
        SetPage("Ocenianie", "Sprawdź odpowiedzi otwarte i przyznaj punkty", new GradingPage(this));
    }

    public void ShowStats(Test? selected = null)
    {
        SetActive("stats");
        SetPage("Statystyki", "Analiza wyników i trudności pytań", new StatsPage(this, selected));
    }

    // ---------- INNE ----------
    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        Close();
    }

    public void RefreshNav() => BuildNav();

    public static Brush AvatarColor(string seed)
    {
        string[] colors = { "#2563EB", "#7C3AED", "#0D9488", "#D97706", "#DB2777", "#0891B2", "#65A30D", "#DC2626" };
        int h = 0;
        foreach (char ch in seed) h = h * 31 + ch;
        int idx = System.Math.Abs(h) % colors.Length;
        return (Brush)new BrushConverter().ConvertFrom(colors[idx])!;
    }
}
