using System.Numerics;
using Avalonia.Input;
using Bertahan.Game;

namespace Bertahan.Core;

/// <summary>Collects keyboard and mouse events between frames and turns them into an <see cref="InputState"/>.</summary>
public sealed class InputMap
{
    private readonly HashSet<Key> _down = [];
    private readonly HashSet<Key> _pressed = [];
    private bool _leftPressed;
    private bool _rightPressed;
    private double _wheel;

    public bool LeftDown { get; set; }

    public bool RightDown { get; set; }

    public Vector2 Mouse { get; set; }

    public bool MouseValid { get; set; }

    public void KeyDown(Key key)
    {
        if (_down.Add(key))
        {
            _pressed.Add(key);
        }
    }

    public void KeyUp(Key key) => _down.Remove(key);

    public void Pointer(bool left, bool right)
    {
        if (left && !LeftDown)
        {
            _leftPressed = true;
        }

        if (right && !RightDown)
        {
            _rightPressed = true;
        }

        LeftDown = left;
        RightDown = right;
    }

    public void Wheel(double delta) => _wheel += delta;

    /// <summary>Forget held keys (focus lost, pause), so nothing stays stuck.</summary>
    public void Reset()
    {
        _down.Clear();
        _pressed.Clear();
        LeftDown = RightDown = false;
        _leftPressed = _rightPressed = false;
        _wheel = 0;
    }

    private bool Down(params Key[] keys) => keys.Any(_down.Contains);

    private bool Pressed(params Key[] keys) => keys.Any(_pressed.Contains);

    public InputState Build(float cameraSpeed)
    {
        Vector2 move = new(
            (Down(Key.D, Key.Right) ? 1 : 0) - (Down(Key.A, Key.Left) ? 1 : 0),
            (Down(Key.W, Key.Up) ? 1 : 0) - (Down(Key.S, Key.Down) ? 1 : 0));
        int slot = -1;
        Key[] digits = [Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9];
        for (int i = 0; i < digits.Length; i++)
        {
            if (_pressed.Contains(digits[i]) || _pressed.Contains(Key.NumPad1 + i))
            {
                slot = i;
            }
        }

        InputState input = new()
        {
            Move = move,
            MouseScreen = Mouse,
            MouseValid = MouseValid,
            Attack = LeftDown || _leftPressed || Down(Key.J),
            Sprint = Down(Key.LeftShift, Key.RightShift),
            Dodge = Pressed(Key.Space, Key.K),
            Throw = _rightPressed || Pressed(Key.G, Key.L),
            Reload = Pressed(Key.R),
            WeaponSlot = slot,
            WeaponCycle = _wheel > 0.3 ? -1 : _wheel < -0.3 ? 1 : 0,
            CameraTurn = ((Down(Key.E) ? 1 : 0) - (Down(Key.Q) ? 1 : 0)) * cameraSpeed,
            Zoom = ((Down(Key.OemPlus, Key.Add) ? 1 : 0) - (Down(Key.OemMinus, Key.Subtract) ? 1 : 0)) * 0.15f,
        };
        _pressed.Clear();
        _leftPressed = _rightPressed = false;
        _wheel = 0;
        return input;
    }
}
