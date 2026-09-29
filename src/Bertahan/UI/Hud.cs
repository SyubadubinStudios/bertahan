using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Bertahan.Game;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using Vector = Avalonia.Vector;

namespace Bertahan.UI;

/// <summary>
/// The in-game HUD, drawn directly every frame: health, lives, weapons,
/// score and combo, waves, boss bar, minimap, damage numbers and banners.
/// Laid out on a 1280x720 design grid scaled to the window.
/// </summary>
public sealed class Hud : Control
{
    private static readonly Typeface Face = new(Kit.Font, FontStyle.Normal, FontWeight.Black);
    private static readonly Typeface FaceBold = new(Kit.Font, FontStyle.Normal, FontWeight.Bold);
    private static readonly IBrush Ink = Kit.InkBrush;
    private readonly Dictionary<(string, double, bool), (Geometry Geometry, Size Size)> _textCache = [];
    private WriteableBitmap? _minimap;
    private Level? _minimapLevel;

    public GameSession? Session { get; set; }

    public Campaign? Run { get; set; }

    public Vector2 Mouse { get; set; }

    public bool MouseValid { get; set; }

    public double Fps { get; set; }

    public bool ShowFps { get; set; }

    /// <summary>Hides banners and the crosshair (e.g. while a menu covers the game).</summary>
    public bool Quiet { get; set; }

    public void Refresh() => InvalidateVisual();

    public override void Render(DrawingContext ctx)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (ShowFps)
        {
            Text(ctx, $"{Fps:0} FPS", w - 8, h - 22, 14, Colors.White, Kit.Ink, 1, false);
        }

        GameSession? s = Session;
        if (s is null || w < 10)
        {
            return;
        }

        Vignette(ctx, s, w, h);
        WorldLabels(ctx, s);

        double k = Math.Min(w / 1280, h / 720);
        using (ctx.PushTransform(Matrix.CreateScale(k, k)))
        {
            double W = w / k, H = h / k;
            PlayerPanel(ctx, s);
            WavePanel(ctx, s, W);
            BossBar(ctx, s, W);
            ScorePanel(ctx, s, W);
            Minimap(ctx, s, W);
            Weapons(ctx, s, W, H);
            if (!Quiet)
            {
                Banner(ctx, s, W, H);
                Downed(ctx, s, W, H);
                Hints(ctx, s, H);
            }
        }

        if (!Quiet && MouseValid && s.Player.Alive && s.State == SessionState.Playing)
        {
            Crosshair(ctx, s);
        }
    }

    // ------------------------------------------------------------------ drawing helpers

    private (Geometry Geometry, Size Size) Glyphs(string text, double size, bool bold)
    {
        if (_textCache.TryGetValue((text, size, bold), out var cached))
        {
            return cached;
        }

        if (_textCache.Count > 600)
        {
            _textCache.Clear();
        }

        FormattedText ft = new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, bold ? FaceBold : Face, size, Brushes.White);
        Geometry g = ft.BuildGeometry(new Point(0, 0)) ?? new StreamGeometry();
        var entry = (g, new Size(ft.WidthIncludingTrailingWhitespace, ft.Height));
        _textCache[(text, size, bold)] = entry;
        return entry;
    }

    /// <summary>Outlined text. align: 0 left, 0.5 centre, 1 right.</summary>
    private Size Text(DrawingContext ctx, string text, double x, double y, double size, Color fill, Color stroke, double align = 0, bool outline = true, double alpha = 1, double scale = 1, bool bold = false)
    {
        if (string.IsNullOrEmpty(text) || alpha <= 0.01)
        {
            return default;
        }

        (Geometry g, Size sz) = Glyphs(text, size, bold);
        double ox = x - (sz.Width * align * scale), oy = y;
        using (ctx.PushOpacity(alpha))
        using (ctx.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(ox, oy)))
        {
            if (outline)
            {
                Pen pen = new(new SolidColorBrush(stroke), Math.Max(3, size / 6), lineJoin: PenLineJoin.Round);
                using (ctx.PushTransform(Matrix.CreateTranslation(0, size / 14)))
                {
                    ctx.DrawGeometry(new SolidColorBrush(stroke), pen, g);
                }

                ctx.DrawGeometry(null, pen, g);
            }

            ctx.DrawGeometry(new SolidColorBrush(fill), null, g);
        }

        return sz * scale;
    }

    private static void Bar(DrawingContext ctx, Rect r, double fraction, Color color, Color? back = null, double radius = -1)
    {
        double rad = radius >= 0 ? radius : r.Height / 2;
        ctx.DrawRectangle(new SolidColorBrush(back ?? Color.FromArgb(220, 58, 36, 18)), null, r, rad, rad);
        fraction = Math.Clamp(fraction, 0, 1);
        if (fraction > 0.001)
        {
            Rect fill = new(r.X + 3, r.Y + 3, Math.Max(0, (r.Width - 6) * fraction), r.Height - 6);
            ctx.DrawRectangle(new SolidColorBrush(color), null, fill, rad - 2, rad - 2);
            // glossy top half
            ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), null, new Rect(fill.X + 2, fill.Y + 1, Math.Max(0, fill.Width - 4), fill.Height * 0.4), rad / 2, rad / 2);
        }

        ctx.DrawRectangle(null, new Pen(Ink, 3), r, rad, rad);
    }

    private static void Paper(DrawingContext ctx, Rect r, double radius = 14, byte alpha = 235)
    {
        ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(110, 58, 36, 18)), null, r.Translate(new Vector(0, 5)), radius, radius);
        ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(alpha, 255, 244, 220)), new Pen(Ink, 3), r, radius, radius);
    }

    private static void Picture(DrawingContext ctx, string name, Rect r, double opacity = 1)
    {
        if (Assets.Image(name) is { } bmp)
        {
            using (ctx.PushOpacity(opacity))
            {
                ctx.DrawImage(bmp, new Rect(bmp.Size), r);
            }
        }
    }

    // ------------------------------------------------------------------ panels

    private void PlayerPanel(DrawingContext ctx, GameSession s)
    {
        Player p = s.Player;
        Paper(ctx, new Rect(14, 14, 340, 104), 20);
        EllipseGeometry circle = new(new Rect(22, 20, 92, 92));
        ctx.DrawGeometry(new SolidColorBrush(Kit.Sun), new Pen(Ink, 3), circle);
        using (ctx.PushGeometryClip(circle))
        {
            Picture(ctx, "portrait_" + p.Def.Id, new Rect(18, 18, 100, 100));
        }

        ctx.DrawGeometry(null, new Pen(Ink, 4), circle);
        string name = Run?.PlayerName ?? p.Def.Name;
        Text(ctx, name, 124, 20, 18, Kit.Ink, Kit.Ink, outline: false);
        double hp = p.Health / p.MaxHealth;
        Color hpColor = hp > 0.5 ? Color.Parse("#5DD13A") : hp > 0.25 ? Color.Parse("#FFC53A") : Color.Parse("#F0483A");
        if (p.Invulnerable && p.Alive && (Environment.TickCount64 / 90) % 2 == 0)
        {
            hpColor = Colors.White;
        }

        Bar(ctx, new Rect(124, 46, 218, 24), hp, hpColor);
        Text(ctx, $"{Math.Ceiling(p.Health)}/{p.MaxHealth:0}", 233, 48, 13, Colors.White, Kit.Ink, 0.5);
        Bar(ctx, new Rect(124, 74, 218, 14), p.Stamina / 100.0, Color.Parse("#3AB8F0"));

        // lives as hearts
        int lives = s.Lives;
        for (int i = 0; i < Math.Max(lives, s.Difficulty.Lives); i++)
        {
            bool full = i < lives;
            Text(ctx, "♥", 126 + (i * 22), 90, 20, full ? Color.Parse("#FF4A5A") : Color.Parse("#C9B79A"), Kit.Ink);
        }

        if (Run is { } run)
        {
            Text(ctx, $"Ulang {run.RetriesLeft}", 342, 94, 13, Kit.Muted, Kit.Ink, 1, false, bold: true);
        }
    }

    private void WavePanel(DrawingContext ctx, GameSession s, double W)
    {
        WaveDirector waves = s.Waves;
        double cx = W / 2;
        Paper(ctx, new Rect(cx - 150, 12, 300, 62), 18);
        string title = waves.Phase == WavePhase.Victory ? "MENANG!" : waves.IsBossWave ? "GELOMBANG BOS" : $"GELOMBANG {Math.Min(waves.Wave + 1, waves.WaveCount)}/{waves.WaveCount}";
        Text(ctx, title, cx, 16, 22, waves.IsBossWave ? Color.Parse("#FF6A4A") : Kit.Sun, Kit.Ink, 0.5);
        string sub = waves.Phase switch
        {
            WavePhase.Intro => s.Level.Def.Name,
            WavePhase.Countdown => $"Zombi datang dalam {Math.Ceiling(waves.Timer)}...",
            WavePhase.Fighting => $"Sisa zombi: {waves.Remaining}",
            WavePhase.Cleared => "Istirahat sebentar!",
            _ => "Kampung aman!",
        };
        Text(ctx, sub, cx, 47, 15, Kit.Ink, Kit.Ink, 0.5, false, bold: true);
        if (waves.Phase == WavePhase.Countdown && !Quiet)
        {
            int n = (int)Math.Ceiling(waves.Timer);
            double frac = waves.Timer - Math.Floor(waves.Timer);
            Text(ctx, n.ToString(), cx, 250, 96, Kit.Sun, Kit.Ink, 0.5, alpha: Math.Clamp(frac * 2.5, 0, 1), scale: 0.8 + (frac * 0.5));
        }
    }

    private void BossBar(DrawingContext ctx, GameSession s, double W)
    {
        if (s.Boss is not { } boss)
        {
            return;
        }

        double cx = W / 2;
        Text(ctx, boss.Def.Name.ToUpperInvariant(), cx, 82, 20, Color.Parse("#B8FF6A"), Kit.Ink, 0.5);
        Bar(ctx, new Rect(cx - 280, 110, 560, 24), boss.Health / boss.MaxHealth, Color.Parse("#9A3AD8"), radius: 8);
        Picture(ctx, "zombie_" + boss.Def.Id, new Rect(cx - 322, 90, 56, 56));
    }

    private void ScorePanel(DrawingContext ctx, GameSession s, double W)
    {
        Paper(ctx, new Rect(W - 246, 14, 232, 72), 18);
        Text(ctx, "SKOR", W - 234, 18, 13, Kit.Muted, Kit.Ink, outline: false, bold: true);
        Text(ctx, s.Score.ToString("N0"), W - 26, 28, 34, Kit.Sun, Kit.Ink, 1);
        if (Run is { TotalScore: > 0 } run)
        {
            // once a level is won its score is already in the run total
            int total = run.TotalScore + (s.State == SessionState.Won ? 0 : s.Score);
            Text(ctx, $"Total {total:N0}", W - 234, 64, 12, Kit.Muted, Kit.Ink, outline: false, bold: true);
        }

        if (s.Combo >= 2)
        {
            int mult = 1 + Math.Min(s.Combo / 5, 4);
            double pulse = 1 + (Math.Max(0, s.ComboTimeLeft - 0.85) * 1.5);
            Text(ctx, $"KOMBO {s.Combo}", W - 30, 92, 24, Color.Parse("#FF8A3A"), Kit.Ink, 1, scale: pulse);
            if (mult > 1)
            {
                Text(ctx, $"x{mult}", W - 170, 94, 22, Kit.Zombie, Kit.Ink, 1);
            }

            Bar(ctx, new Rect(W - 226, 124, 196, 10), s.ComboTimeLeft, Color.Parse("#FF8A3A"));
        }
    }

    private void Minimap(DrawingContext ctx, GameSession s, double W)
    {
        Level level = s.Level;
        if (level.MinimapPixels.Length == 0)
        {
            return;
        }

        if (_minimapLevel != level)
        {
            _minimapLevel = level;
            _minimap?.Dispose();
            _minimap = new WriteableBitmap(new PixelSize(level.MinimapSize, level.MinimapSize), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using ILockedFramebuffer fb = _minimap.Lock();
            for (int y = 0; y < level.MinimapSize; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(level.MinimapPixels, y * level.MinimapSize * 4, fb.Address + (y * fb.RowBytes), level.MinimapSize * 4);
            }
        }

        const double size = 150;
        Rect r = new(W - size - 16, 146, size, size);
        ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(110, 58, 36, 18)), null, r.Translate(new Vector(0, 5)), 16, 16);
        RoundedRect clip = new(r, 16);
        using (ctx.PushClip(clip))
        {
            // show the arena (-38..38) of the 96 m painted ground
            const float world = 96f, arena = 80f;
            double crop = level.MinimapSize * (arena / world);
            double off = (level.MinimapSize - crop) / 2;
            ctx.DrawImage(_minimap!, new Rect(off, off, crop, crop), r);
            Point Map(Vector2 p) => new(r.X + (((p.X / arena) + 0.5) * size), r.Y + (((p.Y / arena) + 0.5) * size));
            foreach ((PickupKind kind, Vector2 pos) in s.Pickups.All)
            {
                ctx.DrawEllipse(new SolidColorBrush(kind == PickupKind.Weapon ? Kit.Sun : Color.Parse("#7CFF7A")), new Pen(Ink, 1.5), Map(pos), 3.5, 3.5);
            }

            foreach (Zombie z in s.Zombies)
            {
                if (z.Active && z.State != ZombieState.Dying)
                {
                    double rad = z.IsBoss ? 6 : 3;
                    ctx.DrawEllipse(new SolidColorBrush(z.IsBoss ? Color.Parse("#B84AFF") : Color.Parse("#FF4A3A")), new Pen(Ink, 1.2), Map(z.Position), rad, rad);
                }
            }

            // player arrow, rotated with their facing
            Point pp = Map(s.Player.Position);
            StreamGeometry arrow = new();
            using (StreamGeometryContext g = arrow.Open())
            {
                g.BeginFigure(new Point(0, 8), true);
                g.LineTo(new Point(5.5, -5));
                g.LineTo(new Point(0, -2));
                g.LineTo(new Point(-5.5, -5));
                g.EndFigure(true);
            }

            double yaw = s.Player.Yaw;
            using (ctx.PushTransform(Matrix.CreateRotation(-yaw) * Matrix.CreateTranslation(pp.X, pp.Y)))
            {
                ctx.DrawGeometry(new SolidColorBrush(Kit.Sun), new Pen(Ink, 1.5), arrow);
            }
        }

        ctx.DrawRectangle(null, new Pen(Ink, 3), r, 16, 16);
        Text(ctx, s.Level.Def.Name, r.Center.X, r.Bottom + 4, 13, Colors.White, Kit.Ink, 0.5);
    }

    private void Weapons(DrawingContext ctx, GameSession s, double W, double H)
    {
        Player p = s.Player;
        int n = p.Inventory.Count;
        const double slot = 64, gap = 8;
        double total = (n * (slot + gap)) + 90;
        double x = (W - total) / 2, y = H - slot - 22;
        for (int i = 0; i < n; i++)
        {
            InventoryItem item = p.Inventory[i];
            bool current = i == p.Current;
            Rect r = new(x, y - (current ? 8 : 0), slot, slot);
            ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(110, 58, 36, 18)), null, r.Translate(new Vector(0, 4)), 14, 14);
            ctx.DrawRectangle(new SolidColorBrush(current ? Kit.Sun : Color.FromArgb(225, 255, 244, 220)), new Pen(Ink, current ? 4 : 3), r, 14, 14);
            Picture(ctx, "weapon_" + item.Weapon.Id, r.Deflate(6), current ? 1 : 0.75);
            Text(ctx, (i + 1).ToString(), r.X + 5, r.Y + 2, 14, Colors.White, Kit.Ink);
            if (item.Weapon.Kind == WeaponKind.Gun)
            {
                string ammo = p.Reloading && current ? "..." : $"{item.Loaded}/{p.AmmoReserve}";
                Text(ctx, ammo, r.Center.X, r.Bottom - 18, 13, item.Loaded == 0 ? Color.Parse("#FF6A5A") : Colors.White, Kit.Ink, 0.5);
                if (current && p.Reloading)
                {
                    Bar(ctx, new Rect(r.X + 4, r.Y - 14, slot - 8, 10), p.ReloadProgress, Kit.Zombie);
                }
            }

            x += slot + gap;
        }

        // molotov stock
        Rect m = new(x + 14, y, slot, slot);
        ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(225, 255, 244, 220)), new Pen(Ink, 3), m, 14, 14);
        Picture(ctx, "weapon_molotov", m.Deflate(8), p.Molotovs > 0 ? 1 : 0.35);
        Text(ctx, $"x{p.Molotovs}", m.Right - 4, m.Bottom - 20, 16, p.Molotovs > 0 ? Kit.Sun : Color.Parse("#C9B79A"), Kit.Ink, 1);
        Text(ctx, "G", m.X + 5, m.Y + 2, 14, Colors.White, Kit.Ink);
        string name = p.Weapon.Weapon.Name;
        Text(ctx, name, W / 2, y - 36, 16, Colors.White, Kit.Ink, 0.5);

        if (s.Message is { } msg && !Quiet)
        {
            (Geometry _, Size sz) = Glyphs(msg, 18, true);
            Rect box = new((W - sz.Width) / 2 - 16, y - 84, sz.Width + 32, 36);
            using (ctx.PushOpacity(s.MessageAlpha))
            {
                Paper(ctx, box, 14);
            }

            Text(ctx, msg, W / 2, box.Y + 6, 18, Kit.Ink, Kit.Ink, 0.5, false, s.MessageAlpha, bold: true);
        }
    }

    private void Banner(DrawingContext ctx, GameSession s, double W, double H)
    {
        if (s.Banner is not { } b)
        {
            return;
        }

        double age = 3.2 - b.Time;
        double alpha = Math.Clamp(b.Time / 0.4, 0, 1) * Math.Clamp(age / 0.15, 0, 1);
        double u = Math.Min(age / 0.35, 1) - 1;
        double scale = 0.4 + (0.6 * (1 + (2.7 * u * u * u) + (1.7 * u * u)));
        Text(ctx, b.Title, W / 2, H * 0.24, 52, Kit.Sun, Kit.Ink, 0.5, alpha: alpha, scale: scale);
        Text(ctx, b.Subtitle, W / 2, (H * 0.24) + 72, 20, Colors.White, Kit.Ink, 0.5, alpha: alpha * Math.Clamp((age - 0.25) / 0.3, 0, 1));
    }

    private void Downed(DrawingContext ctx, GameSession s, double W, double H)
    {
        if (s.Player.Alive || s.State != SessionState.Playing)
        {
            return;
        }

        Text(ctx, "PINGSAN!", W / 2, H * 0.42, 60, Color.Parse("#FF6A5A"), Kit.Ink, 0.5);
        Text(ctx, $"Bangkit lagi sebentar... (sisa nyawa {s.Lives - 1})", W / 2, (H * 0.42) + 76, 20, Colors.White, Kit.Ink, 0.5);
    }

    private void Hints(DrawingContext ctx, GameSession s, double H)
    {
        if (s.PlayTime > 14 || s.Level.Def.Number != 1)
        {
            return;
        }

        double alpha = Math.Clamp((14 - s.PlayTime) / 1.5, 0, 1);
        using (ctx.PushOpacity(alpha))
        {
            Paper(ctx, new Rect(14, H - 196, 270, 182), 16);
            string[] lines = ["WASD  bergerak", "Mouse  bidik", "Klik kiri  serang", "Spasi  berguling", "Shift  lari", "G / klik kanan  molotov", "1-9  ganti senjata", "Esc  istirahat"];
            for (int i = 0; i < lines.Length; i++)
            {
                Text(ctx, lines[i], 28, H - 188 + (i * 21), 15, Kit.Ink, Kit.Ink, outline: false, bold: true);
            }
        }
    }

    // ------------------------------------------------------------------ world space

    private void WorldLabels(DrawingContext ctx, GameSession s)
    {
        CameraRig cam = s.Camera;
        double k = Math.Min(Bounds.Width / 1280, Bounds.Height / 720);
        foreach (Zombie z in s.Zombies)
        {
            if (!z.Targetable || z.IsBoss || z.Health >= z.MaxHealth || z.Health <= 0)
            {
                continue;
            }

            if (cam.Project(z.World + new Vector3(0, z.Height + 0.45f, 0), out Vector2 p))
            {
                double bw = 44 * k, bh = 9 * k;
                Bar(ctx, new Rect(p.X - (bw / 2), p.Y, bw, bh), z.Health / z.MaxHealth, Color.Parse("#FF5A3A"));
            }
        }

        foreach (FloatingText t in s.Texts)
        {
            Vector3 at = t.World + new Vector3(0, t.Age * 1.3f, 0);
            if (!cam.Project(at, out Vector2 p))
            {
                continue;
            }

            double pop = t.Age < 0.12 ? 1.4 - (t.Age / 0.12 * 0.4) : 1;
            double alpha = Math.Clamp((1.1 - t.Age) / 0.35, 0, 1);
            Color c = Color.FromRgb((byte)(t.Color >> 16), (byte)(t.Color >> 8), (byte)t.Color);
            Text(ctx, t.Text, p.X, p.Y, t.Size * k, c, Kit.Ink, 0.5, alpha: alpha, scale: pop);
        }
    }

    private void Crosshair(DrawingContext ctx, GameSession s)
    {
        Point m = new(Mouse.X, Mouse.Y);
        bool gun = s.Player.Weapon.Weapon.Kind == WeaponKind.Gun;
        double r = gun ? 14 : 10;
        Pen outline = new(Ink, 5);
        Pen inner = new(new SolidColorBrush(gun ? Color.Parse("#FF6A4A") : Colors.White), 2.5);
        ctx.DrawEllipse(null, outline, m, r, r);
        ctx.DrawEllipse(null, inner, m, r, r);
        ctx.DrawEllipse(Brushes.White, new Pen(Ink, 1.5), m, 2.5, 2.5);
        if (gun)
        {
            foreach ((double dx, double dy) in new[] { (1.0, 0.0), (-1.0, 0.0), (0.0, 1.0), (0.0, -1.0) })
            {
                Point a = new(m.X + (dx * (r + 3)), m.Y + (dy * (r + 3))), b = new(m.X + (dx * (r + 10)), m.Y + (dy * (r + 10)));
                ctx.DrawLine(outline, a, b);
                ctx.DrawLine(inner, a, b);
            }
        }
    }

    private static void Vignette(DrawingContext ctx, GameSession s, double w, double h)
    {
        Player p = s.Player;
        double low = p.Alive && p.Health / p.MaxHealth < 0.3 ? 0.35 + (0.2 * Math.Sin(Environment.TickCount64 / 180.0)) : 0;
        double amount = Math.Max(p.HurtFlash * 0.8, low);
        if (!p.Alive)
        {
            amount = 0.7;
        }

        if (amount <= 0.01)
        {
            return;
        }

        RadialGradientBrush brush = new()
        {
            GradientStops =
            [
                new GradientStop(Color.FromArgb(0, 200, 20, 20), 0.55),
                new GradientStop(Color.FromArgb((byte)(amount * 220), 170, 10, 10), 1),
            ],
            RadiusX = RelativeScalar.Parse("75%"),
            RadiusY = RelativeScalar.Parse("75%"),
        };
        ctx.DrawRectangle(brush, null, new Rect(0, 0, w, h));
    }
}
