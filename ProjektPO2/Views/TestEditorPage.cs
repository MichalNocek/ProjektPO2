using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ProjektPO2.Services;

namespace ProjektPO2.Views;

public class TestEditorPage : UserControl
{
    private readonly MainWindow _host;
    private readonly Test _draft;
    private readonly bool _locked; // test ma już podejścia — pytań nie można zmieniać
    private int _sel;

    private StackPanel _editorHost = null!;
    private StackPanel _listHost   = null!;
    private TextBlock  _pointsTotal = null!;

    private static readonly (string name, string color)[] Subjects =
    {
        ("Matematyka",    "#2563EB"), ("Historia",    "#D97706"), ("Biologia",  "#15A34A"),
        ("Informatyka",   "#7C3AED"), ("Język polski","#DB2777"), ("Fizyka",    "#0D9488"),
        ("Geografia",     "#0891B2"), ("Chemia",      "#DC2626"),
    };

    public TestEditorPage(MainWindow host, Test? test)
    {
        _host  = host;
        _draft = test != null ? Clone(test) : new Test
        {
            Title = "", Subject = "Matematyka", TimeLimit = 20, PassThreshold = 50,
            MaxAttempts = 1, ColorHex = "#2563EB",
            Questions = new List<Question> { NewQuestion() }
        };
        _locked = test != null && DbRepository.TestHasResults(test.Id);
        Build();
    }

    private static Test Clone(Test t) => new()
    {
        Id = t.Id, Title = t.Title, Subject = t.Subject, TimeLimit = t.TimeLimit,
        PassThreshold = t.PassThreshold, MaxAttempts = t.MaxAttempts, IsActive = t.IsActive,
        ColorHex = t.ColorHex, GroupIds = new List<int>(t.GroupIds),
        Questions = t.Questions.Select(q => new Question
        {
            Id = q.Id, Type = q.Type, Text = q.Text, Points = q.Points,
            Options = new List<string>(q.Options), Correct = new List<int>(q.Correct), Sample = q.Sample
        }).ToList()
    };

    private static Question NewQuestion() => new()
    {
        Type = QuestionType.Single, Text = "", Points = 1,
        Options = new List<string> { "", "", "", "" }, Correct = new List<int>(), Sample = ""
    };

    private void Build()
    {
        var root = new StackPanel();

        var bar = new Grid { Margin = new Thickness(0, 0, 0, 20) };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var cancel = Ui.Btn("‹  Anuluj", "SecondaryButton", (_, _) => _host.ShowTeacherTests());
        Grid.SetColumn(cancel, 0);
        var save = Ui.Btn("✓  Zapisz test", "PrimaryButton", (_, _) => Save());
        Grid.SetColumn(save, 2);
        bar.Children.Add(cancel); bar.Children.Add(save);
        root.Children.Add(bar);
        root.Children.Add(SettingsCard());

        if (_locked) root.Children.Add(LockBanner());

        var grid = new Grid { Margin = new Thickness(0, 22, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        _editorHost = new StackPanel(); Grid.SetColumn(_editorHost, 0); _editorHost.Margin = new Thickness(0, 0, 22, 0);
        var listCard = ListCard(); Grid.SetColumn(listCard, 1);
        grid.Children.Add(_editorHost); grid.Children.Add(listCard);
        // Pytań nie można zmieniać, gdy uczniowie już rozwiązali test (ochrona kluczy obcych).
        grid.IsEnabled = !_locked;
        if (_locked) grid.Opacity = 0.55;
        root.Children.Add(grid);

        RenderEditor();
        RenderList();
        Content = root;
    }

    private Border LockBanner()
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = "🔒", FontSize = 22, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) });
        var t = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        t.Children.Add(new TextBlock { Text = "Test ma już rozwiązania — pytań nie można zmieniać", FontWeight = FontWeights.Bold, FontSize = 14.5, Foreground = Ui.Br("Warning"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        t.Children.Add(new TextBlock { Text = "Możesz edytować ustawienia (nazwę, czas, próg, liczbę podejść) i przypisane grupy. Treść pytań pozostaje zablokowana, aby nie naruszyć już oddanych odpowiedzi.", FontSize = 13, Foreground = Ui.Br("Text2"), TextWrapping = TextWrapping.Wrap, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
        sp.Children.Add(t);
        return new Border { Background = Ui.Br("WarningSoft"), BorderBrush = Ui.Br("Warning"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16), Padding = new Thickness(20), Margin = new Thickness(0, 16, 0, 0), Child = sp };
    }

    private Border SettingsCard()
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = "USTAWIENIA TESTU", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Ui.Br("Muted"), Margin = new Thickness(0, 0, 0, 16), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });

        var r1 = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
        r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        r1.Children.Add(Field("Nazwa testu", MakeBox(_draft.Title, s => _draft.Title = s), 0, 0, 16));
        // Edytowalny ComboBox: można wybrać z listy ALBO wpisać własny przedmiot z ręki.
        var subjCombo = new ComboBox { Height = 44, IsEditable = true };
        subjCombo.ItemsSource = Subjects.Select(x => x.name).ToList();
        subjCombo.Text = _draft.Subject;

        void SyncSubject()
        {
            _draft.Subject = (subjCombo.Text ?? "").Trim();
            // Jeśli wpisany przedmiot pokrywa się ze znanym z palety — dobierz jego kolor.
            int mi = System.Array.FindIndex(Subjects, x => string.Equals(x.name, _draft.Subject, System.StringComparison.OrdinalIgnoreCase));
            if (mi >= 0) _draft.ColorHex = Subjects[mi].color;
        }
        subjCombo.SelectionChanged += (_, _) =>
        {
            if (subjCombo.SelectedItem != null) subjCombo.Text = (string)subjCombo.SelectedItem;
            SyncSubject();
        };
        subjCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => SyncSubject()));
        r1.Children.Add(Field("Przedmiot", subjCombo, 0, 1, 0));
        sp.Children.Add(r1);

        var r2 = new Grid();
        r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
        r2.Children.Add(Field("Limit czasu (min)",   MakeNumBox(_draft.TimeLimit,     v => _draft.TimeLimit     = v, 1, 600), 0, 0, 16));
        r2.Children.Add(Field("Próg zaliczenia (%)", MakeNumBox(_draft.PassThreshold, v => _draft.PassThreshold = v, 0, 100), 0, 1, 16));
        r2.Children.Add(Field("Liczba podejść",      MakeNumBox(_draft.MaxAttempts,   v => _draft.MaxAttempts   = v, 1, 20),  0, 2, 16));

        var groupsPanel = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var g in DbRepository.GetGroups())
            groupsPanel.Children.Add(GroupToggle(g));
        r2.Children.Add(Field("Przypisane grupy", groupsPanel, 0, 3, 0));
        sp.Children.Add(r2);

        // Widoczność testu dla uczniów (aktywny/nieaktywny).
        var activeRow = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        activeRow.Children.Add(new TextBlock { Text = "Widoczność", Style = Ui.St("Label") });
        var activeToggle = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 9, 14, 9), Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Left };
        var activeText = new TextBlock { FontSize = 13.5, FontWeight = FontWeights.Bold, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        activeToggle.Child = activeText;
        void PaintActive()
        {
            bool on = _draft.IsActive;
            activeToggle.Background = Ui.Br(on ? "SuccessSoft" : "Surface3");
            activeText.Foreground   = Ui.Br(on ? "Success" : "Muted");
            activeText.Text          = on ? "● Aktywny — widoczny dla uczniów" : "○ Nieaktywny — ukryty (szkic)";
        }
        PaintActive();
        activeToggle.MouseLeftButtonUp += (_, _) => { _draft.IsActive = !_draft.IsActive; PaintActive(); };
        activeRow.Children.Add(activeToggle);
        sp.Children.Add(activeRow);

        return Ui.Card(sp, 22);
    }

    private FrameworkElement GroupToggle(Group g)
    {
        var border = new Border { CornerRadius = new CornerRadius(100), Padding = new Thickness(11, 5, 11, 5), Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
        var txt = new TextBlock { FontSize = 12.5, FontWeight = FontWeights.Bold, FontFamily = (FontFamily)Application.Current.FindResource("UiFont") };
        border.Child = txt;
        void Paint()
        {
            bool on = _draft.GroupIds.Contains(g.Id);
            border.Background = Ui.Br(on ? "PrimarySoft" : "Surface3");
            txt.Foreground    = Ui.Br(on ? "Primary"     : "Muted");
            txt.Text          = (on ? "✓ " : "") + g.Name;
        }
        Paint();
        border.MouseLeftButtonUp += (_, _) =>
        {
            if (_draft.GroupIds.Contains(g.Id)) _draft.GroupIds.Remove(g.Id);
            else _draft.GroupIds.Add(g.Id);
            Paint();
        };
        return border;
    }

    private FrameworkElement Field(string label, UIElement input, int row, int col, double rightMargin)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, rightMargin, 0) };
        sp.Children.Add(new TextBlock { Text = label, Style = Ui.St("Label") });
        sp.Children.Add(input);
        Grid.SetColumn(sp, col); Grid.SetRow(sp, row);
        return sp;
    }

    private TextBox MakeBox(string val, System.Action<string> onChange)
    {
        var tb = new TextBox { Text = val, Height = 44 };
        tb.TextChanged += (_, _) => onChange(tb.Text);
        return tb;
    }

    private TextBox MakeNumBox(int val, System.Action<int> onChange, int min, int max)
    {
        var tb = new TextBox { Text = val.ToString(), Height = 44 };
        // Wartość zapisujemy zawsze w dozwolonym zakresie (clamp).
        tb.TextChanged += (_, _) => { if (int.TryParse(tb.Text, out var v)) onChange(System.Math.Clamp(v, min, max)); };
        // Po opuszczeniu pola wyrównaj wyświetlaną wartość (puste/ujemne/za duże → do zakresu).
        tb.LostFocus += (_, _) =>
        {
            int v = int.TryParse(tb.Text, out var p) ? System.Math.Clamp(p, min, max) : min;
            if (tb.Text != v.ToString()) tb.Text = v.ToString();
        };
        return tb;
    }

    private Border ListCard()
    {
        var sp = new StackPanel();
        var head = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var h = Ui.T("Pytania", 14, FontWeights.Bold); Grid.SetColumn(h, 0);
        _pointsTotal = Ui.T("", 12.5, FontWeights.SemiBold, "Muted"); _pointsTotal.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(_pointsTotal, 1);
        head.Children.Add(h); head.Children.Add(_pointsTotal);
        sp.Children.Add(head);

        _listHost = new StackPanel();
        sp.Children.Add(_listHost);

        var add = Ui.Btn("+  Dodaj pytanie", "SecondaryButton", (_, _) =>
        {
            _draft.Questions.Add(NewQuestion());
            _sel = _draft.Questions.Count - 1;
            RenderEditor(); RenderList();
        });
        add.Margin = new Thickness(0, 12, 0, 0);
        sp.Children.Add(add);

        return new Border { Style = Ui.St("Card"), Child = sp, Padding = new Thickness(22), VerticalAlignment = VerticalAlignment.Top };
    }

    private void RenderList()
    {
        _listHost.Children.Clear();
        _pointsTotal.Text = _draft.Questions.Sum(q => q.Points) + " pkt";
        for (int i = 0; i < _draft.Questions.Count; i++)
        {
            int ci = i; var q = _draft.Questions[i]; bool active = i == _sel;
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var num = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(8), Background = Ui.Br(active ? "Primary" : "Surface3"), Margin = new Thickness(0, 0, 11, 0), VerticalAlignment = VerticalAlignment.Top };
            num.Child = new TextBlock { Text = (i + 1).ToString(), FontWeight = FontWeights.Bold, FontSize = 13, Foreground = active ? Brushes.White : Ui.Br("Muted"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(num, 0);
            var tx = new StackPanel();
            tx.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(q.Text) ? "Puste pytanie…" : q.Text, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = Ui.Br(string.IsNullOrWhiteSpace(q.Text) ? "Muted2" : "Text"), FontFamily = (FontFamily)Application.Current.FindResource("UiFont") });
            tx.Children.Add(Ui.T($"{TypeLabel(q.Type)} · {q.Points} pkt", 11.5, null, "Muted"));
            Grid.SetColumn(tx, 1);
            grid.Children.Add(num); grid.Children.Add(tx);
            var item = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1.5), BorderBrush = Ui.Br(active ? "Primary" : "Border"), Background = Ui.Br(active ? "PrimarySoft" : "Surface"), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 8), Child = grid, Cursor = Cursors.Hand };
            item.MouseLeftButtonUp += (_, _) => { _sel = ci; RenderEditor(); RenderList(); };
            _listHost.Children.Add(item);
        }
    }

    private static string TypeLabel(QuestionType t) => t switch { QuestionType.Single => "Jedna odp.", QuestionType.Multi => "Wiele odp.", _ => "Otwarte" };

    private void RenderEditor()
    {
        _editorHost.Children.Clear();
        if (_draft.Questions.Count == 0) return;
        if (_sel >= _draft.Questions.Count) _sel = _draft.Questions.Count - 1;
        var q = _draft.Questions[_sel];
        var sp = new StackPanel();

        var head = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var nb = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(8), Background = Ui.Br("Primary"), Margin = new Thickness(0, 0, 10, 0) };
        nb.Child = new TextBlock { Text = (_sel + 1).ToString(), Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(nb); title.Children.Add(Ui.T($"Pytanie {_sel + 1}", 15, FontWeights.Bold));
        Grid.SetColumn(title, 0);

        var headRight = new StackPanel { Orientation = Orientation.Horizontal };
        var typeCombo = new ComboBox { Width = 165, Height = 36, VerticalContentAlignment = VerticalAlignment.Center };
        typeCombo.Items.Add("Jedna odpowiedź"); typeCombo.Items.Add("Wiele odpowiedzi"); typeCombo.Items.Add("Otwarte");
        typeCombo.SelectedIndex = (int)q.Type;
        typeCombo.SelectionChanged += (_, _) =>
        {
            var nt = (QuestionType)typeCombo.SelectedIndex;
            if (nt != q.Type) { q.Type = nt; q.Correct.Clear(); RenderEditor(); RenderList(); }
        };
        var del = Ui.Btn("🗑", "SecondaryButton", (_, _) =>
        {
            if (_draft.Questions.Count <= 1) { MessageBox.Show("Test musi mieć co najmniej jedno pytanie."); return; }
            _draft.Questions.RemoveAt(_sel);
            if (_sel > 0) _sel--;
            RenderEditor(); RenderList();
        });
        del.Width = 38; del.Height = 36; del.Margin = new Thickness(8, 0, 0, 0);
        headRight.Children.Add(typeCombo); headRight.Children.Add(del);
        Grid.SetColumn(headRight, 1);
        head.Children.Add(title); head.Children.Add(headRight);
        sp.Children.Add(head);

        sp.Children.Add(new TextBlock { Text = "Treść pytania", Style = Ui.St("Label") });
        var textBox = new TextBox { Text = q.Text, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 70, Margin = new Thickness(0, 0, 0, 16) };
        textBox.TextChanged += (_, _) => { q.Text = textBox.Text; };
        sp.Children.Add(textBox);

        if (q.Type == QuestionType.Open)
        {
            sp.Children.Add(new TextBlock { Text = "Wzorcowa odpowiedź (do automatycznego sprawdzania)", Style = Ui.St("Label") });
            var sample = new TextBox { Text = q.Sample, Height = 44, Margin = new Thickness(0, 0, 0, 16) };
            sample.TextChanged += (_, _) => q.Sample = sample.Text;
            sp.Children.Add(sample);
        }
        else
        {
            sp.Children.Add(new TextBlock { Text = q.Type == QuestionType.Single ? "Odpowiedzi (zaznacz poprawną)" : "Odpowiedzi (zaznacz wszystkie poprawne)", Style = Ui.St("Label") });
            for (int i = 0; i < q.Options.Count; i++) sp.Children.Add(OptionEditor(q, i));
            if (q.Options.Count < 6)
            {
                var addOpt = Ui.Btn("+  Dodaj odpowiedź", "SecondaryButton", (_, _) => { q.Options.Add(""); RenderEditor(); });
                addOpt.Height = 34; addOpt.FontSize = 13; addOpt.HorizontalAlignment = HorizontalAlignment.Left; addOpt.Margin = new Thickness(0, 4, 0, 0);
                sp.Children.Add(addOpt);
            }
        }

        var ptsLabel = new TextBlock { Text = "Punkty", Style = Ui.St("Label") }; ptsLabel.Margin = new Thickness(0, 16, 0, 6);
        sp.Children.Add(ptsLabel);
        var pts = new TextBox { Text = q.Points.ToString(), Width = 120, Height = 44, HorizontalAlignment = HorizontalAlignment.Left };
        pts.TextChanged += (_, _) => { if (int.TryParse(pts.Text, out var v) && v > 0) { q.Points = v; _pointsTotal.Text = _draft.Questions.Sum(x => x.Points) + " pkt"; } };
        sp.Children.Add(pts);

        _editorHost.Children.Add(new Border { Style = Ui.St("Card"), Padding = new Thickness(20), Child = sp, BorderBrush = Ui.Br("Primary"), BorderThickness = new Thickness(1.5) });
    }

    private FrameworkElement OptionEditor(Question q, int i)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        bool on = q.Correct.Contains(i);
        var toggle = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 11, 0), BorderThickness = new Thickness(2), Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center, BorderBrush = Ui.Br(on ? "Success" : "Border2"), Background = on ? Ui.Br("Success") : Brushes.Transparent, Child = on ? new TextBlock { Text = "✓", Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } : null };
        toggle.MouseLeftButtonUp += (_, _) =>
        {
            if (q.Type == QuestionType.Single) { q.Correct.Clear(); q.Correct.Add(i); }
            else { if (q.Correct.Contains(i)) q.Correct.Remove(i); else q.Correct.Add(i); }
            RenderEditor();
        };
        Grid.SetColumn(toggle, 0);

        var box = new TextBox { Text = q.Options[i], Height = 44, VerticalAlignment = VerticalAlignment.Center };
        box.TextChanged += (_, _) => { if (i < q.Options.Count) q.Options[i] = box.Text; };
        Grid.SetColumn(box, 1);
        grid.Children.Add(toggle); grid.Children.Add(box);

        if (q.Options.Count > 2)
        {
            var rm = Ui.Btn("✕", "GhostButton", (_, _) =>
            {
                q.Options.RemoveAt(i);
                q.Correct = q.Correct.Where(c => c != i).Select(c => c > i ? c - 1 : c).ToList();
                RenderEditor();
            });
            rm.Width = 38; rm.Height = 38; rm.Margin = new Thickness(6, 0, 0, 0); rm.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(rm, 2); grid.Children.Add(rm);
        }
        return grid;
    }

    // Twarda walidacja przed zapisem — blokuje zapisanie testu-pułapki
    // (pytanie bez poprawnej odpowiedzi, puste pola, test bez grupy itd.).
    private List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(_draft.Title))
            errors.Add("• Podaj nazwę testu.");

        if (string.IsNullOrWhiteSpace(_draft.Subject))
            errors.Add("• Podaj przedmiot.");

        if (_draft.GroupIds.Count == 0)
            errors.Add("• Przypisz test do co najmniej jednej grupy — inaczej nikt go nie zobaczy.");

        // Pytań nie sprawdzamy, gdy są zablokowane (test ma już rozwiązania,
        // a istniejące pytania były zwalidowane przy pierwszym zapisie).
        if (_locked) return errors;

        for (int i = 0; i < _draft.Questions.Count; i++)
        {
            var q = _draft.Questions[i];
            int n = i + 1;

            if (string.IsNullOrWhiteSpace(q.Text))
                errors.Add($"• Pytanie {n}: brak treści.");

            if (q.Points <= 0)
                errors.Add($"• Pytanie {n}: liczba punktów musi być większa od 0.");

            if (q.Type == QuestionType.Open) continue;

            if (q.Options.Any(o => string.IsNullOrWhiteSpace(o)))
                errors.Add($"• Pytanie {n}: uzupełnij wszystkie pola odpowiedzi (któreś jest puste).");

            if (q.Correct.Count == 0)
                errors.Add($"• Pytanie {n}: zaznacz poprawną odpowiedź (inaczej uczniowie zawsze dostaną 0 pkt).");
        }

        return errors;
    }

    private void Save()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            MessageBox.Show(
                "Nie można zapisać testu — popraw poniższe błędy:\n\n" + string.Join("\n", errors),
                "Test niekompletny", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!Safe.Run(() => DbRepository.SaveTest(_draft, _host.CurrentUser.Id), "zapisać testu")) return;

        if (_locked)
            MessageBox.Show(
                "Zapisano ustawienia testu.\n\nPytania pozostały bez zmian, ponieważ test ma już rozwiązania uczniów — ich edycja naruszyłaby oddane odpowiedzi.",
                "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);

        _host.RefreshNav();
        _host.ShowTeacherTests();
    }
}
