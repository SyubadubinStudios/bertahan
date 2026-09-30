using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Bertahan.Core;
using Bertahan.Game;

namespace Bertahan.UI;

/// <summary>Top Skor: the best names per level.</summary>
public sealed class TopScoreScreen : Screen
{
    private readonly StackPanel _table = new() { Spacing = 4 };
    private readonly OutlinedText _heading;
    private readonly List<MenuButton> _tabs = [];

    public TopScoreScreen(IShell shell) : base(shell)
    {
        Children.Add(Backdrop(100));
        StackPanel col = Column(10);
        col.Children.Add(Kit.Title("TOP SKOR", 46));
        UniformGrid tabs = new() { Columns = 10 };
        for (int n = 1; n <= Campaign.PlayableLevels; n++)
        {
            int level = n;
            MenuButton tab = new(n.ToString(), () => ShowLevel(level), fontSize: 18, color: Kit.PaperDark) { Margin = new Thickness(3) };
            tab.GotFocus += (_, _) => ShowLevel(level);
            _tabs.Add(tab);
            tabs.Children.Add(tab);
        }

        col.Children.Add(tabs);
        _heading = Kit.Title("", 24, Kit.Orange);
        col.Children.Add(_heading);
        col.Children.Add(new ScrollViewer { Content = _table, Height = 380 });
        col.Children.Add(new MenuButton("KEMBALI", () => Shell.Back(), color: Kit.PaperDark) { Width = 240, HorizontalAlignment = HorizontalAlignment.Center });
        Border panel = Kit.Panel(col, 24);
        panel.Width = 820;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(panel);
        ShowLevel(1);
    }

    private void ShowLevel(int number)
    {
        LevelDef level = LevelDef.All[number - 1];
        _heading.Text = $"Level {number}: {level.Name}";
        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].BaseColor = i + 1 == number ? Kit.Sun : Kit.PaperDark;
            _tabs[i].Enabled = true;
        }

        _table.Children.Clear();
        IReadOnlyList<ScoreEntry> scores = Shell.Settings.Scores(level.Id);
        if (scores.Count == 0)
        {
            TextBlock empty = Kit.Text("Belum ada skor di sini. Jadilah pahlawan pertama!", 18, Kit.MutedBrush, FontWeight.Bold);
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Margin = new Thickness(0, 40);
            _table.Children.Add(empty);
            return;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            ScoreEntry e = scores[i];
            Grid row = new() { ColumnDefinitions = new ColumnDefinitions("54,48,*,130,90,130"), Height = 44 };
            Color medal = i switch { 0 => Color.Parse("#FFD23F"), 1 => Color.Parse("#D8DEE6"), 2 => Color.Parse("#E0A06A"), _ => Kit.PaperDark };
            Border rank = new()
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = new SolidColorBrush(medal),
                BorderBrush = Kit.InkBrush,
                BorderThickness = new Thickness(3),
                Child = new TextBlock { Text = (i + 1).ToString(), FontSize = 17, FontWeight = FontWeight.Black, Foreground = Kit.InkBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            };
            row.Children.Add(rank);
            Image face = Kit.Picture("portrait_" + e.Character, 40, 40);
            Grid.SetColumn(face, 1);
            row.Children.Add(face);
            TextBlock name = Kit.Text(e.Name + (e.Cleared ? "" : "  (gugur)"), 19, Kit.InkBrush, FontWeight.Black, TextWrapping.NoWrap);
            name.VerticalAlignment = VerticalAlignment.Center;
            name.Margin = new Thickness(8, 0);
            Grid.SetColumn(name, 2);
            row.Children.Add(name);
            DifficultyDef d = DifficultyDef.Get(e.Difficulty);
            Border chip = Kit.Chip(d.Name, Kit.Rgb(d.Color), null, 12);
            chip.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(chip, 3);
            row.Children.Add(chip);
            TextBlock date = Kit.Text(e.Date.ToString("dd/MM/yy"), 13, Kit.MutedBrush, FontWeight.Bold);
            date.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(date, 4);
            row.Children.Add(date);
            TextBlock score = Kit.Text(e.Score.ToString("N0"), 22, Kit.OrangeBrush, FontWeight.Black);
            score.HorizontalAlignment = HorizontalAlignment.Right;
            score.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(score, 5);
            row.Children.Add(score);
            _table.Children.Add(new Border
            {
                Background = i % 2 == 0 ? Kit.Brush(0xFFFFFF, 120) : Brushes.Transparent,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2),
                Child = row,
            });
        }
    }
}

/// <summary>Pilihan: sound, music, graphics and controls.</summary>
public sealed class OptionsScreen : Screen
{
    private readonly GameSettings _s;

    public OptionsScreen(IShell shell) : base(shell)
    {
        _s = shell.Settings;
        Children.Add(Backdrop(110));
        StackPanel col = Column(6);
        col.Children.Add(Kit.Title("PILIHAN", 44));
        Grid cols = new() { ColumnDefinitions = new ColumnDefinitions("*,28,*") };

        StackPanel left = Column(6);
        left.Children.Add(Section("SUARA"));
        left.Children.Add(SliderRow("Musik", _s.MusicVolume, v => _s.MusicVolume = v));
        left.Children.Add(SliderRow("Efek suara", _s.SfxVolume, v =>
        {
            _s.SfxVolume = v;
            UiSound.Click();
        }));
        left.Children.Add(Section("KONTROL"));
        left.Children.Add(SliderRow("Putar kamera (Q/E)", (_s.CameraSpeed - 0.4f) / 1.6f, v => _s.CameraSpeed = 0.4f + (v * 1.6f)));
        left.Children.Add(Toggle("Getaran layar", () => _s.ScreenShake, v => _s.ScreenShake = v));
        left.Children.Add(new MenuButton("DAFTAR TOMBOL", () => Shell.Show(new ControlsScreen(Shell)), fontSize: 18, color: Color.Parse("#9FE0FF")));

        StackPanel right = Column(6);
        right.Children.Add(Section("GRAFIK"));
        right.Children.Add(Choice("Kualitas", ["Rendah", "Sedang", "Tinggi"], () => _s.Quality, v => _s.Quality = v));
        right.Children.Add(Toggle("Layar penuh", () => _s.Fullscreen, v => _s.Fullscreen = v));
        right.Children.Add(Toggle("Tampilkan FPS", () => _s.ShowFps, v => _s.ShowFps = v));
        right.Children.Add(Choice("GPU", ["Otomatis", "DX12", "Vulkan"], () => _s.Backend switch { "dx12" => 1, "vulkan" => 2, _ => 0 },
            v => _s.Backend = v switch { 1 => "dx12", 2 => "vulkan", _ => "auto" }));
        right.Children.Add(Kit.Text($"Perubahan GPU berlaku setelah game dibuka ulang. Sekarang: {GpuBackend.Active.ToUpperInvariant()}.", 12, Kit.MutedBrush));

        cols.Children.Add(left);
        Grid.SetColumn(right, 2);
        cols.Children.Add(right);
        col.Children.Add(cols);
        col.Children.Add(new MenuButton("SIMPAN & KEMBALI", Close, color: Kit.Zombie) { Width = 320, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });
        Border panel = Kit.Panel(col, 26);
        panel.Width = 880;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(panel);
    }

    public override MenuView? StageView => null;

    public override void OnBack()
    {
        UiSound.Back();
        Close();
    }

    private void Close()
    {
        _s.Save();
        Shell.ApplySettings();
        Shell.Back();
    }

    private static Control Section(string title)
    {
        TextBlock t = Kit.Text(title, 15, Kit.OrangeBrush, FontWeight.Black);
        t.Margin = new Thickness(0, 8, 0, 0);
        return t;
    }

    private Control SliderRow(string label, float value, Action<float> set)
    {
        Grid row = new() { ColumnDefinitions = new ColumnDefinitions("150,*,50") };
        TextBlock l = Kit.Text(label, 16, Kit.InkBrush, FontWeight.Bold);
        l.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(l);
        TextBlock pct = Kit.Text($"{value * 100:0}%", 15, Kit.InkBrush, FontWeight.Black);
        pct.VerticalAlignment = VerticalAlignment.Center;
        pct.HorizontalAlignment = HorizontalAlignment.Right;
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 1,
            Value = value,
            SmallChange = 0.05,
            LargeChange = 0.1,
            Foreground = Kit.OrangeBrush,
            Margin = new Thickness(6, 0),
        };
        slider.ValueChanged += (_, e) =>
        {
            set((float)e.NewValue);
            pct.Text = $"{e.NewValue * 100:0}%";
            Shell.ApplySettings();
        };
        Grid.SetColumn(slider, 1);
        row.Children.Add(slider);
        Grid.SetColumn(pct, 2);
        row.Children.Add(pct);
        return row;
    }

    private Control Toggle(string label, Func<bool> get, Action<bool> set)
    {
        MenuButton button = null!;
        button = new MenuButton(Label(), () =>
        {
            set(!get());
            button.Label = Label();
            button.BaseColor = get() ? Kit.Zombie : Kit.PaperDark;
            Shell.ApplySettings();
        }, fontSize: 18, color: get() ? Kit.Zombie : Kit.PaperDark);
        return button;

        string Label() => $"{label}: {(get() ? "YA" : "TIDAK")}";
    }

    private Control Choice(string label, string[] options, Func<int> get, Action<int> set)
    {
        StackPanel col = new() { Spacing = 2 };
        col.Children.Add(Kit.Text(label, 16, Kit.InkBrush, FontWeight.Bold));
        UniformGrid row = new() { Columns = options.Length };
        List<MenuButton> buttons = [];
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            MenuButton b = new(options[i], () =>
            {
                set(index);
                for (int k = 0; k < buttons.Count; k++)
                {
                    buttons[k].BaseColor = k == index ? Kit.Sun : Kit.PaperDark;
                    buttons[k].Enabled = true;
                }

                Shell.ApplySettings();
            }, fontSize: 17, color: get() == i ? Kit.Sun : Kit.PaperDark) { Margin = new Thickness(3, 2) };
            buttons.Add(b);
            row.Children.Add(b);
        }

        col.Children.Add(row);
        return col;
    }
}

/// <summary>Daftar tombol.</summary>
public sealed class ControlsScreen : Screen
{
    public static readonly (string Key, string Action)[] Keys =
    [
        ("W A S D / Panah", "Bergerak"),
        ("Mouse", "Membidik"),
        ("Klik kiri / J", "Menyerang / menembak"),
        ("Klik kanan / G", "Lempar bom molotov"),
        ("Spasi", "Berguling menghindar"),
        ("Shift", "Berlari"),
        ("R", "Isi ulang senapan"),
        ("1 - 9 / Roda mouse", "Ganti senjata"),
        ("Q / E", "Putar kamera"),
        ("+ / -", "Dekatkan / jauhkan kamera"),
        ("Esc / P", "Istirahat (pause)"),
        ("F11", "Layar penuh"),
        ("F12", "Simpan screenshot"),
    ];

    public ControlsScreen(IShell shell) : base(shell)
    {
        Children.Add(Backdrop(130));
        StackPanel col = Column(6);
        col.Children.Add(Kit.Title("DAFTAR TOMBOL", 40));
        Grid table = new() { ColumnDefinitions = new ColumnDefinitions("240,*") };
        foreach ((string key, string action) in Keys)
        {
            int r = table.RowDefinitions.Count;
            table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Border k = Kit.Chip(key, Kit.Sun, null, 15);
            k.Margin = new Thickness(0, 3);
            TextBlock a = Kit.Text(action, 17, Kit.InkBrush, FontWeight.Bold);
            a.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(k, r);
            Grid.SetRow(a, r);
            Grid.SetColumn(a, 1);
            table.Children.Add(k);
            table.Children.Add(a);
        }

        col.Children.Add(table);
        col.Children.Add(new MenuButton("KEMBALI", () => Shell.Back(), color: Kit.PaperDark) { Margin = new Thickness(0, 12, 0, 0) });
        Border panel = Kit.Panel(col, 26);
        panel.Width = 560;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(panel);
    }

    public override MenuView? StageView => null;
}

/// <summary>Tentang: scrolling credits.</summary>
public sealed class AboutScreen : Screen
{
    private readonly StackPanel _roll;
    private readonly Canvas _canvas;
    private readonly bool _fromEnding;
    private double _offset;
    private float _speed = 1f;

    public AboutScreen(IShell shell, bool fromEnding = false) : base(shell)
    {
        _fromEnding = fromEnding;
        Children.Add(Backdrop(170));
        _roll = new StackPanel { Width = 760, Spacing = 6 };
        _roll.Children.Add(Kit.Picture("logo", 520));
        Line("Game survival zombi di perkampungan Indonesia", 20, Kit.White, FontWeight.Bold);
        Gap(40);
        Line("DIBUAT OLEH", 18, Kit.SunBrush, FontWeight.Black);
        _roll.Children.Add(Center(Kit.Title("Ariana Mischa Fadhila", 46, Kit.Paper)));
        Line("dari", 18, Kit.White, FontWeight.Bold);
        _roll.Children.Add(Center(Kit.Title("Subadubin Studios", 40, Kit.Zombie)));
        Gap(50);
        Section("PARA PAHLAWAN");
        Faces(CharacterDef.All.Select(c => ("portrait_" + c.Id, c.Name, c.Role)));
        Gap(30);
        Section("PARA ZOMBI");
        Faces(ZombieDef.All.Select(z => ("zombie_" + z.Id, z.Name, z.Id is "dukun" or "genderuwo" ? "Bos" : "Pengganggu")));
        Gap(30);
        Section("WARGA KAMPUNG DAMAI");
        Line("Pak Tani  -  Bu Pedagang  -  Pak Ustad  -  Bocah-bocah kampung", 18, Kit.White, FontWeight.Bold);
        Gap(30);
        Section("TEKNOLOGI");
        Line(".NET 10  -  Avalonia UI  -  Three.Net (wgpu)", 20, Kit.White, FontWeight.Bold);
        Line("Model, rigging & animasi 3D dibuat dengan Blender melalui Blender MCP", 18, Kit.White, FontWeight.SemiBold);
        Line("Musik & efek suara disintesis secara prosedural (AudioGen)", 18, Kit.White, FontWeight.SemiBold);
        Gap(30);
        Section("TERIMA KASIH");
        Line("Kepada semua warga kampung yang tetap berani,", 18, Kit.White, FontWeight.SemiBold);
        Line("dan kepada kamu yang sudah bermain!", 18, Kit.White, FontWeight.SemiBold);
        Gap(60);
        _roll.Children.Add(Center(Kit.Title("BERTAHAN!", 60)));
        Line("(c) Subadubin Studios", 14, Kit.MutedBrush, FontWeight.Bold);
        Gap(200);

        _canvas = new Canvas { ClipToBounds = true };
        _canvas.Children.Add(_roll);
        Children.Add(_canvas);
        TextBlock hint = Kit.Text("Esc: kembali  -  Atas/Bawah: cepat/lambat", 13, Kit.White, FontWeight.Bold);
        hint.HorizontalAlignment = HorizontalAlignment.Right;
        hint.VerticalAlignment = VerticalAlignment.Bottom;
        hint.Margin = new Thickness(0, 0, 24, 90);
        Children.Add(hint);
        MenuButton back = new("KEMBALI", () => OnBack(), color: Kit.PaperDark)
        {
            Width = 220,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 24, 24),
        };
        Children.Add(back);
        _offset = 720;
    }

    public override MenuView? StageView => MenuView.Village;

    public override void OnBack()
    {
        UiSound.Back();
        if (_fromEnding)
        {
            Shell.QuitToTitle();
        }
        else
        {
            Shell.Back();
        }
    }

    public override void HandleKey(Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key is Avalonia.Input.Key.Down or Avalonia.Input.Key.S)
        {
            _speed = Math.Min(6f, _speed + 1f);
            e.Handled = true;
            return;
        }

        if (e.Key is Avalonia.Input.Key.Up or Avalonia.Input.Key.W)
        {
            _speed = Math.Max(-2f, _speed - 1f);
            e.Handled = true;
            return;
        }

        base.HandleKey(e);
    }

    public override void Tick(float dt)
    {
        _offset -= dt * 55 * _speed;
        double height = _roll.Bounds.Height;
        if (height > 0 && _offset < -height)
        {
            _offset = 720;
        }

        Canvas.SetLeft(_roll, (1280 - 760) / 2.0);
        Canvas.SetTop(_roll, _offset);
    }

    private static Control Center(Control c)
    {
        c.HorizontalAlignment = HorizontalAlignment.Center;
        return c;
    }

    private void Line(string text, double size, IBrush color, FontWeight weight)
    {
        TextBlock t = Kit.Text(text, size, color, weight);
        t.TextAlignment = TextAlignment.Center;
        t.HorizontalAlignment = HorizontalAlignment.Center;
        t.Effect = new DropShadowEffect { BlurRadius = 4, OffsetX = 0, OffsetY = 2, Color = Colors.Black, Opacity = 0.8 };
        _roll.Children.Add(t);
    }

    private void Section(string title) => _roll.Children.Add(Center(Kit.Title(title, 28, Kit.Sun)));

    private void Gap(double h) => _roll.Children.Add(new Border { Height = h });

    private void Faces(IEnumerable<(string Image, string Name, string Role)> faces)
    {
        WrapPanel wrap = new() { HorizontalAlignment = HorizontalAlignment.Center, ItemWidth = 180 };
        foreach ((string image, string name, string role) in faces)
        {
            StackPanel s = new() { Spacing = 2, Margin = new Thickness(0, 6) };
            s.Children.Add(Kit.Picture(image, 110, 110));
            TextBlock n = Kit.Text(name, 18, Kit.White, FontWeight.Black);
            n.HorizontalAlignment = HorizontalAlignment.Center;
            s.Children.Add(n);
            TextBlock r = Kit.Text(role, 13, Kit.SunBrush, FontWeight.Bold);
            r.HorizontalAlignment = HorizontalAlignment.Center;
            r.TextAlignment = TextAlignment.Center;
            s.Children.Add(r);
            wrap.Children.Add(s);
        }

        _roll.Children.Add(wrap);
    }
}
