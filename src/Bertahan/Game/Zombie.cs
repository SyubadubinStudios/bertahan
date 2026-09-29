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
    private float _hopPhase;
    private float _zigzag;
    private Vector2 _chargeDir;
    private bool _chargeHit;

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

    public bool IsBoss => Def.Id is "dukun" or "genderuwo";

    public Vector3 World => new(Position.X, 0f, Position.Y);

    public float Height => Def.Id switch
    {
        "tuyul" => 1.0f,
        "genderuwo" => 2.4f,
        _ => 1.7f,
    };

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
        _specialTimer = 3f + (float)_rng.NextDouble() * 3f;
        _summonTimer = 12f;
        _voiceTimer = 1f + (float)_rng.NextDouble() * 4f;
        Model.Root.Visible = true;
        Model.Root.Scale = Vector3.One;
        Model.CancelAction();
        Model.SetBase("spawn", Def.Id == "dukun" ? 0.6f : 1f);
        _game.Fx.Dust(World, 6, 0.6f);
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
        Vector3 at = World + new Vector3(0, Height * 0.6f, 0);
        _game.Fx.Hit(at, new Vector3(away.X, 0.4f, away.Y), heavy);
        _game.AddDamageNumber(at + new Vector3(0, 0.6f, 0), amount, heavy);
        if (Health <= 0f)
        {
            Die();
            return;
        }

        if (!IsBoss || heavy)
        {
            if (_pendingHit < 0f)
            {
                Model.PlayAction("hit", 1.3f);
            }
        }
    }

    private void Die()
    {
        State = ZombieState.Dying;
        _stateTime = 0f;
        Model.CancelAction();
        Model.SetBase("die", 1.2f);
        _game.Fx.Death(World, Def.Id == "genderuwo" ? 1.6f : Def.Id == "tuyul" ? 0.7f : 1f);
        _game.Audio.PlayAt("sfx_zombie_die", World, 0.9f, Def.Id == "tuyul" ? 1.5f : Def.Id == "genderuwo" ? 0.6f : 1f, 0.08);
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
                if (_stateTime > Model.Duration("spawn") / (Def.Id == "dukun" ? 0.6f : 1f))
                {
                    State = ZombieState.Chasing;
                    Model.SetBase("walk");
                }

                break;
            case ZombieState.Dying:
                // sink into the ground after the fall, then return to the pool
                if (_stateTime > 2.2f)
                {
                    Model.Root.Position = World - new Vector3(0, (_stateTime - 2.2f) * 0.8f, 0);
                }

                if (_stateTime > 3.4f)
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

        // hit reaction bounce: squash and stretch
        float s = _squash;
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
            _voiceTimer = 4f + ((float)_rng.NextDouble() * 6f);
            string sound = Def.Id is "warga" or "satpam" or "pocong" ? $"sfx_groan_{_rng.Next(3)}" : Def.Sound;
            if (Def.Id != "dukun")
            {
                _game.Audio.PlayAt(sound, World, 0.55f, 0.85f + ((float)_rng.NextDouble() * 0.3f), 0.5);
            }
        }

        Specials(dt, distance, dirToPlayer);
        if (State != ZombieState.Chasing)
        {
            return;
        }

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

        if (Def.Id == "tuyul")
        {
            _zigzag += dt * 7f;
            desired = Vector2.Normalize(desired + (new Vector2(-desired.Y, desired.X) * MathF.Sin(_zigzag) * 0.6f));
        }

        // the dukun keeps his distance and lets his magic do the work
        if (Def.Id == "dukun" && distance < 7f)
        {
            desired = -dirToPlayer;
        }

        desired += _game.Separation(this) * 1.2f;
        if (desired.LengthSquared() > 1e-4f)
        {
            desired = Vector2.Normalize(desired);
        }

        bool attacking = _pendingHit >= 0f || Model.Action == "attack" || Model.Action == "cast";
        float speed = distance < 8f ? Def.RunSpeed : Def.WalkSpeed;
        speed *= _game.DifficultySpeed;
        if (_game.Level.InSlowZone(Position) && Def.Id is not ("kuntilanak" or "pocong"))
        {
            speed *= 0.7f;
        }

        if (Def.Id == "pocong")
        {
            // pocong only moves while in the air
            _hopPhase += dt * (distance < 8f ? 1f / 0.47f : 1f / 0.67f);
            float air = MathF.Max(0f, MathF.Sin(_hopPhase * MathF.PI * 2f));
            speed *= air * 2.2f;
            if (MathF.Sin(_hopPhase * MathF.PI * 2f) < -0.95f && MathF.Sin((_hopPhase - (dt * 2f)) * MathF.PI * 2f) >= -0.95f)
            {
                _game.Audio.PlayAt("sfx_pocong_hop", World, 0.4f, 1f, 0.15);
            }
        }

        if (_stun > 0f || attacking || _game.Player.Alive == false)
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

        if (Def.Id == "pocong")
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

        // melee attack
        if (distance < Def.AttackRange + player.Radius + 0.3f && _attackCooldown <= 0f && _stun <= 0f && player.Alive && Def.Id != "dukun")
        {
            _attackCooldown = Def.AttackCooldown;
            Model.PlayAction("attack", Def.Id == "genderuwo" ? 0.9f : 1.2f);
            _pendingHit = Model.Duration("attack") / (Def.Id == "genderuwo" ? 0.9f : 1.2f) * 0.55f;
        }

        if (_pendingHit >= 0f)
        {
            _pendingHit -= dt;
            if (_pendingHit < 0f && _stun <= 0f)
            {
                if (Def.Id == "genderuwo")
                {
                    _game.Combat.Shockwave(World + (new Vector3(dirToPlayer.X, 0, dirToPlayer.Y) * 1.2f), 2.8f, Def.Damage * _game.DifficultyDamage, false);
                }
                else if (distance < Def.AttackRange + player.Radius + 0.6f)
                {
                    player.Damage(Def.Damage * _game.DifficultyDamage, Position, 5f);
                    _game.Audio.PlayAt("sfx_hit_blunt", player.World, 0.7f, 0.8f);
                }
            }
        }
    }

    private void Specials(float dt, float distance, Vector2 dir)
    {
        _specialTimer -= dt;
        switch (Def.Id)
        {
            case "kuntilanak" when _specialTimer <= 0f && distance < 9f:
                _specialTimer = 7f;
                Model.PlayAction("cast", 0.9f);
                _game.Audio.PlayAt("sfx_kunti", World, 1f, 1f, 1.0);
                _game.Fx.Emit(Sprite.Ring, World + new Vector3(0, 1.6f, 0), Vector3.Zero, 0.6f, 0.4f, 9f);
                _game.Fx.Magic(World + new Vector3(0, 1.8f, 0), false, 10);
                _game.Player.SlowTimer = 2.5f;
                _game.Camera.AddShake(0.3f);
                _game.ShowMessage("Jeritan Kuntilanak! Gerakanmu melambat...", 2f);
                break;
            case "genderuwo" when _specialTimer <= 0f && distance is > 5f and < 15f:
                _specialTimer = 8f;
                State = ZombieState.Charging;
                _stateTime = 0f;
                _chargeDir = dir;
                _chargeHit = false;
                Yaw = MathF.Atan2(dir.X, dir.Y);
                Model.SetBase("idle");
                Model.PlayAction("cast", 1f);
                _game.Audio.PlayAt("sfx_roar", World, 1f, 0.9f, 0.5);
                _game.Camera.AddShake(0.4f);
                break;
            case "dukun":
                _summonTimer -= dt;
                if (_specialTimer <= 0f && distance < 20f)
                {
                    _specialTimer = Health < MaxHealth * 0.5f ? 2.4f : 3.4f;
                    Model.PlayAction("cast", 1f);
                    _game.Audio.PlayAt("sfx_cast", World, 0.9f, 1f, 0.5);
                    Vector3 hand = World + new Vector3(0, 1.9f, 0);
                    int count = Health < MaxHealth * 0.5f ? 5 : 3;
                    for (int i = 0; i < count; i++)
                    {
                        float spread = (i - ((count - 1) / 2f)) * 0.28f;
                        float a = MathF.Atan2(dir.X, dir.Y) + spread;
                        _game.Combat.Fireball(hand, new Vector2(MathF.Sin(a), MathF.Cos(a)), 14f * _game.DifficultyDamage);
                    }
                }

                if (_summonTimer <= 0f)
                {
                    _summonTimer = 14f;
                    _game.Fx.Magic(World + new Vector3(0, 1, 0), true, 20);
                    _game.Waves.Summon(Position, Health < MaxHealth * 0.5f ? 4 : 3);
                    _game.ShowMessage("Dukun memanggil anak buahnya!", 2f);
                }

                break;
        }
    }

    private void UpdateCharge(float dt)
    {
        // wind up for 0.8 s, then barrel forward
        if (_stateTime < 0.8f)
        {
            Model.SetBase("idle");
            if (_stateTime % 0.1f < dt)
            {
                _game.Fx.Dust(World, 1, 0.5f);
            }

            return;
        }

        Model.SetBase("run", 1.6f);
        Vector2 next = Position + (_chargeDir * 9.5f * dt);
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
            player.Damage(30f * _game.DifficultyDamage, Position - (_chargeDir * 2f), 14f);
            _game.Audio.PlayAt("sfx_hit_blunt", player.World, 1f, 0.6f);
        }

        if (_stateTime > 2.0f || blocked)
        {
            State = ZombieState.Chasing;
            if (blocked)
            {
                _stun = 1.2f;
                _game.Camera.AddShake(0.5f);
                _game.Fx.Hit(World + new Vector3(0, 1.8f, 0), Vector3.Zero, true);
                _game.Audio.PlayAt("sfx_boing", World, 1f, 0.7f);
            }
        }
    }
}
