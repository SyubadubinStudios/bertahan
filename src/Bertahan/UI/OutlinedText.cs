using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Bertahan.UI;

/// <summary>Text drawn with a thick outline and a hard shadow, for titles and the HUD.</summary>
public sealed class OutlinedText : Control
{
    private Geometry? _geometry;
    private Size _size;
    private string _text = "";
    private double _fontSize = 32;
    private Color _fill = Colors.White;
    private Color _stroke = Colors.Black;

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                Rebuild();
            }
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value;
            Rebuild();
        }
    }

    public Color Fill
    {
        get => _fill;
        set
        {
            _fill = value;
            InvalidateVisual();
        }
    }

    public Color Stroke
    {
        get => _stroke;
        set
        {
            _stroke = value;
            InvalidateVisual();
        }
    }

    private double _strokeThickness = 4;

    public double StrokeThickness
    {
        get => _strokeThickness;
        set
        {
            _strokeThickness = value;
            Rebuild();
        }
    }

    public double ShadowOffset { get; set; } = -1;

    public FontWeight Weight { get; set; } = FontWeight.Black;

    private void Rebuild()
    {
        FormattedText ft = new(_text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(Kit.Font, FontStyle.Normal, Weight), _fontSize, Brushes.White);
        double pad = StrokeThickness;
        _geometry = ft.BuildGeometry(new Point(pad, pad));
        double shadow = ShadowOffset >= 0 ? ShadowOffset : _fontSize / 14;
        _size = new Size(ft.WidthIncludingTrailingWhitespace + (pad * 2) + shadow, ft.Height + (pad * 2) + shadow);
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) => _size;

    public override void Render(DrawingContext context)
    {
        if (_geometry is null)
        {
            return;
        }

        double shadow = ShadowOffset >= 0 ? ShadowOffset : _fontSize / 14;
        Pen pen = new(new SolidColorBrush(_stroke), StrokeThickness * 2, lineJoin: PenLineJoin.Round);
        if (shadow > 0)
        {
            using (context.PushTransform(Matrix.CreateTranslation(0, shadow)))
            {
                context.DrawGeometry(new SolidColorBrush(_stroke), pen, _geometry);
            }
        }

        context.DrawGeometry(null, pen, _geometry);
        context.DrawGeometry(new SolidColorBrush(_fill), null, _geometry);
    }
}
