using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Bertahan.Game;

namespace Bertahan.UI;

/// <summary>New Game step 1: the hero's name (default "Si Otong").</summary>
public sealed class NameScreen : Screen
{
    private static readonly string[] Names =
    [
        "Si Otong", "Si Unyil", "Mas Bejo", "Mbak Ratna", "Ucok Baba", "Kang Asep", "Bang Jali", "Neng Geulis", "Cak Mamat", "Mbok Darmi", "Si Kabayan", "Pak RT",
    ];

    private readonly TextBox _input;
    private int _random;

    public NameScreen(IShell shell) : base(shell)
    {
        StackPanel col = Column(12);
        col.Children.Add(Steps(1));
        col.Children.Add(Kit.Title("SIAPA NAMAMU?", 40));
        col.Children.Add(Kit.Text("Nama ini akan tercatat di papan Top Skor kampung.", 16, Kit.MutedBrush));
        _input = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(shell.Settings.LastName) ? "Si Otong" : shell.Settings.LastName,
            Watermark = "Si Otong",
            MaxLength = 16,
            FontSize = 28,
            FontWeight = FontWeight.Black,
            FontFamily = Kit.Font,
            Foreground = Kit.InkBrush,
            Background = Kit.White,
            BorderBrush = Kit.InkBrush,
            BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16, 10),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        // keep the paper look when focused instead of Fluent's dark field
        foreach (string key in new[] { "TextControlBackgroundFocused", "TextControlBackgroundPointerOver" })
        {
            _input.Resources[key] = Kit.White;
        }

        _input.Resources["TextControlBorderBrushFocused"] = Kit.OrangeBrush;
        _input.Resources["TextControlBorderBrushPointerOver"] = Kit.InkBrush;
        _input.Resources["TextControlForegroundFocused"] = Kit.InkBrush;
        _input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                UiSound.Click();
                Next();
            }
        };
        col.Children.Add(_input);
        Grid row = new() { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
        MenuButton dice = new("NAMA ACAK", () =>
        {
            _random = (_random + 1 + Random.Shared.Next(Names.Length - 1)) % Names.Length;
            _input.Text = Names[_random];
        }, color: Color.Parse("#9FE0FF"), fontSize: 20);
        MenuButton def = new("SI OTONG", () => _input.Text = "Si Otong", color: Color.Parse("#FFE9A8"), fontSize: 20);
        Grid.SetColumn(def, 2);
        row.Children.Add(dice);
        row.Children.Add(def);
        col.Children.Add(row);
        Grid nav = new() { ColumnDefinitions = new ColumnDefinitions("*,12,1.4*"), Margin = new Thickness(0, 10, 0, 0) };
        nav.Children.Add(new MenuButton("KEMBALI", () => Shell.Back(), color: Kit.PaperDark));
        MenuButton next = new("LANJUT", Next, color: Kit.Zombie);
        Grid.SetColumn(next, 2);
        nav.Children.Add(next);
        col.Children.Add(nav);
        Border panel = Kit.Panel(col, 28);
        panel.Width = 520;
        panel.HorizontalAlignment = HorizontalAlignment.Left;
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.Margin = new Thickness(60, 0, 0, 0);
        Children.Add(panel);
    }

    public override MenuView? StageView => MenuView.Family;

    public override void OnShown()
    {
        base.OnShown();
        Shell.Menu?.Select(-1);
        _input.Focus();
        _input.SelectAll();
    }

    private void Next()
    {
        string name = (_input.Text ?? "").Trim();
        if (name.Length == 0)
        {
            name = "Si Otong";
        }

        Campaign run = new() { PlayerName = name, Difficulty = Shell.Settings.LastDifficulty, Character = Shell.Settings.LastCharacter };
        Shell.Show(new DifficultyScreen(Shell, run));
    }

    /// <summary>The "1 Nama - 2 Kesulitan - 3 Karakter" breadcrumb.</summary>
    public static Control Steps(int current)
    {
        StackPanel row = new() { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
        string[] names = ["Nama", "Kesulitan", "Karakter"];
        for (int i = 0; i < names.Length; i++)
        {
            bool on = i + 1 == current, done = i + 1 < current;
            row.Children.Add(Kit.Chip($"{i + 1}. {names[i]}", on ? Kit.Orange : done ? Kit.Zombie : Kit.PaperDark, Kit.InkBrush, 13));
        }

        return row;
    }
}

/// <summary>New Game step 2: Bayi, Pemberani or Mimpi Buruk.</summary>
public sealed class DifficultyScreen : Screen
{
    private readonly Campaign _run;
    private readonly List<MenuButton> _cards = [];

    public DifficultyScreen(IShell shell, Campaign run) : base(shell)
    {
        _run = run;
        StackPanel col = new() { Spacing = 14, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        col.Children.Add(NameScreen.Steps(2));
        col.Children.Add(Kit.Title("SEBERAPA BERANI?", 44));
        TextBlock hi = Kit.Text($"Halo, {run.PlayerName}! Pilih tingkat kesulitan:", 18, Kit.White, FontWeight.Bold);
        hi.HorizontalAlignment = HorizontalAlignment.Center;
        hi.Effect = new DropShadowEffect { BlurRadius = 6, OffsetY = 2, OffsetX = 0, Color = Colors.Black, Opacity = 0.9 };
        col.Children.Add(hi);
        StackPanel cards = new() { Orientation = Orientation.Horizontal, Spacing = 22, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (DifficultyDef d in DifficultyDef.All)
        {
            cards.Children.Add(Card(d));
        }

        col.Children.Add(cards);
        MenuButton back = new("KEMBALI", () => Shell.Back(), color: Kit.PaperDark) { Width = 220, HorizontalAlignment = HorizontalAlignment.Center };
        col.Children.Add(back);
        Children.Add(Backdrop(90));
        Children.Add(col);
    }

    public override MenuView? StageView => MenuView.Family;

    public override void OnShown()
    {
        base.OnShown();
        _cards[(int)_run.Difficulty].Focus(NavigationMethod.Directional);
    }

    private Control Card(DifficultyDef d)
    {
        StackPanel body = new() { Spacing = 8, Width = 270 };
        body.Children.Add(Kit.Title(d.Name.ToUpperInvariant(), 34, Kit.Rgb(d.Color)));
        TextBlock tag = Kit.Text(d.Tagline, 16, Kit.InkBrush, FontWeight.Black);
        tag.HorizontalAlignment = HorizontalAlignment.Center;
        body.Children.Add(tag);
        TextBlock desc = Kit.Text(d.Description, 14, Kit.MutedBrush);
        desc.TextAlignment = TextAlignment.Center;
        desc.Height = 60;
        body.Children.Add(desc);
        body.Children.Add(Stat("Nyawa", d.Lives, 5, Kit.BloodBrush));
        body.Children.Add(Stat("Kekuatan zombi", (int)MathF.Round(d.EnemyDamage * 3), 5, Kit.ZombieBrush));
        body.Children.Add(Stat("Skor", (int)MathF.Round(d.ScoreMultiplier * 2), 3, Kit.SunBrush));
        MenuButton card = new("", () =>
        {
            _run.Difficulty = d.Id;
            Shell.Settings.LastDifficulty = d.Id;
            Shell.Show(new CharacterScreen(Shell, _run));
        }, color: Kit.Paper) { Width = 320 };
        card.Child = new Border
        {
            Background = Kit.PaperBrush,
            BorderBrush = Kit.InkBrush,
            BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(20, 18),
            BoxShadow = Kit.PanelShadow,
            Child = body,
        };
        card.GotFocus += (_, _) => ((Border)card.Child).Background = Kit.Brush(0xFFF9EB);
        card.GotFocus += (_, _) => ((Border)card.Child).BorderBrush = new SolidColorBrush(Kit.Rgb(d.Color));
        card.LostFocus += (_, _) => ((Border)card.Child).BorderBrush = Kit.InkBrush;
        card.LostFocus += (_, _) => ((Border)card.Child).Background = Kit.PaperBrush;
        _cards.Add(card);
        return card;
    }

    private static Control Stat(string label, int value, int max, IBrush color)
    {
        Grid row = new() { ColumnDefinitions = new ColumnDefinitions("110,*") };
        row.Children.Add(Kit.Text(label, 13, Kit.InkBrush, FontWeight.Bold));
        StackPanel pips = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
        for (int i = 0; i < max; i++)
        {
            pips.Children.Add(new Border
            {
                Width = 20,
                Height = 14,
                CornerRadius = new CornerRadius(4),
                BorderBrush = Kit.InkBrush,
                BorderThickness = new Thickness(2),
                Background = i < value ? color : Kit.PaperDarkBrush,
            });
        }

        Grid.SetColumn(pips, 1);
        row.Children.Add(pips);
        return row;
    }
}

/// <summary>New Game step 3: pick a family member, then Mulai!</summary>
public sealed class CharacterScreen : Screen
{
    private readonly Campaign _run;
    private readonly List<MenuButton> _tiles = [];
    private readonly TextBlock _name;
    private readonly TextBlock _role;
    private readonly TextBlock _perk;
    private readonly StackPanel _stats;
    private readonly Image _weapon;
    private readonly TextBlock _weaponName;
    private int _index;

    public CharacterScreen(IShell shell, Campaign run) : base(shell)
    {
        _run = run;
        _index = Math.Max(0, Array.IndexOf(MenuStage.Order, run.Character));
        StackPanel col = Column(10);
        col.Children.Add(NameScreen.Steps(3));
        col.Children.Add(Kit.Title("PILIH JAGOANMU!", 36));

        UniformGrid tiles = new() { Columns = 6, Margin = new Thickness(0, 4) };
        for (int i = 0; i < MenuStage.Order.Length; i++)
        {
            int index = i;
            string id = MenuStage.Order[i];
            MenuButton tile = new("", () => Pick(index), color: Kit.PaperDark) { Margin = new Thickness(3) };
            StackPanel face = new() { Spacing = 0 };
            face.Children.Add(Kit.Picture("portrait_" + id, 56, 56));
            TextBlock n = Kit.Text(CharacterDef.Get(id).Name, 12, Kit.InkBrush, FontWeight.Black, TextWrapping.NoWrap);
            n.HorizontalAlignment = HorizontalAlignment.Center;
            face.Children.Add(n);
            tile.Child = new Border
            {
                BorderBrush = Kit.InkBrush,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(2, 4),
                Background = Kit.PaperDarkBrush,
                Child = face,
            };
            tile.GotFocus += (_, _) => Select(index);
            _tiles.Add(tile);
            tiles.Children.Add(tile);
        }

        col.Children.Add(tiles);
        _name = Kit.Text("", 30, Kit.InkBrush, FontWeight.Black);
        _role = Kit.Text("", 15, Kit.OrangeBrush, FontWeight.Black);
        _perk = Kit.Text("", 15, Kit.MutedBrush);
        _perk.Height = 44;
        col.Children.Add(_name);
        col.Children.Add(_role);
        col.Children.Add(_perk);
        _stats = new StackPanel { Spacing = 5 };
        col.Children.Add(_stats);
        StackPanel weapon = new() { Orientation = Orientation.Horizontal, Spacing = 10 };
        _weapon = new Image { Width = 48, Height = 48 };
        _weaponName = Kit.Text("", 15, Kit.InkBrush, FontWeight.Bold);
        _weaponName.VerticalAlignment = VerticalAlignment.Center;
        weapon.Children.Add(_weapon);
        weapon.Children.Add(_weaponName);
        col.Children.Add(weapon);
        Grid nav = new() { ColumnDefinitions = new ColumnDefinitions("*,12,1.5*"), Margin = new Thickness(0, 6, 0, 0) };
        nav.Children.Add(new MenuButton("KEMBALI", () => Shell.Back(), color: Kit.PaperDark));
        MenuButton go = new("MULAI!", Start, color: Kit.Zombie);
        Grid.SetColumn(go, 2);
        nav.Children.Add(go);
        col.Children.Add(nav);

        Border panel = Kit.Panel(col, 22);
        panel.Width = 500;
        panel.HorizontalAlignment = HorizontalAlignment.Left;
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.Margin = new Thickness(40, 0, 0, 0);
        Children.Add(panel);
        Select(_index);
    }

    public override MenuView? StageView => MenuView.Family;

    public override void OnShown()
    {
        base.OnShown();
        _tiles[_index].Focus(NavigationMethod.Directional);
        Shell.Menu?.Select(_index);
    }

    public override void OnBack()
    {
        Shell.Menu?.Select(-1);
        base.OnBack();
    }

    private void Pick(int index)
    {
        Select(index);
        Start();
    }

    private void Select(int index)
    {
        _index = index;
        CharacterDef c = CharacterDef.Get(MenuStage.Order[index]);
        _name.Text = c.Name.ToUpperInvariant();
        _role.Text = c.Role;
        _perk.Text = c.Perk;
        _stats.Children.Clear();
        _stats.Children.Add(Bar("Darah", c.Health / 130f, Kit.Blood));
        _stats.Children.Add(Bar("Kecepatan", c.Speed / 6.3f, Kit.Teal));
        _stats.Children.Add(Bar("Pukulan", c.DamageMultiplier / 1.4f, Kit.Orange));
        _stats.Children.Add(Bar("Kegesitan", c.AttackSpeed / 1.3f, Kit.Zombie));
        WeaponDef w = WeaponDef.Get(c.StartWeapon);
        _weapon.Source = Assets.Image("weapon_" + w.Id);
        _weaponName.Text = $"Senjata awal: {w.Name}";
        for (int i = 0; i < _tiles.Count; i++)
        {
            ((Border)_tiles[i].Child!).Background = i == index ? Kit.SunBrush : Kit.PaperDarkBrush;
        }

        Shell.Menu?.Select(index);
    }

    private static Control Bar(string label, float value, Color color)
    {
        Grid row = new() { ColumnDefinitions = new ColumnDefinitions("100,*") };
        row.Children.Add(Kit.Text(label, 13, Kit.InkBrush, FontWeight.Bold));
        Grid bar = new() { Height = 14 };
        bar.Children.Add(new Border { Background = Kit.PaperDarkBrush, CornerRadius = new CornerRadius(7), BorderBrush = Kit.InkBrush, BorderThickness = new Thickness(2) });
        bar.Children.Add(new Border
        {
            Background = new SolidColorBrush(color),
            CornerRadius = new CornerRadius(7),
            BorderBrush = Kit.InkBrush,
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = Math.Clamp(value, 0.08f, 1f) * 360,
        });
        Grid.SetColumn(bar, 1);
        row.Children.Add(bar);
        return row;
    }

    private void Start()
    {
        _run.Character = MenuStage.Order[_index];
        Shell.Settings.LastCharacter = _run.Character;
        Shell.Settings.LastName = _run.PlayerName;
        Shell.StartRun(_run);
    }
}
