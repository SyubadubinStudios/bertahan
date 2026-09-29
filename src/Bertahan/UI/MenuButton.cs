using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Bertahan.Core;

namespace Bertahan.UI;

/// <summary>Plays the UI clicks; the shell sets the audio manager once it exists.</summary>
public static class UiSound
{
    public static AudioManager? Audio { get; set; }

    public static void Hover() => Audio?.Play("sfx_ui_hover", 0.5f, 1f, 0.05);

    public static void Click() => Audio?.Play("sfx_ui_click", 0.8f);

    public static void Back() => Audio?.Play("sfx_ui_click", 0.7f, 0.8f);
}

/// <summary>
/// A chunky sticker button: sunny fill, ink outline, hard shadow. Grows when
/// hovered or focused, squashes when pressed. Works with mouse and keyboard.
/// </summary>
public sealed class MenuButton : Border
{
    private readonly Border _face;
    private readonly TextBlock _label;
    private readonly TextBlock? _hint;
    private bool _hover;
    private bool _pressed;

    public MenuButton(string text, Action onClick, string? hint = null, double fontSize = 24, Color? color = null)
    {
        Click = onClick;
        BaseColor = color ?? Kit.Sun;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Margin = new Thickness(0, 5);
        _label = Kit.Text(text, fontSize, Kit.InkBrush, FontWeight.Black, TextWrapping.NoWrap);
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        StackPanel content = new() { Spacing = 0 };
        content.Children.Add(_label);
        if (hint is not null)
        {
            _hint = Kit.Text(hint, 13, Kit.MutedBrush, FontWeight.SemiBold);
            _hint.HorizontalAlignment = HorizontalAlignment.Center;
            _hint.TextAlignment = TextAlignment.Center;
            content.Children.Add(_hint);
        }

        _face = new Border
        {
            BorderBrush = Kit.InkBrush,
            BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(18, 8),
            Child = content,
        };
        Child = _face;
        RenderTransformOrigin = RelativePoint.Center;
        Refresh();
    }

    public Action Click { get; set; }

    public Color BaseColor { get; set; }

    public bool Enabled
    {
        get => IsEnabled;
        set
        {
            IsEnabled = value;
            Refresh();
        }
    }

    public string Label
    {
        get => _label.Text ?? "";
        set => _label.Text = value;
    }

    private void Refresh()
    {
        bool hot = (_hover || IsFocused) && IsEnabled;
        Color c = IsEnabled ? BaseColor : Color.Parse("#CFC3AE");
        Color top = hot ? Lighten(c, 0.25) : Lighten(c, 0.1);
        _face.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = [new GradientStop(top, 0), new GradientStop(c, 0.55), new GradientStop(Darken(c, 0.12), 1)],
        };
        _face.BoxShadow = _pressed ? BoxShadows.Parse("0 1 0 0 #3A2412") : BoxShadows.Parse(hot ? "0 7 0 0 #3A2412" : "0 5 0 0 #3A2412");
        _label.Foreground = IsEnabled ? Kit.InkBrush : Kit.MutedBrush;
        double scale = _pressed ? 0.96 : hot ? 1.05 : 1.0;
        double lift = _pressed ? 3 : hot ? -2 : 0;
        RenderTransform = new MatrixTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(0, lift));
    }

    private static Color Lighten(Color c, double t) => Color.FromRgb((byte)(c.R + ((255 - c.R) * t)), (byte)(c.G + ((255 - c.G) * t)), (byte)(c.B + ((255 - c.B) * t)));

    private static Color Darken(Color c, double t) => Color.FromRgb((byte)(c.R * (1 - t)), (byte)(c.G * (1 - t)), (byte)(c.B * (1 - t)));

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (IsEnabled)
        {
            _hover = true;
            UiSound.Hover();
            Focus();
            Refresh();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hover = false;
        _pressed = false;
        Refresh();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && IsEnabled)
        {
            _pressed = true;
            e.Handled = true;
            Refresh();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pressed)
        {
            _pressed = false;
            Refresh();
            e.Handled = true;
            Activate();
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        Refresh();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        Refresh();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Enter or Key.Space && IsEnabled)
        {
            e.Handled = true;
            Activate();
        }
    }

    public void Activate()
    {
        if (!IsEnabled)
        {
            return;
        }

        UiSound.Click();
        Click();
    }
}
