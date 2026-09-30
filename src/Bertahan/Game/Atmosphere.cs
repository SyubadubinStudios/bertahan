using System.Numerics;
using Bertahan.Core;
using ThreeNet;
using ThreeNet.Interop;

namespace Bertahan.Game;

/// <summary>How the sky, weather and wind of a place look.</summary>
public sealed record AtmosphereStyle(
    TimeOfDay Time,
    float CloudCover,
    float CloudSpeed,
    float Wind,
    float Mist = 0f,
    Vector3? MistColor = null,
    float Rain = 0f,
    bool Lightning = false,
    int Fireflies = 0,
    bool DustGusts = false,
    Vector3? FireflyColor = null,
    Vector2? Sun = null,
    bool Embers = false)
{
    public static AtmosphereStyle For(LevelDef level) => level.Id switch
    {
        "sawah" => new(TimeOfDay.Malam, 0.35f, 0.0035f, 0.55f, Mist: 0.5f, MistColor: new Vector3(0.36f, 0.42f, 0.56f), Fireflies: 10),
        "pasar" => new(TimeOfDay.Sore, 0.7f, 0.006f, 1f, Mist: 0.15f, MistColor: new Vector3(0.95f, 0.75f, 0.6f), Rain: 0.4f),
        "kuburan" => new(TimeOfDay.Malam, 0.8f, 0.007f, 0.9f, Mist: 0.6f, MistColor: new Vector3(0.25f, 0.38f, 0.3f), Rain: 0.7f, Lightning: true, Fireflies: 6,
            FireflyColor: new Vector3(0.5f, 1f, 0.4f)),
        "jembatan" => new(TimeOfDay.Malam, 0.5f, 0.004f, 0.6f, Mist: 0.9f, MistColor: new Vector3(0.3f, 0.42f, 0.46f), Fireflies: 8),
        "sekolah" => new(TimeOfDay.Malam, 0.6f, 0.005f, 0.45f, Mist: 0.35f, MistColor: new Vector3(0.3f, 0.32f, 0.42f), Rain: 0.3f),
        "kuburan_kuno" => new(TimeOfDay.Malam, 0.7f, 0.005f, 0.7f, Mist: 0.7f, MistColor: new Vector3(0.22f, 0.38f, 0.26f), Lightning: true, Fireflies: 6,
            FireflyColor: new Vector3(0.5f, 1f, 0.4f)),
        "hutan" => new(TimeOfDay.Malam, 0.3f, 0.003f, 0.5f, Mist: 0.85f, MistColor: new Vector3(0.18f, 0.28f, 0.24f), Fireflies: 16),
        "masjid_rusak" => new(TimeOfDay.Kutukan, 0.7f, 0.011f, 1f, Mist: 0.3f, MistColor: new Vector3(0.45f, 0.18f, 0.16f), Lightning: true, Embers: true),
        "candi" => new(TimeOfDay.Kutukan, 0.5f, 0.006f, 0.4f, Mist: 0.45f, MistColor: new Vector3(0.38f, 0.1f, 0.1f), Embers: true),
        _ => new(TimeOfDay.Siang, 0.5f, 0.004f, 0.8f, DustGusts: true),
    };

    /// <summary>The golden afternoon of the title screen.</summary>
    public static AtmosphereStyle Menu => new(TimeOfDay.Sore, 0.45f, 0.004f, 0.6f, DustGusts: true, Sun: new Vector2(0.7f, 26f));
}

/// <summary>A prop that bends in the wind: its model copy tilts around its base.</summary>
public sealed class Swayer(Node model, Vector2 position, float yaw, float amplitude, float frequency)
{
    public Node Model { get; } = model;

    public Vector2 Position { get; } = position;

    public float Yaw { get; } = yaw;

    public float Amplitude { get; } = amplitude;

    public float Frequency { get; } = frequency;

    public float Phase { get; } = (position.X * 0.37f) + (position.Y * 0.61f);
}

/// <summary>
/// Everything above and around the level: a painted sky dome (sun or moon,
/// stars, horizon haze), a drifting cloud layer, low mist, GPU rain with
/// lightning, fireflies, dust gusts and the wind that bends trees, bamboo,
/// bushes, grass and rice.
/// </summary>
public sealed class Atmosphere
{
    public const float SkyRadius = 120f;

    private const string RainShader = """
        vec3 user_vertex(VertexContext context) {
            vec3 p = context.position;
            float h = context.custom0.y;
            // uv.x carries each drop's start height, so the whole streak wraps together
            float y = mod(context.uv.x * h - context.time * context.custom0.x, h);
            p.y += y;
            p.xz += context.custom1.xy * y * 0.12;
            return p;
        }
        Surface user_surface(SurfaceContext context, Surface surface) {
            surface.alpha = surface.alpha * clamp(context.uv.y, 0.0, 1.0);
            return surface;
        }
        """;

    private readonly Scene _scene;
    private readonly AtmosphereStyle _style;
    private readonly Node _root;
    private readonly Node _sky;
    private readonly Node? _nightSky;
    private readonly Node _clouds;
    private readonly List<(Material Material, Vector2 Drift)> _mist = [];
    private readonly Node? _rain;
    private readonly Random _rng = new(11);
    private readonly Vector2 _windDir;
    private SceneEnvironment? _baseEnvironment;
    private float _time;
    private float _flash;
    private float _nextLightning = 6f;
    private float _thunderIn = -1f;
    private float _gustTimer;

    public Atmosphere(Scene scene, AtmosphereStyle style, bool withNightSky = false)
    {
        _scene = scene;
        _style = style;
        _root = scene.CreateNode(null, "atmosphere");
        float a = 0.6f;
        _windDir = new Vector2(MathF.Cos(a), MathF.Sin(a));

        _sky = Dome("sky", SkyRadius, -30f, 90f, SkyPainter.Paint(style.Time, 1024, 512, style.Sun), blend: false);
        if (withNightSky)
        {
            _nightSky = Dome("sky-night", SkyRadius * 0.995f, -30f, 90f, SkyPainter.Paint(TimeOfDay.Malam, 1024, 512), blend: false);
            _nightSky.Visible = false;
        }

        // the cloud layer turns slowly around the camera: clouds drifting across the sky
        _clouds = Dome("clouds", SkyRadius * 0.96f, 4f, 75f, SkyPainter.Clouds(style.Time, style.CloudCover, 1024, 256), blend: true, 1024, 256);
        if (style.Time is TimeOfDay.Siang or TimeOfDay.Sore && style.CloudCover > 0.2f)
        {
            CloudShadows(style);
        }

        if (style.Mist > 0f)
        {
            Mist(style);
        }

        if (style.Rain > 0f)
        {
            _rain = Rain(style.Rain);
        }
    }

    public List<Swayer> Swayers { get; } = [];

    /// <summary>0..1 brightness of the current lightning flash.</summary>
    public float Flash => _flash;

    /// <summary>For the opening story: switches to the night sky past 0.5.</summary>
    public float Night
    {
        set
        {
            if (_nightSky is not null)
            {
                bool night = value > 0.5f;
                _nightSky.Visible = night;
                _sky.Visible = !night;
                // the golden clouds only belong to the evening sky
                _clouds.Visible = !night;
            }
        }
    }

    // ------------------------------------------------------------------ wind

    private static readonly (string Prefix, float Amplitude, float Frequency)[] Plants =
    [
        ("prop_rumput", 0.2f, 2.3f),
        ("prop_padi", 0.2f, 1.9f),
        ("prop_semak", 0.07f, 1.5f),
        ("prop_rumpun_bambu", 0.07f, 1.1f),
        ("prop_pohon_pisang", 0.06f, 1.2f),
        ("prop_pohon_kelapa", 0.04f, 0.8f),
        ("prop_pohon_mangga", 0.03f, 0.9f),
        ("prop_pohon_beringin", 0.018f, 0.6f),
        ("prop_orang_sawah", 0.05f, 1.3f),
        ("prop_pagar_bambu", 0.01f, 1.6f),
    ];

    /// <summary>Registers a placed prop (the pivot returned by <see cref="PropLibrary.Place"/>) if it is a plant.</summary>
    public static Swayer? SwayerFor(string prop, Node? pivot, Vector3 position, float yaw)
    {
        if (pivot is null || pivot.Children.Count == 0)
        {
            return null;
        }

        foreach ((string prefix, float amp, float freq) in Plants)
        {
            if (prop.StartsWith(prefix, StringComparison.Ordinal))
            {
                return new Swayer(pivot.Children[0], new Vector2(position.X, position.Z), yaw, amp, freq);
            }
        }

        return null;
    }

    public void AddSway(string prop, Node? pivot, Vector3 position, float yaw)
    {
        if (SwayerFor(prop, pivot, position, yaw) is { } s)
        {
            Swayers.Add(s);
        }
    }

    private void UpdateWind()
    {
        float t = _time;
        float wind = _style.Wind;
        foreach (Swayer s in Swayers)
        {
            // a gust front rolling across the level plus a quicker flutter
            float front = (Vector2.Dot(s.Position, _windDir) * 0.18f) - (t * 1.3f);
            float gust = 0.55f + (0.45f * MathF.Sin(front));
            float bend = s.Amplitude * wind * ((gust * 0.8f) + (0.35f * MathF.Sin((t * s.Frequency * 2.2f) + s.Phase)));
            // tilt away from the wind, expressed in the prop's own (yawed) frame
            float c = MathF.Cos(-s.Yaw), sn = MathF.Sin(-s.Yaw);
            Vector3 axisWorld = new(_windDir.Y, 0, -_windDir.X);
            Vector3 axis = new((axisWorld.X * c) + (axisWorld.Z * sn), 0, (-axisWorld.X * sn) + (axisWorld.Z * c));
            s.Model.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), bend);
        }
    }

    // ------------------------------------------------------------------ sky

    /// <summary>A sphere section seen from inside; u runs around, v from the top edge down.</summary>
    private Node Dome(string name, float radius, float elevMin, float elevMax, byte[] rgba, bool blend, int width = 1024, int height = 512)
    {
        const int segs = 48, rings = 24;
        List<Vertex> v = [];
        List<uint> idx = [];
        for (int r = 0; r <= rings; r++)
        {
            float e = (elevMax - ((elevMax - elevMin) * r / rings)) * MathF.PI / 180f;
            for (int s = 0; s <= segs; s++)
            {
                float lon = s / (float)segs * MathF.Tau;
                Vector3 dir = new(MathF.Cos(e) * MathF.Cos(lon), MathF.Sin(e), MathF.Cos(e) * MathF.Sin(lon));
                v.Add(new Vertex(dir * radius, -dir, new Vector2(s / (float)segs, r / (float)rings)));
            }
        }

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < segs; s++)
            {
                uint i0 = (uint)((r * (segs + 1)) + s), i1 = i0 + 1, i2 = i0 + (segs + 1), i3 = i2 + 1;
                idx.AddRange([i0, i1, i2, i1, i3, i2]);
            }
        }

        Texture tex = _scene.CreateTexture(width, height, rgba);
        tex.SetSampler(WrapMode.Repeat, WrapMode.ClampToEdge, true, true, 4);
        MaterialOptions options = MaterialOptions.Basic(Vector4.One) with
        {
            BaseColorMap = tex,
            CullMode = CullMode.None,
            AlphaMode = blend ? AlphaMode.Blend : AlphaMode.Opaque,
            DepthWrite = !blend,
            RenderOrder = blend ? 1 : 0,
        };
        Node node = _scene.AddMesh(_scene.CreateGeometry(v.ToArray(), idx.ToArray()), _scene.CreateMaterial(options), _root, name);
        node.CastShadow = false;
        node.ReceiveShadow = false;
        return node;
    }

    /// <summary>Soft dark patches drifting over the ground: the shadows of the clouds overhead.</summary>
    private void CloudShadows(AtmosphereStyle style)
    {
        Texture tex = _scene.CreateTexture(512, 512, SkyPainter.MistTexture(512, 31));
        tex.SetSampler(WrapMode.Repeat, WrapMode.Repeat, true, true, 4);
        Material m = _scene.CreateMaterial(MaterialOptions.Basic(new Vector4(0.02f, 0.03f, 0.08f, 0.75f * style.CloudCover)) with
        {
            BaseColorMap = tex,
            AlphaMode = AlphaMode.Blend,
            CullMode = CullMode.None,
            DepthWrite = false,
            RenderOrder = 3,
            UvScale = new Vector2(1.1f, 1.1f),
        });
        Node plane = _scene.AddMesh(_scene.CreatePlaneGeometry(160, 160), m, _root, "cloud-shadows");
        plane.EulerAngles = new Vector3(-MathF.PI / 2, 0, 0);
        plane.Position = new Vector3(0, 0.06f, 0);
        plane.CastShadow = false;
        plane.ReceiveShadow = false;
        _mist.Add((m, new Vector2(-_windDir.X, _windDir.Y) * style.CloudSpeed * 2.5f));
    }

    private void Mist(AtmosphereStyle style)
    {
        Vector3 color = style.MistColor ?? new Vector3(0.8f);
        Texture tex = _scene.CreateTexture(512, 512, SkyPainter.MistTexture(512, 5));
        tex.SetSampler(WrapMode.Repeat, WrapMode.Repeat, true, true, 4);
        float[] heights = [0.25f, 0.9f, 1.7f];
        for (int i = 0; i < heights.Length; i++)
        {
            float alpha = style.Mist * (i == 0 ? 0.4f : i == 1 ? 0.28f : 0.15f);
            Material m = _scene.CreateMaterial(MaterialOptions.Basic(new Vector4(color, alpha)) with
            {
                BaseColorMap = tex,
                AlphaMode = AlphaMode.Blend,
                CullMode = CullMode.None,
                DepthWrite = false,
                RenderOrder = 4 + i,
                UvScale = new Vector2(3.2f + i, 3.2f + i),
            });
            Node plane = _scene.AddMesh(_scene.CreatePlaneGeometry(150, 150), m, _root, "mist");
            plane.EulerAngles = new Vector3(-MathF.PI / 2, i * 0.7f, 0);
            plane.Position = new Vector3(0, heights[i], 0);
            plane.CastShadow = false;
            plane.ReceiveShadow = false;
            float speed = 0.004f + (i * 0.002f);
            _mist.Add((m, new Vector2(_windDir.X, _windDir.Y) * speed * (i % 2 == 0 ? 1 : -0.7f)));
        }
    }

    /// <summary>Rain streaks in a box around the camera, animated entirely on the GPU.</summary>
    private Node Rain(float amount)
    {
        const float box = 26f, height = 18f;
        int drops = (int)(900 * amount) + 200;
        List<Vertex> v = [];
        List<uint> idx = [];
        for (int i = 0; i < drops; i++)
        {
            float x = ((float)_rng.NextDouble() - 0.5f) * box * 2, z = ((float)_rng.NextDouble() - 0.5f) * box * 2;
            float start = (float)_rng.NextDouble();
            float len = 0.6f + ((float)_rng.NextDouble() * 0.5f);
            const float w = 0.012f;
            // two crossed quads so the streak reads from any camera angle
            foreach ((float dx, float dz) in new[] { (w, 0f), (0f, w) })
            {
                uint b = (uint)v.Count;
                v.Add(new Vertex(new Vector3(x - dx, 0, z - dz), Vector3.UnitY, new Vector2(start, 0)));
                v.Add(new Vertex(new Vector3(x + dx, 0, z + dz), Vector3.UnitY, new Vector2(start, 0)));
                v.Add(new Vertex(new Vector3(x + dx, len, z + dz), Vector3.UnitY, new Vector2(start, 1)));
                v.Add(new Vertex(new Vector3(x - dx, len, z - dz), Vector3.UnitY, new Vector2(start, 1)));
                idx.AddRange([b, b + 1, b + 2, b, b + 2, b + 3]);
            }
        }

        Shader shader = _scene.CreateShader(RainShader, ShaderLanguage.Glsl, "rain");
        Vector3 tint = _style.Time switch
        {
            TimeOfDay.Malam => new Vector3(0.55f, 0.62f, 0.75f),
            TimeOfDay.Kutukan => new Vector3(0.75f, 0.45f, 0.45f),
            _ => new Vector3(0.8f, 0.84f, 0.9f),
        };
        Material m = _scene.CreateMaterial(MaterialOptions.Basic(new Vector4(tint, 0.28f)) with
        {
            AlphaMode = AlphaMode.Blend,
            CullMode = CullMode.None,
            DepthWrite = false,
            RenderOrder = 15,
            Shader = shader,
            Custom0 = new Vector4(16f, height, 0, 0),
            Custom1 = new Vector4(_windDir.X, _windDir.Y, 0, 0) * _style.Wind,
        });
        Node node = _scene.AddMesh(_scene.CreateGeometry(v.ToArray(), idx.ToArray()), m, _root, "rain");
        node.CastShadow = false;
        node.ReceiveShadow = false;
        return node;
    }

    // ------------------------------------------------------------------ frame

    /// <summary>Call after the lighting of the frame is set.</summary>
    public void Update(float dt, Vector3 camera, Vector3 focus, Particles? fx, AudioManager? audio)
    {
        _time += dt;
        _sky.Position = camera;
        if (_nightSky is not null)
        {
            _nightSky.Position = camera;
        }

        _clouds.Position = camera;
        _clouds.EulerAngles = new Vector3(0, _time * _style.CloudSpeed, 0);
        foreach ((Material m, Vector2 drift) in _mist)
        {
            m.Update(o => o with { UvOffset = drift * _time });
        }

        if (_rain is not null)
        {
            _rain.Position = new Vector3(focus.X, 0, focus.Z);
        }

        UpdateWind();
        if (fx is not null)
        {
            Ambience(dt, focus, fx);
        }

        if (_style.Lightning)
        {
            Lightning(dt, audio);
        }
    }

    private void Ambience(float dt, Vector3 focus, Particles fx)
    {
        float R(float a, float b) => a + ((float)_rng.NextDouble() * (b - a));
        Vector3 wind3 = new(_windDir.X, 0, _windDir.Y);

        // fireflies drifting over the grass
        if (_style.Fireflies > 0 && _rng.NextDouble() < _style.Fireflies * dt)
        {
            Vector3 p = focus + new Vector3(R(-14, 14), R(0.4f, 2.2f), R(-12, 12));
            fx.Emit(_style.FireflyColor is null ? Sprite.Spark : Sprite.MagicGreen, p, new Vector3(R(-0.3f, 0.3f), R(0.05f, 0.25f), R(-0.3f, 0.3f)), R(2f, 4f), 0.14f, 0.05f);
        }

        // rain splashes around the player
        if (_style.Rain > 0f)
        {
            int splashes = (int)(_style.Rain * 60 * dt) + (_rng.NextDouble() < (_style.Rain * 60 * dt) % 1 ? 1 : 0);
            for (int i = 0; i < splashes; i++)
            {
                Vector3 p = focus + new Vector3(R(-12, 12), 0.06f, R(-10, 10));
                fx.Emit(Sprite.Ring, p, Vector3.Zero, 0.25f, 0.02f, 0.2f, flat: true);
            }
        }

        // embers drifting up from the burning village and the ritual fires
        if (_style.Embers && _rng.NextDouble() < 12 * dt)
        {
            Vector3 p = focus + new Vector3(R(-14, 14), R(0.1f, 1f), R(-12, 12));
            fx.Emit(Sprite.Flame, p, new Vector3(R(-0.3f, 0.3f), R(0.8f, 1.6f), R(-0.3f, 0.3f)) + (wind3 * 0.6f), R(1.5f, 2.5f), 0.1f, 0.02f);
        }

        // dust blown along the road on dry days
        if (_style.DustGusts)
        {
            _gustTimer -= dt;
            if (_gustTimer <= 0f)
            {
                _gustTimer = R(0.15f, 0.5f);
                Vector3 p = focus + new Vector3(R(-14, 14), R(0.1f, 0.6f), R(-12, 12));
                fx.Emit(Sprite.Dust, p, (wind3 * R(1.5f, 3f) * _style.Wind) + new Vector3(0, 0.2f, 0), R(1.5f, 2.5f), 0.3f, 1.1f, drag: 0.3f);
            }
        }
    }

    private void Lightning(float dt, AudioManager? audio)
    {
        _baseEnvironment ??= _scene.Environment;
        _nextLightning -= dt;
        _flash = MathF.Max(0f, _flash - (dt * 3.5f));
        if (_nextLightning <= 0f)
        {
            _nextLightning = 7f + ((float)_rng.NextDouble() * 9f);
            _flash = 1f;
            _thunderIn = 0.5f + ((float)_rng.NextDouble() * 0.8f);
        }

        if (_thunderIn > 0f)
        {
            _thunderIn -= dt;
            if (_thunderIn <= 0f)
            {
                audio?.Play("sfx_explosion", 0.45f, 0.35f + ((float)_rng.NextDouble() * 0.1f));
            }
        }

        // a double flicker reads as lightning better than a single pulse
        float f = _flash * (_flash > 0.6f && _flash < 0.8f ? 0.3f : 1f);
        SceneEnvironment b = _baseEnvironment.Value;
        _scene.Environment = b with
        {
            AmbientIntensity = b.AmbientIntensity + (f * 1.4f),
            AmbientColor = Vector3.Lerp(b.AmbientColor, new Vector3(0.8f, 0.85f, 1f), f),
        };
    }

    /// <summary>Lighting changed (e.g. the opening story): forget the cached environment.</summary>
    public void EnvironmentChanged() => _baseEnvironment = null;
}

/// <summary>Procedural sky, cloud and mist textures.</summary>
public static class SkyPainter
{
    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)((x * 374761393) + (y * 668265263) + (seed * 1442695040));
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / (float)uint.MaxValue;
        }
    }

    /// <summary>Value noise that tiles with the given period in x and y.</summary>
    private static float Noise(float x, float y, int px, int py, int seed)
    {
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - (2 * fx));
        fy = fy * fy * (3 - (2 * fy));
        int Wx(int v) => ((v % px) + px) % px;
        int Wy(int v) => ((v % py) + py) % py;
        float a = Hash(Wx(x0), Wy(y0), seed), b = Hash(Wx(x0 + 1), Wy(y0), seed);
        float c = Hash(Wx(x0), Wy(y0 + 1), seed), d = Hash(Wx(x0 + 1), Wy(y0 + 1), seed);
        return float.Lerp(float.Lerp(a, b, fx), float.Lerp(c, d, fx), fy);
    }

    private static float Fbm(float x, float y, int px, int py, int seed, int octaves = 5)
    {
        float sum = 0, amp = 0.5f, norm = 0;
        for (int o = 0; o < octaves; o++)
        {
            sum += Noise(x, y, px, py, seed + o) * amp;
            norm += amp;
            x *= 2;
            y *= 2;
            px *= 2;
            py *= 2;
            amp *= 0.5f;
        }

        return sum / norm;
    }

    private static Vector3 C(uint rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

    /// <summary>Equirectangular sky from +90 (row 0) down to -30 degrees.</summary>
    /// <param name="sun">Azimuth (radians, atan2(z, x)) and elevation (degrees) of the sun or moon; matches the level's light when given.</param>
    public static byte[] Paint(TimeOfDay time, int w, int h, Vector2? sun = null)
    {
        (Vector3 zenith, Vector3 mid, Vector3 horizon, Vector3 below, Vector3 glowCol, float sunAz, float sunEl, float glowSize) = time switch
        {
            // sun positions follow the directional lights in GameSession.Lighting
            TimeOfDay.Sore => (C(0x3B4A8C), C(0xB77AA0), C(0xFFB36B), C(0xE9A27A), C(0xFFD08A), 2.55f, 20f, 0.5f),
            TimeOfDay.Malam => (C(0x05081A), C(0x0E1838), C(0x22305A), C(0x121A30), C(0xB8C8FF), 5.11f, 28f, 0.18f),
            TimeOfDay.Kutukan => (C(0x1A0508), C(0x4A0E14), C(0x8A2A1A), C(0x2A0A0A), C(0x6AFF8A), 4.71f, 34f, 0.25f),
            _ => (C(0x2F6FD0), C(0x6FA8EC), C(0xCFE6FA), C(0xB9D2E4), C(0xFFF6D8), 0.59f, 57f, 0.3f),
        };
        if (sun is { } custom)
        {
            (sunAz, sunEl) = (custom.X, custom.Y);
        }

        byte[] px = new byte[w * h * 4];
        Vector3 sunDir = Dir(sunAz, sunEl * MathF.PI / 180f);
        for (int y = 0; y < h; y++)
        {
            float elev = (90f - (120f * y / (h - 1))) * MathF.PI / 180f;
            for (int x = 0; x < w; x++)
            {
                float az = x / (float)w * MathF.Tau;
                Vector3 dir = Dir(az, elev);
                float e = elev * 180f / MathF.PI;
                Vector3 col;
                if (e >= 0)
                {
                    float t = MathF.Pow(e / 90f, 0.45f);
                    col = t < 0.5f ? Vector3.Lerp(horizon, mid, t * 2) : Vector3.Lerp(mid, zenith, (t - 0.5f) * 2);
                }
                else
                {
                    col = Vector3.Lerp(horizon, below, Math.Clamp(-e / 12f, 0, 1));
                }

                // sun or moon glow and disc
                float cos = Vector3.Dot(dir, sunDir);
                if (time == TimeOfDay.Kutukan)
                {
                    // a green vortex of curses spinning above the village (art/level 5-10.png)
                    float dxv = MathF.IEEERemainder(az - sunAz, MathF.Tau) * MathF.Cos(sunEl * MathF.PI / 180f);
                    float dyv = elev - (sunEl * MathF.PI / 180f);
                    float rv = MathF.Sqrt((dxv * dxv) + (dyv * dyv));
                    float swirl = 0.5f + (0.5f * MathF.Sin((MathF.Atan2(dyv, dxv) * 3f) + (rv * 26f)));
                    float fall = MathF.Exp(-rv / 0.32f);
                    col = Vector3.Lerp(col, C(0x0A1A10), fall * 0.8f);
                    col += C(0x3AFF6A) * swirl * fall * 0.9f;
                    col += C(0xFF3A1A) * (1 - swirl) * fall * 0.25f;
                }

                float ridge = Ridge(az);
                float ang = MathF.Acos(Math.Clamp(cos, -1, 1));
                col += glowCol * (MathF.Exp(-ang * ang / (glowSize * glowSize * 0.5f)) * (time == TimeOfDay.Malam ? 0.35f : time == TimeOfDay.Kutukan ? 0.2f : 0.55f));
                col += glowCol * MathF.Exp(-ang * ang / (glowSize * glowSize * 6f)) * 0.25f;
                float disc = time == TimeOfDay.Malam ? 0.045f : 0.06f;
                if (ang < disc && time != TimeOfDay.Kutukan)
                {
                    col = Vector3.Lerp(col, time == TimeOfDay.Malam ? new Vector3(0.95f, 0.96f, 0.9f) : new Vector3(1f, 0.98f, 0.9f), Math.Clamp((disc - ang) / 0.01f, 0, 1));
                }

                // the volcano and the green hills around Kampung Damai, softened by haze (edges anti-aliased)
                Vector3 hill = time switch
                {
                    TimeOfDay.Malam => C(0x131C30),
                    TimeOfDay.Kutukan => C(0x2A0E14),
                    TimeOfDay.Sore => C(0x7A5A78),
                    _ => C(0x6E9C86),
                };
                Vector3 near = time switch
                {
                    TimeOfDay.Malam => C(0x0C1222),
                    TimeOfDay.Kutukan => C(0x16060A),
                    TimeOfDay.Sore => C(0x5C4466),
                    _ => C(0x4F7E5C),
                };
                float pixelDeg = 120f / h;
                float volcano = Mountain(az), hills = HillHeight(az);
                float cv = Math.Clamp(((volcano - e) / pixelDeg) + 0.5f, 0, 1);
                float ch = Math.Clamp(((hills - e) / pixelDeg) + 0.5f, 0, 1);
                col = Vector3.Lerp(col, Vector3.Lerp(Vector3.Lerp(hill, horizon, 0.5f), hill, Math.Clamp((volcano - e) / 6f, 0, 1) * 0.5f), cv);
                col = Vector3.Lerp(col, Vector3.Lerp(near, horizon, 0.25f * (1 - Math.Clamp((hills - e) / 4f, 0, 1))), ch);

                if (time == TimeOfDay.Malam && e > 3 && e >= ridge)
                {
                    // stars and a faint milky band
                    float band = MathF.Exp(-MathF.Pow((e - 55f - (15f * MathF.Sin(az * 2))) / 12f, 2));
                    col += new Vector3(0.12f, 0.11f, 0.18f) * band * Fbm(x / 16f, y / 16f, w / 16, 64, 3, 4);
                    float s = Hash(x, y, 9);
                    if (s > 0.9965f)
                    {
                        col += new Vector3(0.8f, 0.82f, 0.9f) * ((s - 0.9965f) / 0.0035f) * Math.Clamp(e / 15f, 0, 1);
                    }
                }

                int o = ((y * w) + x) * 4;
                px[o] = (byte)(Math.Clamp(col.X, 0, 1) * 255);
                px[o + 1] = (byte)(Math.Clamp(col.Y, 0, 1) * 255);
                px[o + 2] = (byte)(Math.Clamp(col.Z, 0, 1) * 255);
                px[o + 3] = 255;
            }
        }

        return px;
    }

    /// <summary>Elevation (degrees) of the volcano silhouette at an azimuth.</summary>
    private static float Mountain(float az)
    {
        const float peak = 4.53f; // behind the gate, where the 3D volcano used to stand
        float d = MathF.Abs(MathF.IEEERemainder(az - peak, MathF.Tau));
        float cone = 15f - (d * 15f);
        float crater = d < 0.06f ? -0.8f : 0f;
        return cone + crater;
    }

    private static float HillHeight(float az) =>
        2.2f + (3.2f * Fbm(az / MathF.Tau * 12f, 0.5f, 12, 4, 77, 4)) + (1.5f * MathF.Sin(az * 3f + 1f));

    private static float Ridge(float az) => MathF.Max(Mountain(az), HillHeight(az));

    private static Vector3 Dir(float az, float el) => new(MathF.Cos(el) * MathF.Cos(az), MathF.Sin(el), MathF.Cos(el) * MathF.Sin(az));

    /// <summary>Cloud layer: row 0 is the top of the cloud dome, the last row the horizon.</summary>
    public static byte[] Clouds(TimeOfDay time, float cover, int w, int h)
    {
        (Vector3 lit, Vector3 shade) = time switch
        {
            TimeOfDay.Sore => (C(0xFFD7B0), C(0x8E6A8E)),
            TimeOfDay.Malam => (C(0x5A6690), C(0x1A2038)),
            TimeOfDay.Kutukan => (C(0x8A2A2A), C(0x2A0A10)),
            _ => (C(0xFFFFFF), C(0xA8B8CC)),
        };
        byte[] px = new byte[w * h * 4];
        float threshold = 1f - (cover * 0.75f) - 0.12f;
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            for (int x = 0; x < w; x++)
            {
                // stretch the noise sideways so clouds look flatter near the horizon
                float n = Fbm(x / 64f, y / (32f + (v * 24f)), w / 64, 64, 21, 5);
                float d = Math.Clamp((n - threshold) / 0.22f, 0, 1);
                float edge = Math.Clamp((1 - v) / 0.05f, 0, 1) * Math.Clamp(v / 0.2f, 0, 1);
                float alpha = d * edge * (time is TimeOfDay.Malam or TimeOfDay.Kutukan ? 0.75f : 0.92f);
                // denser middles are darker underneath
                Vector3 col = Vector3.Lerp(lit, shade, Math.Clamp((d - 0.4f) * 1.2f, 0, 1) * 0.7f);
                int o = ((y * w) + x) * 4;
                px[o] = (byte)(col.X * 255);
                px[o + 1] = (byte)(col.Y * 255);
                px[o + 2] = (byte)(col.Z * 255);
                px[o + 3] = (byte)(Math.Clamp(alpha, 0, 1) * 255);
            }
        }

        return px;
    }

    /// <summary>Soft tileable patches of white with alpha, for ground mist.</summary>
    public static byte[] MistTexture(int size, int seed)
    {
        byte[] px = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Fbm(x / 64f, y / 64f, size / 64, size / 64, seed, 4);
                float a = Math.Clamp((n - 0.38f) / 0.4f, 0, 1);
                int o = ((y * size) + x) * 4;
                px[o] = px[o + 1] = px[o + 2] = 255;
                px[o + 3] = (byte)(a * a * 255);
            }
        }

        return px;
    }
}
