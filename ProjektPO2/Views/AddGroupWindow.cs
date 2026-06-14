using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class AddGroupWindow : Window
{
    private readonly TextBox _nameBox;
    private readonly TextBox _descBox;
    private readonly TextBlock _errorText;

    public AddGroupWindow()
    {
        Title  = "Nowa grupa";
        Width  = 420; Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode  = ResizeMode.NoResize;
        Background  = (Brush)Application.Current.FindResource("Bg");
        FontFamily  = (FontFamily)Application.Current.FindResource("UiFont");

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // nagłówek
        var header = new StackPanel { Margin = new Thickness(24, 22, 24, 16) };
        header.Children.Add(Ui.T("Dodaj nową grupę", 18, FontWeights.Bold));
        header.Children.Add(Ui.T("Podaj nazwę i opcjonalny opis grupy.", 13, null, "Muted"));
        header.Children.Add(new Border { Height = 1, Background = (Brush)Application.Current.FindResource("Border"), Margin = new Thickness(0, 16, 0, 0) });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // formularz
        var form = new StackPanel { Margin = new Thickness(24, 4, 24, 0) };

        form.Children.Add(Ui.T("Nazwa grupy", 13, FontWeights.SemiBold, "Text2"));
        _nameBox = new TextBox { Height = 44, Margin = new Thickness(0, 6, 0, 16) };
        form.Children.Add(_nameBox);

        form.Children.Add(Ui.T("Opis (opcjonalnie)", 13, FontWeights.SemiBold, "Text2"));
        _descBox = new TextBox { Height = 90, Margin = new Thickness(0, 6, 0, 0), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalContentAlignment = VerticalAlignment.Top };
        form.Children.Add(_descBox);

        _errorText = new TextBlock { Foreground = (Brush)Application.Current.FindResource("Danger"), FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), Visibility = Visibility.Collapsed, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        form.Children.Add(_errorText);

        Grid.SetRow(form, 1);
        root.Children.Add(form);

        // stopka
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
        var saveBtn = Ui.Btn("Utwórz grupę", "PrimaryButton", (_, _) => Save());
        footerRow.Children.Add(cancelBtn);
        footerRow.Children.Add(saveBtn);
        footer.Child = footerRow;
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        Content = root;
        Loaded += (_, _) => _nameBox.Focus();
    }

    private void Save()
    {
        var name = _nameBox.Text.Trim();
        var desc = _descBox.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            _errorText.Text       = "Podaj nazwę grupy.";
            _errorText.Visibility = Visibility.Visible;
            return;
        }
        bool exists = false;
        if (!Safe.Run(() => exists = DbRepository.GroupExists(name), "sprawdzić nazwy grupy"))
            return;
        if (exists)
        {
            _errorText.Text       = "Grupa o tej nazwie już istnieje.";
            _errorText.Visibility = Visibility.Visible;
            return;
        }

        if (!Safe.Run(() => DbRepository.CreateGroup(name, desc), "utworzyć grupy"))
            return;
        DialogResult = true;
        Close();
    }
}
