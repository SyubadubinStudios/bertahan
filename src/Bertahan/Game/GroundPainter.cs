using System.Numerics;

namespace Bertahan.Game;

/// <summary>Paints the ground texture of a level: grass noise, dirt roads, stones, water.</summary>
public sealed class GroundPainter
{
    private readonly float[] _r, _g, _b;
    private readonly Random _rng;

    public GroundPainter(int size, float worldSize, int seed)
    {
        Size = size;
        WorldSize = worldSize;
        _r = new float[size * size];
        _g = new float[size * size];
        _b = new float[size * size];
        _rng = new Random(seed);
    }

    public int Size { get; }

    public float WorldSize { get; }

    private static float Hash(int x, int y, int seed)
    {
        uint h = (uint)((x * 374761393) + (y * 668265263) + (seed * 982451653));
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
    }

    /// <summary>Smooth value noise in 0..1.</summary>
    public static float Noise(float x, float y, int seed)
    {
        int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - (2 * fx));
        fy = fy * fy * (3 - (2 * fy));
        float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
        return float.Lerp(float.Lerp(a, b, fx), float.Lerp(c, d, fx), fy);
    }

    public static float Fbm(float x, float y, int seed) =>
        (Noise(x, y, seed) * 0.5f) + (Noise(x * 2.1f, y * 2.1f, seed + 1) * 0.3f) + (Noise(x * 4.3f, y * 4.3f, seed + 2) * 0.2f);

    private Vector2 ToPixel(Vector2 world) => (world / WorldSize + new Vector2(0.5f)) * Size;

    public Vector2 ToWorld(int x, int y) => ((new Vector2(x + 0.5f, y + 0.5f) / Size) - new Vector2(0.5f)) * WorldSize;

    /// <summary>Base fill blending two colours with fbm noise (grass).</summary>
    public void Fill(Vector3 a, Vector3 b, float scale, int seed)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 w = ToWorld(x, y);
                float n = Fbm(w.X * scale, w.Y * scale, seed);
                float fine = Hash(x, y, seed + 7) * 0.08f;
                Vector3 c = Vector3.Lerp(a, b, n) * (0.96f + fine);
                Set(x, y, c);
            }
        }
    }

    private void Set(int x, int y, Vector3 c)
    {
        int i = (y * Size) + x;
        _r[i] = c.X;
        _g[i] = c.Y;
        _b[i] = c.Z;
    }

    private Vector3 Get(int x, int y)
    {
        int i = (y * Size) + x;
        return new Vector3(_r[i], _g[i], _b[i]);
    }

    /// <summary>Blends a colour into every pixel within <paramref name="width"/> of the polyline, with a soft ragged edge.</summary>
    public void Path(IReadOnlyList<Vector2> points, float width, Vector3 color, Vector3 color2, int seed, float edge = 0.6f)
    {
        float pxPerMeter = Size / WorldSize;
        for (int s = 0; s < points.Count - 1; s++)
        {
            Vector2 a = points[s], b = points[s + 1];
            Vector2 pa = ToPixel(a), pb = ToPixel(b);
            float r = (width * 0.5f + edge) * pxPerMeter;
            int x0 = (int)MathF.Max(0, MathF.Min(pa.X, pb.X) - r), x1 = (int)MathF.Min(Size - 1, MathF.Max(pa.X, pb.X) + r);
            int y0 = (int)MathF.Max(0, MathF.Min(pa.Y, pb.Y) - r), y1 = (int)MathF.Min(Size - 1, MathF.Max(pa.Y, pb.Y) + r);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 w = ToWorld(x, y);
                    float d = DistanceToSegment(w, a, b);
                    float ragged = (Noise(w.X * 1.7f, w.Y * 1.7f, seed) - 0.5f) * edge * 1.5f;
                    float t = Math.Clamp(((width * 0.5f) + ragged - d) / edge + 0.5f, 0f, 1f);
                    if (t <= 0f)
                    {
                        continue;
                    }

                    float n = Fbm(w.X * 0.8f, w.Y * 0.8f, seed + 3);
                    Vector3 c = Vector3.Lerp(color, color2, n) * (0.94f + (Hash(x, y, seed) * 0.1f));
                    Set(x, y, Vector3.Lerp(Get(x, y), c, t));
                }
            }
        }
    }

    /// <summary>Soft rectangle (plazas, graves, market floor).</summary>
    public void Rect(Vector2 min, Vector2 max, Vector3 color, Vector3 color2, int seed, float edge = 0.8f, bool tiles = false)
    {
        Vector2 pa = ToPixel(min - new Vector2(edge)), pb = ToPixel(max + new Vector2(edge));
        for (int y = Math.Max(0, (int)pa.Y); y < Math.Min(Size, (int)pb.Y); y++)
        {
            for (int x = Math.Max(0, (int)pa.X); x < Math.Min(Size, (int)pb.X); x++)
            {
                Vector2 w = ToWorld(x, y);
                float dx = MathF.Max(min.X - w.X, w.X - max.X), dy = MathF.Max(min.Y - w.Y, w.Y - max.Y);
                float d = MathF.Max(dx, dy);
                float t = Math.Clamp(0.5f - (d / edge), 0, 1);
                if (t <= 0)
                {
                    continue;
                }

                Vector3 c = Vector3.Lerp(color, color2, Fbm(w.X, w.Y, seed));
                if (tiles)
                {
                    float gx = MathF.Abs((w.X % 1.2f + 1.2f) % 1.2f - 0.6f), gy = MathF.Abs((w.Y % 1.2f + 1.2f) % 1.2f - 0.6f);
                    if (gx > 0.56f || gy > 0.56f)
                    {
                        c *= 0.7f;
                    }
                }

                Set(x, y, Vector3.Lerp(Get(x, y), c, t));
            }
        }
    }

    /// <summary>Scatters little stones and leaves for texture.</summary>
    public void Speckle(Vector3 color, int count, float radiusPx, int seed)
    {
        Random r = new(seed);
        for (int k = 0; k < count; k++)
        {
            int cx = r.Next(Size), cy = r.Next(Size);
            int rad = Math.Max(1, (int)(radiusPx * (0.5 + r.NextDouble())));
            for (int y = -rad; y <= rad; y++)
            {
                for (int x = -rad; x <= rad; x++)
                {
                    if ((x * x) + (y * y) > rad * rad)
                    {
                        continue;
                    }

                    int px = cx + x, py = cy + y;
                    if (px >= 0 && py >= 0 && px < Size && py < Size)
                    {
                        Set(px, py, Vector3.Lerp(Get(px, py), color, 0.6f));
                    }
                }
            }
        }
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Math.Clamp(Vector2.Dot(p - a, ab) / MathF.Max(ab.LengthSquared(), 1e-6f), 0, 1);
        return Vector2.Distance(p, a + (ab * t));
    }

    /// <summary>RGBA8 sRGB bytes. Texture row 0 is world -Z (the plane's far edge).</summary>
    public byte[] ToRgba()
    {
        byte[] px = new byte[Size * Size * 4];
        for (int i = 0; i < Size * Size; i++)
        {
            px[(i * 4) + 0] = (byte)(Math.Clamp(_r[i], 0, 1) * 255);
            px[(i * 4) + 1] = (byte)(Math.Clamp(_g[i], 0, 1) * 255);
            px[(i * 4) + 2] = (byte)(Math.Clamp(_b[i], 0, 1) * 255);
            px[(i * 4) + 3] = 255;
        }

        return px;
    }

    /// <summary>A small downsampled copy for the minimap (BGRA premultiplied opaque).</summary>
    public byte[] Minimap(int size)
    {
        byte[] px = new byte[size * size * 4];
        int step = Size / size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector3 c = Get(x * step, y * step);
                int o = ((y * size) + x) * 4;
                px[o + 0] = (byte)(Math.Clamp(c.Z, 0, 1) * 255);
                px[o + 1] = (byte)(Math.Clamp(c.Y, 0, 1) * 255);
                px[o + 2] = (byte)(Math.Clamp(c.X, 0, 1) * 255);
                px[o + 3] = 255;
            }
        }

        return px;
    }
}
