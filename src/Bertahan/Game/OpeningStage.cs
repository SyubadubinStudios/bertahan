using System.Numerics;
using Bertahan.Core;
using ThreeNet;

namespace Bertahan.Game;

/// <summary>
/// The opening story, played in engine: a peaceful Kampung Damai, the dukun's
/// ritual in the old graveyard, the dead rising, the villages overrun and the
/// one brave family that stays to fight. The UI shows <see cref="Caption"/>.
/// </summary>
public sealed class OpeningStage : IDisposable
{
    private sealed record Shot(float Start, float End, string Caption, Vector3 FromPos, Vector3 FromTarget, Vector3 ToPos, Vector3 ToTarget, float Night);

    private static readonly Vector3 Grave = new(70, 0, 0);

    private static readonly Shot[] Shots =
    [
        new(0f, 8.5f, "Di kaki gunung, ada sebuah desa yang asri bernama Kampung Damai. Warganya hidup rukun dan bahagia...",
            new Vector3(-6, 14, 34), new Vector3(0, 0, 4), new Vector3(-20, 5, 12), new Vector3(-11, 1.2f, -1), 0f),
        new(8.5f, 17f, "Hingga suatu malam, di kuburan tua, seorang dukun melakukan ritual terlarang...",
            Grave + new Vector3(-7, 2.2f, 8), Grave + new Vector3(0, 1.4f, -2), Grave + new Vector3(4, 1.2f, 5), Grave + new Vector3(0, 1.6f, -2), 1f),
        new(17f, 24.5f, "Kutukannya membangkitkan orang mati. Zombi bermunculan dari dalam tanah!",
            Grave + new Vector3(0, 0.6f, 9), Grave + new Vector3(0, 1.4f, 2), Grave + new Vector3(0, 1.8f, 12), Grave + new Vector3(0, 1.2f, 2), 1f),
        new(24.5f, 32.5f, "Wabah zombi menyerang kampung demi kampung. Warga lari tunggang langgang!",
            new Vector3(-14, 3, 17), new Vector3(-4, 1, 8), new Vector3(10, 3.5f, 17), new Vector3(2, 1, 8), 0.85f),
        new(32.5f, 41f, "Tapi satu keluarga pemberani menolak menyerah. Mereka akan BERTAHAN!",
            new Vector3(0, 0.8f, 9), new Vector3(0, 1.4f, 0), new Vector3(0, 1.6f, 5.2f), new Vector3(0, 1.3f, 0), 0.7f),
        new(41f, 47f, "",
            new Vector3(0, 1.6f, 5.2f), new Vector3(0, 1.3f, 0), new Vector3(0, 2.4f, 7.5f), new Vector3(0, 1.6f, 0), 0.7f),
    ];

    private readonly VillageLife _village;
    private readonly Atmosphere _atmosphere;
    private readonly Particles _fx;
    private readonly Node _sun;
    private readonly Node _fill;
    private readonly Node _ritualLight;
    private readonly Node _familyLight;
    private readonly AnimatedModel _dukun;
    private readonly List<(AnimatedModel Model, float Rise, Vector3 At)> _risers = [];
    private readonly HashSet<AnimatedModel> _risen = [];
    private readonly List<AnimatedModel> _family = [];
    private readonly Random _rng = new(5);
    private int _shot = -1;
    private float _castTimer;
    private float _flash;
    private float _nextLightning = 10.5f;
    private bool _cheered;

    public OpeningStage(AudioManager audio)
    {
        Audio = audio;
        Scene = new Scene();
        Camera = new CameraRig(Scene);
        PropLibrary props = new(Scene);
        Node root = Scene.CreateNode(null, "opening");

        _sun = Scene.AddLight(Light.Directional(Vector3.One, 3f) with { CastShadow = true, ShadowNormalBias = 2f }, name: "sun");
        _fill = Scene.AddLight(Light.Directional(new Vector3(0.5f, 0.55f, 0.9f), 0.5f), name: "fill");
        _fx = new Particles(Scene, Scene.CreateTexture(256, 256, Particles.PaintAtlas()));
        _village = new VillageLife(Scene, props, root, audio, _fx) { RandomIntruders = false };
        _atmosphere = new Atmosphere(Scene, AtmosphereStyle.Menu, withNightSky: true);
        _atmosphere.Swayers.AddRange(_village.Swayers);

        // the old graveyard, far enough away to sit in its own fog
        Node grave = Scene.CreateNode(root, "graveyard");
        GroundPainter paint = new(256, 30f, 4);
        paint.Fill(new Vector3(0.2f, 0.3f, 0.16f), new Vector3(0.14f, 0.22f, 0.12f), 0.2f, 3);
        paint.Rect(new Vector2(-2, -4), new Vector2(2, 2), new Vector3(0.3f, 0.24f, 0.18f), new Vector3(0.22f, 0.18f, 0.14f), 5, 1f);
        Texture gtex = Scene.CreateTexture(256, 256, paint.ToRgba());
        Node gground = Scene.AddMesh(Scene.CreatePlaneGeometry(30, 30), Scene.CreateMaterial(MaterialOptions.Pbr(Vector4.One, 0, 1f) with { BaseColorMap = gtex }), grave);
        gground.EulerAngles = new Vector3(-MathF.PI / 2, 0, 0);
        gground.Position = Grave + new Vector3(0, 0.01f, 0);
        gground.CastShadow = false;
        props.Place("prop_altar_dukun", grave, Grave + new Vector3(0, 0, -3));
        props.Place("prop_gerbang_kubur", grave, Grave + new Vector3(0, 0, 11));
        props.Place("prop_pohon_beringin", grave, Grave + new Vector3(-8, 0, -6), 0.6f, 1.2f);
        props.Place("prop_pohon_beringin", grave, Grave + new Vector3(9, 0, -8), 2f);
        for (int i = 0; i < 6; i++)
        {
            props.Place("prop_lilin", grave, Grave + new Vector3(MathF.Cos(i * 1.05f) * 1.8f, 0, -1.2f + (MathF.Sin(i * 1.05f) * 1.2f)));
        }

        string[] stones = ["prop_nisan_a", "prop_nisan_b", "prop_nisan_c"];
        for (int x = -3; x <= 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                if (x == 0)
                {
                    continue;
                }

                props.Place(stones[(x + z + 6) % 3], grave, Grave + new Vector3(x * 2.1f, 0, 1.5f + (z * 2.4f)), (x * 0.13f) + (z * 0.2f));
            }
        }

        _ritualLight = Scene.AddLight(Light.Point(new Vector3(0.35f, 1f, 0.3f), 0f, 14f), grave, "ritual");
        _ritualLight.Position = Grave + new Vector3(0, 2.5f, -1);
        _dukun = AnimatedModel.Load(Scene, "zombie_dukun", grave);
        _dukun.Root.Position = Grave + new Vector3(0, 0, -1.2f);
        _dukun.Root.EulerAngles = new Vector3(0, 0, 0);

        string[] kinds = ["warga", "pocong", "tuyul", "warga", "satpam", "pocong", "kuntilanak", "warga"];
        for (int i = 0; i < kinds.Length; i++)
        {
            AnimatedModel z = AnimatedModel.Load(Scene, "zombie_" + kinds[i], grave);
            Vector3 at = Grave + new Vector3(((i % 4) - 1.5f) * 2.6f, 0, 2.6f + ((i / 4) * 2.6f) + ((i % 2) * 0.6f));
            z.Root.Position = at;
            z.Root.Visible = false;
            _risers.Add((z, 17.6f + (i * 0.65f), at));
        }

        // the family in front of their house
        _familyLight = Scene.AddLight(Light.Point(new Vector3(1f, 0.75f, 0.45f), 0f, 10f), root, "family-lamp");
        _familyLight.Position = new Vector3(0, 3.2f, 3.5f);
        for (int i = 0; i < MenuStage.Order.Length; i++)
        {
            AnimatedModel m = AnimatedModel.Load(Scene, "char_" + MenuStage.Order[i], root);
            m.ShowWeapon(CharacterDef.Get(MenuStage.Order[i]).StartWeapon);
            m.Root.Position = new Vector3((i - 2.5f) * 1.2f, 0, 0.8f - (MathF.Abs(i - 2.5f) * 0.3f));
            m.Root.EulerAngles = new Vector3(0, (i - 2.5f) * -0.1f, 0);
            m.Root.Visible = false;
            _family.Add(m);
        }

        Lighting(0f);
    }

    public Scene Scene { get; }

    public CameraRig Camera { get; }

    public AudioManager Audio { get; }

    public float Time { get; private set; }

    public static float Duration => Shots[^1].End;

    public bool Finished => Time >= Duration;

    public string Caption => _shot >= 0 ? Shots[_shot].Caption : "";

    /// <summary>Seconds into the current caption, for the typewriter effect.</summary>
    public float CaptionTime => _shot >= 0 ? Time - Shots[_shot].Start : 0f;

    /// <summary>0..1 fade to black at the cuts.</summary>
    public float Fade { get; private set; } = 1f;

    /// <summary>0..1 white flash of lightning.</summary>
    public float Flash => _flash;

    public bool ShowLogo => Time >= Shots[^1].Start;

    public float LogoTime => Time - Shots[^1].Start;

    private void Lighting(float night)
    {
        Vector3 skyDay = new(0.93f, 0.62f, 0.42f), skyNight = new(0.02f, 0.03f, 0.07f);
        Vector3 sky = Vector3.Lerp(skyDay, skyNight, night);
        float flash = _flash;
        Scene.Environment = Scene.Environment with
        {
            Background = new Vector4(sky + (new Vector3(0.5f, 0.6f, 0.8f) * flash * 0.6f), 1f),
            AmbientColor = Vector3.Lerp(new Vector3(0.62f, 0.52f, 0.66f), new Vector3(0.3f, 0.38f, 0.65f), night),
            AmbientIntensity = float.Lerp(0.5f, 0.3f, night) + (flash * 1.2f),
            FogColor = Vector3.Lerp(new Vector3(0.95f, 0.64f, 0.46f), new Vector3(0.03f, 0.05f, 0.09f), night),
            FogDensity = float.Lerp(0.004f, 0.0055f, night),
            FogStart = 1000f,
            FogEnd = 5000f,
        };
        Vector3 dir = Vector3.Normalize(Vector3.Lerp(new Vector3(-0.7f, -0.5f, -0.6f), new Vector3(-0.35f, -0.8f, 0.45f), night));
        _sun.Light = _sun.Light!.Value with
        {
            Color = Vector3.Lerp(new Vector3(1f, 0.74f, 0.48f), new Vector3(0.55f, 0.65f, 1f), night),
            Intensity = float.Lerp(3f, 0.7f, night),
        };
        _sun.Position = -dir * 60f;
        _sun.LookAt(Vector3.Zero);
        _fill.Position = new Vector3(-20, 10, 10);
        _fill.LookAt(Vector3.Zero);
        foreach (Node lamp in _village.Lamps)
        {
            lamp.Light = lamp.Light!.Value with { Intensity = float.Lerp(4f, 12f, night) };
        }
    }

    private void Enter(int shot)
    {
        _shot = shot;
        switch (shot)
        {
            case 0:
                Audio.PlayMusic("music_day");
                break;
            case 1:
                _village.HideIntruders();
                Audio.PlayMusic("music_night");
                break;
            case 3:
                // the horde arrives: every zombie runs through the village at once
                Vector2[][] paths =
                [
                    [new(-20, 8.4f), new(-6, 8.6f), new(30, 8)],
                    [new(-26, 7.2f), new(-10, 8), new(40, 8.4f)],
                    [new(-19, 10.5f), new(0, 10), new(40, 6)],
                    [new(-3, 24), new(0.5f, 12), new(-2, -2), new(-30, -10)],
                    [new(-24, 5), new(-8, 12), new(20, 20)],
                    [new(-30, 9), new(-14, 7.6f), new(40, 9)],
                ];
                for (int i = 0; i < _village.Intruders.Count; i++)
                {
                    _village.SpawnIntruder(_village.Intruders[i].Id, paths[i % paths.Length], 2f);
                }

                Audio.PlayMusic("music_boss");
                break;
            case 4:
                _village.HideIntruders();
                foreach (AnimatedModel m in _family)
                {
                    m.Root.Visible = true;
                    m.SetBase("idle");
                }

                foreach (Villager v in _village.Villagers)
                {
                    v.Model.Root.Visible = false;
                }

                Audio.PlayMusic("music_menu");
                break;
        }
    }

    public void Update(float dt)
    {
        Time += dt;
        int index = Array.FindLastIndex(Shots, s => Time >= s.Start);
        if (index != _shot && index >= 0)
        {
            Enter(index);
        }

        Shot shot = Shots[Math.Max(0, _shot)];
        float t = Math.Clamp((Time - shot.Start) / (shot.End - shot.Start), 0f, 1f);
        float ease = t * t * (3f - (2f * t));
        Camera.Place(Vector3.Lerp(shot.FromPos, shot.ToPos, ease), Vector3.Lerp(shot.FromTarget, shot.ToTarget, ease));

        // fade through black at every cut
        float intoShot = Time - shot.Start, toEnd = shot.End - Time;
        Fade = _shot == Shots.Length - 1 ? Math.Clamp(1f - (intoShot / 0.6f), 0f, 1f) : Math.Clamp(MathF.Max(1f - (intoShot / 0.6f), 1f - (toEnd / 0.5f)), 0f, 1f);

        _flash = MathF.Max(0f, _flash - (dt * 3f));
        if (_shot is 1 or 2 && Time > _nextLightning)
        {
            _nextLightning = Time + 2.2f + ((float)_rng.NextDouble() * 2f);
            _flash = 1f;
            Audio.Play("sfx_explosion", 0.5f, 0.45f);
        }

        Lighting(shot.Night);
        _atmosphere.Night = shot.Night;
        _atmosphere.Update(dt, Camera.Position, Camera.Position + (Camera.Forward * 10f), _fx, null);
        _village.Update(dt);
        UpdateRitual(dt);
        UpdateFamily(dt);

        // the village burns during the attack
        if (_shot == 3)
        {
            _fx.Fire(new Vector3(-10, 0, -8), 2.5f);
            _fx.Fire(new Vector3(11, 0, -8.5f), 2f);
        }

        Scene.UpdateAnimations(dt);
        _fx.Update(dt, Camera.Right, Camera.Up);
        Audio.Update(dt, Camera.Position, Camera.Forward);
    }

    private void UpdateRitual(float dt)
    {
        bool active = _shot is 1 or 2;
        _ritualLight.Light = _ritualLight.Light!.Value with { Intensity = active ? 14f + (MathF.Sin(Time * 9f) * 4f) : 0f };
        if (active)
        {
            _castTimer -= dt;
            if (_castTimer <= 0f)
            {
                _castTimer = 1.6f;
                _dukun.PlayAction("cast", 0.9f);
                Audio.PlayAt("sfx_cast", _dukun.Root.Position, 0.8f);
            }

            if (_rng.Next(3) == 0)
            {
                _fx.Magic(_dukun.Root.Position + new Vector3(0, 1.9f, 0), true, 1);
            }

            // wisps of cursed mist creeping over the graves
            if (_rng.Next(8) == 0)
            {
                _fx.Emit(Sprite.MagicGreen, Grave + new Vector3(((float)_rng.NextDouble() - 0.5f) * 12f, 0.15f, ((float)_rng.NextDouble() * 6f) - 3f),
                    new Vector3(0, 0.35f, 0), 1.4f, 0.2f, 0.6f);
            }
        }

        _dukun.Update(dt);
        foreach ((AnimatedModel z, float rise, Vector3 at) in _risers)
        {
            if (Time >= rise && _risen.Add(z))
            {
                z.Root.Visible = true;
                z.SetBase("spawn");
                _fx.Dust(at, 8, 0.7f);
                Audio.PlayAt($"sfx_groan_{_rng.Next(3)}", at, 0.8f);
            }

            if (_risen.Contains(z))
            {
                if (Time > rise + 2.2f)
                {
                    z.SetBase("walk", 0.7f);
                    z.Root.Position += new Vector3(0, 0, dt * 0.5f);
                }

                z.Update(dt);
            }
        }
    }

    private void UpdateFamily(float dt)
    {
        if (_shot < 4)
        {
            return;
        }

        _familyLight.Light = _familyLight.Light!.Value with { Intensity = 10f };
        if (!_cheered && Time > 36.5f)
        {
            _cheered = true;
            foreach (AnimatedModel m in _family)
            {
                m.SetBase("cheer");
            }

            _fx.Confetti(new Vector3(0, 0.5f, 1.5f));
            Audio.Play("jingle_victory", 0.8f);
        }

        foreach (AnimatedModel m in _family)
        {
            m.Update(dt);
        }
    }

    public void Dispose() => Scene.Dispose();
}
