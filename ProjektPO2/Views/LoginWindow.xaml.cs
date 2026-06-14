using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        BuildFeatures();
        SetMode("login");
    }

    private void BuildFeatures()
    {
        var items = new[]
        {
            ("✏️", "Edytor testów",  "Pytania ABCD, wielokrotnego wyboru i otwarte"),
            ("⏱️", "Testy z czasem", "Timer i automatyczne sprawdzanie odpowiedzi"),
            ("📊", "Statystyki",     "Zdawalność, najtrudniejsze pytania, wyniki uczniów"),
        };
        foreach (var (ico, title, desc) in items)
        {
            var row     = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            var iconBox = new Border
            {
                Width = 40, Height = 40, CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 0, 14, 0)
            };
            iconBox.Child = new TextBlock { Text = ico, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new TextBlock { Text = title, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 15 });
            texts.Children.Add(new TextBlock { Text = desc,  Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)), FontSize = 13 });
            row.Children.Add(iconBox);
            row.Children.Add(texts);
            Features.Children.Add(row);
        }
    }

    private void Mode_Click(object sender, RoutedEventArgs e)
        => SetMode((string)((Button)sender).Tag);

    private void SetMode(string mode)
    {
        LoginPanel.Visibility    = mode == "login"    ? Visibility.Visible : Visibility.Collapsed;
        RegisterPanel.Visibility = mode == "register" ? Visibility.Visible : Visibility.Collapsed;
        StyleTab(TabLogin,    mode == "login");
        StyleTab(TabRegister, mode == "register");
    }

    private void StyleTab(Button tab, bool active)
    {
        tab.Background = active ? (Brush)FindResource("Surface") : Brushes.Transparent;
        tab.Foreground = active ? (Brush)FindResource("Primary") : (Brush)FindResource("Muted");
        tab.Effect     = active ? (System.Windows.Media.Effects.Effect)FindResource("CardShadow") : null;
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginError.Visibility = Visibility.Collapsed;

        User? user = null;
        if (!Safe.Run(() => user = DbRepository.LoginByCredentials(LoginBox.Text.Trim(), PassBox.Password), "zalogować się"))
            return;
        if (user == null)
        {
            LoginError.Text       = "Nieprawidłowy login lub hasło.";
            LoginError.Visibility = Visibility.Visible;
            return;
        }
        new MainWindow(user).Show();
        Close();
    }

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        RegError.Visibility = Visibility.Collapsed;

        string firstName = RegFirstName.Text.Trim();
        string lastName  = RegLastName.Text.Trim();
        string login     = RegLogin.Text.Trim();
        string pass      = RegPass.Password;
        string passConf  = RegPassConfirm.Password;

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            { ShowRegError("Podaj imię i nazwisko."); return; }
        if (string.IsNullOrEmpty(login))
            { ShowRegError("Podaj login."); return; }
        if (pass.Length < 4)
            { ShowRegError("Hasło musi mieć co najmniej 4 znaki."); return; }
        if (pass != passConf)
            { ShowRegError("Hasła nie są zgodne."); return; }
        bool exists = false;
        if (!Safe.Run(() => exists = DbRepository.UsernameExists(login), "sprawdzić dostępności loginu"))
            return;
        if (exists)
            { ShowRegError("Ten login jest już zajęty."); return; }

        bool asTeacher = RegRoleTeacher.IsChecked == true;
        User? user = null;
        if (!Safe.Run(() => user = asTeacher
                ? DbRepository.RegisterTeacher(firstName, lastName, login, pass)
                : DbRepository.RegisterStudent(firstName, lastName, login, pass), "utworzyć konta"))
            return;
        new MainWindow(user!).Show();
        Close();
    }

    private void ShowRegError(string msg)
    {
        RegError.Text       = msg;
        RegError.Visibility = Visibility.Visible;
    }
}
