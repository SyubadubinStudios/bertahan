using System.Numerics;
using Bertahan.Core;
using ThreeNet;

namespace Bertahan.Game;

public enum CritterKind
{
    Walker,
    Flyer,
    Slither,
    Person,
}

/// <summary>An ambient creature: livestock, wild animals and villagers going about their day.</summary>
public sealed record CritterDef(
    string Id,
    string Model,
    CritterKind Kind,
    float WalkSpeed,
    float RunSpeed,
    float FleeRadius,
    string Sound,
    float SoundEvery,
    float ActChance = 0.3f,
    float Scale = 1f,
    string? Tool = null,
    float AnimWalk = 1f,
    float AnimRun = 1f)
{
    public static readonly CritterDef[] All =
    [
        new("ayam", "animal_ayam", CritterKind.Walker, 0.7f, 3.2f, 5f, "sfx_ayam", 9f, 0.55f, 1.4f, AnimWalk: 0.5f, AnimRun: 2.6f),
        new("sapi", "animal_sapi", CritterKind.Walker, 0.6f, 2.6f, 6f, "sfx_sapi", 16f, 0.5f, 1f, AnimWalk: 0.7f, AnimRun: 2.4f),
        new("kambing", "animal_kambing", CritterKind.Walker, 0.8f, 3.4f, 6f, "sfx_kambing", 12f, 0.5f, 1.1f, AnimWalk: 0.7f, AnimRun: 2.8f),
        new("kucing", "animal_kucing", CritterKind.Walker, 0.9f, 4.5f, 6f, "sfx_kucing", 14f, 0.2f, 1.2f, AnimWalk: 0.9f, AnimRun: 3.5f),
        new("anjing", "animal_anjing", CritterKind.Walker, 1.2f, 5f, 4.5f, "sfx_anjing", 10f, 0.25f, 1.1f, AnimWalk: 1f, AnimRun: 4f),
        new("ular", "animal_ular", CritterKind.Slither, 0.35f, 1.6f, 4f, "sfx_ular", 18f, 0.15f, 1.2f, AnimWalk: 0.5f, AnimRun: 1.6f),
        new("burung", "animal_burung", CritterKind.Flyer, 0.5f, 6f, 7f, "sfx_burung", 7f, 0.5f, 1.8f, AnimWalk: 0.4f, AnimRun: 1f),
        new("petani", "npc_petani", CritterKind.Person, 1.2f, 4.6f, 7f, "sfx_teriak_pria", 0f, 0.3f, 1f, "pacul", 2.2f, 5.5f),
        new("pedagang", "npc_pedagang", CritterKind.Person, 1.1f, 4.2f, 7f, "sfx_teriak_wanita", 0f, 0.3f, 1f, "sapu", 2.2f, 5.5f),
        new("ustad", "npc_ustad", CritterKind.Person, 1.1f, 4.4f, 7f, "sfx_teriak_pria", 0f, 0.1f, 1f, null, 2.2f, 5.5f),
        new("bocah", "npc_bocah", CritterKind.Person, 1.5f, 5f, 7f, "sfx_teriak_wanita", 0f, 0.1f, 1f, null, 2.2f, 5.5f),
        new("hansip", "npc_hansip", CritterKind.Person, 1.3f, 4.8f, 5f, "sfx_teriak_pria", 0f, 0.2f, 1f, "pentungan", 2.2f, 5.5f),
        new("bakso", "npc_bakso", CritterKind.Person, 1f, 4.3f, 7f, "sfx_teriak_pria", 0f, 0.1f, 1f, null, 2.2f, 5.5f),
        new("jamu", "npc_jamu", CritterKind.Person, 1f, 4f, 7f, "sfx_teriak_wanita", 0f, 0.1f, 1f, null, 2.2f, 5.5f),
        new("ojek", "npc_ojek", CritterKind.Person, 1.2f, 4.6f, 7f, "sfx_teriak_pria", 0f, 0.1f, 1f, null, 2.2f, 5.5f),
        new("guru", "npc_guru", CritterKind.Person, 1.1f, 4.2f, 7f, "sfx_teriak_wanita", 0f, 0.1f, 1f, null, 2.2f, 5.5f),
    ];

    public static CritterDef Get(string id) => All.First(c => c.Id == id);
}

public enum CritterState
{
    Idle,
    Wander,
    Act,
    Flee,
    Fly,
    Land,
}

public sealed class Critter
{
    public required CritterDef Def;
    public required AnimatedModel Model;
    public Vector2 Home;
    public float HomeRadius;
    public Vector2 Position;
    public float Height;
    public float Yaw;
    public CritterState State;
    public float Timer;
    public Vector2 Target;
    public Vector2 FleeFrom;
    public float VoiceTimer;
    public bool Alarmed;
}

/// <summary>
/// Livestock, wild animals and villagers that make the village feel alive.
/// They wander around their home spot, peck, graze, bark and chatter, and run
/// (birds take off) as soon as a zombie comes close. Zombies ignore them.
/// </summary>
public sealed class Fauna
{
    private readonly Scene _scene;
    private readonly Node _parent;
    private readonly AudioManager? _audio;
    private readonly Particles? _fx;
    private readonly Random _rng;
    private readonly Func<Vector2, float, Vector2>? _resolve;
    private readonly float _bounds;

    public Fauna(Scene scene, Node parent, AudioManager? audio, Particles? fx, int seed, Func<Vector2, float, Vector2>? resolve = null, float bounds = 40f)
    {
        _scene = scene;
        _parent = parent;
        _audio = audio;
        _fx = fx;
        _rng = new Random(seed);
        _resolve = resolve;
        _bounds = bounds;
    }

    public List<Critter> Critters { get; } = [];

    /// <summary>Scales how often creatures make noise (0 mutes them).</summary>
    public float Chatter { get; set; } = 1f;

    private float R(float a, float b) => a + ((float)_rng.NextDouble() * (b - a));

    /// <summary>Places <paramref name="count"/> creatures of a kind around a home spot.</summary>
    public void Add(string id, Vector2 home, float radius, int count = 1)
    {
        CritterDef def = CritterDef.Get(id);
        for (int i = 0; i < count; i++)
        {
            AnimatedModel model = AnimatedModel.Load(_scene, def.Model, _parent);
            model.ShowWeapon(def.Tool);
            model.Root.Scale = new Vector3(def.Scale);
            float a = R(0, MathF.Tau), d = R(0, radius);
            Vector2 p = home + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * d);
            Critter c = new()
            {
                Def = def,
                Model = model,
                Home = home,
                HomeRadius = radius,
                Position = p,
                Yaw = R(0, MathF.Tau),
                Timer = R(0.5f, 4f),
                VoiceTimer = R(2f, def.SoundEvery + 2f),
            };
            Critters.Add(c);
            Pose(c, 0f);
        }
    }

    public void Update(float dt, IReadOnlyList<Vector2> threats)
    {
        foreach (Critter c in Critters)
        {
            UpdateOne(c, dt, threats);
        }

        Separate(dt);
    }

    /// <summary>Gently pushes grounded creatures apart so a flock does not stack up.</summary>
    private void Separate(float dt)
    {
        for (int i = 0; i < Critters.Count; i++)
        {
            Critter a = Critters[i];
            if (a.Height > 0.01f)
            {
                continue;
            }

            for (int k = i + 1; k < Critters.Count; k++)
            {
                Critter b = Critters[k];
                if (b.Height > 0.01f)
                {
                    continue;
                }

                float min = (Radius(a.Def) + Radius(b.Def)) * 1.1f;
                Vector2 d = a.Position - b.Position;
                float len2 = d.LengthSquared();
                if (len2 < min * min && len2 > 1e-6f)
                {
                    float len = MathF.Sqrt(len2);
                    Vector2 push = d / len * (min - len) * MathF.Min(1f, dt * 8f) * 0.5f;
                    a.Position += push;
                    b.Position -= push;
                }
            }
        }
    }

    private static float Radius(CritterDef d) => d.Id switch
    {
        "sapi" => 0.7f,
        "kambing" => 0.4f,
        "anjing" => 0.35f,
        "ayam" or "kucing" => 0.25f,
        "burung" => 0.12f,
        "ular" => 0.3f,
        _ => 0.35f,
    };

    private void UpdateOne(Critter c, float dt, IReadOnlyList<Vector2> threats)
    {
        CritterDef d = c.Def;
        c.Timer -= dt;

        // nearest threat
        float best = float.MaxValue;
        Vector2 threat = default;
        foreach (Vector2 t in threats)
        {
            float dist = Vector2.DistanceSquared(t, c.Position);
            if (dist < best)
            {
                best = dist;
                threat = t;
            }
        }

        best = MathF.Sqrt(best);
        bool danger = best < d.FleeRadius;

        // ambient calls
        if (d.SoundEvery > 0 && Chatter > 0 && c.State is not CritterState.Fly)
        {
            c.VoiceTimer -= dt * Chatter;
            if (c.VoiceTimer <= 0f)
            {
                c.VoiceTimer = R(d.SoundEvery * 0.6f, d.SoundEvery * 1.6f);
                string sound = d.Id == "ayam" && _rng.Next(5) == 0 ? "sfx_jago" : d.Sound;
                _audio?.PlayAt(sound, new Vector3(c.Position.X, 0.5f, c.Position.Y), 0.5f, R(0.9f, 1.15f), 0.8);
            }
        }

        if (danger && c.State is not (CritterState.Flee or CritterState.Fly))
        {
            Alarm(c, threat, best);
        }

        switch (c.State)
        {
            case CritterState.Idle:
                c.Model.SetBase("idle");
                if (c.Timer <= 0f)
                {
                    if (_rng.NextDouble() < d.ActChance && c.Model.Has("attack"))
                    {
                        c.State = CritterState.Act;
                        c.Timer = c.Model.Duration("attack") * 1.1f;
                        c.Model.PlayAction("attack", 1f);
                    }
                    else
                    {
                        float a = R(0, MathF.Tau), r = R(0.3f, 1f) * c.HomeRadius;
                        c.Target = c.Home + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * r);
                        c.State = CritterState.Wander;
                        c.Timer = 12f;
                    }
                }

                break;
            case CritterState.Act:
                if (c.Timer <= 0f)
                {
                    c.State = CritterState.Idle;
                    c.Timer = R(1f, 4f);
                }

                break;
            case CritterState.Wander:
                if (MoveTo(c, c.Target, d.WalkSpeed, dt, "walk", d.AnimWalk) || c.Timer <= 0f)
                {
                    c.State = CritterState.Idle;
                    c.Timer = R(1.5f, 5f);
                }

                break;
            case CritterState.Flee:
            {
                Vector2 away = c.Position - c.FleeFrom;
                away = away.LengthSquared() > 1e-4f ? Vector2.Normalize(away) : new Vector2(1, 0);
                // steer back inside the play area rather than running off the map
                if (c.Position.Length() > _bounds - 4f)
                {
                    away = Vector2.Normalize(away - (Vector2.Normalize(c.Position) * 1.5f));
                }

                Step(c, away, d.RunSpeed, dt, "run", d.AnimRun);
                if (danger)
                {
                    c.FleeFrom = threat;
                    c.Timer = 1.5f;
                }
                else if (c.Timer <= 0f)
                {
                    c.State = CritterState.Wander;
                    c.Target = c.Home;
                    c.Timer = 20f;
                    c.Alarmed = false;
                }

                break;
            }

            case CritterState.Fly:
                FlyAway(c, dt);
                break;
            case CritterState.Land:
                Landing(c, dt);
                break;
        }

        Pose(c, dt);
    }

    private void Alarm(Critter c, Vector2 threat, float distance)
    {
        CritterDef d = c.Def;
        c.Model.CancelAction();
        c.FleeFrom = threat;
        if (d.Kind == CritterKind.Flyer)
        {
            c.State = CritterState.Fly;
            c.Timer = R(6f, 10f);
            Vector2 away = c.Position - threat;
            c.Target = away.LengthSquared() > 1e-3f ? Vector2.Normalize(away) : new Vector2(0, 1);
            _audio?.PlayAt(d.Sound, new Vector3(c.Position.X, 0.5f, c.Position.Y), 0.6f, 1.2f, 0.3);
            return;
        }

        // a brave dog barks first when the zombie is still a few steps away
        if (d.Id == "anjing" && distance > 3f && !c.Alarmed)
        {
            c.Alarmed = true;
            c.Yaw = MathF.Atan2(threat.X - c.Position.X, threat.Y - c.Position.Y);
            c.Model.PlayAction("attack", 1.2f);
            _audio?.PlayAt("sfx_anjing", new Vector3(c.Position.X, 0.5f, c.Position.Y), 0.8f, R(0.9f, 1.1f), 0.4);
        }

        c.State = CritterState.Flee;
        c.Timer = 2f;
        if (d.Kind == CritterKind.Person)
        {
            _audio?.PlayAt(d.Sound, new Vector3(c.Position.X, 1.5f, c.Position.Y), 0.55f, R(0.9f, 1.15f), 0.9);
            _fx?.Emit(Sprite.Spark, new Vector3(c.Position.X, 2.3f, c.Position.Y), new Vector3(0, 1.5f, 0), 0.5f, 0.35f, 0.1f);
        }
        else if (d.Id == "ular")
        {
            _audio?.PlayAt("sfx_ular", new Vector3(c.Position.X, 0.2f, c.Position.Y), 0.7f, 1f, 0.8);
        }
        else if (d.Id == "ayam")
        {
            _audio?.PlayAt("sfx_ayam", new Vector3(c.Position.X, 0.3f, c.Position.Y), 0.8f, 1.3f, 0.4);
            _fx?.Emit(Sprite.Dust, new Vector3(c.Position.X, 0.3f, c.Position.Y), new Vector3(0, 0.8f, 0), 0.6f, 0.2f, 0.5f);
        }
    }

    private void FlyAway(Critter c, float dt)
    {
        c.Model.SetBase("run", 1.6f);
        c.Height = MathF.Min(c.Height + (dt * 3f), 6f);
        c.Position += c.Target * c.Def.RunSpeed * dt;
        c.Yaw = Player.AngleLerp(c.Yaw, MathF.Atan2(c.Target.X, c.Target.Y), dt * 6f);
        if (c.Timer <= 0f)
        {
            // circle back to a quiet spot near home
            c.State = CritterState.Land;
            float a = R(0, MathF.Tau);
            c.Target = c.Home + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * R(0, c.HomeRadius));
            c.Timer = 15f;
        }
    }

    private void Landing(Critter c, float dt)
    {
        c.Model.SetBase("run", 1.2f);
        Vector2 to = c.Target - c.Position;
        float len = to.Length();
        if (len > 0.2f)
        {
            Vector2 dir = to / len;
            c.Position += dir * MathF.Min(len, c.Def.RunSpeed * 0.8f * dt);
            c.Yaw = Player.AngleLerp(c.Yaw, MathF.Atan2(dir.X, dir.Y), dt * 5f);
        }

        // glide down as the landing spot comes near
        float want = Math.Clamp(len * 0.4f, 0f, 6f);
        c.Height += (want - c.Height) * MathF.Min(1f, dt * 2f);
        if ((len < 0.3f && c.Height < 0.15f) || c.Timer <= 0f)
        {
            c.Height = 0f;
            c.State = CritterState.Idle;
            c.Timer = R(2f, 5f);
        }
    }

    /// <summary>Walks towards a point; true when arrived.</summary>
    private bool MoveTo(Critter c, Vector2 target, float speed, float dt, string clip, float animRef)
    {
        Vector2 to = target - c.Position;
        float len = to.Length();
        if (len < 0.15f)
        {
            return true;
        }

        Step(c, to / len, speed, dt, clip, animRef);
        return false;
    }

    private void Step(Critter c, Vector2 dir, float speed, float dt, string clip, float animRef)
    {
        Vector2 next = c.Position + (dir * speed * dt);
        next = Vector2.Clamp(next, new Vector2(-_bounds), new Vector2(_bounds));
        if (_resolve is not null && c.Height <= 0.01f)
        {
            next = _resolve(next, 0.25f * c.Def.Scale);
        }

        c.Position = next;
        c.Yaw = Player.AngleLerp(c.Yaw, MathF.Atan2(dir.X, dir.Y), dt * 8f);
        c.Model.SetBase(clip, Math.Clamp(speed / MathF.Max(animRef, 0.05f), 0.5f, 2.5f));
    }

    private static void Pose(Critter c, float dt)
    {
        c.Model.Root.Position = new Vector3(c.Position.X, c.Height, c.Position.Y);
        c.Model.Root.EulerAngles = new Vector3(0, c.Yaw, 0);
        c.Model.Update(dt);
    }
}
