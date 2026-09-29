using System.Numerics;

namespace Bertahan.Game;

/// <summary>A static obstacle on the ground plane: an oriented box or a circle.</summary>
public readonly record struct Obstacle(Vector2 Centre, Vector2 HalfSize, float Yaw, float Radius)
{
    public bool IsCircle => Radius > 0f;

    public static Obstacle Box(Vector2 centre, Vector2 halfSize, float yaw) => new(centre, halfSize, yaw, 0f);

    public static Obstacle Circle(Vector2 centre, float radius) => new(centre, Vector2.Zero, 0f, radius);

    /// <summary>Pushes a circle out of this obstacle. Returns true when it moved.</summary>
    public bool Resolve(ref Vector2 position, float radius)
    {
        if (IsCircle)
        {
            Vector2 d = position - Centre;
            float min = Radius + radius;
            float dist2 = d.LengthSquared();
            if (dist2 >= min * min)
            {
                return false;
            }

            float dist = MathF.Sqrt(dist2);
            position = Centre + (dist > 1e-4f ? d / dist : Vector2.UnitX) * min;
            return true;
        }

        // Work in the box's local frame.
        float c = MathF.Cos(Yaw), s = MathF.Sin(Yaw);
        Vector2 rel = position - Centre;
        Vector2 local = new((rel.X * c) - (rel.Y * s), (rel.X * s) + (rel.Y * c));
        Vector2 closest = Vector2.Clamp(local, -HalfSize, HalfSize);
        Vector2 delta = local - closest;
        float d2 = delta.LengthSquared();
        if (d2 >= radius * radius)
        {
            return false;
        }

        Vector2 push;
        if (d2 > 1e-8f)
        {
            float d = MathF.Sqrt(d2);
            push = delta / d * (radius - d);
        }
        else
        {
            // Centre inside the box: leave through the nearest face.
            float px = HalfSize.X - MathF.Abs(local.X);
            float py = HalfSize.Y - MathF.Abs(local.Y);
            push = px < py
                ? new Vector2(MathF.Sign(local.X == 0 ? 1 : local.X) * (px + radius), 0)
                : new Vector2(0, MathF.Sign(local.Y == 0 ? 1 : local.Y) * (py + radius));
        }

        local += push;
        position = Centre + new Vector2((local.X * c) + (local.Y * s), (-local.X * s) + (local.Y * c));
        return true;
    }

    public bool Contains(Vector2 point, float margin)
    {
        if (IsCircle)
        {
            return Vector2.DistanceSquared(point, Centre) < (Radius + margin) * (Radius + margin);
        }

        float c = MathF.Cos(Yaw), s = MathF.Sin(Yaw);
        Vector2 rel = point - Centre;
        Vector2 local = new((rel.X * c) - (rel.Y * s), (rel.X * s) + (rel.Y * c));
        return MathF.Abs(local.X) < HalfSize.X + margin && MathF.Abs(local.Y) < HalfSize.Y + margin;
    }
}

/// <summary>
/// Static collision plus a breadth-first flow field towards the player, so
/// zombies walk around houses instead of pressing into walls.
/// </summary>
public sealed class Navigation
{
    private readonly List<Obstacle> _obstacles = [];
    private float[] _distance = [];
    private bool[] _blocked = [];
    private readonly Queue<int> _queue = new();
    private int _width;
    private int _height;
    private Vector2 _origin;
    private const float Cell = 1f;

    public Navigation(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }

    public Vector2 Min { get; }

    public Vector2 Max { get; }

    public IReadOnlyList<Obstacle> Obstacles => _obstacles;

    public void Add(Obstacle obstacle) => _obstacles.Add(obstacle);

    /// <summary>Rasterises obstacles into the grid. Call once after the level is built.</summary>
    public void Bake()
    {
        _origin = Min;
        _width = (int)MathF.Ceiling((Max.X - Min.X) / Cell);
        _height = (int)MathF.Ceiling((Max.Y - Min.Y) / Cell);
        _blocked = new bool[_width * _height];
        _distance = new float[_width * _height];
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                Vector2 p = CellCentre(x, y);
                foreach (Obstacle o in _obstacles)
                {
                    if (o.Contains(p, 0.35f))
                    {
                        _blocked[(y * _width) + x] = true;
                        break;
                    }
                }
            }
        }
    }

    private Vector2 CellCentre(int x, int y) => _origin + new Vector2((x + 0.5f) * Cell, (y + 0.5f) * Cell);

    private bool ToCell(Vector2 p, out int x, out int y)
    {
        x = (int)MathF.Floor((p.X - _origin.X) / Cell);
        y = (int)MathF.Floor((p.Y - _origin.Y) / Cell);
        return x >= 0 && y >= 0 && x < _width && y < _height;
    }

    public bool IsBlocked(Vector2 p) => !ToCell(p, out int x, out int y) || _blocked[(y * _width) + x];

    /// <summary>Recomputes distances to <paramref name="target"/> (8-connected BFS with diagonal cost).</summary>
    public void UpdateField(Vector2 target)
    {
        Array.Fill(_distance, float.MaxValue);
        if (!ToCell(target, out int tx, out int ty))
        {
            return;
        }

        int start = (ty * _width) + tx;
        _distance[start] = 0;
        _queue.Clear();
        _queue.Enqueue(start);
        while (_queue.Count > 0)
        {
            int index = _queue.Dequeue();
            int cx = index % _width, cy = index / _width;
            float d = _distance[index];
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                    {
                        continue;
                    }

                    int nx = cx + ox, ny = cy + oy;
                    if (nx < 0 || ny < 0 || nx >= _width || ny >= _height)
                    {
                        continue;
                    }

                    int n = (ny * _width) + nx;
                    if (_blocked[n])
                    {
                        continue;
                    }

                    // no corner cutting past blocked cells
                    if (ox != 0 && oy != 0 && (_blocked[(cy * _width) + nx] || _blocked[(ny * _width) + cx]))
                    {
                        continue;
                    }

                    float nd = d + (ox != 0 && oy != 0 ? 1.414f : 1f);
                    if (nd < _distance[n])
                    {
                        _distance[n] = nd;
                        _queue.Enqueue(n);
                    }
                }
            }
        }
    }

    /// <summary>Direction to walk from <paramref name="p"/> following the field (zero when unknown).</summary>
    public Vector2 Direction(Vector2 p)
    {
        if (!ToCell(p, out int cx, out int cy))
        {
            return Vector2.Zero;
        }

        float best = _distance[(cy * _width) + cx];
        Vector2 dir = Vector2.Zero;
        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int nx = cx + ox, ny = cy + oy;
                if ((ox == 0 && oy == 0) || nx < 0 || ny < 0 || nx >= _width || ny >= _height)
                {
                    continue;
                }

                float d = _distance[(ny * _width) + nx];
                if (d < best)
                {
                    best = d;
                    dir = CellCentre(nx, ny) - p;
                }
            }
        }

        return dir.LengthSquared() > 1e-6f ? Vector2.Normalize(dir) : Vector2.Zero;
    }

    /// <summary>Keeps a circle inside the arena and out of every obstacle.</summary>
    public Vector2 Resolve(Vector2 position, float radius)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (Obstacle o in _obstacles)
            {
                o.Resolve(ref position, radius);
            }
        }

        return Vector2.Clamp(position, Min + new Vector2(radius), Max - new Vector2(radius));
    }

    /// <summary>True when the straight segment is free of obstacles (sampled).</summary>
    public bool LineClear(Vector2 a, Vector2 b)
    {
        float length = Vector2.Distance(a, b);
        int steps = Math.Max(1, (int)(length / 0.5f));
        for (int i = 1; i < steps; i++)
        {
            if (IsBlocked(Vector2.Lerp(a, b, i / (float)steps)))
            {
                return false;
            }
        }

        return true;
    }

    public Vector2 RandomFreePoint(Random rng, Vector2 min, Vector2 max)
    {
        for (int i = 0; i < 60; i++)
        {
            Vector2 p = new(min.X + ((float)rng.NextDouble() * (max.X - min.X)), min.Y + ((float)rng.NextDouble() * (max.Y - min.Y)));
            if (!IsBlocked(p))
            {
                return p;
            }
        }

        return (min + max) * 0.5f;
    }
}
