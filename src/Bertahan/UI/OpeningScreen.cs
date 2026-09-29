using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Bertahan.Game;

namespace Bertahan.UI;

/// <summary>Cinema bars, the story captions typed out, fades, lightning and the logo at the end.</summary>
public sealed class OpeningScreen : Screen
{
    private readonly OpeningStage _stage;
    private readonly Action _done;
    private readonly TextBlock _caption;
    private readonly Border _captionBox;
    private readonly Border _fade;
    private readonly Border _flash;
    private readonly Image _logo;
    private readonly TextBlock _press;
    private string _shown = "";

    public OpeningScreen(IShell shell, OpeningStage stage, Action done) : base(shell)
    {
        _stage = stage;
        _done = done;
        Background = Brushes.Transparent;
        Children.Add(new Border { Height = 72, Background = Brushes.Black, VerticalAlignment = VerticalAlignment.Top });
        Children.Add(new Border { Height = 72, Background = Brushes.Black, VerticalAlignment = VerticalAlignment.Bottom });
        _caption = Kit.Text("", 24, Kit.InkBrush, FontWeight.Bold);
        _caption.TextAlignment = TextAlignment.Center;
        _captionBox = new Border
        {
            Background = Kit.Brush(0xFFF4DC, 240),
            BorderBrush = Kit.InkBrush,
            BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(24, 14),
            MaxWidth = 900,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 96),
            BoxShadow = Kit.PanelShadow,
            Child = _caption,
        };
        Children.Add(_captionBox);
        _logo = new Image
        {
            Source = Assets.Image("logo"),
            Width = 760,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 120),
            RenderTransformOrigin = RelativePoint.Center,
            Opacity = 0,
        };
        Children.Add(_logo);
        _flash = new Border { Background = Brushes.White, Opacity = 0, IsHitTestVisible = false };
        Children.Add(_flash);
        _fade = new Border { Background = Brushes.Black, Opacity = 1, IsHitTestVisible = false };
        Children.Add(_fade);
        _press = Kit.Text("Enter / Esc / klik: lewati", 14, Kit.White, FontWeight.Bold);
        _press.HorizontalAlignment = HorizontalAlignment.Right;
        _press.VerticalAlignment = VerticalAlignment.Bottom;
        _press.Margin = new Thickness(0, 0, 24, 26);
        Children.Add(_press);
        PointerPressed += (_, e) =>
        {
            e.Handled = true;
            _done();
        };
    }

    public override MenuView? StageView => null;

    public override void OnBack() => _done();

    public override void HandleKey(KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.Enter or Key.Space || _stage.ShowLogo)
        {
            e.Handled = true;
            _done();
        }
    }

    public override void Tick(float dt)
    {
        string caption = _stage.Caption;
        int chars = Math.Clamp((int)(_stage.CaptionTime * 38f) - 10, 0, caption.Length);
        string text = caption[..chars];
        if (text != _shown)
        {
            _shown = text;
            _caption.Text = text;
        }

        _captionBox.Opacity = caption.Length == 0 || chars == 0 ? 0 : 1;
        _fade.Opacity = _stage.Fade;
        _flash.Opacity = _stage.Flash * 0.7;
        if (_stage.ShowLogo)
        {
            float t = _stage.LogoTime;
            _logo.Opacity = Math.Clamp(t / 0.4f, 0f, 1f);
            // pop in with an overshoot, then a slow breathe
            double u = Math.Min(t / 0.7, 1.0) - 1.0;
            double back = 1 + (2.7 * u * u * u) + (1.7 * u * u);
            double s = (0.3 + (0.7 * back)) * (1.0 + (Math.Sin(t * 2.2) * 0.02));
            _logo.RenderTransform = new ScaleTransform(s, s);
            _press.Text = t > 1.2f ? "Tekan tombol apa saja untuk mulai" : "Enter / Esc / klik: lewati";
        }
    }
}
