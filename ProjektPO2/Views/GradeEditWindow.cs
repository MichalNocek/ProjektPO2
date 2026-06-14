using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

// Okno ręcznej edycji oceny pojedynczego podejścia.
// Pozwala nauczycielowi przyznać punkty każdemu pytaniu — także zamkniętym
// (nadpisanie automatycznej oceny). Wynik jest przeliczany przy zapisie.
public class GradeEditWindow : Window
{
    private readonly Test _test;
    private readonly Result _result;
    private readonly Dictionary<int, int> _pts = new();           // questionId -> przyznane punkty
    private TextBlock _total = null!;
    private Button _save = null!;

    public GradeEditWindow(Test test, Result result, User student)
    {
        _test = test; _result = result;
        _result.Manual ??= new();

        Title  = $"Edytuj ocenę — {student.Name}";
        Width  = 640; Height = 720;
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
        header.Children.Add(Ui.T($"Edytuj ocenę — {student.Name}", 18, FontWeights.Bold));
        header.Children.Add(Ui.T($"{_test.Title} · ręczne przyznawanie punktów", 13, null, "Muted"));
        header.Children.Add(new Border { Height = 1, Background = Ui.Br("Border"), Margin = new Thickness(0, 16, 0, 0) });
        Grid.SetRow(header, 0); root.Children.Add(header);

        // lista pytań
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(24, 0, 24, 0) };
        var list = new StackPanel();
        foreach (var q in _test.Questions) list.Children.Add(QuestionRow(q));
        scroll.Content = list; Grid.SetRow(scroll, 1); root.Children.Add(scroll);

        // stopka
        var footer = new Border
        {
            BorderBrush = Ui.Br("Border"), BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(24, 14, 24, 14), Background = Ui.Br("Surface")
        };
        var fr = new Grid();
        fr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _total = Ui.T("", 14, FontWeights.Bold); _total.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(_total, 0);
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = Ui.Btn("Anuluj", "SecondaryButton", (_, _) => { DialogResult = false; Close(); });
        cancel.Margin = new Thickness(0, 0, 12, 0);
        _save = Ui.Btn("✓  Zapisz ocenę", "PrimaryButton", (_, _) => Save());
        btnRow.Children.Add(cancel); btnRow.Children.Add(_save); Grid.SetColumn(btnRow, 1);
        fr.Children.Add(_total); fr.Children.Add(btnRow);
        footer.Child = fr; Grid.SetRow(footer, 2); root.Children.Add(footer);

        Content = root;
        UpdateTotal();
    }

    private FrameworkElement QuestionRow(Question q)
    {
        bool answered = _result.Answers.ContainsKey(q.Id);
        _result.Answers.TryGetValue(q.Id, out var ans);
        var g = GradingService.Grade(q, ans, _result.Manual);
        _pts[q.Id] = g.pts; // bieżąca ocena (nadpisana lub automatyczna; otwarte nieocenione = 0)

        var sp = new StackPanel();
        string typeLabel = q.Type switch
        {
            QuestionType.Single => "Jedna odpowiedź",
            QuestionType.Multi  => "Wiele odpowiedzi",
            _                   => "Odpowiedź otwarta"
        };
        sp.Children.Add(Ui.T($"{typeLabel.ToUpper()} · MAKS {q.Points} PKT", 11.5, FontWeights.Bold, "Primary"));
        sp.Children.Add(new TextBlock
        {
            Text = q.Text, FontSize = 14.5, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap,
            Foreground = Ui.Br("Text"), Margin = new Thickness(0, 4, 0, 8),
            FontFamily = (FontFamily)Application.Current.FindResource("UiFont")
        });

        sp.Children.Add(AnswerSummary(q, ans, answered));

        if (answered)
            sp.Children.Add(PointButtons(q));
        else
            sp.Children.Add(Ui.T("Brak odpowiedzi — punktów nie można przyznać.", 12.5, null, "Muted"));

        return new Border { Style = Ui.St("Card"), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 12), Child = sp };
    }

    private FrameworkElement AnswerSummary(Question q, object? ans, bool answered)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        if (!answered)
        {
            sp.Children.Add(Ui.T("— brak odpowiedzi —", 13, null, "Muted"));
            return sp;
        }

        if (q.Type == QuestionType.Open)
        {
            string txt = GradingService.AsText(ans);
            sp.Children.Add(Ui.T("Odpowiedź ucznia:", 12, FontWeights.Bold, "Muted"));
            sp.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(txt) ? "—" : txt, FontSize = 13.5, TextWrapping = TextWrapping.Wrap,
                Foreground = Ui.Br("Text"), Margin = new Thickness(0, 2, 0, 0),
                FontFamily = (FontFamily)Application.Current.FindResource("UiFont")
            });
            if (!string.IsNullOrEmpty(q.Sample))
                sp.Children.Add(Ui.T("Wzorcowa: " + q.Sample, 12.5, null, "Muted", true));
        }
        else
        {
            var chosen = GradingService.AsIndices(ans);
            for (int i = 0; i < q.Options.Count; i++)
            {
                bool correct = q.Correct.Contains(i), picked = chosen.Contains(i);
                string tag  = (correct ? "  ✓ poprawna" : "") + (picked ? "  · wybrana" : "");
                string tone = correct ? "Success" : picked ? "Danger" : "Muted";
                sp.Children.Add(Ui.T($"{(char)('A' + i)}. {q.Options[i]}{tag}", 13,
                    (correct || picked) ? FontWeights.SemiBold : FontWeights.Normal, tone, true));
            }
        }
        return sp;
    }

    private FrameworkElement PointButtons(Question q)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        var label = Ui.T($"Przyznaj punkty (0–{q.Points}):", 12.5, FontWeights.Bold, "Text2");
        label.VerticalAlignment = VerticalAlignment.Center; label.Margin = new Thickness(0, 0, 8, 0);
        row.Children.Add(label);

        var box = new TextBox
        {
            Width = 70, Height = 40, FontSize = 13, Text = _pts[q.Id].ToString(),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        box.TextChanged += (_, _) =>
        {
            // Bez cichego ucinania — wartość poza zakresem oznaczamy i blokujemy zapis.
            _pts[q.Id] = int.TryParse(box.Text, out var v) ? v : -1;
            bool valid = _pts[q.Id] >= 0 && _pts[q.Id] <= q.Points;
            box.BorderBrush = (valid || box.Text.Length == 0) ? Ui.Br("Border2") : Ui.Br("Danger");
            UpdateTotal();
        };
        row.Children.Add(box);

        var maxLbl = Ui.T($"/ {q.Points} pkt", 12.5, FontWeights.SemiBold, "Muted");
        maxLbl.VerticalAlignment = VerticalAlignment.Center; maxLbl.Margin = new Thickness(8, 0, 0, 0);
        row.Children.Add(maxLbl);
        return row;
    }

    private void UpdateTotal()
    {
        int score = 0, max = 0; bool ok = true;
        foreach (var q in _test.Questions)
        {
            max += q.Points;
            if (!_result.Answers.ContainsKey(q.Id)) continue;
            int p = _pts[q.Id];
            if (p < 0 || p > q.Points) ok = false;
            else score += p;
        }
        _save.IsEnabled = ok;
        _total.Text = ok ? $"Razem: {score} / {max} pkt" : "Punkty poza zakresem — popraw, aby zapisać";
        _total.Foreground = ok ? Ui.Br("Text") : Ui.Br("Danger");
    }

    private void Save()
    {
        _result.Manual ??= new();
        // Zapisz ręcznie przyznane punkty dla pytań, na które uczeń odpowiedział.
        foreach (var q in _test.Questions)
            if (_result.Answers.ContainsKey(q.Id))
                _result.Manual[q.Id] = _pts[q.Id];

        GradingService.Recompute(_test, _result);
        if (!Safe.Run(() => DbRepository.UpdateGrading(_result), "zapisać oceny")) return;
        DialogResult = true;
        Close();
    }
}
