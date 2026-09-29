using System.Numerics;
using ThreeNet;

namespace Bertahan.Game;

/// <summary>
/// Third person follow camera looking down at the player from behind. Also
/// projects world points to the screen (HUD labels) and turns the mouse cursor
/// into a ray (aiming).
/// </summary>
public sealed class CameraRig
{
    private Vector3 _position;
    private Vector3 _target;
    private float _shake;
    private float _shakeTime;
    private readonly Random _rng = new(3);

    public CameraRig(Scene scene)
    {
        Node = scene.AddCamera(Camera.Perspective(Fov, 0.3f, 600f), new Vector3(0, 10, 10));
    }

    public Node Node { get; }

    public const float Fov = 50f * MathF.PI / 180f;

    /// <summary>Orbit angle around the player (radians); 0 looks towards -Z.</summary>
    public float Yaw { get; set; }

    public float Pitch { get; set; } = 48f * MathF.PI / 180f;

    public float Distance { get; set; } = 11.5f;

    public Vector2 ViewportSize { get; set; } = new(1280, 720);

    public Matrix4x4 View { get; private set; } = Matrix4x4.Identity;

    public Matrix4x4 Projection { get; private set; } = Matrix4x4.Identity;

    public Vector3 Position => _position;

    public Vector3 Forward => Vector3.Normalize(_target - _position);

    /// <summary>Camera relative movement axes on the ground plane.</summary>
    public Vector2 GroundForward => new(-MathF.Sin(Yaw), -MathF.Cos(Yaw));

    public Vector2 GroundRight => new(MathF.Cos(Yaw), -MathF.Sin(Yaw));

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));

    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

    public void AddShake(float amount) => _shake = MathF.Min(1.2f, _shake + amount);

    public void Snap(Vector3 focus)
    {
        _target = focus + new Vector3(0, 1.1f, 0);
        _position = Desired(focus);
        Apply(0f);
    }

    private Vector3 Desired(Vector3 focus)
    {
        Vector3 back = new(MathF.Sin(Yaw), 0, MathF.Cos(Yaw));
        return focus + (back * (Distance * MathF.Cos(Pitch))) + new Vector3(0, (Distance * MathF.Sin(Pitch)) + 1.1f, 0);
    }

    public void Follow(Vector3 focus, Vector3 lead, float dt, bool shakeEnabled)
    {
        Vector3 target = focus + new Vector3(0, 1.1f, 0) + lead;
        _target = Vector3.Lerp(_target, target, 1f - MathF.Exp(-8f * dt));
        _position = Vector3.Lerp(_position, Desired(focus + lead), 1f - MathF.Exp(-6f * dt));
        _shake = MathF.Max(0f, _shake - (dt * 2.5f));
        if (!shakeEnabled)
        {
            _shake = 0;
        }

        _shakeTime += dt;
        Apply(dt);
    }

    /// <summary>Free placement for cinematics and the menu.</summary>
    public void Place(Vector3 position, Vector3 target)
    {
        _position = position;
        _target = target;
        Apply(0f);
    }

    private void Apply(float dt)
    {
        Vector3 pos = _position;
        if (_shake > 0.001f)
        {
            float a = _shake * _shake * 0.35f;
            pos += new Vector3(
                MathF.Sin(_shakeTime * 53f) * a,
                MathF.Sin(_shakeTime * 61f + 1f) * a,
                MathF.Cos(_shakeTime * 47f) * a);
        }

        Node.Position = pos;
        Node.LookAt(_target);
        View = Matrix4x4.CreateLookAt(pos, _target, Vector3.UnitY);
        float aspect = ViewportSize.X / MathF.Max(1f, ViewportSize.Y);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(Fov, aspect, 0.3f, 600f);
    }

    /// <summary>World to viewport pixels; false when behind the camera.</summary>
    public bool Project(Vector3 world, out Vector2 screen)
    {
        Vector4 clip = Vector4.Transform(new Vector4(world, 1f), View * Projection);
        if (clip.W <= 0.01f)
        {
            screen = default;
            return false;
        }

        Vector3 ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
        screen = new Vector2((ndc.X * 0.5f + 0.5f) * ViewportSize.X, (1f - (ndc.Y * 0.5f + 0.5f)) * ViewportSize.Y);
        return true;
    }

    /// <summary>Intersects the ray under a viewport pixel with the horizontal plane at <paramref name="height"/>.</summary>
    public Vector3? GroundPoint(Vector2 screen, float height = 0f)
    {
        if (!Matrix4x4.Invert(View * Projection, out Matrix4x4 inverse))
        {
            return null;
        }

        float x = (screen.X / ViewportSize.X * 2f) - 1f;
        float y = 1f - (screen.Y / ViewportSize.Y * 2f);
        Vector4 near = Vector4.Transform(new Vector4(x, y, 0f, 1f), inverse);
        Vector4 far = Vector4.Transform(new Vector4(x, y, 1f, 1f), inverse);
        Vector3 a = new Vector3(near.X, near.Y, near.Z) / near.W;
        Vector3 b = new Vector3(far.X, far.Y, far.Z) / far.W;
        Vector3 dir = b - a;
        if (MathF.Abs(dir.Y) < 1e-5f)
        {
            return null;
        }

        float t = (height - a.Y) / dir.Y;
        return t < 0 ? null : a + (dir * t);
    }
}
