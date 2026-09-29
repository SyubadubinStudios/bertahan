using System.Numerics;
using Bertahan.Core;
using ThreeNet;

namespace Bertahan.Game;

public enum MenuView
{
    /// <summary>Slow tour over the busy village.</summary>
    Village,

    /// <summary>The family lined up in front of their house.</summary>
    Family,
}

/// <summary>
/// The 3D backdrop behind every menu: Kampung Damai at golden hour, villagers
/// busy with their chores, zombies wandering in and scaring them off, and the
/// family ready in front of their house for the character select.
/// </summary>
public sealed class MenuStage : IDisposable
{
    public static readonly string[] Order = ["bapak", "ibu", "kakak", "ade", "kake", "nene"];

    private static readonly (Vector3 Pos, Vector3 Target)[] Tour =
    [
        (new Vector3(-4, 7.5f, 22), new Vector3(-2, 1, 4)),
        (new Vector3(-26, 6, 14), new Vector3(-12, 1.2f, 2)),
        (new Vector3(4, 5, 18), new Vector3(-8, 1.5f, 11)),
        (new Vector3(30, 8, 14), new Vector3(17, 1, 3)),
        (new Vector3(12, 4.5f, 26), new Vector3(4, 1.2f, 14)),
        (new Vector3(0, 12, 30), new Vector3(0, 0, 0)),
        (new Vector3(2.5f, 2f, 28), new Vector3(-3, 8, -12)),
    ];

    private readonly List<AnimatedModel> _family = [];
    private readonly Particles _fx;
    private readonly VillageLife _village;
    private readonly Atmosphere _atmosphere;
    private double _time;
    private int _selected = -1;
    private Vector3 _camPos;
    private Vector3 _camTarget;
    private int _tourIndex;
    private float _tourTime;

    public MenuStage(AudioManager audio)
    {
        Audio = audio;
        Scene = new Scene();
        Camera = new CameraRig(Scene);
        PropLibrary props = new(Scene);
        Node root = Scene.CreateNode(null, "stage");

        Scene.Environment = Scene.Environment with
        {
            Background = new Vector4(0.93f, 0.62f, 0.42f, 1f),
            AmbientColor = new Vector3(0.62f, 0.52f, 0.66f),
            AmbientIntensity = 0.5f,
            FogColor = new Vector3(0.95f, 0.64f, 0.46f),
            FogDensity = 0.004f,
            FogStart = 1000f,
            FogEnd = 5000f,
        };
        Node sun = Scene.AddLight(Light.Directional(new Vector3(1f, 0.74f, 0.48f), 3.0f) with { CastShadow = true, ShadowNormalBias = 2f }, name: "sun");
        sun.Position = new Vector3(30, 22, 25);
        sun.LookAt(Vector3.Zero);
        Node fill = Scene.AddLight(Light.Directional(new Vector3(0.5f, 0.55f, 0.9f), 0.5f), name: "fill");
        fill.Position = new Vector3(-20, 10, 10);
        fill.LookAt(Vector3.Zero);

        _fx = new Particles(Scene, Scene.CreateTexture(256, 256, Particles.PaintAtlas()));
        _village = new VillageLife(Scene, props, root, audio, _fx);
        _atmosphere = new Atmosphere(Scene, AtmosphereStyle.Menu);
        _atmosphere.Swayers.AddRange(_village.Swayers);
        foreach (Node lamp in _village.Lamps)
        {
            lamp.Light = lamp.Light is { } l ? l with { Intensity = 6f } : null;
        }

        for (int i = 0; i < Order.Length; i++)
        {
            AnimatedModel m = AnimatedModel.Load(Scene, "char_" + Order[i], root);
            m.ShowWeapon(CharacterDef.Get(Order[i]).StartWeapon);
            m.Root.Position = Home(i);
            m.Root.EulerAngles = new Vector3(0, (i - 2.5f) * -0.08f, 0);
            _family.Add(m);
        }

        _camPos = Tour[0].Pos;
        _camTarget = Tour[0].Target;
        Camera.Place(_camPos, _camTarget);
    }

    public Scene Scene { get; }

    public CameraRig Camera { get; }

    public AudioManager Audio { get; }

    public MenuView View { get; set; } = MenuView.Village;

    /// <summary>Shift of the framing to the right, so the menu panel on the left does not cover the action.</summary>
    public float PanelOffset { get; set; } = 1f;

    private static Vector3 Home(int i) => new((i - 2.5f) * 1.25f, 0, 1.2f - (MathF.Abs(i - 2.5f) * 0.25f));

    /// <summary>-1 shows everybody, otherwise that family member steps forward and cheers.</summary>
    public void Select(int index)
    {
        if (index == _selected)
        {
            return;
        }

        _selected = index;
        for (int i = 0; i < _family.Count; i++)
        {
            _family[i].SetBase(i == index ? "cheer" : "idle");
        }

        if (index >= 0)
        {
            _fx.Confetti(Home(index) + new Vector3(0, 0, 1.2f));
            Audio.Play("sfx_pickup", 0.6f, 1.1f);
        }
    }

    public void Update(float dt)
    {
        _time += dt;
        _village.Update(dt);
        for (int i = 0; i < _family.Count; i++)
        {
            AnimatedModel m = _family[i];
            Vector3 target = Home(i) + (i == _selected ? new Vector3(0, 0, 1.4f) : Vector3.Zero);
            m.Root.Position = Vector3.Lerp(m.Root.Position, target, 1f - MathF.Exp(-6f * dt));
            m.Update(dt);
        }

        Vector3 desiredPos, desiredTarget;
        if (View == MenuView.Family)
        {
            if (_selected >= 0)
            {
                Vector3 home = Home(_selected) + new Vector3(0, 0, 1.4f);
                desiredPos = home + new Vector3(1.6f, 1.6f, 4.2f);
                desiredTarget = home + new Vector3(-0.9f * PanelOffset, 0.9f, 0);
            }
            else
            {
                float sway = MathF.Sin((float)_time * 0.2f) * 0.8f;
                desiredPos = new Vector3(sway - 1.5f, 3.0f, 10.5f);
                desiredTarget = new Vector3(-2.2f * PanelOffset, 1.2f, 0);
            }
        }
        else
        {
            // drift between points of interest, each one slowly dollying
            _tourTime += dt;
            if (_tourTime > 11f)
            {
                _tourTime = 0f;
                _tourIndex = (_tourIndex + 1) % Tour.Length;
            }

            (Vector3 pos, Vector3 target) = Tour[_tourIndex];
            Vector3 side = Vector3.Normalize(Vector3.Cross(target - pos, Vector3.UnitY));
            desiredPos = pos + (side * (_tourTime - 5.5f) * 0.35f);
            desiredTarget = target - (side * 2.5f * PanelOffset);
        }

        float follow = View == MenuView.Village ? 0.6f : 3f;
        _camPos = Vector3.Lerp(_camPos, desiredPos, 1f - MathF.Exp(-follow * dt));
        _camTarget = Vector3.Lerp(_camTarget, desiredTarget, 1f - MathF.Exp(-follow * dt));
        Camera.Place(_camPos, _camTarget);
        _atmosphere.Update(dt, _camPos, _camTarget, _fx, null);
        Scene.UpdateAnimations(dt);
        _fx.Update(dt, Camera.Right, Camera.Up);
        Audio.Update(dt, _camPos, Vector3.Normalize(_camTarget - _camPos));
    }

    public void Dispose() => Scene.Dispose();
}
