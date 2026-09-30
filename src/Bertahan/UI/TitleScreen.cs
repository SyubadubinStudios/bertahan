using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bertahan.Game;

namespace Bertahan.UI;

/// <summary>Main menu over the living village.</summary>
public sealed class TitleScreen : Screen
{
    private static readonly string[] News =
    [
        "Kentongan: zombi warga terlihat mondar-mandir di dekat warung Bu Sri!",
        "Pengumuman: ronda malam ini dijaga keluarga pemberani.",
        "Awas! Tuyul zombi suka mencuri sandal di depan masjid.",
        "Kabar sawah: orang-orangan sawah tidak mempan menakuti pocong.",
        "Tips: wajan Ibu membuat zombi pusing tujuh keliling.",
        "Info: nasi bungkus dan jamu memulihkan darah pahlawan.",
    ];

    private readonly TextBlock _news;
    private readonly Border _newsBox;
    private float _newsTime;
    private int _newsIndex;

    public TitleScreen(IShell shell) : base(shell)
    {
        StackPanel menu = Column();
        menu.Children.Add(new MenuButton("MULAI BARU", () => Shell.Show(new NameScreen(Shell))));
        if (Shell.Settings.SavedRun is { } saved)
        {
            menu.Children.Add(new MenuButton("LANJUTKAN", () => Shell.StartRun(saved),
                $"{saved.PlayerName} - Level {saved.Level} - {saved.DifficultyDef.Name}", color: Kit.Zombie));
        }

        menu.Children.Add(new MenuButton("TOP SKOR", () => Shell.Show(new TopScoreScreen(Shell))));
        menu.Children.Add(new MenuButton("PILIHAN", () => Shell.Show(new OptionsScreen(Shell))));
        menu.Children.Add(new MenuButton("CERITA", () => Shell.PlayOpening(), color: Color.Parse("#FFC06A")));
        menu.Children.Add(new MenuButton("TENTANG", () => Shell.Show(new AboutScreen(Shell))));
        menu.Children.Add(new MenuButton("KELUAR", () => Shell.Show(new ConfirmScreen(Shell, "Mau keluar dari game?", "Kampung Damai masih butuh pahlawan...", Shell.Exit)),
            color: Color.Parse("#FF9A7A")));

        StackPanel left = new()
        {
            Width = 380,
            Margin = new Thickness(48, 24, 0, 24),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
        };
        left.Children.Add(Kit.Picture("logo", 420));
        left.Children.Add(Kit.Panel(menu, 18));
        Children.Add(left);

        // village notice board ticker
        _news = Kit.Text(News[0], 16, Kit.InkBrush, FontWeight.Bold);
        StackPanel ticker = new() { Orientation = Orientation.Horizontal, Spacing = 10 };
        ticker.Children.Add(Kit.Chip("KABAR KAMPUNG", Kit.Blood, Kit.White, 12));
        ticker.Children.Add(_news);
        _newsBox = new Border
        {
            Background = Kit.PaperBrush,
            BorderBrush = Kit.InkBrush,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 36, 28),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MaxWidth = 720,
            BoxShadow = Kit.PanelShadow,
            Child = ticker,
            Transitions = [new Avalonia.Animation.DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(300) }],
        };
        Children.Add(_newsBox);

        TextBlock credit = Kit.Text($"Subadubin Studios  -  v{typeof(TitleScreen).Assembly.GetName().Version?.ToString(3)}", 13, Kit.White, FontWeight.Bold);
        credit.HorizontalAlignment = HorizontalAlignment.Right;
        credit.VerticalAlignment = VerticalAlignment.Top;
        credit.Margin = new Thickness(0, 16, 24, 0);
        credit.Effect = new DropShadowEffect { BlurRadius = 4, OffsetX = 0, OffsetY = 1, Color = Colors.Black, Opacity = 0.8 };
        Children.Add(credit);
    }

    public override void OnBack() => Shell.Show(new ConfirmScreen(Shell, "Mau keluar dari game?", "Kampung Damai masih butuh pahlawan...", Shell.Exit));

    public override void Tick(float dt)
    {
        _newsTime += dt;
        if (_newsTime > 6.5f)
        {
            _newsTime = 0f;
            _newsIndex = (_newsIndex + 1) % News.Length;
            _news.Text = News[_newsIndex];
        }

        _newsBox.Opacity = _newsTime is < 0.3f or > 6.2f ? 0.2 : 1;
    }
}

/// <summary>A yes / no question.</summary>
public sealed class ConfirmScreen : Screen
{
    public ConfirmScreen(IShell shell, string question, string detail, Action yes, string yesText = "YA", string noText = "TIDAK") : base(shell)
    {
        Children.Add(Backdrop(120));
        StackPanel col = Column(12);
        col.Children.Add(Kit.Title(question, 30, Kit.Sun));
        TextBlock sub = Kit.Text(detail, 17, Kit.InkBrush);
        sub.TextAlignment = TextAlignment.Center;
        col.Children.Add(sub);
        Grid buttons = new() { ColumnDefinitions = new ColumnDefinitions("*,16,*"), Margin = new Thickness(0, 8, 0, 0) };
        MenuButton no = new(noText, () => Shell.Back());
        MenuButton ok = new(yesText, yes, color: Color.Parse("#FF9A7A"));
        Grid.SetColumn(ok, 2);
        buttons.Children.Add(no);
        buttons.Children.Add(ok);
        col.Children.Add(buttons);
        Border panel = Kit.Panel(col, 28);
        panel.Width = 520;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(panel);
    }

    public override MenuView? StageView => null;
}
