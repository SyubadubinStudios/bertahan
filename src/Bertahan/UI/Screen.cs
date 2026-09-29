using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Bertahan.Core;
using Bertahan.Game;

namespace Bertahan.UI;

/// <summary>What the screens can ask of the game shell.</summary>
public interface IShell
{
    GameSettings Settings { get; }

    AudioManager Audio { get; }

    MenuStage? Menu { get; }

    Campaign? Run { get; }

    GameSession? Session { get; }

    void Show(Screen screen);

    void Replace(Screen screen);

    void Back();

    void ToTitle();

    void StartRun(Campaign run);

    void PlayLevel();

    void ResumeGame();

    void RestartLevel();

    void QuitToTitle();

    void PlayOpening();

    void ApplySettings();

    void Exit();
}

/// <summary>
/// A full window UI page laid out on a 1280x720 design canvas (the shell
/// scales it to the window). Arrow keys move between buttons, Esc goes back.
/// </summary>
public abstract class Screen : Grid
{
    protected Screen(IShell shell)
    {
        Shell = shell;
        Width = 1280;
        Height = 720;
        Opacity = 0;
        Transitions =
        [
            new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(220) },
        ];
    }

    public IShell Shell { get; }

    /// <summary>How the 3D menu backdrop should look while this screen is up (null leaves it alone).</summary>
    public virtual MenuView? StageView => MenuView.Village;

    /// <summary>Dims the 3D behind the screen (0..1).</summary>
    public virtual double Dim => 0;

    public virtual void OnShown()
    {
        Opacity = 1;
        FocusFirst();
    }

    public virtual void Tick(float dt)
    {
    }

    /// <summary>Esc: by default go back one screen.</summary>
    public virtual void OnBack()
    {
        UiSound.Back();
        Shell.Back();
    }

    public void FocusFirst()
    {
        MenuButton? first = Buttons().FirstOrDefault(b => b.IsEnabled);
        first?.Focus(NavigationMethod.Directional);
    }

    protected IEnumerable<MenuButton> Buttons() => this.GetVisualDescendants().OfType<MenuButton>().Where(b => b.IsEffectivelyVisible && b.IsEnabled);

    /// <summary>Called by the shell for keys nobody handled.</summary>
    public virtual void HandleKey(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                e.Handled = true;
                OnBack();
                break;
            case Key.Up or Key.Down or Key.Left or Key.Right or Key.W or Key.S:
                if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox && e.Key is Key.Left or Key.Right or Key.W or Key.S)
                {
                    return;
                }

                e.Handled = MoveFocus(e.Key);
                break;
        }
    }

    /// <summary>Spatial navigation: the closest button in the pressed direction.</summary>
    private bool MoveFocus(Key key)
    {
        List<MenuButton> buttons = Buttons().ToList();
        if (buttons.Count == 0)
        {
            return false;
        }

        MenuButton? current = buttons.FirstOrDefault(b => b.IsFocused);
        if (current is null)
        {
            buttons[0].Focus(NavigationMethod.Directional);
            return true;
        }

        Point Centre(Visual v) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), this) ?? default;
        Point from = Centre(current);
        Vector dir = key switch
        {
            Key.Up or Key.W => new Vector(0, -1),
            Key.Down or Key.S => new Vector(0, 1),
            Key.Left => new Vector(-1, 0),
            _ => new Vector(1, 0),
        };
        MenuButton? best = null;
        double bestScore = double.MaxValue;
        foreach (MenuButton b in buttons)
        {
            if (b == current)
            {
                continue;
            }

            Vector d = Centre(b) - from;
            double along = (d.X * dir.X) + (d.Y * dir.Y);
            if (along <= 4)
            {
                continue;
            }

            double across = Math.Abs((d.X * dir.Y) - (d.Y * dir.X));
            double score = along + (across * 2.5);
            if (score < bestScore)
            {
                bestScore = score;
                best = b;
            }
        }

        if (best is null)
        {
            return false;
        }

        best.Focus(NavigationMethod.Directional);
        return true;
    }

    /// <summary>A column of buttons inside a paper panel, the usual menu shape.</summary>
    protected static StackPanel Column(double spacing = 4) => new() { Spacing = spacing };

    protected static Border Backdrop(byte alpha = 150) => new() { Background = new SolidColorBrush(Color.FromArgb(alpha, 20, 12, 30)) };
}
