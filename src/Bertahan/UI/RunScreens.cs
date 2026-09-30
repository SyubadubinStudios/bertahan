using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Bertahan.Core;
using Bertahan.Game;

namespace Bertahan.UI;

/// <summary>Peta Petualangan: the ten levels, which are done and which is next.</summary>
public sealed class LevelMapScreen : Screen
{
    public LevelMapScreen(IShell shell) : base(shell)
    {
        Campaign run = shell.Run!;
        Children.Add(Backdrop(110));
        StackPanel col = new() { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        col.Children.Add(Kit.Title("PETA PETUALANGAN", 44));
        StackPanel info = new() { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
        info.Children.Add(Kit.Chip(run.PlayerName, Kit.Sun, null, 15));
        info.Children.Add(Kit.Chip(run.CharacterDef.Name, Kit.PaperDark, null, 15));
        info.Children.Add(Kit.Chip(run.DifficultyDef.Name, Kit.Rgb(run.DifficultyDef.Color), null, 15));
        info.Children.Add(Kit.Chip($"Skor {run.TotalScore:N0}", Kit.Zombie, null, 15));
        info.Children.Add(Kit.Chip($"Kesempatan ulang {run.RetriesLeft}/{Campaign.MaxRetries}", Kit.Paper, null, 15));
        col.Children.Add(info);

        UniformGrid levels = new() { Columns = 5, Margin = new Thickness(0, 4) };
        foreach (LevelDef l in LevelDef.All)
        {
            levels.Children.Add(LevelCard(l, run));
        }

        col.Children.Add(levels);
        Grid nav = new() { ColumnDefinitions = new ColumnDefinitions("260,16,360"), HorizontalAlignment = HorizontalAlignment.Center };
        nav.Children.Add(new MenuButton("MENU UTAMA", () => Shell.QuitToTitle(), color: Kit.PaperDark));
        MenuButton play = new($"MAIN LEVEL {run.Level}!", () => Shell.PlayLevel(), run.LevelDef.Name, color: Kit.Zombie);
        Grid.SetColumn(play, 2);
        nav.Children.Add(play);
        col.Children.Add(nav);
        Children.Add(col);
    }

    public override MenuView? StageView => MenuView.Village;

    public override void OnShown()
    {
        base.OnShown();
        Buttons().LastOrDefault()?.Focus();
    }

    public override void OnBack() => Shell.QuitToTitle();

    private static Control LevelCard(LevelDef l, Campaign run)
    {
        bool done = l.Number < run.Level, current = l.Number == run.Level;
        StackPanel s = new() { Spacing = 4 };
        Grid pic = new() { Height = 92 };
        pic.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(10),
            ClipToBounds = true,
            Child = new Image { Source = Assets.Image("scene_level_" + l.Number), Stretch = Stretch.UniformToFill, Opacity = done || current ? 1 : 0.55 },
        });
        if (done)
        {
            Border tick = Kit.Chip("SELESAI", Kit.Zombie, null, 13);
            tick.Margin = new Thickness(6);
            tick.VerticalAlignment = VerticalAlignment.Top;
            pic.Children.Add(tick);
        }
        else if (current)
        {
            Border here = Kit.Chip("DI SINI", Kit.Orange, null, 13);
            here.Margin = new Thickness(6);
            here.VerticalAlignment = VerticalAlignment.Top;
            pic.Children.Add(here);
        }

        s.Children.Add(pic);
        s.Children.Add(Kit.Text($"LEVEL {l.Number}", 13, Kit.OrangeBrush, FontWeight.Black));
        s.Children.Add(Kit.Text(l.Name, 15, Kit.InkBrush, FontWeight.Black, TextWrapping.NoWrap));
        string time = l.Time switch { TimeOfDay.Malam => "Malam", TimeOfDay.Sore => "Sore", TimeOfDay.Kutukan => "Kutukan", _ => "Siang" };
        s.Children.Add(Kit.Text($"{l.Zone} - {time}", 11, Kit.MutedBrush, FontWeight.Bold, TextWrapping.NoWrap));
        return new Border
        {
            Width = 214,
            Margin = new Thickness(4),
            Padding = new Thickness(7),
            CornerRadius = new CornerRadius(16),
            Background = current ? Kit.Brush(0xFFF1B8) : Kit.PaperBrush,
            BorderBrush = current ? Kit.OrangeBrush : Kit.InkBrush,
            BorderThickness = new Thickness(current ? 5 : 3),
            BoxShadow = Kit.PanelShadow,
            Child = s,
        };
    }
}

/// <summary>The level artwork, its story and a tip while the level is built.</summary>
public sealed class LoadingScreen : Screen
{
    private static readonly string[] Tips =
    [
        "Tekan SPASI untuk berguling menghindar. Kamu kebal sesaat saat berguling!",
        "Bom molotov (G atau klik kanan) membakar gerombolan zombi sekaligus.",
        "Kombo 5 pukulan beruntun melipatgandakan skormu.",
        "Pocong hanya bergerak saat melompat. Pukul saat ia mendarat!",
        "Jeritan Kuntilanak membuatmu lambat. Jaga jarak darinya!",
        "Genderuwo suka menyeruduk. Menghindar ke samping, lalu balas!",
        "Wajan membuat zombi pusing. Bambu runcing menembus barisan.",
        "Senjata baru tergeletak di peta. Tekan angka 1-9 untuk berganti senjata.",
        "Air sawah memperlambat semua orang, kecuali pocong dan kuntilanak.",
        "Jembatan bambu adalah satu-satunya jalan menyeberangi sungai. Jangan sampai terkepung!",
        "Siluman Harimau suka menerkam dari jauh. Berguling ke samping saat ia merunduk!",
        "Lingkaran di tanah berarti hantaman akan datang. Cepat menyingkir!",
        "Kuntilanak Geni meninggalkan jejak api. Jangan berdiri di atasnya.",
        "Bos yang berakar di sumur, rawa, atau kolam tidak bisa mengejar. Serang dari tepi!",
        "Tahan SHIFT untuk berlari, tapi awasi tenagamu.",
    ];

    private readonly TextBlock _dots;
    private float _time;

    public LoadingScreen(IShell shell, LevelDef level) : base(shell)
    {
        Background = Brushes.Black;
        Children.Add(new Image { Source = Assets.Image("scene_level_" + level.Number) ?? Assets.Image("loading"), Stretch = Stretch.UniformToFill });
        Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = [new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.35), new GradientStop(Color.FromArgb(235, 15, 8, 20), 1)],
            },
        });
        StackPanel col = new() { Spacing = 6, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(64, 0, 64, 48) };
        col.Children.Add(Kit.Chip($"LEVEL {level.Number}  -  {level.Zone.ToUpperInvariant()}", Kit.Orange, null, 15));
        OutlinedText title = Kit.Title(level.Name.ToUpperInvariant(), 58);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        col.Children.Add(title);
        col.Children.Add(Kit.Text(level.Description, 20, Kit.White, FontWeight.Bold));
        Border tip = new()
        {
            Margin = new Thickness(0, 14, 0, 0),
            Background = Kit.Brush(0xFFF4DC, 235),
            BorderBrush = Kit.InkBrush,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Kit.Text("TIPS: " + Tips[Random.Shared.Next(Tips.Length)], 16, Kit.InkBrush, FontWeight.Bold),
        };
        col.Children.Add(tip);
        Children.Add(col);
        _dots = Kit.Text("Memuat", 22, Kit.SunBrush, FontWeight.Black);
        _dots.HorizontalAlignment = HorizontalAlignment.Right;
        _dots.VerticalAlignment = VerticalAlignment.Bottom;
        _dots.Margin = new Thickness(0, 0, 64, 52);
        _dots.Width = 130;
        Children.Add(_dots);
    }

    public override MenuView? StageView => null;

    public override void OnBack()
    {
    }

    public override void Tick(float dt)
    {
        _time += dt;
        _dots.Text = "Memuat" + new string('.', 1 + ((int)(_time * 3) % 3));
    }
}

/// <summary>Esc during play.</summary>
public sealed class PauseScreen : Screen
{
    public PauseScreen(IShell shell) : base(shell)
    {
        Children.Add(Backdrop(150));
        GameSession s = shell.Session!;
        Campaign run = shell.Run!;
        StackPanel col = Column();
        col.Children.Add(Kit.Title("ISTIRAHAT DULU", 38));
        TextBlock where = Kit.Text($"Level {s.Level.Def.Number}: {s.Level.Def.Name}  -  Gelombang {Math.Min(s.Waves.Wave + 1, s.Waves.WaveCount)}/{s.Waves.WaveCount}", 15, Kit.MutedBrush, FontWeight.Bold);
        where.HorizontalAlignment = HorizontalAlignment.Center;
        col.Children.Add(where);
        TextBlock stats = Kit.Text($"Skor {s.Score:N0}  -  Zombi {s.Kills}  -  Nyawa {s.Lives}  -  Kesempatan ulang {run.RetriesLeft}", 15, Kit.InkBrush, FontWeight.Bold);
        stats.HorizontalAlignment = HorizontalAlignment.Center;
        stats.Margin = new Thickness(0, 0, 0, 8);
        col.Children.Add(stats);
        col.Children.Add(new MenuButton("LANJUTKAN", () => Shell.ResumeGame(), color: Kit.Zombie));
        col.Children.Add(new MenuButton("ULANGI LEVEL", () => Shell.Show(new ConfirmScreen(Shell, "Ulangi level ini?", "Skor level ini akan hilang. Kesempatan ulang tidak berkurang.", Shell.RestartLevel))));
        col.Children.Add(new MenuButton("PILIHAN", () => Shell.Show(new OptionsScreen(Shell))));
        col.Children.Add(new MenuButton("KONTROL", () => Shell.Show(new ControlsScreen(Shell))));
        col.Children.Add(new MenuButton("KELUAR KE MENU", () => Shell.Show(new ConfirmScreen(Shell, "Keluar ke menu utama?", "Petualangan tersimpan di awal level ini. Kamu bisa melanjutkannya nanti.", Shell.QuitToTitle)),
            color: Color.Parse("#FF9A7A")));
        Border panel = Kit.Panel(col, 26);
        panel.Width = 520;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(panel);
    }

    public override MenuView? StageView => null;

    public override void OnBack() => Shell.ResumeGame();
}

public enum ResultKind
{
    Won,
    Retry,
    GameOver,
}

/// <summary>After a level: tally on a win, the retry count or game over on a loss.</summary>
public sealed class ResultScreen : Screen
{
    private readonly List<(TextBlock Label, int Target)> _counters = [];
    private float _time;

    public ResultScreen(IShell shell, ResultKind kind, GameSession session, int rank, int stars) : base(shell)
    {
        Campaign run = shell.Run!;
        Children.Add(Backdrop(kind == ResultKind.Won ? (byte)120 : (byte)170));
        StackPanel col = Column(8);
        switch (kind)
        {
            case ResultKind.Won:
                col.Children.Add(Kit.Title("KAMPUNG AMAN!", 50, Kit.Sun));
                col.Children.Add(StarsRow(stars));
                break;
            case ResultKind.Retry:
                col.Children.Add(Kit.Title("NYAWA HABIS!", 50, Color.Parse("#FF7A5A")));
                col.Children.Add(Centered(Kit.Text($"Zombi menguasai {session.Level.Def.Name}... tapi pahlawan tidak menyerah!", 17, Kit.InkBrush, FontWeight.Bold)));
                col.Children.Add(Centered(Kit.Chip($"Kesempatan mengulang tersisa: {run.RetriesLeft} dari {Campaign.MaxRetries}", Kit.Sun, null, 16)));
                break;
            default:
                col.Children.Add(Kit.Title("GAME OVER", 56, Color.Parse("#FF5A4A")));
                col.Children.Add(Centered(Kit.Text("Semua kesempatan habis. Petualangan dimulai lagi dari Level 1.", 17, Kit.InkBrush, FontWeight.Bold)));
                break;
        }

        Grid table = new() { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(10, 8) };
        void Row(string label, int value, bool big = false)
        {
            int r = table.RowDefinitions.Count;
            table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            TextBlock l = Kit.Text(label, big ? 22 : 17, Kit.InkBrush, big ? FontWeight.Black : FontWeight.Bold);
            TextBlock v = Kit.Text("0", big ? 26 : 18, big ? Kit.OrangeBrush : Kit.InkBrush, FontWeight.Black);
            v.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetRow(l, r);
            Grid.SetRow(v, r);
            Grid.SetColumn(v, 1);
            table.Children.Add(l);
            table.Children.Add(v);
            _counters.Add((v, value));
        }

        Row("Zombi dikalahkan", session.Kills);
        Row("Kombo terbaik", session.BestCombo);
        TextBlock time = Kit.Text($"Waktu bertahan: {TimeSpan.FromSeconds(session.PlayTime):mm\\:ss}", 15, Kit.MutedBrush, FontWeight.Bold);
        if (kind == ResultKind.Won)
        {
            Row("Bonus waktu", session.TimeBonus);
            Row("Bonus darah", session.HealthBonus);
            Row("Bonus nyawa", session.LivesBonus);
            Row("Skor level", session.Score, true);
            Row("Total skor", run.TotalScore, true);
        }
        else
        {
            Row("Skor level", session.Score, true);
        }

        col.Children.Add(table);
        col.Children.Add(Centered(time));
        if (rank > 0)
        {
            col.Children.Add(Centered(Kit.Chip($"MASUK TOP SKOR {session.Level.Def.Name.ToUpperInvariant()} - PERINGKAT #{rank}!", Kit.Zombie, null, 15)));
        }

        Grid nav = new() { ColumnDefinitions = new ColumnDefinitions("*,14,*"), Margin = new Thickness(0, 8, 0, 0) };
        nav.Children.Add(new MenuButton("MENU UTAMA", () => Shell.QuitToTitle(), color: Kit.PaperDark));
        MenuButton next = kind switch
        {
            ResultKind.Won when session.Level.Def.Number >= Campaign.PlayableLevels => new MenuButton("LANJUT", () => Shell.Replace(new EndingScreen(Shell)), color: Kit.Zombie),
            ResultKind.Won => new MenuButton("LANJUT", () => Shell.Replace(new LevelMapScreen(Shell)), color: Kit.Zombie),
            ResultKind.Retry => new MenuButton("ULANGI LEVEL", () => Shell.PlayLevel(), color: Kit.Sun),
            _ => new MenuButton("MULAI LAGI", () => Shell.PlayLevel(), "dari Level 1", color: Kit.Sun),
        };
        Grid.SetColumn(next, 2);
        nav.Children.Add(next);
        col.Children.Add(nav);
        Border panel = Kit.Panel(col, 26);
        panel.Width = 600;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(panel);
    }

    public override MenuView? StageView => null;

    public override void OnShown()
    {
        base.OnShown();
        Buttons().LastOrDefault()?.Focus();
    }

    public override void OnBack() => Shell.QuitToTitle();

    public override void Tick(float dt)
    {
        _time += dt;
        for (int i = 0; i < _counters.Count; i++)
        {
            float t = Math.Clamp((_time - 0.3f - (i * 0.25f)) / 0.7f, 0f, 1f);
            (TextBlock label, int target) = _counters[i];
            label.Text = ((int)(target * t)).ToString("N0");
        }
    }

    private static Control Centered(Control c)
    {
        c.HorizontalAlignment = HorizontalAlignment.Center;
        if (c is TextBlock t)
        {
            t.TextAlignment = TextAlignment.Center;
        }

        return c;
    }

    private static Control StarsRow(int stars)
    {
        StackPanel row = new() { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
        for (int i = 0; i < 3; i++)
        {
            OutlinedText star = Kit.Title("★", 54, i < stars ? Kit.Sun : Kit.PaperDark);
            row.Children.Add(star);
        }

        return row;
    }
}

/// <summary>After level 4: the story so far is over, levels 5-10 are coming.</summary>
public sealed class EndingScreen : Screen
{
    public EndingScreen(IShell shell) : base(shell)
    {
        Campaign run = shell.Run!;
        Children.Add(Backdrop(120));
        StackPanel col = Column(10);
        col.Children.Add(Kit.Title("SELAMAT, PAHLAWAN!", 46));
        TextBlock story = Kit.Text(
            $"{run.PlayerName} dan keluarga menembus Zona Terlarang dan mengalahkan Demon King Abyss di Candi Terlarang. " +
            "Pusaran kutukan lenyap, para zombi kembali tenang, dan Kampung Damai akhirnya benar-benar damai!", 18, Kit.InkBrush, FontWeight.Bold);
        story.TextAlignment = TextAlignment.Center;
        col.Children.Add(story);
        StackPanel chips = new() { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
        chips.Children.Add(Kit.Chip($"TOTAL SKOR {run.TotalScore:N0}", Kit.Sun, null, 18));
        chips.Children.Add(Kit.Chip($"{run.TotalKills} ZOMBI", Kit.Zombie, null, 18));
        chips.Children.Add(Kit.Chip(run.DifficultyDef.Name.ToUpperInvariant(), Kit.Rgb(run.DifficultyDef.Color), null, 18));
        col.Children.Add(chips);
        OutlinedText soon = Kit.Title("TAMAT - KAMU PAHLAWAN KAMPUNG DAMAI!", 26, Kit.Zombie);
        soon.Margin = new Thickness(0, 8);
        col.Children.Add(soon);
        Grid nav = new() { ColumnDefinitions = new ColumnDefinitions("*,14,*") };
        nav.Children.Add(new MenuButton("MENU UTAMA", () => Shell.QuitToTitle(), color: Kit.PaperDark));
        MenuButton credits = new("LIHAT KREDIT", () => Shell.Replace(new AboutScreen(Shell, fromEnding: true)), color: Kit.Zombie);
        Grid.SetColumn(credits, 2);
        nav.Children.Add(credits);
        col.Children.Add(nav);
        Border panel = Kit.Panel(col, 30);
        panel.Width = 680;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(panel);
    }

    public override MenuView? StageView => MenuView.Family;

    public override void OnShown()
    {
        base.OnShown();
        Shell.Menu?.Select(Math.Max(0, Array.IndexOf(MenuStage.Order, Shell.Run?.Character)));
        Buttons().LastOrDefault()?.Focus();
    }

    public override void OnBack() => Shell.QuitToTitle();
}
