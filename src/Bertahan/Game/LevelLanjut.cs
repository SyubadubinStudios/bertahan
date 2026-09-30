using System.Numerics;
using ThreeNet;

namespace Bertahan.Game;

/// <summary>The later levels (5-10), after art/level 5-10.png: Kampung Damai - Zona Terlarang.</summary>
public sealed partial class LevelBuilder
{
    // ------------------------------------------------------------------ helpers

    /// <summary>A water surface. Deep water blocks movement (rivers, the swamp, the pool).</summary>
    private void Water(Vector2 min, Vector2 max, uint color, bool deep, float alpha = 0.85f)
    {
        Vector2 size = max - min;
        Vector2 centre = (min + max) * 0.5f;
        Material m = _scene.CreateMaterial(MaterialOptions.Pbr(new Vector4(Hex(color), alpha), 0.1f, 0.05f) with
        {
            AlphaMode = AlphaMode.Blend,
            Reflectance = 0.8f,
        });
        Node plane = _scene.AddMesh(Flat(size.X, size.Y, size.X / 4f), m, _root, "air");
        plane.Position = new Vector3(centre.X, 0.05f, centre.Y);
        plane.CastShadow = false;
        _ground.Rect(min, max, Srgb(0x1E2A22), Srgb(0x16201A), (int)(min.X * 7 + min.Y), 0.6f);
        if (!deep)
        {
            _level.SlowZones.Add((min, max));
        }
    }

    /// <summary>A round pond (the swamp, the mosque pool) with a blocking centre.</summary>
    private void Pond(Vector2 centre, float radius, uint color, float block)
    {
        Material m = _scene.CreateMaterial(MaterialOptions.Pbr(new Vector4(Hex(color), 0.9f), 0.1f, 0.05f) with
        {
            AlphaMode = AlphaMode.Blend,
            Reflectance = 0.9f,
        });
        Node disc = _scene.AddMesh(_scene.CreateCylinderGeometry(radius, radius, 0.04f, 40, 1, false), m, _root, "kolam");
        disc.Position = new Vector3(centre.X, 0.04f, centre.Y);
        disc.CastShadow = false;
        _ground.Rect(centre - new Vector2(radius + 1), centre + new Vector2(radius + 1), Srgb(0x2A2A20), Srgb(0x1E1E18), 77, 1.5f);
        Circle(centre.X, centre.Y, block);
    }

    /// <summary>A wall-only obstacle box in a prop's local frame (for buildings the player can walk into).</summary>
    private void LocalBox(float x, float z, float yaw, Vector2 localCentre, Vector2 half)
    {
        float c = MathF.Cos(yaw), s = MathF.Sin(yaw);
        Vector2 centre = new Vector2(x, z) + new Vector2((localCentre.X * c) + (localCentre.Y * s), (-localCentre.X * s) + (localCentre.Y * c));
        _level.Nav.Add(Obstacle.Box(centre, half, yaw));
    }

    /// <summary>The ruined classroom: walls block, the inside is open. In prop space the front faces -Y (game +Z after export).</summary>
    private void Classroom(float x, float z, float yaw)
    {
        Prop("sekolah", x, z, yaw, Col.None);
        const float w = 9f, d = 6f;
        // back wall (+Y in Blender = -Z in game), side walls, and the front wall with the door gap on the right
        LocalBox(x, z, yaw, new Vector2(0, -d / 2), new Vector2(w / 2, 0.2f));
        LocalBox(x, z, yaw, new Vector2(-w / 2, 0), new Vector2(0.2f, d / 2));
        LocalBox(x, z, yaw, new Vector2(w / 2, 0), new Vector2(0.2f, d / 2));
        LocalBox(x, z, yaw, new Vector2(-1.1f, d / 2), new Vector2(3.4f, 0.2f));
        // desks inside, in rows facing the blackboard
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float lx = -2.6f + (col * 2.4f), lz = 0.8f - (row * 1.8f);
                float c = MathF.Cos(yaw), s = MathF.Sin(yaw);
                Vector2 p = new Vector2(x, z) + new Vector2((lx * c) + (lz * s), (-lx * s) + (lz * c));
                Prop(_rng.Next(5) == 0 ? "meja_rebah" : "meja_sekolah", p.X, p.Y, yaw + R(-0.2f, 0.2f), Col.Auto, 1f, 0.8f);
            }
        }
    }

    private void Fire(float x, float z, float intensity = 10f)
    {
        _level.FirePoints.Add(new Vector3(x, 0, z));
        Lamp(x, z, 0, Hex(0xFF7A2A), intensity, 12f, pole: false);
    }

    // ------------------------------------------------------------------ level 5: jembatan bambu

    private void Jembatan()
    {
        _ground.Fill(Srgb(0x3E5A40), Srgb(0x2C4430), 0.12f, 71);
        _ground.Speckle(Srgb(0x5A7A4A), 1500, 2f, 72);
        _level.PlayerStart = new Vector2(0, 18);

        // the river runs east-west across the middle; two bamboo bridges are the only crossings
        const float riverZ0 = -5f, riverZ1 = 5f;
        Water(new Vector2(-48, riverZ0), new Vector2(48, riverZ1), 0x2A4A50, deep: true);
        _ground.Rect(new Vector2(-48, riverZ0 - 1.5f), new Vector2(48, riverZ0), Srgb(0x6A5A42), Srgb(0x54463A), 73, 0.8f);
        _ground.Rect(new Vector2(-48, riverZ1), new Vector2(48, riverZ1 + 1.5f), Srgb(0x6A5A42), Srgb(0x54463A), 74, 0.8f);
        float[] bridges = [-14f, 13f];
        float prev = -48f;
        foreach (float bx in bridges)
        {
            _level.Nav.Add(Obstacle.Box(new Vector2((prev + bx - 1.3f) / 2f, 0), new Vector2(((bx - 1.3f) - prev) / 2f, (riverZ1 - riverZ0) / 2f), 0));
            prev = bx + 1.3f;
            Prop("jembatan_bambu", bx, 0, 0, Col.None);
            _ground.Path([new(bx, 7), new(bx, 30)], 3f, Srgb(0x7A6448), Srgb(0x5E4A36), 75);
            _ground.Path([new(bx, -7), new(bx, -30)], 3f, Srgb(0x7A6448), Srgb(0x5E4A36), 76);
        }

        _level.Nav.Add(Obstacle.Box(new Vector2((prev + 48f) / 2f, 0), new Vector2((48f - prev) / 2f, (riverZ1 - riverZ0) / 2f), 0));

        // boats, poles and the village on both banks
        Prop("perahu", -24, -6.5f, 0.3f);
        Prop("perahu", 28, 6.8f, -2.8f);
        foreach (float x in new[] { -30f, -4f, 6f, 24f })
        {
            Prop("tiang_perahu", x, R(-3, 3), R(0, 6), Col.None);
        }

        Prop("rumah_panggung", -26, 16, 0.4f);
        Prop("rumah_panggung", 26, 17, -0.3f);
        Prop("rumah_biru", -8, 26, MathF.PI);
        Prop("gubuk", 22, -18, 0.6f);
        Prop("motor_merah", -16, 9, 0.9f);
        Prop("peti", 10, 9, 0.2f);
        Prop("peti", 11, 10, 1.1f);
        Prop("tumpukan_kayu", 17, 11, 0.4f);
        Prop("karung", -10, -10, 0.8f);
        foreach (Vector2 p in new Vector2[] { new(-14, 7.5f), new(13, 7.5f), new(-14, -7.5f), new(13, -7.5f) })
        {
            Prop("lilin", p.X + 1.8f, p.Y, 0, Col.None, 1.3f);
            Lamp(p.X + 1.8f, p.Y, 0, Hex(0xFFB060), 6f, 10f, pole: false);
        }

        // the old well on the far bank (the boss lives in it) and a grove of bamboo
        _ground.Rect(new Vector2(-6, -30), new Vector2(6, -20), Srgb(0x4A4A3E), Srgb(0x3A3A30), 78, 1.2f);
        foreach (Vector2 p in new Vector2[] { new(-5, -30), new(5, -30), new(-6, -22), new(6, -22) })
        {
            Prop("lilin", p.X, p.Y, R(0, 6), Col.None, 1.4f);
            Lamp(p.X, p.Y, 0, Hex(0xFFE08A), 4f, 8f, pole: false);
        }

        foreach (Vector2 p in new Vector2[] { new(-22, -14), new(-30, -22), new(20, -28), new(30, -10), new(-32, 26), new(32, 30) })
        {
            Prop("rumpun_bambu", p.X, p.Y, R(0, 6), Col.Trunk, R(0.9f, 1.2f));
        }

        foreach (Vector2 p in new Vector2[] { new(-20, 26), new(8, 14), new(-6, -16), new(18, -8), new(28, -24) })
        {
            Prop(_rng.Next(2) == 0 ? "pohon_kelapa" : "pohon_pisang", p.X, p.Y, R(0, 6), Col.Trunk);
        }

        Scatter("rumput", 70, new Vector2(-36, -36), new Vector2(36, 36), 0.9f, 1.6f, avoid: 3);
        Scatter("semak_b", 10, new Vector2(-36, -36), new Vector2(36, 36));
        Scatter("batu_a", 10, new Vector2(-36, -36), new Vector2(36, 36), 0.6f, 1.2f);
        Border(["rumpun_bambu", "pohon_kelapa", "pohon_pisang"]);
        _level.SpawnPoints.AddRange([new(-14, -34), new(13, -34), new(-30, -30), new(30, -30), new(-35, -14), new(35, -14),
            new(-34, 22), new(34, 22), new(0, 35), new(-20, 34)]);
        _level.PickupPoints.AddRange([new(0, 14), new(-8, 10), new(8, 20), new(-14, -12), new(13, -14), new(0, -12)]);
        _level.WeaponSpots.AddRange([("kampak", new Vector2(-4, 12)), ("linggis", new Vector2(6, 16)), ("senapan", new Vector2(13, -12)), ("pacul", new Vector2(-18, 20))]);
        _level.BossSpot = new Vector2(0, -25);
    }

    // ------------------------------------------------------------------ level 6: sekolah terbengkalai

    private void Sekolah()
    {
        _ground.Fill(Srgb(0x4E5E3E), Srgb(0x3A4A30), 0.12f, 81);
        // the schoolyard: packed earth with a paved assembly square in the middle
        _ground.Rect(new Vector2(-26, -26), new Vector2(26, 22), Srgb(0x7A6A52), Srgb(0x625440), 82, 1.2f);
        _ground.Rect(new Vector2(-8, -8), new Vector2(8, 8), Srgb(0x8A8578), Srgb(0x747064), 83, 0.5f, tiles: true);
        _ground.Path([new(0, 48), new(0, 22)], 4f, Srgb(0x7A6A52), Srgb(0x625440), 84);
        _ground.Speckle(Srgb(0x3A3A2E), 1400, 1.5f, 85);
        _level.PlayerStart = new Vector2(0, 14);

        // three classroom blocks around the yard, open towards it
        Classroom(0, -20, 0);
        Classroom(-20, -2, MathF.PI / 2);
        Classroom(20, -2, -MathF.PI / 2);
        Prop("tiang_bendera", 0, 0, 0, Col.Trunk);
        Prop("papan_tulis", -6, 8, 0.4f);
        Prop("meja_rebah", 7, 9, 1.2f);
        Prop("meja_sekolah", 9, 5, 0.3f);
        Prop("meja_rebah", -9, -8, -0.8f);
        Prop("ban_bekas", 12, 14);
        Prop("tong_merah", -12, 16);
        Prop("sumur", 22, 18, 0.2f);
        Prop("pos_ronda", -22, 18, 0.5f);
        Lamp(-6, 14, MathF.PI / 2, Hex(0xFFD9A0), 10f, 12f);
        Lamp(6, -10, -MathF.PI / 2, Hex(0xCFE0FF), 8f, 12f);
        foreach (Vector2 p in new Vector2[] { new(0, -18), new(-18, -2), new(18, -2) })
        {
            Lamp(p.X, p.Y, 0, Hex(0xFFE0A0), 5f, 9f, pole: false);
        }

        foreach (Vector2 p in new Vector2[] { new(-30, -26), new(30, -26), new(-30, 20), new(30, 22), new(-12, 26), new(12, 26) })
        {
            Prop("pohon_mangga", p.X, p.Y, R(0, 6), Col.Trunk, R(1f, 1.2f));
        }

        for (float x = -34; x <= 34; x += 2f)
        {
            if (MathF.Abs(x) > 4f)
            {
                Prop("pagar_bambu", x, 30.5f, 0, Col.None);
            }
        }

        Scatter("rumput", 60, new Vector2(-36, -36), new Vector2(36, 36), 0.8f, 1.5f, avoid: 4);
        Scatter("semak_a", 10, new Vector2(-36, -36), new Vector2(36, 36));
        Border(["pohon_mangga", "rumpun_bambu", "pohon_kelapa"]);
        _level.SpawnPoints.AddRange([new(0, 34), new(-30, 34), new(30, 34), new(-34, 0), new(34, 0), new(-34, -32), new(34, -32), new(0, -32),
            new(-20, -2), new(20, -2)]);
        _level.PickupPoints.AddRange([new(0, 8), new(-6, -4), new(6, 4), new(0, -18), new(-18, -2), new(18, -2)]);
        _level.WeaponSpots.AddRange([("linggis", new Vector2(-3, 6)), ("kampak", new Vector2(0, -18)), ("senapan", new Vector2(18, -2)), ("pacul", new Vector2(-18, 2))]);
        _level.BossSpot = new Vector2(0, -8);
    }

    // ------------------------------------------------------------------ level 7: kuburan kuno

    private void KuburanKuno()
    {
        _ground.Fill(Srgb(0x344A2A), Srgb(0x263A20), 0.16f, 91);
        _ground.Path([new(0, 48), new(1, 18), new(-2, 0), new(0, -30)], 3f, Srgb(0x5A5040), Srgb(0x443C30), 92);
        _ground.Speckle(Srgb(0x1E2A18), 2400, 2f, 93);
        _ground.Speckle(Srgb(0x6A6A58), 500, 1.5f, 94);
        _level.PlayerStart = new Vector2(0, 22);

        // rows of old mossy graves swallowed by the forest
        string[] graves = ["nisan_kuno_a", "nisan_kuno_b"];
        for (float x = -30; x <= 30; x += 5f)
        {
            if (MathF.Abs(x) < 4f)
            {
                continue;
            }

            for (float z = -22; z <= 26; z += 6f)
            {
                if (_rng.NextDouble() < 0.65)
                {
                    Prop(graves[_rng.Next(2)], x + R(-0.8f, 0.8f), z + R(-0.8f, 0.8f), R(-0.3f, 0.3f), Col.Auto, 1f, 0.85f);
                }
            }
        }

        Prop("cungkup", -12, -10, 0.2f);
        Prop("cungkup", 14, 6, -0.3f);
        Prop("arca", -6, -28, 0.3f);
        Prop("arca", 6, -28, -0.3f);
        Prop("reruntuhan", -24, -26, 0.4f);
        Prop("reruntuhan", 22, -18, -0.9f);
        Prop("reruntuhan", -28, 12, 1.4f);
        Prop("gerbang_kubur", 0, 30, 0, Col.None);
        Circle(-1.6f, 30, 0.5f);
        Circle(1.6f, 30, 0.5f);
        foreach (Vector2 p in new Vector2[] { new(-30, -30), new(30, -30), new(-32, 2), new(32, 16), new(18, 28) })
        {
            Prop("pohon_beringin", p.X, p.Y, R(0, 6), Col.Trunk, R(1f, 1.2f));
            Circle(p.X, p.Y, 1.6f);
        }

        Prop("pohon_larangan_a", -16, 18, 0.5f, Col.Trunk);
        Circle(-16, 18, 1.2f);
        Prop("pohon_larangan_b", 20, -6, 1.3f, Col.Trunk);
        Circle(20, -6, 1.2f);
        foreach (Vector2 p in new Vector2[] { new(-4, -24), new(4, -24), new(-10, 0), new(10, 14), new(-18, -16) })
        {
            Prop("jamur_nyala", p.X, p.Y, R(0, 6), Col.None);
            Lamp(p.X, p.Y, 0, Hex(0x6AE0FF), 3f, 7f, pole: false);
        }

        foreach (Vector2 p in new Vector2[] { new(-12, -8), new(14, 8) })
        {
            Prop("lilin", p.X, p.Y + 1.5f, 0, Col.None, 1.3f);
            Lamp(p.X, p.Y + 1.5f, 0, Hex(0xFFB060), 5f, 9f, pole: false);
        }

        Scatter("semak_duri", 12, new Vector2(-36, -36), new Vector2(36, 36), 0.8f, 1.3f, Col.Auto, 5);
        Scatter("rumput", 80, new Vector2(-36, -36), new Vector2(36, 36), 0.9f, 1.7f);
        Scatter("batu_b", 12, new Vector2(-36, -36), new Vector2(36, 36), 0.6f, 1.3f);
        Border(["pohon_beringin", "pohon_larangan_a", "rumpun_bambu"], 0.7f);
        _level.SpawnPoints.AddRange([new(-20, -12), new(20, -14), new(-18, 14), new(18, 10), new(-28, -24), new(28, -24), new(-8, -22), new(8, -20),
            new(-30, 30), new(30, 30)]);
        _level.PickupPoints.AddRange([new(0, 16), new(0, 2), new(-3, -12), new(-10, 22), new(10, 22), new(4, -6)]);
        _level.WeaponSpots.AddRange([("pacul", new Vector2(-3, 18)), ("senapan", new Vector2(3, 6)), ("kampak", new Vector2(-2, -6)), ("linggis", new Vector2(2, 26))]);
        _level.BossSpot = new Vector2(0, -24);
    }

    // ------------------------------------------------------------------ level 8: hutan larangan

    private void Hutan()
    {
        _ground.Fill(Srgb(0x2A3A24), Srgb(0x1E2C1A), 0.2f, 101);
        _ground.Path([new(0, 48), new(-4, 20), new(3, 8), new(-2, -6)], 3f, Srgb(0x4A3E2E), Srgb(0x3A3024), 102);
        _ground.Path([new(-48, 10), new(-20, 4), new(-2, -6)], 2.4f, Srgb(0x4A3E2E), Srgb(0x3A3024), 103);
        _ground.Speckle(Srgb(0x16200F), 3000, 2f, 104);
        _level.PlayerStart = new Vector2(0, 24);

        // the black swamp in the middle of the forest: the kraken's home
        Pond(new Vector2(0, -18), 8f, 0x14261E, 4.3f);
        foreach (Vector2 p in new Vector2[] { new(-9, -22), new(9, -14), new(-6, -10), new(7, -26) })
        {
            Prop("tiang_perahu", p.X, p.Y, R(0, 6), Col.None, 0.8f);
        }

        // giant twisted trees everywhere, their roots making a maze
        Random r = new(8);
        for (int i = 0; i < 22; i++)
        {
            Vector2 p = new(R(-34, 34), R(-34, 34));
            if (Vector2.Distance(p, new Vector2(0, -18)) < 11f || Vector2.Distance(p, _level.PlayerStart) < 7f || MathF.Abs(p.X) < 4f)
            {
                continue;
            }

            Prop(r.Next(2) == 0 ? "pohon_larangan_a" : "pohon_larangan_b", p.X, p.Y, R(0, 6), Col.Trunk, R(0.9f, 1.25f));
            Circle(p.X, p.Y, 1.3f);
        }

        Prop("batang_tumbang", -14, 4, 0.4f);
        Prop("batang_tumbang", 16, 14, -1.1f);
        Prop("batang_tumbang", 10, -32, 0.2f);
        foreach (Vector2 p in new Vector2[] { new(-4, 16), new(5, 6), new(-12, -6), new(12, -4), new(-20, 18), new(20, 24), new(0, -30), new(-24, -20) })
        {
            Prop("jamur_nyala", p.X, p.Y, R(0, 6), Col.None, R(0.9f, 1.4f));
            Lamp(p.X, p.Y, 0, Hex(0x6AE0FF), 4f, 8f, pole: false);
        }

        Scatter("semak_duri", 16, new Vector2(-36, -36), new Vector2(36, 36), 0.8f, 1.4f, Col.Auto, 6);
        Scatter("semak_b", 16, new Vector2(-36, -36), new Vector2(36, 36));
        Scatter("rumput", 70, new Vector2(-36, -36), new Vector2(36, 36), 0.9f, 1.7f);
        Border(["pohon_larangan_a", "pohon_larangan_b", "pohon_beringin"], 0.9f);
        _level.SpawnPoints.AddRange([new(-34, -34), new(34, -34), new(-34, 30), new(34, 30), new(-35, 0), new(35, 0), new(-20, -34), new(20, -34),
            new(0, 36), new(-30, 12)]);
        _level.PickupPoints.AddRange([new(0, 18), new(-6, 8), new(6, 2), new(-14, -4), new(14, -2), new(0, -4)]);
        _level.WeaponSpots.AddRange([("kampak", new Vector2(-3, 20)), ("senapan", new Vector2(4, 10)), ("pacul", new Vector2(-10, 2)), ("linggis", new Vector2(10, 18))]);
        _level.BossSpot = new Vector2(0, -18);
    }

    // ------------------------------------------------------------------ level 9: masjid rusak

    private void MasjidRusak()
    {
        _ground.Fill(Srgb(0x3A3428), Srgb(0x2A261E), 0.14f, 111);
        _ground.Rect(new Vector2(-14, -26), new Vector2(14, 10), Srgb(0x6A6458), Srgb(0x54504A), 112, 0.8f, tiles: true);
        _ground.Path([new(0, 48), new(0, 10)], 5f, Srgb(0x5A4E3E), Srgb(0x463C30), 113);
        _ground.Path([new(-48, 16), new(48, 14)], 4f, Srgb(0x5A4E3E), Srgb(0x463C30), 114);
        _ground.Speckle(Srgb(0x1A1614), 2600, 2.2f, 115);
        _level.PlayerStart = new Vector2(0, 24);

        // the broken mosque at the north, the pool of the leviathan in front of it
        Prop("masjid_rusak", 0, -20, 0);
        Pond(new Vector2(0, -2), 6.5f, 0x16362E, 3.4f);
        // the burning village around the square
        foreach ((float x, float z, float yaw) in new[] { (-22f, -12f, 0.4f), (22f, -10f, -0.5f), (-24f, 24f, 2.8f), (24f, 26f, -2.6f) })
        {
            Prop("rumah_terbakar", x, z, yaw);
            Fire(x, z, 12f);
        }

        foreach ((float x, float z) in new[] { (-10f, 14f), (12f, 12f), (-26f, 6f), (28f, 4f), (-8f, -30f), (14f, -32f) })
        {
            Prop(_rng.Next(2) == 0 ? "puing_a" : "puing_b", x, z, R(0, 6));
        }

        Fire(-26, 6, 6f);
        Fire(12, 12, 6f);
        Prop("rumah_kuning", -30, -28, 0.3f);
        Prop("rumah_biru", 30, -26, -0.2f);
        Prop("motor_merah", 8, 20, 1.2f);
        Prop("barikade", -6, 18, 0.2f);
        Prop("barikade", 6, 30, -0.4f);
        Prop("tiang_listrik", -14, 16, 0.3f, Col.Trunk);
        Lamp(5, 16, -MathF.PI / 2, Hex(0xFFB080), 8f, 12f);
        foreach (Vector2 p in new Vector2[] { new(-26, -30), new(26, 18), new(-30, 20), new(30, -12) })
        {
            Prop("pohon_kelapa", p.X, p.Y, R(0, 6), Col.Trunk);
        }

        Scatter("rumput", 40, new Vector2(-36, -36), new Vector2(36, 36), 0.8f, 1.3f, avoid: 4);
        Border(["pohon_kelapa", "pohon_mangga", "rumpun_bambu"], 0.8f);
        _level.SpawnPoints.AddRange([new(-34, -34), new(34, -34), new(-34, 30), new(34, 30), new(-35, 14), new(35, 14), new(-18, -34), new(18, -34),
            new(0, 36), new(-30, -4)]);
        _level.PickupPoints.AddRange([new(0, 16), new(-10, 8), new(10, 6), new(-12, -10), new(12, -12), new(0, 30)]);
        _level.WeaponSpots.AddRange([("kampak", new Vector2(-4, 18)), ("senapan", new Vector2(4, 20)), ("pacul", new Vector2(-14, -6)), ("linggis", new Vector2(14, -6))]);
        _level.BossSpot = new Vector2(0, -2);
    }

    // ------------------------------------------------------------------ level 10: candi terlarang

    private void Candi()
    {
        _ground.Fill(Srgb(0x3A2E28), Srgb(0x2A221E), 0.14f, 121);
        _ground.Rect(new Vector2(-24, -28), new Vector2(24, 24), Srgb(0x6A5A50), Srgb(0x54463E), 122, 0.6f, tiles: true);
        _ground.Path([new(0, 48), new(0, 24)], 5f, Srgb(0x5A4A40), Srgb(0x463A32), 123);
        _ground.Speckle(Srgb(0x5A1A14), 700, 2f, 124);
        _level.PlayerStart = new Vector2(0, 18);

        // the temple court: walls all around, the split gate in the south
        for (float x = -21; x <= 21; x += 6f)
        {
            Prop("candi_dinding", x, -28, 0);
            if (MathF.Abs(x) > 4f)
            {
                Prop("candi_dinding", x, 26, MathF.PI);
            }
        }

        for (float z = -22; z <= 20; z += 6f)
        {
            Prop("candi_dinding", -26, z, MathF.PI / 2);
            Prop("candi_dinding", 26, z, -MathF.PI / 2);
        }

        Prop("candi_gapura", 0, 26, 0, Col.None);
        Circle(-2.2f, 26, 1.1f);
        Circle(2.2f, 26, 1.1f);
        foreach (float z in new[] { -16f, -6f, 4f, 14f })
        {
            Prop("candi_pilar", -10, z, 0);
            Prop("candi_pilar", 10, z, 0);
        }

        // the ritual altar where the demon king appears
        Prop("altar_ritual", 0, -18, 0, Col.None);
        Circle(0, -18, 1.6f);
        Lamp(0, -18, 0, Hex(0xFF3A2A), 16f, 16f, pole: false);
        Prop("arca", -6, -24, 0.4f);
        Prop("arca", 6, -24, -0.4f);
        foreach (Vector2 p in new Vector2[] { new(-4, -12), new(5, -13), new(-16, -22), new(18, -20), new(-18, 8), new(17, 10) })
        {
            Prop("tengkorak", p.X, p.Y, R(0, 6), Col.None);
        }

        foreach (Vector2 p in new Vector2[] { new(-6, 6), new(6, 6), new(-6, -8), new(6, -8), new(-18, 20), new(18, 20), new(-20, -12), new(20, -12) })
        {
            Prop("obor", p.X, p.Y, 0, Col.Trunk);
            Fire(p.X, p.Y, 7f);
        }

        Scatter("semak_duri", 8, new Vector2(-36, -36), new Vector2(36, 36), 0.8f, 1.2f, Col.Auto, 4);
        Border(["pohon_larangan_a", "pohon_larangan_b", "pohon_beringin"], 0.7f);
        _level.SpawnPoints.AddRange([new(0, 34), new(-20, 34), new(20, 34), new(-20, 18), new(20, 18), new(-18, -18), new(18, -18), new(-18, 0), new(18, 0),
            new(0, 6)]);
        _level.PickupPoints.AddRange([new(0, 10), new(-6, 0), new(6, -2), new(-14, -14), new(14, -14), new(0, 20)]);
        _level.WeaponSpots.AddRange([("kampak", new Vector2(-3, 12)), ("senapan", new Vector2(3, 12)), ("pacul", new Vector2(-14, 0)), ("linggis", new Vector2(14, 0))]);
        _level.BossSpot = new Vector2(0, -15);
    }
}
