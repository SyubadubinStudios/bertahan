using System.Numerics;
using ThreeNet;
using ThreeNet.Interop;

namespace Bertahan.Game;

/// <summary>Sprites in the 4x4 effects atlas.</summary>
public enum Sprite
{
    GooGreen = 0,
    GooPurple = 1,
    Star = 2,
    Spark = 3,
    Flame = 4,
    FlameCore = 5,
    Smoke = 6,
    Dust = 7,
    Ring = 8,
    MagicBlue = 9,
    MagicGreen = 10,
    ConfettiRed = 11,
    ConfettiYellow = 12,
    ConfettiBlue = 13,
    Heart = 14,
    Splat = 15,
}

/// <summary>
/// Every particle is a camera facing quad (or a flat decal) written into one
/// dynamic mesh per blend group, so hundreds of particles cost two draw calls.
/// </summary>
public sealed class Particles
{
    private struct P
    {
        public Vector3 Pos;
        public Vector3 Vel;
        public float Life;
        public float MaxLife;
        public float Size0;
        public float Size1;
        public float Gravity;
        public float Drag;
        public float Rot;
        public float Spin;
        public Sprite Sprite;
        public bool Flat;
    }

    private sealed class Batch
    {
        public readonly List<P> Items = [];
        public Vertex[] Vertices = new Vertex[4];
        public uint[] Indices = new uint[6];
        public Geometry Geometry = null!;
        public Node Node = null!;
        public int Capacity;
    }

    private readonly Batch _glow = new() { Capacity = 700 };
    private readonly Batch _plain = new() { Capacity = 900 };
    private readonly Random _rng = new(42);

    public Particles(Scene scene, Texture atlas)
    {
        Setup(scene, _glow, atlas, glow: true);
        Setup(scene, _plain, atlas, glow: false);
    }

    private static void Setup(Scene scene, Batch batch, Texture atlas, bool glow)
    {
        batch.Vertices = new Vertex[batch.Capacity * 4];
        batch.Indices = new uint[batch.Capacity * 6];
        for (int i = 0; i < batch.Capacity; i++)
        {
            uint v = (uint)(i * 4);
            batch.Indices[(i * 6) + 0] = v;
            batch.Indices[(i * 6) + 1] = v + 1;
            batch.Indices[(i * 6) + 2] = v + 2;
            batch.Indices[(i * 6) + 3] = v;
            batch.Indices[(i * 6) + 4] = v + 2;
            batch.Indices[(i * 6) + 5] = v + 3;
        }

        batch.Geometry = scene.CreateGeometry(batch.Vertices, batch.Indices);
        MaterialOptions options = MaterialOptions.Basic(Vector4.One) with
        {
            BaseColorMap = atlas,
            AlphaMode = AlphaMode.Blend,
            CullMode = CullMode.None,
            DepthWrite = false,
            RenderOrder = glow ? 20 : 10,
        };
        if (glow)
        {
            options = options with { EmissiveMap = atlas, Emissive = Vector3.One, EmissiveIntensity = 2.5f };
        }

        batch.Node = scene.AddMesh(batch.Geometry, scene.CreateMaterial(options), name: glow ? "fx-glow" : "fx-plain");
        batch.Node.CastShadow = false;
        batch.Node.ReceiveShadow = false;
    }

    public int Count => _glow.Items.Count + _plain.Items.Count;

    private static bool IsGlow(Sprite s) => s is Sprite.Star or Sprite.Spark or Sprite.Flame or Sprite.FlameCore or Sprite.MagicBlue or Sprite.MagicGreen or Sprite.Ring;

    public void Emit(Sprite sprite, Vector3 pos, Vector3 vel, float life, float size0, float size1, float gravity = 0f, float drag = 0f, bool flat = false)
    {
        Batch b = IsGlow(sprite) ? _glow : _plain;
        if (b.Items.Count >= b.Capacity)
        {
            // Recycle the oldest particle rather than dropping the new one.
            b.Items.RemoveAt(0);
        }

        b.Items.Add(new P
        {
            Pos = pos,
            Vel = vel,
            Life = life,
            MaxLife = life,
            Size0 = size0,
            Size1 = size1,
            Gravity = gravity,
            Drag = drag,
            Rot = (float)(_rng.NextDouble() * MathF.Tau),
            Spin = (float)((_rng.NextDouble() - 0.5) * 6),
            Sprite = sprite,
            Flat = flat,
        });
    }

    private Vector3 RandomDir(float up = 0.5f)
    {
        Vector3 v = new((float)(_rng.NextDouble() * 2 - 1), (float)_rng.NextDouble() * up, (float)(_rng.NextDouble() * 2 - 1));
        return v.LengthSquared() > 1e-4f ? Vector3.Normalize(v) : Vector3.UnitY;
    }

    private float R(float a, float b) => a + ((float)_rng.NextDouble() * (b - a));

    // ------------------------------------------------------------------ presets

    public void Hit(Vector3 at, Vector3 direction, bool heavy)
    {
        for (int i = 0; i < (heavy ? 5 : 3); i++)
        {
            Emit(Sprite.Star, at, (RandomDir(1f) * R(2f, 5f)) + (direction * 2f), R(0.25f, 0.45f), R(0.25f, 0.4f), 0.05f, 4f, 2f);
        }

        Emit(Sprite.Ring, at, Vector3.Zero, 0.22f, 0.2f, heavy ? 1.6f : 1.0f);
        for (int i = 0; i < (heavy ? 9 : 5); i++)
        {
            Emit(i % 3 == 0 ? Sprite.GooPurple : Sprite.GooGreen, at, (RandomDir(1.2f) * R(2f, 5f)) + (direction * 3f), R(0.4f, 0.8f), R(0.12f, 0.22f), 0.05f, 14f, 1f);
        }
    }

    public void Splat(Vector3 at, float size = 1f)
    {
        Emit(Sprite.Splat, new Vector3(at.X, 0.03f + (R(0f, 0.01f)), at.Z), Vector3.Zero, R(8f, 12f), size * R(0.8f, 1.3f), size * R(0.9f, 1.4f), flat: true);
    }

    public void Death(Vector3 at, float scale)
    {
        for (int i = 0; i < 16; i++)
        {
            Emit(i % 2 == 0 ? Sprite.GooGreen : Sprite.GooPurple, at + new Vector3(0, 0.8f * scale, 0), RandomDir(1.5f) * R(2f, 6f) * scale, R(0.5f, 1.0f), R(0.15f, 0.35f) * scale, 0.05f, 12f, 1f);
        }

        for (int i = 0; i < 6; i++)
        {
            Emit(Sprite.Smoke, at + new Vector3(0, 0.5f, 0), RandomDir(0.8f) * R(0.5f, 1.5f), R(0.8f, 1.4f), 0.4f * scale, 1.4f * scale, -0.5f, 1.5f);
        }

        Splat(at, 1.2f * scale);
    }

    public void Dust(Vector3 at, int count = 3, float size = 0.35f)
    {
        for (int i = 0; i < count; i++)
        {
            Emit(Sprite.Dust, at + new Vector3(R(-0.2f, 0.2f), 0.1f, R(-0.2f, 0.2f)), RandomDir(0.6f) * R(0.3f, 1.2f), R(0.4f, 0.8f), size * 0.6f, size * 1.6f, -0.3f, 2f);
        }
    }

    public void Fire(Vector3 at, float radius)
    {
        Vector3 p = at + new Vector3(R(-radius, radius), 0.1f, R(-radius, radius));
        Emit(_rng.Next(3) == 0 ? Sprite.FlameCore : Sprite.Flame, p, new Vector3(R(-0.3f, 0.3f), R(1.5f, 3f), R(-0.3f, 0.3f)), R(0.4f, 0.8f), R(0.5f, 0.9f), 0.1f, -1f, 1f);
        if (_rng.Next(4) == 0)
        {
            Emit(Sprite.Smoke, p + new Vector3(0, 1.2f, 0), new Vector3(0, R(0.8f, 1.5f), 0), R(1.2f, 2f), 0.6f, 1.8f, -0.2f, 0.5f);
        }
    }

    public void Explosion(Vector3 at, float radius)
    {
        Emit(Sprite.Ring, at + new Vector3(0, 0.2f, 0), Vector3.Zero, 0.35f, 0.5f, radius * 2.2f);
        for (int i = 0; i < 26; i++)
        {
            Emit(i % 2 == 0 ? Sprite.Flame : Sprite.FlameCore, at + new Vector3(0, 0.3f, 0), RandomDir(1.2f) * R(2f, 7f), R(0.3f, 0.7f), R(0.6f, 1.1f), 0.1f, 2f, 3f);
        }

        for (int i = 0; i < 10; i++)
        {
            Emit(Sprite.Smoke, at + new Vector3(0, 0.6f, 0), RandomDir(1f) * R(1f, 3f), R(1f, 1.8f), 0.8f, 2.2f, -0.6f, 1.5f);
        }
    }

    public void Muzzle(Vector3 at, Vector3 dir)
    {
        Emit(Sprite.FlameCore, at, dir * 2f, 0.08f, 0.5f, 0.2f);
        Emit(Sprite.Spark, at, dir * 4f, 0.1f, 0.35f, 0.1f);
        for (int i = 0; i < 3; i++)
        {
            Emit(Sprite.Smoke, at, (dir * R(0.5f, 1.5f)) + (RandomDir(1f) * 0.3f), R(0.4f, 0.8f), 0.15f, 0.6f, -0.4f, 2f);
        }
    }

    public void Tracer(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        int n = Math.Clamp((int)(d.Length() / 0.7f), 2, 30);
        for (int i = 0; i < n; i++)
        {
            Emit(Sprite.Spark, from + (d * (i / (float)n)), Vector3.Zero, 0.06f + (i * 0.004f), 0.12f, 0.02f);
        }
    }

    public void Magic(Vector3 at, bool green, int count = 6)
    {
        for (int i = 0; i < count; i++)
        {
            Emit(green ? Sprite.MagicGreen : Sprite.MagicBlue, at, RandomDir(1f) * R(0.5f, 2f), R(0.4f, 0.9f), R(0.25f, 0.5f), 0.05f, -1f, 1f);
        }
    }

    public void Confetti(Vector3 at)
    {
        for (int i = 0; i < 40; i++)
        {
            Sprite s = (Sprite)((int)Sprite.ConfettiRed + (i % 3));
            Emit(s, at + new Vector3(0, 2f, 0), (RandomDir(2f) * R(2f, 6f)) + new Vector3(0, 3f, 0), R(1.5f, 2.5f), 0.18f, 0.18f, 5f, 1.2f);
        }
    }

    public void Hearts(Vector3 at)
    {
        for (int i = 0; i < 6; i++)
        {
            Emit(Sprite.Heart, at + new Vector3(R(-0.4f, 0.4f), R(0.5f, 1.5f), R(-0.4f, 0.4f)), new Vector3(0, R(1f, 2f), 0), R(0.7f, 1.1f), 0.35f, 0.15f);
        }
    }

    public void Clear()
    {
        _glow.Items.Clear();
        _plain.Items.Clear();
    }

    // ------------------------------------------------------------------ update

    public void Update(float dt, Vector3 cameraRight, Vector3 cameraUp)
    {
        Step(_glow, dt);
        Step(_plain, dt);
        Write(_glow, cameraRight, cameraUp);
        Write(_plain, cameraRight, cameraUp);
    }

    private static void Step(Batch b, float dt)
    {
        List<P> items = b.Items;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            P p = items[i];
            p.Life -= dt;
            if (p.Life <= 0f)
            {
                items.RemoveAt(i);
                continue;
            }

            p.Vel.Y -= p.Gravity * dt;
            p.Vel *= MathF.Max(0f, 1f - (p.Drag * dt));
            p.Pos += p.Vel * dt;
            if (p.Pos.Y < 0.05f && p.Gravity > 0f && !p.Flat)
            {
                p.Pos.Y = 0.05f;
                p.Vel *= 0.3f;
            }

            p.Rot += p.Spin * dt;
            items[i] = p;
        }
    }

    private static void Write(Batch b, Vector3 right, Vector3 up)
    {
        int count = b.Items.Count;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(right, up));
        for (int i = 0; i < count; i++)
        {
            P p = b.Items[i];
            float t = 1f - (p.Life / p.MaxLife);
            float size = p.Size0 + ((p.Size1 - p.Size0) * t);
            // decals fade in their last 20 percent of life
            if (p.Flat && t > 0.8f)
            {
                size *= 1f - ((t - 0.8f) / 0.2f * 0.3f);
            }

            float c = MathF.Cos(p.Rot) * size, s = MathF.Sin(p.Rot) * size;
            Vector3 ax, ay, n;
            if (p.Flat)
            {
                ax = new Vector3(c, 0, s);
                ay = new Vector3(-s, 0, c);
                n = Vector3.UnitY;
            }
            else
            {
                ax = (right * c) + (up * s);
                ay = (up * c) - (right * s);
                n = normal;
            }

            int cell = (int)p.Sprite;
            float u0 = (cell % 4) * 0.25f, v0 = (cell / 4) * 0.25f;
            const float e = 0.004f;
            int v = i * 4;
            b.Vertices[v + 0] = new Vertex(p.Pos - ax - ay, n, new Vector2(u0 + e, v0 + 0.25f - e));
            b.Vertices[v + 1] = new Vertex(p.Pos + ax - ay, n, new Vector2(u0 + 0.25f - e, v0 + 0.25f - e));
            b.Vertices[v + 2] = new Vertex(p.Pos + ax + ay, n, new Vector2(u0 + 0.25f - e, v0 + e));
            b.Vertices[v + 3] = new Vertex(p.Pos - ax + ay, n, new Vector2(u0 + e, v0 + e));
        }

        // Unused quads collapse to a point far below the ground.
        for (int i = count; i < b.Capacity; i++)
        {
            int v = i * 4;
            if (b.Vertices[v].Position.Y == -1000f)
            {
                break;
            }

            Vertex dead = new(new Vector3(0, -1000f, 0), Vector3.UnitY, Vector2.Zero);
            b.Vertices[v] = b.Vertices[v + 1] = b.Vertices[v + 2] = b.Vertices[v + 3] = dead;
        }

        b.Geometry.Update(b.Vertices, b.Indices);
        b.Node.Visible = count > 0;
    }

    // ------------------------------------------------------------------ atlas

    /// <summary>Paints the 4x4 sprite atlas (256 px) procedurally.</summary>
    public static byte[] PaintAtlas(int size = 256)
    {
        byte[] px = new byte[size * size * 4];
        int cell = size / 4;
        for (int index = 0; index < 16; index++)
        {
            int ox = (index % 4) * cell, oy = (index / 4) * cell;
            for (int y = 0; y < cell; y++)
            {
                for (int x = 0; x < cell; x++)
                {
                    float u = ((x + 0.5f) / cell * 2) - 1, v = ((y + 0.5f) / cell * 2) - 1;
                    float r = MathF.Sqrt((u * u) + (v * v));
                    float a = MathF.Atan2(v, u);
                    (Vector3 col, float alpha) = Shade((Sprite)index, u, v, r, a);
                    int o = (((oy + y) * size) + ox + x) * 4;
                    px[o + 0] = (byte)(Math.Clamp(col.X, 0, 1) * 255);
                    px[o + 1] = (byte)(Math.Clamp(col.Y, 0, 1) * 255);
                    px[o + 2] = (byte)(Math.Clamp(col.Z, 0, 1) * 255);
                    px[o + 3] = (byte)(Math.Clamp(alpha, 0, 1) * 255);
                }
            }
        }

        return px;
    }

    private static (Vector3, float) Shade(Sprite sprite, float u, float v, float r, float a)
    {
        static float Soft(float r, float edge) => Math.Clamp((1 - r) / edge, 0, 1);
        switch (sprite)
        {
            case Sprite.GooGreen:
            case Sprite.GooPurple:
            {
                float wobble = 0.85f + (0.1f * MathF.Sin(a * 5));
                float alpha = Soft(r / wobble, 0.12f);
                Vector3 baseCol = sprite == Sprite.GooGreen ? new Vector3(0.45f, 0.85f, 0.2f) : new Vector3(0.55f, 0.3f, 0.75f);
                float hi = MathF.Max(0, 1 - (new Vector2(u + 0.3f, v + 0.3f).Length() * 3));
                return (baseCol + new Vector3(hi * 0.6f), alpha);
            }

            case Sprite.Star:
            {
                float star = 0.55f + (0.4f * MathF.Cos(a * 5));
                float alpha = Soft(r / star, 0.2f);
                return (new Vector3(1f, 0.9f, 0.35f), alpha);
            }

            case Sprite.Spark:
                return (new Vector3(1f, 0.95f, 0.8f), MathF.Pow(Soft(r, 1f), 2.2f));
            case Sprite.Flame:
                return (new Vector3(1f, 0.45f + (0.3f * Soft(r, 1f)), 0.1f), MathF.Pow(Soft(r, 1f), 1.6f));
            case Sprite.FlameCore:
                return (new Vector3(1f, 0.85f, 0.4f), MathF.Pow(Soft(r, 1f), 1.4f));
            case Sprite.Smoke:
            {
                float n = 0.8f + (0.2f * MathF.Sin(a * 3 + (r * 6)));
                return (new Vector3(0.45f, 0.45f, 0.48f) * n, Soft(r, 0.9f) * 0.55f);
            }

            case Sprite.Dust:
                return (new Vector3(0.62f, 0.5f, 0.36f), Soft(r, 0.9f) * 0.6f);
            case Sprite.Ring:
            {
                float d = MathF.Abs(r - 0.8f);
                return (new Vector3(1f, 1f, 0.85f), Math.Clamp(1 - (d / 0.12f), 0, 1));
            }

            case Sprite.MagicBlue:
                return (new Vector3(0.4f, 0.7f, 1f), MathF.Pow(Soft(r, 1f), 1.8f));
            case Sprite.MagicGreen:
                return (new Vector3(0.55f, 1f, 0.35f), MathF.Pow(Soft(r, 1f), 1.8f));
            case Sprite.ConfettiRed:
                return (new Vector3(0.95f, 0.25f, 0.3f), MathF.Abs(u) < 0.6f && MathF.Abs(v) < 0.35f ? 1 : 0);
            case Sprite.ConfettiYellow:
                return (new Vector3(1f, 0.85f, 0.2f), MathF.Abs(u) < 0.6f && MathF.Abs(v) < 0.35f ? 1 : 0);
            case Sprite.ConfettiBlue:
                return (new Vector3(0.25f, 0.6f, 1f), MathF.Abs(u) < 0.6f && MathF.Abs(v) < 0.35f ? 1 : 0);
            case Sprite.Heart:
            {
                // heart curve (flipped so the point is at the bottom of the quad)
                float x = u * 1.2f, y = (-v * 1.2f) + 0.25f;
                float h = MathF.Pow((x * x) + (y * y) - 0.5f, 3) - (x * x * y * y * y * 0.9f);
                return (new Vector3(1f, 0.3f, 0.4f), h < 0 ? 1 : 0);
            }

            default: // Splat: flat goo puddle with droplets
            {
                float blob = 0.62f + (0.12f * MathF.Sin(a * 7)) + (0.06f * MathF.Sin(a * 13));
                float alpha = Soft(r / blob, 0.1f);
                for (int k = 0; k < 5; k++)
                {
                    float ang = k * 1.3f;
                    Vector2 c = new(MathF.Cos(ang) * 0.8f, MathF.Sin(ang) * 0.8f);
                    alpha = MathF.Max(alpha, Soft(Vector2.Distance(new Vector2(u, v), c) / 0.12f, 0.3f));
                }

                return (new Vector3(0.35f, 0.62f, 0.18f), alpha * 0.85f);
            }
        }
    }
}
