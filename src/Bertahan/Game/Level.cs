using System.Numerics;
using ThreeNet;
using ThreeNet.Interop;

namespace Bertahan.Game;

/// <summary>A built level: the static world, collision and the places things appear.</summary>
public sealed class Level
{
    public required LevelDef Def { get; init; }

    public required Navigation Nav { get; init; }

    public Vector2 PlayerStart { get; set; }

    public List<Vector2> SpawnPoints { get; } = [];

    public List<Vector2> PickupPoints { get; } = [];

    /// <summary>Weapons lying around at the start: (weapon id, position).</summary>
    public List<(string Weapon, Vector2 Position)> WeaponSpots { get; } = [];

    /// <summary>Shallow water (rice paddies) slows everyone down.</summary>
    public List<(Vector2 Min, Vector2 Max)> SlowZones { get; } = [];

    public List<Node> Lamps { get; } = [];

    public byte[] MinimapPixels { get; set; } = [];

    public int MinimapSize { get; set; }

    public Vector2 BossSpot { get; set; }

    /// <summary>Tall props (houses, trees) that are hidden while they stand between the camera and the player.</summary>
    public List<Occluder> Occluders { get; } = [];

    /// <summary>Plants that bend in the wind (see <see cref="Atmosphere"/>).</summary>
    public List<Swayer> Swayers { get; } = [];

    public bool IsNight => Def.Time == TimeOfDay.Malam;

    public bool InSlowZone(Vector2 p)
    {
        foreach ((Vector2 min, Vector2 max) in SlowZones)
        {
            if (p.X > min.X && p.X < max.X && p.Y > min.Y && p.Y < max.Y)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class Occluder(Node node, Vector2 centre, float radius)
{
    public Node Node { get; } = node;

    public Vector2 Centre { get; } = centre;

    public float Radius { get; } = radius;

    public bool Hidden { get; set; }
}

/// <summary>Builds the four levels from Blender props, a painted ground and lights.</summary>
public sealed class LevelBuilder
{
    private const float World = 96f;
    private const float Half = 38f;
    private readonly Scene _scene;
    private readonly PropLibrary _props;
    private readonly Node _root;
    private readonly Random _rng;
    private Level _level = null!;
    private GroundPainter _ground = null!;

    public LevelBuilder(Scene scene, PropLibrary props, int seed)
    {
        _scene = scene;
        _props = props;
        _root = scene.CreateNode(null, "level");
        _rng = new Random(seed);
    }

    private static Vector3 Hex(uint rgb) => new(
        MathHelpers.SrgbToLinear(((rgb >> 16) & 255) / 255f),
        MathHelpers.SrgbToLinear(((rgb >> 8) & 255) / 255f),
        MathHelpers.SrgbToLinear((rgb & 255) / 255f));

    private static Vector3 Srgb(uint rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

    private float R(float a, float b) => a + ((float)_rng.NextDouble() * (b - a));

    public Level Build(LevelDef def)
    {
        _level = new Level { Def = def, Nav = new Navigation(new Vector2(-Half), new Vector2(Half)) };
        _ground = new GroundPainter(1024, World, def.Number * 17);
        switch (def.Id)
        {
            case "gerbang":
                Gerbang();
                break;
            case "sawah":
                Sawah();
                break;
            case "pasar":
                Pasar();
                break;
            default:
                Kuburan();
                break;
        }

        BuildGround();
        _level.Nav.Bake();
        _level.MinimapSize = 128;
        _level.MinimapPixels = _ground.Minimap(128);
        return _level;
    }

    // ------------------------------------------------------------------ placement helpers

    private enum Col
    {
        Auto,
        None,
        Trunk,
    }

    private Node? Prop(string name, float x, float z, float yaw = 0f, Col col = Col.Auto, float scale = 1f, float shrink = 0.92f)
    {
        Node? node = _props.Place("prop_" + name, _root, new Vector3(x, 0, z), yaw, scale);
        if (Atmosphere.SwayerFor("prop_" + name, node, new Vector3(x, 0, z), yaw) is { } swayer)
        {
            _level.Swayers.Add(swayer);
        }

        if (node is not null && _props.Get("prop_" + name) is { } tall && tall.Max.Y * scale > 2.8f)
        {
            float r = MathF.Max(tall.Max.X - tall.Min.X, tall.Max.Z - tall.Min.Z) * 0.5f * scale;
            _level.Occluders.Add(new Occluder(node, new Vector2(x, z), MathF.Max(r, 1.2f)));
        }

        if (node is null || col == Col.None || _props.Get("prop_" + name) is not { } proto)
        {
            return node;
        }

        if (col == Col.Trunk)
        {
            _level.Nav.Add(Obstacle.Circle(new Vector2(x, z), 0.45f * scale));
            return node;
        }

        Vector2 min = new(proto.Min.X, proto.Min.Z), max = new(proto.Max.X, proto.Max.Z);
        Vector2 centreLocal = (min + max) * 0.5f * scale;
        Vector2 half = (max - min) * 0.5f * scale * shrink;
        // rotate the local centre offset by yaw (same convention as Obstacle)
        float c = MathF.Cos(yaw), s = MathF.Sin(yaw);
        Vector2 centre = new Vector2(x, z) + new Vector2((centreLocal.X * c) + (centreLocal.Y * s), (-centreLocal.X * s) + (centreLocal.Y * c));
        _level.Nav.Add(Obstacle.Box(centre, half, yaw));
        return node;
    }

    private void Circle(float x, float z, float r) => _level.Nav.Add(Obstacle.Circle(new Vector2(x, z), r));

    private void Lamp(float x, float z, float yaw, Vector3 color, float intensity = 14f, float range = 14f, bool pole = true)
    {
        if (pole)
        {
            Prop("lampu_jalan", x, z, yaw, Col.Trunk);
        }

        // the lamp head hangs 1 m in front of the pole
        Vector3 head = new Vector3(x, 4.8f, z) + (Vector3.Transform(new Vector3(0, 0, 1.05f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw)) * (pole ? 1 : 0));
        Node light = _scene.AddLight(Light.Point(color, intensity, range), _root, "lamp");
        light.Position = pole ? head : new Vector3(x, 1.2f, z);
        _level.Lamps.Add(light);
    }

    private void Scatter(string name, int count, Vector2 min, Vector2 max, float scaleMin = 0.8f, float scaleMax = 1.2f, Col col = Col.None, float avoid = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 p = new(R(min.X, max.X), R(min.Y, max.Y));
            if (avoid > 0f && Vector2.Distance(p, _level.PlayerStart) < avoid)
            {
                continue;
            }

            Prop(name, p.X, p.Y, R(0, MathF.Tau), col, R(scaleMin, scaleMax));
        }
    }

    /// <summary>Trees, bamboo and fences along the arena border so the edge feels natural.</summary>
    private void Border(string[] trees, float density = 1f)
    {
        for (float t = -Half; t <= Half; t += 5.5f / density)
        {
            foreach ((float x, float z) in new[] { (t, -Half - 2.5f), (t, Half + 2.5f), (-Half - 2.5f, t), (Half + 2.5f, t) })
            {
                string tree = trees[_rng.Next(trees.Length)];
                Prop(tree, x + R(-1.5f, 1.5f), z + R(-1.5f, 1.5f), R(0, MathF.Tau), Col.None, R(0.8f, 1.25f));
            }
        }
    }

    // ------------------------------------------------------------------ level 1

    private void Gerbang()
    {
        Vector3 grass1 = Srgb(0x6FA046), grass2 = Srgb(0x4E8A36);
        Vector3 dirt1 = Srgb(0x9C7A52), dirt2 = Srgb(0x7E5E3C);
        _ground.Fill(grass1, grass2, 0.12f, 3);
        Vector2[] road = [new(0, -48), new(0, -20), new(2, -6), new(0, 8), new(-3, 22), new(0, 48)];
        _ground.Path(road, 6.5f, dirt1, dirt2, 5);
        _ground.Path([new(-48, 4), new(-14, 3), new(0, 2), new(16, 0), new(48, -2)], 4f, dirt1, dirt2, 6);
        _ground.Rect(new Vector2(-7, -9), new Vector2(7, 6), dirt1 * 1.05f, dirt2, 8, 1.5f);
        _ground.Speckle(Srgb(0x8D8D80), 900, 1.5f, 9);
        _ground.Speckle(Srgb(0x88B84E), 1500, 2f, 10);
        _level.PlayerStart = new Vector2(0, 4);

        // gate at the north end, zombies pour in through it
        Prop("gapura", 0, -30, 0, Col.None);
        foreach (float x in new[] { -2.8f, 2.8f })
        {
            Circle(x, -30, 0.5f);
        }

        for (float x = -34; x <= 34; x += 2f)
        {
            if (MathF.Abs(x) > 5f)
            {
                Prop("pagar_bambu", x, -30.5f, 0, Col.None);
            }
        }

        _level.Nav.Add(Obstacle.Box(new Vector2(-20, -30.5f), new Vector2(15, 0.25f), 0));
        _level.Nav.Add(Obstacle.Box(new Vector2(20, -30.5f), new Vector2(15, 0.25f), 0));
        Prop("pos_ronda", 7.5f, -24, -0.4f);
        Prop("barikade", -3.5f, -24, 0.3f);
        Prop("barikade", 4.5f, -19, -0.5f);
        Prop("motor_merah", -7, -26, 0.8f);
        Prop("ban_bekas", -9, -23);

        // houses along the road, facing it
        string[] houses = ["rumah_kuning", "rumah_biru", "rumah_hijau", "rumah_merah", "rumah_panggung"];
        float[] zs = [-20, -9, 13, 24];
        int k = 0;
        foreach (float z in zs)
        {
            Prop(houses[k++ % houses.Length], -13, z, MathF.PI / 2);
            Prop(houses[k++ % houses.Length], 14, z + 2, -MathF.PI / 2);
        }

        Prop("masjid", -28, -18, 0.3f);
        Prop("sumur", -7, 18);
        Prop("gerobak", 7, 10, 0.9f);
        Prop("motor_biru", 9, -12, -0.6f);
        Prop("tumpukan_kayu", -6, -15, 0.2f);
        Prop("peti", 6, 5, 0.3f);
        Prop("peti", 6.8f, 4.2f, 1.1f);
        Prop("karung", -6, 9, 1.4f);
        Prop("tong_biru", 5.2f, -3);
        Prop("tong_merah", -6, -4);
        Prop("tiang_listrik", 4, -16, 0, Col.Trunk);
        Prop("tiang_listrik", -4, 16, 0, Col.Trunk);
        Lamp(-4, -8, MathF.PI / 2, Hex(0xFFD9A0));
        Lamp(4.5f, 20, -MathF.PI / 2, Hex(0xFFD9A0));

        // rice field to the east with a hut
        _level.SlowZones.Add((new Vector2(22, -26), new Vector2(36, 8)));
        Paddy(new Vector2(22, -26), new Vector2(36, 8), 3);
        Prop("gubuk", 30, 14, 0.4f);
        Prop("orang_sawah", 28, -8, 0.3f, Col.Trunk);

        // greenery
        foreach (Vector2 p in new Vector2[] { new(-22, 2), new(-20, 30), new(22, 30), new(-30, 20), new(-24, -5), new(26, 20), new(19, -30) })
        {
            Prop(_rng.Next(2) == 0 ? "pohon_kelapa" : "pohon_mangga", p.X, p.Y, R(0, 6), Col.Trunk, R(0.9f, 1.15f));
        }

        foreach (Vector2 p in new Vector2[] { new(-8, -12), new(9, 18), new(-9, 26), new(8, -26), new(-18, 8) })
        {
            Prop("pohon_pisang", p.X, p.Y, R(0, 6), Col.Trunk);
        }

        Prop("rumpun_bambu", -33, 8, 0, Col.Trunk, 1.1f);
        Prop("rumpun_bambu", 33, 26, 0, Col.Trunk);
        Scatter("semak_a", 14, new Vector2(-36, -28), new Vector2(-16, 34), col: Col.None, avoid: 6);
        Scatter("semak_b", 10, new Vector2(16, 10), new Vector2(36, 34), col: Col.None);
        Scatter("rumput", 70, new Vector2(-36, -28), new Vector2(36, 36), 0.8f, 1.6f, avoid: 3);
        Scatter("batu_a", 8, new Vector2(-30, -28), new Vector2(30, 30), 0.6f, 1.2f);
        Border(["pohon_kelapa", "pohon_mangga", "rumpun_bambu", "pohon_pisang"]);

        _level.SpawnPoints.AddRange([new(0, -36), new(-2, -35), new(2, -35), new(-30, -34), new(30, -34), new(-35, 0), new(35, -12), new(0, 36), new(-30, 34), new(30, 34)]);
        _level.PickupPoints.AddRange([new(-4, 0), new(4, 8), new(0, -12), new(-8, 22), new(9, -6), new(18, -18)]);
        _level.WeaponSpots.AddRange([("linggis", new Vector2(-5, 12)), ("kampak", new Vector2(7, -8)), ("pacul", new Vector2(24, 10))]);
        _level.BossSpot = new Vector2(0, -34);
    }

    // ------------------------------------------------------------------ level 2

    private void Sawah()
    {
        Vector3 grass1 = Srgb(0x4B7A3A), grass2 = Srgb(0x355F2C);
        Vector3 dike1 = Srgb(0x7A6242), dike2 = Srgb(0x5E4A32);
        _ground.Fill(grass1, grass2, 0.1f, 21);
        _level.PlayerStart = new Vector2(0, 0);

        // a grid of paddies separated by walkable dikes (pematang)
        float[] edges = [-34, -20, -7, 7, 20, 34];
        for (int i = 0; i < edges.Length - 1; i++)
        {
            for (int j = 0; j < edges.Length - 1; j++)
            {
                Vector2 min = new(edges[i] + 1.2f, edges[j] + 1.2f), max = new(edges[i + 1] - 1.2f, edges[j + 1] - 1.2f);
                bool plaza = i == 2 && j == 2;
                if (plaza)
                {
                    continue;
                }

                _level.SlowZones.Add((min, max));
                Paddy(min, max, (i * 5) + j);
            }
        }

        foreach (float e in edges)
        {
            _ground.Path([new(e, -48), new(e, 48)], 2.4f, dike1, dike2, 31);
            _ground.Path([new(-48, e), new(48, e)], 2.4f, dike1, dike2, 32);
        }

        _ground.Rect(new Vector2(-6, -6), new Vector2(6, 6), dike1, dike2, 33, 1.2f);
        Prop("gubuk", 3, -3, 0.3f);
        Prop("orang_sawah", -13, -13, 0.4f, Col.Trunk);
        Prop("orang_sawah", 13, 27, -0.6f, Col.Trunk);
        Prop("orang_sawah", 27, -13, 1.2f, Col.Trunk);
        Prop("pohon_beringin", -28, -28, 0.3f, Col.Trunk, 1.1f);
        Circle(-28, -28, 1.6f);
        Prop("rumah_panggung", 28, 28, -2.3f);
        Prop("gubuk", -27, 27, 0.8f);
        Prop("nisan_a", -24, -33, 0.1f);
        Prop("nisan_c", -21, -34, -0.2f);
        Prop("pohon_pisang", 6, 8, 0, Col.Trunk);
        Prop("pohon_pisang", -8, 5, 0, Col.Trunk);
        Prop("tumpukan_kayu", -4, 4, 0.5f);
        Prop("gerobak", 5, 4, 2.2f);
        // lanterns on the dikes (no poles, low warm lights)
        foreach (Vector2 p in new Vector2[] { new(-7, -20), new(20, 7), new(-20, 20), new(7, -34), new(0, 0) })
        {
            Prop("lilin", p.X + 0.8f, p.Y + 0.8f, 0, Col.None, 1.3f);
            Lamp(p.X + 0.8f, p.Y + 0.8f, 0, Hex(0xFFB060), 5f, 9f, pole: false);
        }

        Border(["pohon_kelapa", "pohon_pisang", "rumpun_bambu", "pohon_mangga"], 0.9f);
        Scatter("rumput", 60, new Vector2(-36, -36), new Vector2(36, 36), 0.8f, 1.5f);
        _level.SpawnPoints.AddRange([new(-34, -34), new(34, -34), new(-34, 34), new(34, 34), new(0, -35), new(0, 35), new(-35, 0), new(35, 0), new(-20, -34), new(20, 34)]);
        _level.PickupPoints.AddRange([new(0, 3), new(-7, -7), new(7, 7), new(-20, 7), new(20, -7), new(7, -20)]);
        _level.WeaponSpots.AddRange([("kampak", new Vector2(-3, 5)), ("pacul", new Vector2(7, -7)), ("senapan", new Vector2(-20, -7))]);
        _level.BossSpot = new Vector2(-24, -24);
    }

    /// <summary>A rice paddy: a water surface with rows of rice clumps.</summary>
    private void Paddy(Vector2 min, Vector2 max, int seed)
    {
        Vector2 size = max - min;
        Vector2 centre = (min + max) * 0.5f;
        Material water = WaterMaterial;
        Node plane = _scene.AddMesh(Flat(size.X, size.Y, size.X / 4f), water, _root, "paddy");
        plane.Position = new Vector3(centre.X, 0.04f, centre.Y);
        plane.CastShadow = false;
        _ground.Rect(min, max, Srgb(0x3A4A2A), Srgb(0x2E3C22), seed, 0.4f);
        Random r = new(seed);
        for (float x = min.X + 0.9f; x < max.X - 0.5f; x += 1.8f)
        {
            for (float z = min.Y + 0.9f; z < max.Y - 0.5f; z += 1.8f)
            {
                if (r.NextDouble() < 0.72)
                {
                    Prop("padi", x + R(-0.2f, 0.2f), z + R(-0.2f, 0.2f), R(0, 6), Col.None, R(0.9f, 1.3f));
                }
            }
        }
    }

    private Material? _water;

    private Material WaterMaterial => _water ??= _scene.CreateMaterial(MaterialOptions.Pbr(new Vector4(Hex(0x3E6A78), 0.82f), 0.1f, 0.05f) with
    {
        AlphaMode = AlphaMode.Blend,
        Reflectance = 0.8f,
    });

    // ------------------------------------------------------------------ level 3

    private void Pasar()
    {
        Vector3 asphalt1 = Srgb(0x7A7268), asphalt2 = Srgb(0x5E5850);
        _ground.Fill(Srgb(0x6E8A48), Srgb(0x587238), 0.1f, 41);
        _ground.Rect(new Vector2(-40, -40), new Vector2(40, 40), asphalt1, asphalt2, 42, 2f);
        _ground.Rect(new Vector2(-9, -34), new Vector2(9, 34), Srgb(0x8E8474), Srgb(0x777062), 43, 0.5f, tiles: true);
        _ground.Speckle(Srgb(0x4A443E), 1600, 1.2f, 44);
        _ground.Speckle(Srgb(0xB85A3A), 120, 2f, 45);
        _level.PlayerStart = new Vector2(0, 6);

        // two rows of shophouses facing the market street
        string[] rukos = ["ruko_a", "ruko_b", "ruko_c"];
        for (int i = 0; i < 7; i++)
        {
            float z = -30 + (i * 10);
            Prop(rukos[i % 3], -15, z, MathF.PI / 2);
            Prop(rukos[(i + 1) % 3], 15, z, -MathF.PI / 2);
        }

        // market stalls in the middle
        string[] kios = ["kios_a", "kios_b", "kios_c"];
        foreach (float z in new[] { -24f, -12f, 12f, 24f })
        {
            Prop(kios[_rng.Next(3)], -5, z, MathF.PI / 2);
            Prop(kios[_rng.Next(3)], 5, z + 3, -MathF.PI / 2);
        }

        Prop("masjid", -30, -30, 0.5f);
        foreach (Vector2 p in new Vector2[] { new(-9, -18), new(9, 16), new(-2, -30), new(8, -4), new(-8, 20) })
        {
            Prop("peti", p.X, p.Y, R(0, 3));
            Prop("karung", p.X + 1.2f, p.Y + R(-1, 1), R(0, 3));
        }

        Prop("motor_merah", -10, 2, 1.3f);
        Prop("motor_biru", -10.5f, 4, 1.4f);
        Prop("motor_merah", 10, -14, -1.8f);
        Prop("gerobak", 9, 28, -0.4f);
        Prop("tong_merah", -9, -6);
        Prop("tong_biru", 9.5f, 6);
        Prop("ban_bekas", 10, -24);
        Prop("tumpukan_kayu", -10, 28, 0.8f);
        Prop("barikade", 0, -34, 0);
        Prop("barikade", 0, 34, 0.8f);
        for (int i = 0; i < 4; i++)
        {
            Lamp(-8.5f, -26 + (i * 17), MathF.PI / 2, Hex(0xFFC27A), 16f, 16f);
            Lamp(8.5f, -18 + (i * 17), -MathF.PI / 2, Hex(0xFFC27A), 16f, 16f);
        }

        // side yards behind the shops
        Scatter("pohon_pisang", 8, new Vector2(-36, -32), new Vector2(-24, 32), col: Col.Trunk);
        Scatter("pohon_mangga", 6, new Vector2(24, -32), new Vector2(36, 32), col: Col.Trunk);
        Scatter("semak_a", 12, new Vector2(-36, -36), new Vector2(36, 36));
        Border(["pohon_mangga", "pohon_kelapa", "rumpun_bambu"]);
        _level.SpawnPoints.AddRange([new(0, -36), new(0, 36), new(-3, -35), new(3, 35), new(-30, 0), new(30, 0), new(-30, -20), new(30, 20), new(-30, 30), new(30, -30)]);
        _level.PickupPoints.AddRange([new(0, 0), new(0, -18), new(0, 18), new(-8, 8), new(8, -8), new(-25, 10)]);
        _level.WeaponSpots.AddRange([("linggis", new Vector2(2, -8)), ("senapan", new Vector2(-2, 20)), ("pacul", new Vector2(-26, -8)), ("kampak", new Vector2(26, 8))]);
        _level.BossSpot = new Vector2(0, -33);
    }

    // ------------------------------------------------------------------ level 4

    private void Kuburan()
    {
        _ground.Fill(Srgb(0x3F5A32), Srgb(0x2C4226), 0.14f, 61);
        _ground.Path([new(0, 48), new(0, 10), new(-2, -10), new(0, -30)], 3.5f, Srgb(0x6E6250), Srgb(0x54493A), 62);
        _ground.Rect(new Vector2(-8, -34), new Vector2(8, -24), Srgb(0x4E4638), Srgb(0x3A3328), 63, 1.5f);
        _ground.Speckle(Srgb(0x2A3A20), 2000, 2f, 64);
        _ground.Speckle(Srgb(0x6A5E4E), 700, 1.5f, 65);
        _level.PlayerStart = new Vector2(0, 20);

        Prop("gerbang_kubur", 0, 30, 0, Col.None);
        Circle(-1.6f, 30, 0.5f);
        Circle(1.6f, 30, 0.5f);
        // graves in rows, leaving the path clear
        string[] graves = ["nisan_a", "nisan_b", "nisan_c", "nisan_a"];
        for (float x = -30; x <= 30; x += 4.2f)
        {
            if (MathF.Abs(x) < 4f)
            {
                continue;
            }

            for (float z = -20; z <= 24; z += 5.5f)
            {
                if (_rng.NextDouble() < 0.7)
                {
                    Prop(graves[_rng.Next(graves.Length)], x + R(-0.6f, 0.6f), z + R(-0.6f, 0.6f), R(-0.2f, 0.2f), Col.Auto, 1f, 0.8f);
                }
            }
        }

        // the dukun's lair at the north end
        Prop("altar_dukun", 0, -32, 0);
        foreach (Vector2 p in new Vector2[] { new(-5, -30), new(5, -30), new(-6, -26), new(6, -26), new(-3, -34), new(3, -34) })
        {
            Prop("lilin", p.X, p.Y, R(0, 6), Col.None, 1.4f);
            Lamp(p.X, p.Y, 0, Hex(0x9CFF6A), 4f, 8f, pole: false);
        }

        foreach (Vector2 p in new Vector2[] { new(-26, -30), new(26, -28), new(-30, 10), new(30, 14) })
        {
            Prop("pohon_beringin", p.X, p.Y, R(0, 6), Col.Trunk, R(0.9f, 1.15f));
            Circle(p.X, p.Y, 1.5f);
        }

        Prop("rumpun_bambu", -34, -10, 0, Col.Trunk);
        Prop("rumpun_bambu", 34, -6, 0, Col.Trunk);
        Lamp(-3, 26, MathF.PI / 2, Hex(0xFFD9A0), 12f, 14f);
        foreach (Vector2 p in new Vector2[] { new(-12, 4), new(12, -8), new(-14, -18), new(16, 16) })
        {
            Prop("lilin", p.X, p.Y, 0, Col.None, 1.2f);
            Lamp(p.X, p.Y, 0, Hex(0xFFB060), 4f, 8f, pole: false);
        }

        Scatter("rumput", 80, new Vector2(-36, -36), new Vector2(36, 36), 0.9f, 1.7f);
        Scatter("batu_b", 14, new Vector2(-36, -36), new Vector2(36, 36), 0.6f, 1.3f);
        Border(["pohon_beringin", "rumpun_bambu", "pohon_mangga"], 0.7f);
        _level.SpawnPoints.AddRange([new(-20, -10), new(20, -10), new(-16, 12), new(16, 8), new(-28, -24), new(28, -24), new(-8, -20), new(8, -18), new(-30, 30), new(30, 30)]);
        _level.PickupPoints.AddRange([new(0, 14), new(0, 0), new(-2, -12), new(-10, 20), new(10, 22), new(4, -6)]);
        _level.WeaponSpots.AddRange([("pacul", new Vector2(-3, 16)), ("senapan", new Vector2(3, 8)), ("kampak", new Vector2(-2, -4)), ("linggis", new Vector2(2, 24))]);
        _level.BossSpot = new Vector2(0, -28);
    }

    // ------------------------------------------------------------------ ground and sky

    /// <summary>A flat XZ quad grid with UVs running with world X/Z (row 0 of a texture at -Z).</summary>
    private Geometry Flat(float width, float depth, float uvRepeat = 1f)
    {
        Vertex[] v =
        [
            new(new Vector3(-width / 2, 0, -depth / 2), Vector3.UnitY, new Vector2(0, 0)),
            new(new Vector3(width / 2, 0, -depth / 2), Vector3.UnitY, new Vector2(uvRepeat, 0)),
            new(new Vector3(width / 2, 0, depth / 2), Vector3.UnitY, new Vector2(uvRepeat, uvRepeat)),
            new(new Vector3(-width / 2, 0, depth / 2), Vector3.UnitY, new Vector2(0, uvRepeat)),
        ];
        uint[] i = [0, 2, 1, 0, 3, 2];
        Geometry g = _scene.CreateGeometry(v, i);
        return g;
    }

    private void BuildGround()
    {
        byte[] pixels = _ground.ToRgba();
        Texture texture = _scene.CreateTexture(_ground.Size, _ground.Size, pixels);
        texture.SetSampler(wrapU: WrapMode.ClampToEdge, wrapV: WrapMode.ClampToEdge, linearFilter: true, mipmaps: true, anisotropy: 8);
        Material ground = _scene.CreateMaterial(MaterialOptions.Pbr(Vector4.One, 0f, 0.95f) with { BaseColorMap = texture });
        Node node = _scene.AddMesh(Flat(World, World), ground, _root, "ground");
        node.CastShadow = false;

        // the land beyond the arena, blending into the fog
        Vector3 edge = _level.Def.Id switch
        {
            "pasar" => Hex(0x587238),
            "kuburan" => Hex(0x2C4226),
            "sawah" => Hex(0x355F2C),
            _ => Hex(0x4E8A36),
        };
        // matte: a PBR plane at grazing angles mirrors the sky and reads as a pale band
        Material outside = _scene.CreateMaterial(MaterialOptions.Lambert(new Vector4(edge * 0.75f, 1)));
        Node far = _scene.AddMesh(Flat(700, 700), outside, _root, "far-ground");
        far.Position = new Vector3(0, -0.05f, 0);
        far.CastShadow = false;
    }

}
