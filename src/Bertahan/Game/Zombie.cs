using System.Numerics;

namespace Bertahan.Game;

public enum ZombieState
{
    Inactive,
    Spawning,
    Chasing,
    Charging,
    Dying,
}

public sealed class Zombie
{
    private readonly GameSession _game;
    private readonly Random _rng;
    private float _stateTime;
    private float _attackCooldown;
    private float _pendingHit = -1f;
    private float _stun;
    private Vector2 _knockback;
    private float _squash;
    private float _voiceTimer;
    private float _specialTimer;
    private float _summonTimer;
    private float _secondTimer;
    private float _hopPhase;
    private float _zigzag;
    private Vector2 _chargeDir;
    private bool _chargeHit;
    private float _chargeWind;
    private float _chargeTime;
    private float _chargeSpeed;
    private float _chargeDamage;

    public Zombie(GameSession game, ZombieDef def, AnimatedModel model, int seed)
    {
        _game = game;
        Def = def;
        Model = model;
        _rng = new Random(seed);
        Model.Root.Visible = false;
    }

    public ZombieDef Def { get; }

    public AnimatedModel Model { get; }

    public ZombieState State { get; private set; } = ZombieState.Inactive;

    public Vector2 Position { get; set; }

    public float Yaw { get; set; }

    public float Health { get; private set; }

    public float MaxHealth { get; private set; }

    public bool Active => State != ZombieState.Inactive;

    public bool Targetable => State is ZombieState.Chasing or ZombieState.Charging;

    public bool IsBoss => Def.Boss;

    /// <summary>Below half health bosses fight harder.</summary>
    public bool Enraged => IsBoss && Health < MaxHealth * 0.5f;

    public Vector3 World => new(Position.X, 0f, Position.Y);

    public float Height => Def.Height * Def.Scale;

    private float SpawnSpeed => IsBoss || Def.Caster ? 0.6f : 1f;

    public void Spawn(Vector2 at, float healthScale)
    {
        Position = at;
        Yaw = MathF.Atan2(_game.Player.Position.X - at.X, _game.Player.Position.Y - at.Y);
        MaxHealth = Def.Health * healthScale;
        Health = MaxHealth;
        State = ZombieState.Spawning;
        _stateTime = 0f;
        _attackCooldown = 1f;
        _pendingHit = -1f;
        _stun = 0f;
        _knockback = Vector2.Zero;
        _specialTimer = 3f + ((float)_rng.NextDouble() * 3f);
        _secondTimer = 5f + ((float)_rng.NextDouble() * 3f);
        _summonTimer = IsBoss ? 10f : 12f;
        _voiceTimer = 1f + ((float)_rng.NextDouble() * 4f);
        Model.Root.Visible = true;
        Model.Root.Scale = new Vector3(Def.Scale);
        Model.CancelAction();
        Model.SetBase("spawn", SpawnSpeed);
        _game.Fx.Dust(World, IsBoss ? 16 : 6, IsBoss ? 1.2f : 0.6f);
        if (IsBoss)
        {
            _game.Camera.AddShake(0.6f);
        }

        _game.Audio.PlayAt(Def.Sound, World, 0.8f, 0.9f + ((float)_rng.NextDouble() * 0.2f), 0.4);
    }

    public void Damage(float amount, Vector2 from, float knockback, float stun, bool heavy)
    {
        if (!Targetable && State != ZombieState.Spawning)
        {
            return;
        }

        Health -= amount;
        Vector2 away = Position - from;
        if (away.LengthSquared() > 1e-4f)
        {
            _knockback += Vector2.Normalize(away) * knockback / Def.Mass;
        }

        _stun = MathF.Max(_stun, stun / MathF.Sqrt(Def.Mass));
        _squash = 1f;
        Vector3 at = World + new Vector3(0, MathF.Min(Height * 0.6f, 2.2f), 0);
        _game.Fx.Hit(at, new Vector3(away.X, 0.4f, away.Y), heavy);
        _game.AddDamageNumber(at + new Vector3(0, 0.6f, 0), amount, heavy);
        if (Health <= 0f)
        {
            Die();
            return;
        }

        if ((!IsBoss || heavy) && _pendingHit < 0f)
        {
            Model.PlayAction("hit", 1.3f);
        }
    }

    private void Die()
    {
        State = ZombieState.Dying;
        _stateTime = 0f;
        Model.CancelAction();
        Model.SetBase("die", 1.2f);
        float size = Height / 1.7f;
        _game.Fx.Death(World, Math.Clamp(size, 0.7f, 2.5f));
        _game.Audio.PlayAt("sfx_zombie_die", World, 0.9f, Math.Clamp(1.4f / size, 0.5f, 1.5f), 0.08);
        if (IsBoss)
        {
            _game.Camera.AddShake(1f);
            _game.HitStop(0.25f);
        }

        _game.OnZombieKilled(this);
    }

    public void Update(float dt)
    {
        if (State == ZombieState.Inactive)
        {
            return;
        }

        _stateTime += dt;
        _attackCooldown -= dt;
        _stun -= dt;
        _squash = MathF.Max(0f, _squash - (dt * 5f));

        switch (State)
        {
            case ZombieState.Spawning:
                if (_stateTime > Model.Duration("spawn") / SpawnSpeed)
                {
                    State = ZombieState.Chasing;
                    Model.SetBase(Def.Stationary ? "idle" : "walk");
                }

                break;
            case ZombieState.Dying:
                // sink into the ground after the fall, then return to the pool
                if (_stateTime > 2.2f)
                {
                    Model.Root.Position = World - new Vector3(0, (_stateTime - 2.2f) * 0.8f * MathF.Max(1f, Height / 2f), 0);
                }

                if (_stateTime > (IsBoss ? 4.5f : 3.4f))
                {
                    State = ZombieState.Inactive;
                    Model.Root.Visible = false;
                }

                Model.Update(dt);
                return;
            case ZombieState.Charging:
                UpdateCharge(dt);
                break;
            default:
                UpdateChase(dt);
                break;
        }

        // hit reaction bounce: squash and stretch (the big ones barely wobble)
        float s = _squash * (IsBoss ? 0.3f : 1f);
        Model.Root.Scale = new Vector3(1f + (s * 0.18f), 1f - (s * 0.15f), 1f + (s * 0.18f)) * Def.Scale;
        Model.Root.Position = World;
        Model.Root.EulerAngles = new Vector3(0, Yaw, 0);
        Model.Update(dt);
    }

    private void UpdateChase(float dt)
    {
        Player player = _game.Player;
        Vector2 toPlayer = player.Position - Position;
        float distance = toPlayer.Length();
        Vector2 dirToPlayer = distance > 1e-3f ? toPlayer / distance : Vector2.UnitY;

        _voiceTimer -= dt;
        if (_voiceTimer <= 0f)
        {
            _voiceTimer = (IsBoss ? 7f : 4f) + ((float)_rng.NextDouble() * 6f);
            string sound = Def.Id is "warga" or "satpam" or "pocong" or "pocong_penjaga" ? $"sfx_groan_{_rng.Next(3)}" : Def.Sound;
            if (!Def.Caster)
            {
                _game.Audio.PlayAt(sound, World, IsBoss ? 0.9f : 0.55f, (IsBoss ? 0.7f : 0.85f) + ((float)_rng.NextDouble() * 0.3f), 0.5);
            }
        }

        Specials(dt, distance, dirToPlayer);
        if (State != ZombieState.Chasing)
        {
            return;
        }

        bool attacking = _pendingHit >= 0f || Model.Action == "attack" || Model.Action == "cast";
        if (Def.Stationary)
        {
            // rooted bosses (in a well, a swamp, a pool) only turn to face the player
            Yaw = Player.AngleLerp(Yaw, MathF.Atan2(dirToPlayer.X, dirToPlayer.Y), dt * 3f);
            Model.SetBase("idle");
        }
        else
        {
            Move(dt, player, distance, dirToPlayer, attacking);
        }

        // melee attack
        bool canMelee = !Def.Caster && Def.Id is not ("tuyul_serdadu" or "kuntilanak_geni" or "kunti_penguasa") || distance < 2.2f;
        if (canMelee && distance < Def.AttackRange + player.Radius + 0.3f && _attackCooldown <= 0f && _stun <= 0f && player.Alive)
        {
            _attackCooldown = Def.AttackCooldown * (Enraged ? 0.75f : 1f);
            float speed = Def.Brute ? 0.9f : 1.2f;
            Model.PlayAction("attack", speed);
            _pendingHit = Model.Duration("attack") / speed * 0.55f;
        }

        if (_pendingHit >= 0f)
        {
            _pendingHit -= dt;
            if (_pendingHit < 0f && _stun <= 0f)
            {
                LandBlow(player, distance, dirToPlayer);
            }
        }
    }

    private void LandBlow(Player player, float distance, Vector2 dirToPlayer)
    {
        float damage = Def.Damage * _game.DifficultyDamage;
        if (Def.Brute)
        {
            // big hitters smash the ground in front of them
            float reach = Def.Stationary ? distance : MathF.Min(distance, Def.Radius + 1.2f);
            Vector3 at = World + (new Vector3(dirToPlayer.X, 0, dirToPlayer.Y) * reach);
            float radius = Def.Id switch { "genderuwo" => 2.8f, "jeng_roro" => 3.2f, "kraken_raja" or "leviathan" => 3f, _ => 3.4f };
            _game.Combat.Shockwave(at, radius, damage, false);
        }
        else if (Def.Id == "pocong_penjaga")
        {
            _game.Combat.Shockwave(World + (new Vector3(dirToPlayer.X, 0, dirToPlayer.Y) * 1.2f), 2.2f, damage, false);
        }
        else if (distance < Def.AttackRange + player.Radius + 0.6f)
        {
            player.Damage(damage, Position, 5f);
            _game.Audio.PlayAt("sfx_hit_blunt", player.World, 0.7f, 0.8f);
        }
    }

    private void Move(float dt, Player player, float distance, Vector2 dirToPlayer, bool attacking)
    {
        // steering: flow field around buildings, straight line when the way is clear
        Vector2 desired;
        if (distance < 7f && _game.Level.Nav.LineClear(Position, player.Position))
        {
            desired = dirToPlayer;
        }
        else
        {
            desired = _game.Level.Nav.Direction(Position);
            if (desired == Vector2.Zero)
            {
                desired = dirToPlayer;
            }
        }

        if (Def.Zigzag)
        {
            _zigzag += dt * 7f;
            desired = Vector2.Normalize(desired + (new Vector2(-desired.Y, desired.X) * MathF.Sin(_zigzag) * 0.6f));
        }

        // casters keep their distance and let their magic do the work
        if (Def.Caster && distance < 7f)
        {
            desired = -dirToPlayer;
        }

        desired += _game.Separation(this) * 1.2f;
        if (desired.LengthSquared() > 1e-4f)
        {
            desired = Vector2.Normalize(desired);
        }

        float speed = distance < 8f ? Def.RunSpeed : Def.WalkSpeed;
        speed *= _game.DifficultySpeed * (Enraged ? 1.2f : 1f);
        if (_game.Level.InSlowZone(Position) && !Def.IgnoresWater)
        {
            speed *= 0.7f;
        }

        if (Def.Hopper)
        {
            // pocong only moves while in the air
            _hopPhase += dt * (distance < 8f ? 1f / 0.47f : 1f / 0.67f);
            float air = MathF.Max(0f, MathF.Sin(_hopPhase * MathF.PI * 2f));
            speed *= air * 2.2f;
            if (MathF.Sin(_hopPhase * MathF.PI * 2f) < -0.95f && MathF.Sin((_hopPhase - (dt * 2f)) * MathF.PI * 2f) >= -0.95f)
            {
                _game.Audio.PlayAt("sfx_pocong_hop", World, 0.4f, Def.Id == "pocong_penjaga" ? 0.8f : 1f, 0.15);
            }
        }

        if (_stun > 0f || attacking || !_game.Player.Alive)
        {
            speed = _stun > 0f ? 0f : speed * 0.2f;
        }

        _knockback *= MathF.Exp(-7f * dt);
        Vector2 velocity = (desired * speed) + _knockback;
        Position = _game.Level.Nav.Resolve(Position + (velocity * dt), Def.Radius);

        if (_stun <= 0f)
        {
            float targetYaw = MathF.Atan2(distance < 4f ? dirToPlayer.X : desired.X, distance < 4f ? dirToPlayer.Y : desired.Y);
            Yaw = Player.AngleLerp(Yaw, targetYaw, dt * 8f);
        }

        if (Def.Hopper)
        {
            Model.SetBase(distance < 8f ? "run" : "walk");
        }
        else if (_stun > 0f)
        {
            Model.SetBase("idle");
        }
        else
        {
            Model.SetBase(distance < 8f && Def.RunSpeed > Def.WalkSpeed * 1.3f ? "run" : "walk", Math.Clamp(speed / MathF.Max(Def.WalkSpeed, 0.1f), 0.6f, 1.6f));
        }
    }

    // ------------------------------------------------------------------ special moves

    private Vector3 Mouth => World + new Vector3(0, MathF.Min(Height * 0.75f, 3.2f), 0);

    /// <summary>A fan of projectiles aimed at the player.</summary>
    private void Fan(Vector2 dir, int count, float spread, Sprite sprite, float speed, float damage, bool burn = false, Vector3? from = null)
    {
        float baseAngle = MathF.Atan2(dir.X, dir.Y);
        for (int i = 0; i < count; i++)
        {
            float a = baseAngle + ((i - ((count - 1) / 2f)) * spread);
            _game.Combat.Fireball(from ?? Mouth, new Vector2(MathF.Sin(a), MathF.Cos(a)), damage * _game.DifficultyDamage, sprite, speed, burn);
        }
    }

    private void Summon(int count, string banner)
    {
        _game.Fx.Magic(World + new Vector3(0, 1, 0), true, 20);
        _game.Waves.Summon(Position, count, Def.Summons);
        _game.ShowMessage(banner, 2f);
    }

    private void Scream(float slow, string message)
    {
        Model.PlayAction("cast", 0.9f);
        _game.Audio.PlayAt("sfx_kunti", World, 1f, IsBoss ? 0.75f : 1f, 1.0);
        _game.Fx.Emit(Sprite.Ring, World + new Vector3(0, 1.6f, 0), Vector3.Zero, 0.6f, 0.4f, IsBoss ? 16f : 9f);
        _game.Fx.Magic(World + new Vector3(0, 1.8f, 0), false, 10);
        _game.Player.SlowTimer = slow;
        _game.Camera.AddShake(0.3f);
        _game.ShowMessage(message, 2f);
    }

    private void StartCharge(Vector2 dir, float wind, float time, float speed, float damage, string sound)
    {
        State = ZombieState.Charging;
        _stateTime = 0f;
        _chargeDir = dir;
        _chargeHit = false;
        _chargeWind = wind;
        _chargeTime = time;
        _chargeSpeed = speed;
        _chargeDamage = damage;
        Yaw = MathF.Atan2(dir.X, dir.Y);
        Model.SetBase("idle");
        Model.PlayAction("cast", 1f);
        _game.Audio.PlayAt(sound, World, 1f, 0.9f, 0.5);
        _game.Camera.AddShake(0.3f);
    }

    /// <summary>Marks the ground under the player; the blow lands a moment later.</summary>
    private void SlamAtPlayer(float radius, float delay, Vector2 offset = default)
    {
        Vector2 p = _game.Player.Position + offset;
        Model.PlayAction("attack", 0.9f);
        _game.Combat.Slam(new Vector3(p.X, 0, p.Y), delay, radius, Def.Damage * 0.8f * _game.DifficultyDamage);
    }

    private void Specials(float dt, float distance, Vector2 dir)
    {
        _specialTimer -= dt;
        _secondTimer -= dt;
        _summonTimer -= dt;
        bool ready = _specialTimer <= 0f;
        switch (Def.Id)
        {
            case "kuntilanak" when ready && distance < 9f:
                _specialTimer = 7f;
                Scream(2.5f, "Jeritan Kuntilanak! Gerakanmu melambat...");
                break;
            case "genderuwo" or "genderuwo_raksasa" when ready && distance is > 5f and < 15f:
                _specialTimer = Def.Id == "genderuwo" ? 8f : 7f;
                StartCharge(dir, 0.8f, 1.2f, Def.Id == "genderuwo" ? 9.5f : 10.5f, Def.Id == "genderuwo" ? 30f : 38f, "sfx_roar");
                break;
            case "siluman_harimau" when ready && distance is > 3.5f and < 10f:
                // a tiger's pounce: a short crouch, then a long leap
                _specialTimer = 4.5f + (float)_rng.NextDouble() * 2f;
                StartCharge(dir, 0.35f, 0.55f, 13f, 20f, "sfx_roar");
                break;
            case "tuyul_serdadu" when ready && distance is > 3f and < 13f:
                _specialTimer = 2.6f + (float)_rng.NextDouble() * 1.5f;
                Model.PlayAction("attack", 1.4f);
                _game.Audio.PlayAt("sfx_gunshot", World, 0.35f, 1.5f, 0.2);
                Fan(dir, 1, 0f, Sprite.Spark, 16f, 7f, from: World + new Vector3(0, 0.7f, 0));
                break;
            case "kuntilanak_geni":
                if (ready && distance < 12f)
                {
                    _specialTimer = 4.5f;
                    Model.PlayAction("cast", 1f);
                    _game.Audio.PlayAt("sfx_fire_whoosh", World, 0.8f, 1.2f, 0.3);
                    Fan(dir, 3, 0.3f, Sprite.Flame, 10f, 12f, burn: true);
                }

                if (_secondTimer <= 0f)
                {
                    // she leaves burning footprints behind
                    _secondTimer = 2.5f;
                    _game.Combat.HostileFire(World, 1.3f, 3.5f);
                }

                break;
            case "dukun" or "dukun_santet":
                if (ready && distance < 20f)
                {
                    bool santet = Def.Id == "dukun_santet";
                    _specialTimer = Health < MaxHealth * 0.5f ? 2.4f : 3.4f;
                    Model.PlayAction("cast", 1f);
                    _game.Audio.PlayAt("sfx_cast", World, 0.9f, santet ? 0.8f : 1f, 0.5);
                    Fan(dir, Health < MaxHealth * 0.5f ? 5 : 3, 0.28f, santet ? Sprite.GooPurple : Sprite.MagicGreen, 9f, santet ? 16f : 14f);
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 14f;
                    Summon(Health < MaxHealth * 0.5f ? 4 : 3, Def.Id == "dukun" ? "Dukun memanggil anak buahnya!" : "Santet! Arwah penjaga dipanggil!");
                }

                break;

            // ------------------------------------------------------------ bosses 5-10
            case "jeng_roro":
                if (ready && distance < 22f)
                {
                    // splashes of cursed well water
                    _specialTimer = Enraged ? 2.4f : 3.2f;
                    Model.PlayAction("cast", 1.1f);
                    _game.Audio.PlayAt("sfx_fireball", World, 0.8f, 0.6f, 0.3);
                    Fan(dir, Enraged ? 7 : 5, 0.22f, Sprite.MagicBlue, 10f, 13f);
                }

                if (_secondTimer <= 0f && distance < 14f)
                {
                    _secondTimer = 9f;
                    Scream(2.5f, "Tembang Jeng Roro! Tubuhmu terasa berat...");
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 15f;
                    Summon(3, "Jeng Roro memanggil pocong dari sumur!");
                }

                break;
            case "kunti_penguasa":
                if (ready && distance < 20f)
                {
                    _specialTimer = Enraged ? 2.2f : 3f;
                    Model.PlayAction("attack", 1f);
                    _game.Audio.PlayAt("sfx_fireball", World, 0.8f, 0.8f, 0.3);
                    Fan(dir, Enraged ? 7 : 5, 0.24f, Sprite.MagicGreen, 9.5f, 12f);
                }

                if (_secondTimer <= 0f && distance < 12f)
                {
                    _secondTimer = 7f;
                    Scream(3f, "Lima jeritan sekaligus! Kamu melambat...");
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 16f;
                    Summon(2, "Penguasa kutukan memanggil kuntilanak!");
                }

                break;
            case "genderuwo_raja":
                if (ready && distance is > 5f and < 18f)
                {
                    _specialTimer = Enraged ? 5f : 7f;
                    StartCharge(dir, 0.9f, 1.4f, 11f, 42f, "sfx_roar");
                }

                if (Enraged && _secondTimer <= 0f)
                {
                    _secondTimer = 5f;
                    Model.PlayAction("attack", 0.9f);
                    _game.Combat.Shockwave(World, 5.5f, Def.Damage * 0.7f * _game.DifficultyDamage, false);
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 18f;
                    Summon(3, "Genderuwo Raja memanggil pasukannya!");
                }

                break;
            case "kraken_raja":
                if (ready && distance < 26f)
                {
                    _specialTimer = Enraged ? 2f : 2.6f;
                    SlamAtPlayer(2.6f, 1.0f);
                    if (Enraged)
                    {
                        SlamAtPlayer(2.2f, 1.3f, new Vector2(((float)_rng.NextDouble() - 0.5f) * 6f, ((float)_rng.NextDouble() - 0.5f) * 6f));
                    }
                }

                if (_secondTimer <= 0f)
                {
                    _secondTimer = 5f;
                    Model.PlayAction("cast", 1f);
                    Fan(dir, 3, 0.35f, Sprite.GooPurple, 8f, 14f);
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 20f;
                    Summon(2, "Kraken Raja memanggil makhluk rawa!");
                }

                break;
            case "leviathan":
                if (ready && distance < 26f)
                {
                    // all three heads breathe at once
                    _specialTimer = Enraged ? 2.6f : 3.4f;
                    Model.PlayAction("cast", 1f);
                    _game.Audio.PlayAt("sfx_fire_whoosh", World, 1f, 0.6f, 0.3);
                    Fan(dir, Enraged ? 9 : 7, 0.18f, Sprite.MagicGreen, 11f, 14f);
                }

                if (_secondTimer <= 0f && Enraged)
                {
                    _secondTimer = 4f;
                    SlamAtPlayer(3f, 1.1f);
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 18f;
                    Summon(3, "Pusaran kutukan memuntahkan musuh baru!");
                }

                break;
            case "demon_king":
                if (ready && distance < 22f)
                {
                    _specialTimer = Enraged ? 2.2f : 3f;
                    Model.PlayAction("cast", 1f);
                    _game.Audio.PlayAt("sfx_fire_whoosh", World, 1f, 0.5f, 0.3);
                    Fan(dir, Enraged ? 9 : 7, 0.2f, Sprite.Flame, 11f, 15f, burn: true);
                }

                if (_secondTimer <= 0f)
                {
                    _secondTimer = Enraged ? 4f : 6f;
                    if (distance is > 6f and < 18f)
                    {
                        StartCharge(dir, 0.7f, 1.3f, 12f, 45f, "sfx_roar");
                    }
                    else
                    {
                        SlamAtPlayer(3.2f, 1.0f);
                    }
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 14f;
                    Summon(Enraged ? 4 : 3, "Demon King membuka gerbang neraka!");
                }

                break;
        }
    }

    private void UpdateCharge(float dt)
    {
        // wind up, then barrel forward
        if (_stateTime < _chargeWind)
        {
            Model.SetBase("idle");
            if (_stateTime % 0.1f < dt)
            {
                _game.Fx.Dust(World, 1, 0.5f);
            }

            return;
        }

        Model.SetBase("run", 1.6f);
        Vector2 next = Position + (_chargeDir * _chargeSpeed * dt);
        Vector2 resolved = _game.Level.Nav.Resolve(next, Def.Radius);
        bool blocked = Vector2.Distance(next, resolved) > 0.05f;
        Position = resolved;
        if (_stateTime % 0.08f < dt)
        {
            _game.Fx.Dust(World, 2, 0.7f);
        }

        Player player = _game.Player;
        if (!_chargeHit && Vector2.Distance(player.Position, Position) < Def.Radius + player.Radius + 0.4f)
        {
            _chargeHit = true;
            player.Damage(_chargeDamage * _game.DifficultyDamage, Position - (_chargeDir * 2f), 14f);
            _game.Audio.PlayAt("sfx_hit_blunt", player.World, 1f, 0.6f);
        }

        if (_stateTime > _chargeWind + _chargeTime || blocked)
        {
            State = ZombieState.Chasing;
            if (blocked && Def.Brute)
            {
                _stun = 1.2f;
                _game.Camera.AddShake(0.5f);
                _game.Fx.Hit(World + new Vector3(0, 1.8f, 0), Vector3.Zero, true);
                _game.Audio.PlayAt("sfx_boing", World, 1f, 0.7f);
            }
        }
    }
}
