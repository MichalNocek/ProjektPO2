using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProjektPO2.Views;

public static class Ui
{
    public static Brush Br(string key) => (Brush)Application.Current.FindResource(key);
    public static Style St(string key) => (Style)Application.Current.FindResource(key);
    public static SolidColorBrush Hex(string hex) => (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
    public static SolidColorBrush HexA(string hex, byte a)
    {
        var c = ((SolidColorBrush)new BrushConverter().ConvertFrom(hex)!).Color;
        return new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
    }

    public static TextBlock T(string text, double size = 14, FontWeight? w = null, string fg = "Text", bool wrap = false)
        => new()
        {
            Text = text, FontSize = size, FontWeight = w ?? FontWeights.Normal,
            Foreground = Br(fg), TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            FontFamily = (FontFamily)Application.Current.FindResource("UiFont")
        };

    public static Border Card(UIElement child, double pad = 22)
        => new()
        {
            Style = St("Card"), Child = child,
            Padding = new Thickness(pad)
        };

    public static Border Badge(string text, string tone = "gray", bool dot = false)
    {
        (string bg, string fg) = tone switch
        {
            "green"  => ("SuccessSoft", "Success"),
            "red"    => ("DangerSoft",  "Danger"),
            "blue"   => ("PrimarySoft", "Primary"),
            "amber"  => ("WarningSoft", "Warning"),
            "violet" => ("VioletSoft",  "Violet"),
            _        => ("Surface3",    "Muted"),
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (dot)
            sp.Children.Add(new Border
            {
                Width = 7, Height = 7, CornerRadius = new CornerRadius(4),
                Background = Br(fg), Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        sp.Children.Add(new TextBlock
        {
            Text = text, FontSize = 12.5, FontWeight = FontWeights.Bold,
            Foreground = Br(fg), VerticalAlignment = VerticalAlignment.Center
        });
        return new Border
        {
            Background = Br(bg), CornerRadius = new CornerRadius(100),
            Padding = new Thickness(11, 3, 11, 3), Child = sp,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center
        };
    }

    public static Border IconChip(string emoji, string bgHex, double size = 44, double corner = 12)
        => new()
        {
            Width = size, Height = size, CornerRadius = new CornerRadius(corner),
            Background = HexA(bgHex, 0x1A),
            Child = new TextBlock
            {
                Text = emoji, FontSize = size * 0.42,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

    public static Button Btn(string content, string style, RoutedEventHandler? click = null)
    {
        var b = new Button { Content = content, Style = St(style) };
        if (click != null) b.Click += click;
        return b;
    }

    public static StackPanel HStack(double gap = 0, params UIElement[] children)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < children.Length; i++)
        {
            if (i > 0 && gap > 0 && children[i] is FrameworkElement fe)
                fe.Margin = new Thickness(gap, 0, 0, 0);
            sp.Children.Add(children[i]);
        }
        return sp;
    }
}
