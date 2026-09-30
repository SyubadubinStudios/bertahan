using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Bertahan.UI;

/// <summary>
/// The look of Bertahan's UI: cream paper panels with thick brown ink
/// outlines, sticker-like titles and sunny yellow buttons, like a village
/// notice board with comic lettering.
/// </summary>
public static class Kit
{
    public static readonly Color Ink = Color.Parse("#3A2412");
    public static readonly Color Paper = Color.Parse("#FFF4DC");
    public static readonly Color PaperDark = Color.Parse("#F3DDB0");
    public static readonly Color Sun = Color.Parse("#FFD23F");
    public static readonly Color Orange = Color.Parse("#FF8A1F");
    public static readonly Color Zombie = Color.Parse("#7CD13A");
    public static readonly Color Blood = Color.Parse("#E8443A");
    public static readonly Color Teal = Color.Parse("#2BB3A3");
    public static readonly Color Night = Color.Parse("#1B1530");
    public static readonly Color Muted = Color.Parse("#8A6E52");

    public static readonly IBrush InkBrush = new SolidColorBrush(Ink);
    public static readonly IBrush PaperBrush = new SolidColorBrush(Paper);
    public static readonly IBrush PaperDarkBrush = new SolidColorBrush(PaperDark);
    public static readonly IBrush SunBrush = new SolidColorBrush(Sun);
    public static readonly IBrush OrangeBrush = new SolidColorBrush(Orange);
    public static readonly IBrush ZombieBrush = new SolidColorBrush(Zombie);
    public static readonly IBrush BloodBrush = new SolidColorBrush(Blood);
    public static readonly IBrush TealBrush = new SolidColorBrush(Teal);
    public static readonly IBrush MutedBrush = new SolidColorBrush(Muted);
    public static readonly IBrush White = Brushes.White;

    public static readonly FontFamily Font = new("fonts:Inter#Inter, $Default");

    public static readonly BoxShadows PanelShadow = BoxShadows.Parse("0 8 0 0 #663A2412");

    public static IBrush Brush(uint rgb, byte alpha = 255) =>
        new SolidColorBrush(Color.FromArgb(alpha, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));

    public static Color Rgb(uint rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    /// <summary>A cream paper card with an ink outline and a hard drop shadow.</summary>
    public static Border Panel(Control child, double padding = 24, IBrush? background = null) => new()
    {
        Background = background ?? PaperBrush,
        BorderBrush = InkBrush,
        BorderThickness = new Thickness(4),
        CornerRadius = new CornerRadius(22),
        Padding = new Thickness(padding),
        BoxShadow = PanelShadow,
        Child = child,
    };

    public static TextBlock Text(string text, double size = 18, IBrush? color = null, FontWeight weight = FontWeight.SemiBold, TextWrapping wrap = TextWrapping.Wrap) => new()
    {
        Text = text,
        FontSize = size,
        FontFamily = Font,
        FontWeight = weight,
        Foreground = color ?? InkBrush,
        TextWrapping = wrap,
    };

    /// <summary>Sticker lettering: bold fill, fat ink outline.</summary>
    public static OutlinedText Title(string text, double size = 44, Color? fill = null, Color? stroke = null, double outline = 0) => new()
    {
        Text = text,
        FontSize = size,
        Fill = fill ?? Sun,
        Stroke = stroke ?? Ink,
        StrokeThickness = outline > 0 ? outline : Math.Max(3, size / 9),
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    public static Image Picture(string name, double width, double height = double.NaN) => new()
    {
        Source = Assets.Image(name),
        Width = width,
        Height = height,
        Stretch = Stretch.Uniform,
    };

    /// <summary>A small rounded tag, e.g. "DI SINI".</summary>
    public static Border Chip(string text, Color background, IBrush? foreground = null, double size = 13) => new()
    {
        Background = new SolidColorBrush(background),
        BorderBrush = InkBrush,
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(8, 2),
        HorizontalAlignment = HorizontalAlignment.Left,
        Child = Text(text, size, foreground ?? InkBrush, FontWeight.Black, TextWrapping.NoWrap),
    };
}

/// <summary>Bitmaps from Assets/UI, loaded once.</summary>
public static class Assets
{
    private static readonly Dictionary<string, Bitmap?> Cache = [];

    public static Bitmap? Image(string name)
    {
        if (Cache.TryGetValue(name, out Bitmap? bitmap))
        {
            return bitmap;
        }

        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "UI", name + ".png");
        try
        {
            bitmap = File.Exists(path) ? new Bitmap(path) : null;
        }
        catch (Exception)
        {
            bitmap = null;
        }

        Cache[name] = bitmap;
        return bitmap;
    }
}
