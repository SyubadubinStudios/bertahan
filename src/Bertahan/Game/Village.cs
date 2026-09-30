using System.Numerics;
using Bertahan.Core;
using ThreeNet;

namespace Bertahan.Game;

public enum Chore
{
    Sweep,
    Hoe,
    Stroll,
    Chat,
    Play,
}

/// <summary>A villager going about their day until a zombie shows up.</summary>
public sealed class Villager
{
    public required AnimatedModel Model;
    public required Chore Chore;
    public required Vector2 Home;
    public string? Tool;
    public Vector2 Position;
    public float Yaw;
    public float Face;
    public Vector2[] Route = [];
    public int RouteIndex;
    public float Timer;
    public float Phase;
    public bool Fleeing;
    public float FleeTime;
    public Vector2 FleeFrom;
    public bool Returning;
    public float Pitch = 1f;

    /// <summary>A pushcart (tukang bakso) rolled along in front of the villager.</summary>
    public Node? Cart;
}

/// <summary>A zombie wandering through the village along a path.</summary>
public sealed class Intruder
{
    public required AnimatedModel Model;
    public required string Id;
    public Vector2[] Path = [];
    public int Index;
    public Vector2 Position;
    public float Yaw;
    public float Speed;
    public bool Active;
    public float Pause;
    public float Voice;
    public float AttackCooldown;
}

/// <summary>
/// Kampung Damai at rest: houses, fields and a warung, villagers doing their
/// chores and, every now and then, a zombie wandering through and sending
/// everybody running. Shared by the title screen and the opening story.
/// </summary>
public sealed class VillageLife
{
    private readonly Scene _scene;
    private readonly PropLibrary _props;
    private readonly Node _root;
    private readonly AudioManager _audio;
    private readonly Particles _fx;
    private readonly Random _rng = new(42);
    private float _nextIntruder = 6f;

    public VillageLife(Scene scene, PropLibrary props, Node root, AudioManager audio, Particles fx)
    {
        _scene = scene;
        _props = props;
        _root = root;
        _audio = audio;
        _fx = fx;
        BuildGround();
        BuildProps();
        BuildVillagers();
        BuildIntruders();
    }

    public List<Villager> Villagers { get; } = [];

    public List<Intruder> Intruders { get; } = [];

    /// <summary>When true a zombie wanders in every 9-15 seconds.</summary>
    public bool RandomIntruders { get; set; } = true;

    public List<Node> Lamps { get; } = [];

    // ------------------------------------------------------------------ set

    private void BuildGround()
    {
        GroundPainter paint = new(1024, 90f, 99);
        Vector3 dirt = new(0.62f, 0.48f, 0.32f), dirt2 = new(0.5f, 0.38f, 0.25f);
        paint.Fill(new Vector3(0.45f, 0.62f, 0.28f), new Vector3(0.32f, 0.5f, 0.22f), 0.15f, 5);
        paint.Rect(new Vector2(-6, -6), new Vector2(6, 4), dirt, dirt2, 6, 1.4f);
        paint.Path([new Vector2(0, 3), new Vector2(0.5f, 14), new Vector2(-1, 45)], 3.2f, dirt, dirt2, 7);
        paint.Path([new Vector2(-45, 8), new Vector2(-10, 8.5f), new Vector2(12, 7.5f), new Vector2(45, 8)], 3.4f, dirt, dirt2, 8);
        paint.Rect(new Vector2(-18, -7), new Vector2(-8, 1), dirt, dirt2, 9, 1.2f);
        paint.Rect(new Vector2(12, -3), new Vector2(26, 5), new Vector3(0.36f, 0.42f, 0.22f), new Vector3(0.3f, 0.36f, 0.2f), 10, 0.6f);
        paint.Speckle(new Vector3(0.55f, 0.7f, 0.3f), 900, 1.5f, 11);
        Texture tex = _scene.CreateTexture(1024, 1024, paint.ToRgba());
        tex.SetSampler(WrapMode.ClampToEdge, WrapMode.ClampToEdge, true, true, 8);
        Material groundMat = _scene.CreateMaterial(MaterialOptions.Pbr(Vector4.One, 0, 0.95f) with { BaseColorMap = tex });
        ThreeNet.Interop.Vertex[] v =
        [
            new(new Vector3(-45, 0, -45), Vector3.UnitY, new Vector2(0, 0)),
            new(new Vector3(45, 0, -45), Vector3.UnitY, new Vector2(1, 0)),
            new(new Vector3(45, 0, 45), Vector3.UnitY, new Vector2(1, 1)),
            new(new Vector3(-45, 0, 45), Vector3.UnitY, new Vector2(0, 1)),
        ];
        Node ground = _scene.AddMesh(_scene.CreateGeometry(v, [0, 2, 1, 0, 3, 2]), groundMat, _root, "ground");
        ground.CastShadow = false;
        Node far = _scene.AddMesh(_scene.CreatePlaneGeometry(800, 800), _scene.CreateMaterial(MaterialOptions.Lambert(new Vector4(0.3f, 0.45f, 0.2f, 1))), _root);
        far.EulerAngles = new Vector3(-MathF.PI / 2, 0, 0);
        far.Position = new Vector3(0, -0.05f, 0);
        far.CastShadow = false;
    }

    private void P(string name, float x, float z, float yaw = 0f, float scale = 1f)
    {
        Node? node = _props.Place("prop_" + name, _root, new Vector3(x, 0, z), yaw, scale);
        if (Atmosphere.SwayerFor("prop_" + name, node, new Vector3(x, 0, z), yaw) is { } swayer)
        {
            Swayers.Add(swayer);
        }
    }

    /// <summary>Plants that bend in the wind.</summary>
    public List<Swayer> Swayers { get; } = [];

    private void BuildProps()
    {
        // the family home and its yard
        P("rumah_kuning", 0, -7.5f);
        P("pohon_kelapa", -5.5f, -3.5f, 0.4f);
        P("pohon_pisang", 6.2f, -3.8f, 1f);
        P("semak_b", -3.5f, -4.8f);
        P("semak_a", 3.8f, -4.6f);
        P("pagar_bambu", -3, -5.6f);
        P("pagar_bambu", 3, -5.6f);
        P("lampu_jalan", 4.5f, 3.5f, -MathF.PI / 2);
        P("sumur", 8.5f, -3.5f, 0.3f);

        // neighbours
        P("rumah_biru", -10, -10, 0.25f);
        P("rumah_panggung", 11, -10.5f, -0.3f);
        P("rumah_merah", -22, 1, MathF.PI / 2);
        P("rumah_hijau", -6, 21, MathF.PI);
        P("rumah_biru", 9, 21, MathF.PI + 0.2f);
        P("masjid", -24, -26, 0.2f);
        P("gapura", 0, 34);

        // warung corner
        P("kios_a", -14, -5, 0.35f);
        P("kios_b", -18.5f, -2.5f, 0.9f);
        P("gerobak", -9.5f, -1.5f, -0.7f);
        P("motor_merah", -7.5f, 2, 0.9f);
        P("motor_biru", -16, 3, -0.4f);
        P("karung", -12, -2.5f);
        P("tong_biru", -15.5f, 0.5f);

        // pos ronda by the crossroads
        P("pos_ronda", -9, 13, MathF.PI * 0.5f);
        P("tiang_listrik", -15, 10.5f);
        P("tiang_listrik", 16, 10);
        P("tiang_listrik", -24, 10);

        // rice field on the right
        for (int x = 0; x < 6; x++)
        {
            for (int z = 0; z < 4; z++)
            {
                P("padi", 13.5f + (x * 2.2f), -1.5f + (z * 2.0f), x * 0.7f + z);
            }
        }

        P("orang_sawah", 19, 1.5f, -0.4f);
        P("gubuk", 27, 1, -MathF.PI / 2);
        P("rumpun_bambu", 15, 14);
        P("tumpukan_kayu", 6, 12, 0.4f);

        // trees all around, the village sits in the green
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * MathF.Tau;
            float r = 30f + ((i * 7) % 5);
            string tree = (i % 3) switch { 0 => "pohon_kelapa", 1 => "pohon_mangga", _ => "pohon_pisang" };
            P(tree, MathF.Cos(a) * r, MathF.Sin(a) * r, a, 1.1f);
        }

        Random scatter = new(7);
        for (int i = 0; i < 70; i++)
        {
            float x = (float)((scatter.NextDouble() * 70) - 35), z = (float)((scatter.NextDouble() * 60) - 30);
            // keep the yards, roads and the fields clear
            if ((MathF.Abs(x) < 8 && z > -8 && z < 5) || MathF.Abs(z - 8) < 2.5f || MathF.Abs(x) < 2.5f || (x > 12 && x < 26 && z > -3 && z < 6))
            {
                continue;
            }

            P(i % 5 == 0 ? "semak_a" : "rumput", x, z, (float)scatter.NextDouble() * MathF.Tau, 0.8f + ((float)scatter.NextDouble() * 0.6f));
        }

        P("pohon_beringin", -27, 18, 0.5f);
        P("pohon_mangga", 22, -14, 0.3f);
        P("pohon_kelapa", -30, -8, 0.3f);

        foreach (Vector3 at in new[] { new Vector3(3.5f, 4.8f, 3.5f), new Vector3(-9, 3.2f, 13), new Vector3(-14, 3f, -3) })
        {
            Node lamp = _scene.AddLight(Light.Point(new Vector3(1f, 0.78f, 0.45f), 0f, 12f), _root, "lamp");
            lamp.Position = at;
            Lamps.Add(lamp);
        }
    }

    private Villager AddVillager(string model, Chore chore, Vector2 home, string? tool, float face = 0f, Vector2[]? route = null)
    {
        AnimatedModel m = AnimatedModel.Load(_scene, model, _root);
        m.ShowWeapon(tool);
        Villager v = new()
        {
            Model = m,
            Chore = chore,
            Home = home,
            Tool = tool,
            Position = route is { Length: > 0 } ? route[0] : home,
            Yaw = face,
            Face = face,
            Route = route ?? [],
            Phase = (float)_rng.NextDouble() * 5f,
            Pitch = model.Contains("bocah") ? 1.35f : model.Contains("pedagang") || model.Contains("jamu") || model.Contains("guru") ? 1.1f : 0.9f,
        };
        Villagers.Add(v);
        return v;
    }

    private void BuildVillagers()
    {
        AddVillager("npc_pedagang", Chore.Sweep, new Vector2(-12, -1f), "sapu", 0.3f);
        AddVillager("npc_petani", Chore.Hoe, new Vector2(15.5f, 3.2f), "pacul", -MathF.PI / 2);
        AddVillager("npc_petani", Chore.Hoe, new Vector2(21, -0.5f), "pacul", MathF.PI / 2);
        AddVillager("npc_ustad", Chore.Chat, new Vector2(-7.4f, 12.2f), null, -MathF.PI / 2 - 0.3f);
        AddVillager("npc_pedagang", Chore.Chat, new Vector2(-8.8f, 11), null, MathF.PI / 2 - 0.5f);
        AddVillager("npc_pedagang", Chore.Stroll, new Vector2(0, 8), null, 0f,
            [new Vector2(-20, 8.2f), new Vector2(-6, 8.6f), new Vector2(8, 7.6f), new Vector2(22, 8), new Vector2(8, 7.6f), new Vector2(-6, 8.6f)]);
        AddVillager("npc_ustad", Chore.Stroll, new Vector2(0, 20), null, 0f,
            [new Vector2(0.6f, 30), new Vector2(0.4f, 14), new Vector2(-12, 8.4f), new Vector2(0.4f, 14)]);
        for (int i = 0; i < 3; i++)
        {
            AddVillager("npc_bocah", Chore.Play, new Vector2(6, 16), i == 0 ? "bambu" : null).Phase = i * MathF.Tau / 3f;
        }

        // the rest of the village at work
        AddVillager("npc_hansip", Chore.Stroll, new Vector2(-9, 13), "pentungan", 0f,
            [new Vector2(-9, 11), new Vector2(-9, 3), new Vector2(-2, 4), new Vector2(3, 9), new Vector2(-4, 9.5f)]);
        Villager bakso = AddVillager("npc_bakso", Chore.Stroll, new Vector2(10, 8), null, 0f,
            [new Vector2(24, 7.8f), new Vector2(-18, 8.6f)]);
        bakso.Cart = _props.Place("prop_gerobak", _root, new Vector3(bakso.Position.X, 0, bakso.Position.Y));
        AddVillager("npc_jamu", Chore.Stroll, new Vector2(0, 18), null, 0f,
            [new Vector2(-1, 26), new Vector2(0.5f, 12), new Vector2(8, 7.8f), new Vector2(0.5f, 12)]);
        AddVillager("npc_ojek", Chore.Chat, new Vector2(-6.6f, 3.1f), null, 2.4f);
        AddVillager("npc_guru", Chore.Chat, new Vector2(8.9f, 15.2f), null, -1.9f);

        // livestock and wild animals
        _fauna = new Fauna(_scene, _root, _audio, _fx, 17) { Chatter = 0.8f };
        _fauna.Add("ayam", new Vector2(4.5f, -1.5f), 2.5f, 4);
        _fauna.Add("ayam", new Vector2(-15.5f, 1.5f), 2f, 3);
        _fauna.Add("kambing", new Vector2(13.5f, -6), 2.2f, 2);
        _fauna.Add("sapi", new Vector2(24, 11), 3f, 2);
        _fauna.Add("kucing", new Vector2(-2.5f, -6.2f), 1.5f, 1);
        _fauna.Add("kucing", new Vector2(-11, -6.5f), 2f, 1);
        _fauna.Add("anjing", new Vector2(-8, 9), 3f, 1);
        _fauna.Add("burung", new Vector2(17, 12), 4f, 5);
        _fauna.Add("burung", new Vector2(-5, 17), 3f, 4);
        _fauna.Add("ular", new Vector2(12, -5.5f), 2.5f, 1);
    }

    private Fauna? _fauna;

    private readonly List<Vector2> _threats = [];

    private void BuildIntruders()
    {
        foreach (string id in new[] { "warga", "tuyul", "pocong", "satpam", "kuntilanak", "warga" })
        {
            AnimatedModel z = AnimatedModel.Load(_scene, "zombie_" + id, _root);
            z.Root.Visible = false;
            Intruders.Add(new Intruder { Model = z, Id = id });
        }
    }

    // ------------------------------------------------------------------ control

    private static readonly Vector2[][] Paths =
    [
        [new(44, 8), new(10, 7.6f), new(-10, 8.6f), new(-44, 8)],
        [new(-44, 8.2f), new(-12, 8.4f), new(-12, -1), new(-26, -12)],
        [new(0.5f, 44), new(0.4f, 14), new(14, 4), new(40, -6)],
        [new(-40, -20), new(-14, -3), new(-4, 9), new(0.5f, 44)],
        [new(40, 20), new(20, 2), new(8, 12), new(-10, 12), new(-40, 16)],
    ];

    /// <summary>Sends a zombie through the village. Returns false when all of that kind are busy.</summary>
    public bool SpawnIntruder(string? id = null, Vector2[]? path = null, float speedScale = 1f)
    {
        Intruder? z = Intruders.FirstOrDefault(i => !i.Active && (id is null || i.Id == id));
        if (z is null)
        {
            return false;
        }

        z.Path = path ?? Paths[_rng.Next(Paths.Length)];
        if (path is null && _rng.Next(2) == 0)
        {
            z.Path = z.Path.Reverse().ToArray();
        }

        z.Index = 1;
        z.Position = z.Path[0];
        z.Speed = (z.Id switch { "tuyul" => 3.6f, "pocong" => 2.2f, "kuntilanak" => 2.4f, "satpam" => 1.5f, _ => 1.4f }) * speedScale;
        z.Active = true;
        z.Pause = 0f;
        z.Voice = 1f;
        z.Model.Root.Visible = true;
        z.Model.CancelAction();
        z.Model.SetBase(z.Id is "tuyul" or "pocong" ? "run" : "walk");
        return true;
    }

    public void HideIntruders()
    {
        foreach (Intruder z in Intruders)
        {
            z.Active = false;
            z.Model.Root.Visible = false;
        }
    }

    public void Update(float dt)
    {
        if (RandomIntruders)
        {
            _nextIntruder -= dt;
            if (_nextIntruder <= 0f)
            {
                _nextIntruder = 9f + ((float)_rng.NextDouble() * 6f);
                if (Intruders.Count(i => i.Active) < 2)
                {
                    SpawnIntruder(Intruders[_rng.Next(Intruders.Count)].Id);
                }
            }
        }

        foreach (Intruder z in Intruders)
        {
            UpdateIntruder(z, dt);
        }

        foreach (Villager v in Villagers)
        {
            UpdateVillager(v, dt);
        }

        _threats.Clear();
        foreach (Intruder z in Intruders)
        {
            if (z.Active)
            {
                _threats.Add(z.Position);
            }
        }

        _fauna?.Update(dt, _threats);
    }

    private void UpdateIntruder(Intruder z, float dt)
    {
        if (!z.Active)
        {
            return;
        }

        z.AttackCooldown -= dt;
        z.Voice -= dt;
        if (z.Voice <= 0f)
        {
            z.Voice = 3f + ((float)_rng.NextDouble() * 4f);
            string sound = z.Id switch { "tuyul" => "sfx_tuyul", "kuntilanak" => "sfx_kunti", _ => $"sfx_groan_{_rng.Next(3)}" };
            _audio.PlayAt(sound, new Vector3(z.Position.X, 1, z.Position.Y), 0.6f, 0.9f + ((float)_rng.NextDouble() * 0.2f), 0.6);
        }

        if (z.Pause > 0f)
        {
            z.Pause -= dt;
        }
        else
        {
            Vector2 target = z.Path[z.Index];
            Vector2 d = target - z.Position;
            float len = d.Length();
            if (len < 0.3f)
            {
                z.Index++;
                if (z.Index >= z.Path.Length)
                {
                    z.Active = false;
                    z.Model.Root.Visible = false;
                    return;
                }
            }
            else
            {
                Vector2 dir = d / len;
                z.Position += dir * MathF.Min(len, z.Speed * dt);
                z.Yaw = Player.AngleLerp(z.Yaw, MathF.Atan2(dir.X, dir.Y), dt * 6f);
            }

            // lunge at a villager that did not run fast enough
            foreach (Villager v in Villagers)
            {
                if (z.AttackCooldown <= 0f && Vector2.Distance(v.Position, z.Position) < 1.6f)
                {
                    z.AttackCooldown = 3f;
                    z.Pause = 0.8f;
                    z.Model.PlayAction("attack", 1.2f);
                    z.Yaw = MathF.Atan2(v.Position.X - z.Position.X, v.Position.Y - z.Position.Y);
                    break;
                }
            }
        }

        z.Model.Root.Position = new Vector3(z.Position.X, 0, z.Position.Y);
        z.Model.Root.EulerAngles = new Vector3(0, z.Yaw, 0);
        z.Model.Update(dt);
    }

    private void UpdateVillager(Villager v, float dt)
    {
        v.Timer -= dt;
        v.Phase += dt;

        // who is the closest zombie?
        Intruder? threat = null;
        float best = float.MaxValue;
        foreach (Intruder z in Intruders)
        {
            if (z.Active)
            {
                float d = Vector2.Distance(z.Position, v.Position);
                if (d < best)
                {
                    best = d;
                    threat = z;
                }
            }
        }

        if (threat is not null && best < 6.5f)
        {
            if (!v.Fleeing)
            {
                v.Fleeing = true;
                v.Model.CancelAction();
                _audio.PlayAt(v.Pitch > 1f ? "sfx_teriak_wanita" : "sfx_teriak_pria", new Vector3(v.Position.X, 1.5f, v.Position.Y), 0.55f, v.Pitch, 0.8);
                _fx.Emit(Sprite.Spark, new Vector3(v.Position.X, 2.3f, v.Position.Y), new Vector3(0, 1.5f, 0), 0.5f, 0.35f, 0.1f);
            }

            v.FleeFrom = threat.Position;
            v.FleeTime = 2.5f;
        }

        if (v.Fleeing)
        {
            v.FleeTime -= dt;
            Vector2 away = v.Position - v.FleeFrom;
            away = away.LengthSquared() > 1e-3f ? Vector2.Normalize(away) : new Vector2(1, 0);
            // bend away from the village edge so nobody runs off the map
            if (v.Position.Length() > 26f)
            {
                away = Vector2.Normalize(away - (Vector2.Normalize(v.Position) * 1.2f));
            }

            Move(v, away, 4.6f, dt, "run");
            if (v.FleeTime <= 0f)
            {
                v.Fleeing = false;
                v.Returning = true;
            }

            Pose(v, dt);
            return;
        }

        switch (v.Chore)
        {
            case Chore.Stroll:
                if (v.Route.Length > 0)
                {
                    Vector2 target = v.Route[v.RouteIndex];
                    if (Vector2.Distance(v.Position, target) < 0.4f)
                    {
                        v.RouteIndex = (v.RouteIndex + 1) % v.Route.Length;
                    }
                    else
                    {
                        Move(v, Vector2.Normalize(target - v.Position), 1.4f, dt, "walk");
                    }
                }

                v.Returning = false;
                break;
            case Chore.Play:
            {
                // kids chase each other round and round
                float a = v.Phase * 0.9f;
                Vector2 target = v.Home + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * 2.6f);
                Vector2 d = target - v.Position;
                if (v.Returning && d.Length() > 1.2f)
                {
                    Move(v, Vector2.Normalize(d), 3f, dt, "run");
                }
                else
                {
                    v.Returning = false;
                    v.Position = Vector2.Lerp(v.Position, target, 1f - MathF.Exp(-8f * dt));
                    Vector2 tangent = new(-MathF.Sin(a), MathF.Cos(a));
                    v.Yaw = MathF.Atan2(tangent.X, tangent.Y);
                    v.Model.SetBase("run", 0.8f);
                }

                break;
            }

            default:
                if (Vector2.Distance(v.Position, v.Home) > 0.3f)
                {
                    Move(v, Vector2.Normalize(v.Home - v.Position), v.Returning ? 1.8f : 1.2f, dt, "walk");
                    break;
                }

                v.Returning = false;
                v.Yaw = Player.AngleLerp(v.Yaw, v.Face, dt * 4f);
                Work(v);
                break;
        }

        Pose(v, dt);
    }

    private void Work(Villager v)
    {
        switch (v.Chore)
        {
            case Chore.Sweep when v.Timer <= 0f:
                v.Timer = 1.1f;
                v.Model.SetBase("idle");
                v.Model.PlayAction("swing", 0.55f);
                if (_rng.Next(3) == 0)
                {
                    _fx.Dust(new Vector3(v.Position.X, 0, v.Position.Y) + (new Vector3(MathF.Sin(v.Yaw), 0, MathF.Cos(v.Yaw)) * 1.2f), 2, 0.35f);
                }

                break;
            case Chore.Hoe when v.Timer <= 0f:
                v.Timer = 1.8f;
                v.Model.SetBase("idle");
                v.Model.PlayAction("swing", 0.45f);
                _fx.Dust(new Vector3(v.Position.X, 0, v.Position.Y) + (new Vector3(MathF.Sin(v.Yaw), 0, MathF.Cos(v.Yaw)) * 1.3f), 3, 0.4f);
                break;
            case Chore.Chat when v.Timer <= 0f:
                // talking with the hands, now and then a good laugh
                v.Timer = 2f + ((float)_rng.NextDouble() * 3f);
                v.Model.SetBase(_rng.Next(4) == 0 ? "cheer" : "idle");
                break;
            case Chore.Sweep or Chore.Hoe or Chore.Chat:
                break;
            default:
                v.Model.SetBase("idle");
                break;
        }
    }

    private static void Move(Villager v, Vector2 dir, float speed, float dt, string clip)
    {
        v.Position += dir * speed * dt;
        v.Position = Vector2.Clamp(v.Position, new Vector2(-40), new Vector2(40));
        v.Yaw = Player.AngleLerp(v.Yaw, MathF.Atan2(dir.X, dir.Y), dt * 10f);
        v.Model.SetBase(clip, clip == "run" ? speed / 5f : speed / 1.6f);
    }

    private static void Pose(Villager v, float dt)
    {
        v.Model.Root.Position = new Vector3(v.Position.X, 0, v.Position.Y);
        v.Model.Root.EulerAngles = new Vector3(0, v.Yaw, 0);
        if (v.Cart is not null)
        {
            Vector2 ahead = v.Position + (new Vector2(MathF.Sin(v.Yaw), MathF.Cos(v.Yaw)) * 1.3f);
            v.Cart.Position = new Vector3(ahead.X, 0, ahead.Y);
            v.Cart.EulerAngles = new Vector3(0, v.Yaw + (MathF.PI / 2), 0);
        }

        v.Model.Update(dt);
    }
}
