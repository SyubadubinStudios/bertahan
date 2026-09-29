using System.Numerics;
using Bertahan.Core;
using ThreeNet;

namespace Bertahan.Game;

public enum SessionState
{
    Playing,
    Won,
    Lost,
}

public sealed record FloatingText(Vector3 World, string Text, uint Color, float Size)
{
    public float Age { get; set; }
}

/// <summary>One play through of a level: owns the scene, the actors and the rules.</summary>
public sealed class GameSession : IDisposable
{
    private readonly List<Zombie> _zombies = [];
    private readonly Dictionary<string, Queue<Zombie>> _pool = [];
    private readonly GameSettings _settings;
    private readonly Node _lantern;
    private readonly Random _rng = new();
    private float _fieldTimer;
    private float _hitStop;
    private float _comboTimer;
    private float _endTimer;
    private float _messageTime;

    public GameSession(LevelDef level, CharacterDef character, DifficultyDef difficulty, AudioManager audio, GameSettings settings)
    {
        _settings = settings;
        Difficulty = difficulty;
        Lives = difficulty.Lives;
        Audio = audio;
        Scene = new Scene();
        Props = new PropLibrary(Scene);
        Camera = new CameraRig(Scene);
        Node actors = Scene.CreateNode(null, "actors");

        Level = new LevelBuilder(Scene, Props, level.Number).Build(level);
        Lighting(level.Time);

        Atmosphere = new Atmosphere(Scene, AtmosphereStyle.For(level));
        Atmosphere.Swayers.AddRange(Level.Swayers);

        Texture atlas = Scene.CreateTexture(256, 256, Particles.PaintAtlas());
        Fx = new Particles(Scene, atlas);

        AnimatedModel playerModel = AnimatedModel.Load(Scene, "char_" + character.Id, actors);
        Player = new Player(this, character, playerModel) { Position = Level.PlayerStart, Yaw = MathF.PI };
        _lantern = Scene.AddLight(Light.Point(new Vector3(1f, 0.78f, 0.5f), Level.IsNight ? 9f : 0f, 11f), actors, "lantern");

        BuildPools(level, actors);
        Combat = new Combat(this, Props, actors);
        Pickups = new Pickups(this, Props, actors);
        foreach ((string weapon, Vector2 at) in Level.WeaponSpots)
        {
            Pickups.Spawn(PickupKind.Weapon, at, weapon);
        }

        Pickups.Spawn(PickupKind.Molotov, Level.PickupPoints[0]);
        Waves = new WaveDirector(this);

        DifficultySpeed = (1f + ((level.Number - 1) * 0.06f)) * difficulty.EnemySpeed;
        DifficultyDamage = (1f + ((level.Number - 1) * 0.12f)) * difficulty.EnemyDamage;
        Level.Nav.UpdateField(Player.Position);
        Camera.Yaw = 0f;
        Camera.Snap(Player.World);
        Audio.PlayMusic(level.Music);
        ShowBanner(level.Name.ToUpperInvariant(), $"{level.Zone} - {level.Description}");
    }

    public Scene Scene { get; }

    public PropLibrary Props { get; }

    public CameraRig Camera { get; }

    public Level Level { get; }

    public Player Player { get; }

    public Particles Fx { get; }

    public Atmosphere Atmosphere { get; }

    public Combat Combat { get; }

    public Pickups Pickups { get; }

    public WaveDirector Waves { get; }

    public AudioManager Audio { get; }

    public IReadOnlyList<Zombie> Zombies => _zombies;

    public SessionState State { get; private set; } = SessionState.Playing;

    public DifficultyDef Difficulty { get; }

    /// <summary>Tries left in this attempt, including the current one.</summary>
    public int Lives { get; private set; }

    public float DifficultySpeed { get; }

    public float DifficultyDamage { get; }

    public float PowerBonus { get; private set; } = 1f;

    public int Score { get; private set; }

    public int Kills { get; private set; }

    public int TimeBonus { get; private set; }

    public int HealthBonus { get; private set; }

    public int LivesBonus { get; private set; }

    /// <summary>Seconds since the player went down, while waiting to get back up.</summary>
    public float DownTime { get; private set; }

    public int Combo { get; private set; }

    public int BestCombo { get; private set; }

    public float ComboTimeLeft => Math.Clamp(_comboTimer / 3f, 0f, 1f);

    public double PlayTime { get; private set; }

    public string? Message { get; private set; }

    public float MessageAlpha => Math.Clamp(_messageTime / 0.3f, 0f, 1f);

    public (string Title, string Subtitle, float Time)? Banner { get; private set; }

    public List<FloatingText> Texts { get; } = [];

    public Zombie? Boss => _zombies.FirstOrDefault(z => z.IsBoss && z.Active && z.State != ZombieState.Dying);

    public bool Finished => State != SessionState.Playing && _endTimer > 2.5f;

    // ------------------------------------------------------------------ setup

    private void Lighting(TimeOfDay time)
    {
        (Vector3 dir, Vector3 color, float intensity, Vector3 ambient, float ambientI, Vector3 sky, float fog) = time switch
        {
            TimeOfDay.Sore => (new Vector3(0.75f, -0.42f, -0.5f), new Vector3(1f, 0.62f, 0.36f), 2.9f,
                new Vector3(0.55f, 0.45f, 0.62f), 0.42f, new Vector3(0.95f, 0.55f, 0.32f), 0.004f),
            TimeOfDay.Malam => (new Vector3(-0.35f, -0.8f, 0.45f), new Vector3(0.55f, 0.65f, 1f), 0.75f,
                new Vector3(0.3f, 0.38f, 0.65f), 0.32f, new Vector3(0.02f, 0.03f, 0.075f), 0.0055f),
            _ => (new Vector3(-0.45f, -0.85f, -0.3f), new Vector3(1f, 0.96f, 0.88f), 3.3f,
                new Vector3(0.6f, 0.7f, 0.88f), 0.42f, new Vector3(0.62f, 0.78f, 0.94f), 0.0035f),
        };

        Node sun = Scene.AddLight(Light.Directional(color, intensity) with { CastShadow = true, ShadowNormalBias = 2f }, name: "sun");
        sun.Position = -Vector3.Normalize(dir) * 60f;
        sun.LookAt(Vector3.Zero);
        Node fill = Scene.AddLight(Light.Directional(ambient, 0.35f), name: "fill");
        fill.Position = new Vector3(-dir.X, 0.5f, -dir.Z) * 40f;
        fill.LookAt(Vector3.Zero);
        Scene.Environment = Scene.Environment with
        {
            Background = new Vector4(sky, 1f),
            AmbientColor = ambient,
            AmbientIntensity = ambientI,
            FogColor = time == TimeOfDay.Malam ? new Vector3(0.025f, 0.04f, 0.09f) : sky,
            FogDensity = fog,
            // Three.Net also applies linear fog between FogStart and FogEnd (10-100 m by default): keep it out of the way
            FogStart = 1000f,
            FogEnd = 5000f,
        };
    }

    private void BuildPools(LevelDef level, Node parent)
    {
        Dictionary<string, int> caps = new() { ["warga"] = 12, ["tuyul"] = 7, ["pocong"] = 7, ["satpam"] = 5, ["kuntilanak"] = 3, ["genderuwo"] = 2, ["dukun"] = 1 };
        Dictionary<string, int> need = [];
        foreach (WaveDef wave in level.Waves)
        {
            foreach (IGrouping<string, SpawnGroup> g in wave.Groups.GroupBy(g => g.Zombie))
            {
                need[g.Key] = Math.Max(need.GetValueOrDefault(g.Key), g.Sum(x => x.Count));
            }
        }

        if (need.ContainsKey("dukun"))
        {
            need["warga"] = need.GetValueOrDefault("warga") + 4;
            need["tuyul"] = need.GetValueOrDefault("tuyul") + 3;
        }

        int seed = 1;
        foreach ((string id, int count) in need)
        {
            Queue<Zombie> queue = new();
            ZombieDef def = ZombieDef.Get(id);
            for (int i = 0; i < Math.Min(count, caps[id]); i++)
            {
                AnimatedModel model = AnimatedModel.Load(Scene, "zombie_" + id, parent);
                Zombie z = new(this, def, model, seed++);
                _zombies.Add(z);
                queue.Enqueue(z);
            }

            _pool[id] = queue;
        }
    }

    // ------------------------------------------------------------------ gameplay api

    public bool SpawnZombie(string id, Vector2 at)
    {
        if (!_pool.TryGetValue(id, out Queue<Zombie>? queue))
        {
            return false;
        }

        for (int i = 0; i < queue.Count; i++)
        {
            Zombie z = queue.Dequeue();
            queue.Enqueue(z);
            if (!z.Active)
            {
                z.Spawn(Level.Nav.Resolve(at, z.Def.Radius), (1f + ((Level.Def.Number - 1) * 0.1f)) * Difficulty.EnemyHealth);
                return true;
            }
        }

        return false;
    }

    /// <summary>Keeps zombies from stacking on top of each other and of the player.</summary>
    public Vector2 Separation(Zombie self)
    {
        Vector2 push = Vector2.Zero;
        foreach (Zombie other in _zombies)
        {
            if (other == self || !other.Targetable)
            {
                continue;
            }

            Vector2 d = self.Position - other.Position;
            float min = self.Def.Radius + other.Def.Radius + 0.15f;
            float len2 = d.LengthSquared();
            if (len2 < min * min && len2 > 1e-6f)
            {
                float len = MathF.Sqrt(len2);
                push += d / len * ((min - len) / min) * (other.Def.Mass / (self.Def.Mass + other.Def.Mass)) * 2f;
            }
        }

        return push;
    }

    public void OnZombieKilled(Zombie z)
    {
        Kills++;
        Combo++;
        BestCombo = Math.Max(BestCombo, Combo);
        _comboTimer = 3f;
        int multiplier = 1 + Math.Min(Combo / 5, 4);
        Score += (int)MathF.Round(z.Def.Score * multiplier * Difficulty.ScoreMultiplier);
        if (Combo >= 5 && Combo % 5 == 0)
        {
            AddText(Player.World + new Vector3(0, 2.6f, 0), $"KOMBO x{multiplier}!", 0xFFD34A, 30);
            Audio.Play("sfx_pickup", 0.7f, 1.3f);
        }

        if (z.Def.Id == "dukun")
        {
            Fx.Confetti(z.World);
        }

        // occasional drops
        double roll = _rng.NextDouble() / Difficulty.DropChance;
        if (roll < 0.07)
        {
            Pickups.Spawn(PickupKind.Nasi, z.Position, life: 20f);
        }
        else if (roll < 0.12 && Player.Inventory.Any(i => i.Weapon.Kind == WeaponKind.Gun))
        {
            Pickups.Spawn(PickupKind.Ammo, z.Position, life: 20f);
        }
        else if (roll < 0.15)
        {
            Pickups.Spawn(PickupKind.Molotov, z.Position, life: 20f);
        }
    }

    public void OnWaveCleared(int wave)
    {
        int bonus = 100 * (wave + 1);
        Score += bonus;
        bool last = wave + 1 >= Level.Def.Waves.Length;
        ShowBanner(last ? "SEMUA ZOMBI KALAH!" : $"GELOMBANG {wave + 1} SELESAI", last ? "Kampung aman untuk sementara..." : $"Bonus +{bonus}. Istirahat sebentar, ambil perbekalan!");
        if (!last)
        {
            List<Vector2> points = Level.PickupPoints.OrderBy(_ => _rng.Next()).ToList();
            Pickups.Spawn(wave % 2 == 0 ? PickupKind.Nasi : PickupKind.Jamu, points[0]);
            Pickups.Spawn(PickupKind.Molotov, points[1]);
            if (Player.Inventory.Any(i => i.Weapon.Kind == WeaponKind.Gun))
            {
                Pickups.Spawn(PickupKind.Ammo, points[2]);
            }
            else
            {
                Pickups.Spawn(PickupKind.Nasi, points[2]);
            }
        }
    }

    public void OnVictory()
    {
        State = SessionState.Won;
        _endTimer = 0f;
        Player.Model.CancelAction();
        Player.Model.SetBase("cheer");
        Fx.Confetti(Player.World);
        Audio.StopMusic();
        Audio.Play("jingle_victory", 0.9f);
        int timeBonus = Math.Max(0, 3000 - ((int)PlayTime * 5));
        TimeBonus = (int)(timeBonus * Difficulty.ScoreMultiplier);
        HealthBonus = (int)(Player.Health * 5 * Difficulty.ScoreMultiplier);
        LivesBonus = (int)((Lives - 1) * 250 * Difficulty.ScoreMultiplier);
        Score += TimeBonus + HealthBonus + LivesBonus;
        _settings.UnlockedLevel = Math.Max(_settings.UnlockedLevel, Math.Min(LevelDef.All.Length, Level.Def.Number + 1));
        _settings.Save();
    }

    /// <summary>Spends a life: the hero gets back up where they fell, zombies close by are blown away.</summary>
    private void Respawn()
    {
        Lives--;
        DownTime = 0f;
        Vector2 at = Level.Nav.Resolve(Player.Position, Player.Radius);
        foreach (Zombie z in _zombies)
        {
            if (z.Targetable && Vector2.Distance(z.Position, at) < 5f)
            {
                z.Damage(0.01f, at, 16f, 1.5f, false);
            }
        }

        Player.Revive(at);
        Fx.Hearts(Player.World);
        Fx.Emit(Sprite.Ring, Player.World + new Vector3(0, 0.15f, 0), Vector3.Zero, 0.6f, 0.4f, 10f, flat: true);
        Camera.AddShake(0.4f);
        Audio.Play("sfx_heal", 1f, 0.8f);
        ShowBanner("BANGKIT LAGI!", Lives == 1 ? "Ini nyawa terakhirmu. Hati-hati!" : $"Sisa nyawa: {Lives}");
    }

    public void ShowMessage(string text, float seconds)
    {
        Message = text;
        _messageTime = seconds;
    }

    public void ShowBanner(string title, string subtitle) => Banner = (title, subtitle, 3.2f);

    public void AddDamageNumber(Vector3 at, float amount, bool heavy) =>
        AddText(at + new Vector3((_rng.NextSingle() - 0.5f) * 0.6f, 0, 0), ((int)MathF.Round(amount)).ToString(), heavy ? 0xFFB23Fu : 0xFFFFFFu, heavy ? 26 : 20);

    public void AddText(Vector3 at, string text, uint color, float size)
    {
        if (Texts.Count > 40)
        {
            Texts.RemoveAt(0);
        }

        Texts.Add(new FloatingText(at, text, color, size));
    }

    public void HitStop(float seconds) => _hitStop = MathF.Max(_hitStop, seconds);

    // ------------------------------------------------------------------ frame

    public void Update(float rawDt, InputState input)
    {
        // brief freeze on heavy hits sells the impact
        float dt = rawDt;
        if (_hitStop > 0f)
        {
            _hitStop -= rawDt;
            dt = rawDt * 0.08f;
        }

        PlayTime += dt;
        _messageTime -= rawDt;
        if (_messageTime <= 0f)
        {
            Message = null;
        }

        if (Banner is { } b)
        {
            Banner = b.Time - rawDt > 0 ? b with { Time = b.Time - rawDt } : null;
        }

        _comboTimer -= dt;
        if (_comboTimer <= 0f)
        {
            Combo = 0;
        }

        // camera controls
        Camera.Yaw += input.CameraTurn * rawDt * 2.2f;
        Camera.Distance = Math.Clamp(Camera.Distance - (input.Zoom * 1.2f), 7f, 17f);

        if (State != SessionState.Playing)
        {
            _endTimer += rawDt;
            input = new InputState { WeaponSlot = -1 };
        }

        Player.Update(dt, input);
        if (!Player.Alive && State == SessionState.Playing)
        {
            DownTime += dt;
            if (Lives > 1)
            {
                if (Player.DeathFinished)
                {
                    Respawn();
                }
            }
            else
            {
                Lives = 0;
                State = SessionState.Lost;
                _endTimer = 0f;
                Audio.StopMusic();
                Audio.Play("jingle_defeat", 0.9f);
            }
        }

        _fieldTimer -= dt;
        if (_fieldTimer <= 0f)
        {
            _fieldTimer = 0.25f;
            Level.Nav.UpdateField(Player.Position);
        }

        foreach (Zombie z in _zombies)
        {
            z.Update(dt);
        }

        Combat.Update(dt);
        Pickups.Update(dt);
        if (State == SessionState.Playing)
        {
            Waves.Update(dt);
        }

        Scene.UpdateAnimations(dt);

        // lantern follows the player at night
        _lantern.Position = Player.World + new Vector3(0.4f, 2.2f, 0.3f);

        Vector3 lead = new Vector3(Player.Velocity.X, 0, Player.Velocity.Y) * 0.25f;
        Camera.Follow(Player.World, lead, rawDt, _settings.ScreenShake);
        CutAway();
        Atmosphere.Update(rawDt, Camera.Position, Player.World, Fx, Audio);
        Fx.Update(dt, Camera.Right, Camera.Up);
        Audio.Update(rawDt, Camera.Position, Camera.Forward);

        for (int i = Texts.Count - 1; i >= 0; i--)
        {
            FloatingText t = Texts[i];
            t.Age += rawDt;
            if (t.Age > 1.1f)
            {
                Texts.RemoveAt(i);
            }
        }
    }

    /// <summary>Hides houses and trees that stand between the camera and the player.</summary>
    private void CutAway()
    {
        Vector2 cam = new(Camera.Position.X, Camera.Position.Z);
        Vector2 p = Player.Position;
        Vector2 seg = cam - p;
        float len2 = MathF.Max(seg.LengthSquared(), 1e-3f);
        foreach (Occluder o in Level.Occluders)
        {
            // distance from the prop to the player-camera line, only on the camera side of the player
            float t = Math.Clamp(Vector2.Dot(o.Centre - p, seg) / len2, 0f, 1f);
            float d = Vector2.Distance(o.Centre, p + (seg * t));
            bool hide = t > 0.02f && d < o.Radius + 0.8f;
            if (hide != o.Hidden)
            {
                o.Hidden = hide;
                o.Node.Visible = !hide;
            }
        }
    }

    public void Dispose()
    {
        Combat.Clear();
        Pickups.Clear();
        Fx.Clear();
        Scene.Dispose();
    }
}
