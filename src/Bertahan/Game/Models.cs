using System.Numerics;
using ThreeNet;

namespace Bertahan.Game;

/// <summary>
/// Static props made in Blender: each GLB is imported once into a hidden
/// prototype and placed with <see cref="Node.Clone"/>, so every copy shares its
/// GPU buffers.
/// </summary>
public sealed class PropLibrary
{
    private readonly Scene _scene;
    private readonly Node _prototypes;
    private readonly Dictionary<string, Prototype?> _cache = [];

    public sealed record Prototype(Node Root, Vector3 Min, Vector3 Max)
    {
        public Vector3 Size => Max - Min;
    }

    public PropLibrary(Scene scene)
    {
        _scene = scene;
        _prototypes = scene.CreateNode(null, "prototypes");
        _prototypes.Visible = false;
    }

    public static string ModelPath(string file) => Path.Combine(AppContext.BaseDirectory, "Assets", "Models", file + ".glb");

    public Prototype? Get(string name)
    {
        if (_cache.TryGetValue(name, out Prototype? cached))
        {
            return cached;
        }

        Prototype? prototype = null;
        string path = ModelPath(name);
        if (File.Exists(path))
        {
            Node holder = _scene.CreateNode(_prototypes, name);
            ImportResult import = _scene.LoadGltf(path, holder);
            BoundingBox bounds = _scene.GetBounds(import.Root);
            prototype = new Prototype(holder, bounds.Min, bounds.Max);
        }

        _cache[name] = prototype;
        return prototype;
    }

    /// <summary>Places a copy standing at <paramref name="position"/>, turned by <paramref name="yaw"/> radians.</summary>
    public Node? Place(string name, Node parent, Vector3 position, float yaw = 0f, float scale = 1f)
    {
        if (Get(name) is not { } prototype)
        {
            return null;
        }

        Node pivot = _scene.CreateNode(parent, name);
        pivot.Position = position;
        pivot.EulerAngles = new Vector3(0f, yaw, 0f);
        pivot.Scale = new Vector3(scale);
        Node copy = prototype.Root.Clone(pivot);
        copy.Visible = true;
        return pivot;
    }
}

/// <summary>
/// A skinned, animated model (family member or zombie). One player per clip is
/// created at load in a fixed order - locomotion first, upper body actions last -
/// so the actions always layer on top. Blending is done purely with weights.
/// </summary>
public sealed class AnimatedModel
{
    private static readonly Dictionary<string, byte[]> Files = [];
    private static readonly string[] BaseOrder = ["idle", "walk", "run", "spawn", "dodge", "die", "cheer"];
    private static readonly string[] ActionOrder = ["aim", "swing", "thrust", "attack", "cast", "throw", "shoot", "hit"];

    private readonly Dictionary<string, AnimationPlayer> _players = [];
    private readonly Dictionary<string, float> _durations = [];
    private readonly Dictionary<string, float> _weights = [];
    private readonly Dictionary<string, float> _speeds = [];
    private readonly Dictionary<string, Node> _weapons = [];
    private string _base = "idle";
    private string? _action;
    private float _actionTime;
    private float _actionLength;
    private float _actionWeight;
    private string? _holdAction;

    private AnimatedModel(Scene scene, Node root)
    {
        Scene = scene;
        Root = root;
    }

    public Scene Scene { get; }

    /// <summary>Holder node: move and rotate this.</summary>
    public Node Root { get; }

    public string Base => _base;

    public string? Action => _action;

    public bool ActionPlaying => _action is not null;

    /// <summary>0..1 progress through the current action.</summary>
    public float ActionProgress => _action is null ? 1f : Math.Clamp(_actionTime / MathF.Max(_actionLength, 1e-3f), 0f, 1f);

    public static AnimatedModel Load(Scene scene, string file, Node? parent = null)
    {
        if (!Files.TryGetValue(file, out byte[]? bytes))
        {
            bytes = File.ReadAllBytes(PropLibrary.ModelPath(file));
            Files[file] = bytes;
        }

        Node holder = scene.CreateNode(parent, file);
        int before = scene.Animations.Count;
        scene.LoadGltf(bytes, holder);
        AnimatedModel model = new(scene, holder);
        Dictionary<string, AnimationClip> clips = scene.Animations.Skip(before).ToDictionary(c => c.Name);
        foreach (string name in BaseOrder.Concat(ActionOrder))
        {
            if (clips.TryGetValue(name, out AnimationClip? clip))
            {
                AnimationPlayer player = clip.Play(loop: true, speed: 1f, weight: name == "idle" ? 1f : 0f);
                model._players[name] = player;
                model._durations[name] = clip.Duration;
                model._weights[name] = name == "idle" ? 1f : 0f;
                model._speeds[name] = 1f;
            }
        }

        model.CollectWeapons(holder);
        return model;
    }

    private void CollectWeapons(Node node)
    {
        foreach (Node child in node.Children)
        {
            string name = child.Name;
            if (name.StartsWith("W_", StringComparison.Ordinal))
            {
                _weapons[name[2..]] = child;
            }

            CollectWeapons(child);
        }
    }

    public bool Has(string clip) => _players.ContainsKey(clip);

    public float Duration(string clip) => _durations.GetValueOrDefault(clip, 0.5f);

    public void ShowWeapon(string? id)
    {
        foreach ((string key, Node node) in _weapons)
        {
            node.Visible = key == id;
        }
    }

    /// <summary>Switches the locomotion layer. One-shot clips (die, spawn, dodge) restart and stop at the end.</summary>
    public void SetBase(string name, float speed = 1f)
    {
        if (!_players.ContainsKey(name))
        {
            return;
        }

        if (_base != name)
        {
            bool once = name is "die" or "spawn" or "dodge";
            AnimationPlayer player = _players[name];
            if (once || name is "cheer")
            {
                player.Time = 0f;
                player.Loop = !once;
                player.IsPlaying = true;
            }

            _base = name;
        }

        SetSpeed(name, speed);
    }

    private void SetSpeed(string name, float speed)
    {
        if (MathF.Abs(_speeds[name] - speed) > 0.02f)
        {
            _players[name].Speed = speed;
            _speeds[name] = speed;
        }
    }

    /// <summary>Plays an upper body action once (swing, shoot, hit...).</summary>
    public void PlayAction(string name, float speed = 1f)
    {
        if (!_players.TryGetValue(name, out AnimationPlayer? player))
        {
            return;
        }

        if (_action is not null && _action != name)
        {
            SetWeight(_action, 0f);
        }

        player.Time = 0f;
        player.Loop = false;
        player.IsPlaying = true;
        SetSpeed(name, speed);
        _action = name;
        _actionTime = 0f;
        _actionLength = _durations[name] / MathF.Max(speed, 0.01f);
        _actionWeight = 1f;
    }

    /// <summary>A looping upper body pose that stays on under actions (the rifle "aim").</summary>
    public void HoldAction(string? name)
    {
        if (_holdAction == name)
        {
            return;
        }

        if (_holdAction is not null)
        {
            SetWeight(_holdAction, 0f);
        }

        _holdAction = name is not null && _players.ContainsKey(name) ? name : null;
    }

    public void CancelAction()
    {
        if (_action is not null)
        {
            SetWeight(_action, 0f);
            _action = null;
        }
    }

    public void Update(float dt)
    {
        float fade = 1f - MathF.Exp(-dt * 14f);
        foreach (string name in BaseOrder)
        {
            if (_weights.ContainsKey(name))
            {
                float target = name == _base ? 1f : 0f;
                float w = _weights[name] + ((target - _weights[name]) * fade);
                if (target == 1f && w > 0.98f)
                {
                    w = 1f;
                }
                else if (target == 0f && w < 0.02f)
                {
                    w = 0f;
                }

                SetWeight(name, w);
            }
        }

        if (_holdAction is not null)
        {
            SetWeight(_holdAction, 1f);
        }

        if (_action is not null)
        {
            _actionTime += dt;
            float remaining = _actionLength - _actionTime;
            float w = remaining < 0.08f ? MathF.Max(0f, remaining / 0.08f) : 1f;
            SetWeight(_action, w * _actionWeight);
            if (_actionTime >= _actionLength)
            {
                SetWeight(_action, 0f);
                _action = null;
            }
        }
    }

    private void SetWeight(string name, float weight)
    {
        if (MathF.Abs(_weights[name] - weight) > 0.001f)
        {
            _players[name].Weight = weight;
            _weights[name] = weight;
        }
    }
}
