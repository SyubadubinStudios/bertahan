using System.Numerics;

namespace Bertahan.Game;

/// <summary>Per frame input, already mapped from keyboard, mouse and gamepad.</summary>
public struct InputState
{
    /// <summary>X = right, Y = forward, relative to the camera.</summary>
    public Vector2 Move;
    public Vector2 MouseScreen;
    public bool MouseValid;
    public Vector2 AimStick;
    public bool Attack;
    public bool Sprint;
    public bool Dodge;
    public bool Throw;
    public bool Reload;
    public int WeaponSlot;
    public int WeaponCycle;
    public float CameraTurn;
    public float Zoom;
}

public sealed class InventoryItem(WeaponDef weapon)
{
    public WeaponDef Weapon { get; } = weapon;

    public int Loaded { get; set; } = weapon.Magazine;
}

public sealed class Player
{
    private readonly GameSession _game;
    private float _attackCooldown;
    private float _pendingHit = -1f;
    private float _pendingThrow = -1f;
    private float _dodgeTime;
    private Vector2 _dodgeDir;
    private float _invulnerable;
    private float _reloadTime;
    private float _hurtCooldown;
    private float _stepTimer;
    private Vector2 _knockback;
    private float _deathTimer;

    public Player(GameSession game, CharacterDef def, AnimatedModel model)
    {
        _game = game;
        Def = def;
        Model = model;
        MaxHealth = def.Health;
        Health = def.Health;
        Inventory.Add(new InventoryItem(WeaponDef.Get(def.StartWeapon)));
        Equip(0);
    }

    public CharacterDef Def { get; }

    public AnimatedModel Model { get; }

    public Vector2 Position { get; set; }

    public float Yaw { get; set; }

    public Vector2 Velocity { get; private set; }

    public float Radius => 0.38f;

    public float Health { get; private set; }

    public float MaxHealth { get; }

    public float Stamina { get; private set; } = 100f;

    public List<InventoryItem> Inventory { get; } = [];

    public int Current { get; private set; }

    public InventoryItem Weapon => Inventory[Current];

    public int Molotovs { get; set; } = 2;

    public int AmmoReserve { get; set; } = 12;

    public bool Alive => Health > 0f;

    public bool DeathFinished => !Alive && _deathTimer > 2.6f;

    public float SlowTimer { get; set; }

    public bool Reloading => _reloadTime > 0f;

    public float ReloadProgress => Weapon.Weapon.ReloadTime > 0 ? 1f - (_reloadTime / Weapon.Weapon.ReloadTime) : 1f;

    /// <summary>0..1 red flash for the HUD vignette.</summary>
    public float HurtFlash { get; private set; }

    public Vector3 World => new(Position.X, 0f, Position.Y);

    public Vector2 Facing => new(MathF.Sin(Yaw), MathF.Cos(Yaw));

    public Vector3 AimPoint { get; private set; }

    public void Equip(int index)
    {
        if (index < 0 || index >= Inventory.Count)
        {
            return;
        }

        Current = index;
        _reloadTime = 0f;
        Model.ShowWeapon(Weapon.Weapon.Id);
        Model.HoldAction(Weapon.Weapon.Kind == WeaponKind.Gun ? "aim" : null);
        _pendingHit = -1f;
    }

    public bool Give(WeaponDef weapon)
    {
        int existing = Inventory.FindIndex(i => i.Weapon.Id == weapon.Id);
        if (existing >= 0)
        {
            if (weapon.Kind == WeaponKind.Gun)
            {
                AmmoReserve += 12;
                return true;
            }

            return false;
        }

        Inventory.Add(new InventoryItem(weapon));
        Equip(Inventory.Count - 1);
        return true;
    }

    public bool Invulnerable => _invulnerable > 0f;

    /// <summary>Back on their feet after losing a life, briefly untouchable.</summary>
    public void Revive(Vector2 at)
    {
        Position = at;
        Health = MaxHealth;
        Stamina = 100f;
        _deathTimer = 0f;
        _invulnerable = 3f;
        _knockback = Vector2.Zero;
        _dodgeTime = 0f;
        _pendingHit = -1f;
        _pendingThrow = -1f;
        SlowTimer = 0f;
        Model.CancelAction();
        Model.SetBase("idle");
        Equip(Current);
    }

    public void Heal(float amount) => Health = MathF.Min(MaxHealth, Health + amount);

    public void Damage(float amount, Vector2 from, float knockback = 4f)
    {
        if (!Alive || _invulnerable > 0f)
        {
            return;
        }

        Health = MathF.Max(0f, Health - amount);
        HurtFlash = 1f;
        Vector2 away = Position - from;
        if (away.LengthSquared() > 1e-4f)
        {
            _knockback += Vector2.Normalize(away) * knockback;
        }

        _game.Camera.AddShake(0.35f + (amount * 0.02f));
        _game.Audio.Play(Def.Voice, 0.9f, 0.95f + (Random.Shared.NextSingle() * 0.1f), 0.25);
        if (_hurtCooldown <= 0f && !Model.ActionPlaying)
        {
            Model.PlayAction("hit", 1.4f);
        }

        _hurtCooldown = 0.4f;
        _invulnerable = 0.25f;
        if (Health <= 0f)
        {
            Model.CancelAction();
            Model.HoldAction(null);
            Model.SetBase("die");
            _game.Audio.Play("sfx_player_die");
        }
    }

    public void Update(float dt, in InputState input)
    {
        _attackCooldown -= dt;
        _invulnerable -= dt;
        _hurtCooldown -= dt;
        SlowTimer -= dt;
        HurtFlash = MathF.Max(0f, HurtFlash - (dt * 2f));
        if (Def.Regen > 0f && Alive)
        {
            Heal(Def.Regen * dt);
        }

        if (!Alive)
        {
            _deathTimer += dt;
            Velocity = Vector2.Zero;
            UpdateModel(dt);
            return;
        }

        HandleWeaponSelect(input);
        Aim(input);
        Move(dt, input);
        Actions(dt, input);
        UpdateModel(dt);
    }

    private void HandleWeaponSelect(in InputState input)
    {
        if (input.WeaponSlot >= 0 && input.WeaponSlot < Inventory.Count && input.WeaponSlot != Current)
        {
            Equip(input.WeaponSlot);
            _game.Audio.Play("sfx_ui_click", 0.6f);
        }
        else if (input.WeaponCycle != 0 && Inventory.Count > 1)
        {
            Equip(((Current + input.WeaponCycle) % Inventory.Count + Inventory.Count) % Inventory.Count);
            _game.Audio.Play("sfx_ui_click", 0.6f);
        }
    }

    private void Aim(in InputState input)
    {
        Vector2 dir = Vector2.Zero;
        if (input.AimStick.LengthSquared() > 0.1f)
        {
            dir = (_game.Camera.GroundRight * input.AimStick.X) + (_game.Camera.GroundForward * input.AimStick.Y);
            AimPoint = World + (new Vector3(dir.X, 0, dir.Y) * 6f);
        }
        else if (input.MouseValid && _game.Camera.GroundPoint(input.MouseScreen, 0.9f) is { } hit)
        {
            AimPoint = hit;
            dir = new Vector2(hit.X, hit.Z) - Position;
        }
        else if (input.Move.LengthSquared() > 0.01f)
        {
            dir = (_game.Camera.GroundRight * input.Move.X) + (_game.Camera.GroundForward * input.Move.Y);
        }

        if (dir.LengthSquared() > 0.04f && _dodgeTime <= 0f)
        {
            float target = MathF.Atan2(dir.X, dir.Y);
            Yaw = AngleLerp(Yaw, target, 18f * (1f / 60f));
        }
    }

    public static float AngleLerp(float a, float b, float t)
    {
        float d = ((b - a + MathF.PI) % MathF.Tau + MathF.Tau) % MathF.Tau - MathF.PI;
        return a + (d * Math.Clamp(t, 0f, 1f));
    }

    private void Move(float dt, in InputState input)
    {
        Vector2 wish = (_game.Camera.GroundRight * input.Move.X) + (_game.Camera.GroundForward * input.Move.Y);
        if (wish.LengthSquared() > 1f)
        {
            wish = Vector2.Normalize(wish);
        }

        bool sprinting = input.Sprint && wish.LengthSquared() > 0.1f && Stamina > 5f && _dodgeTime <= 0f;
        float speed = Def.Speed * (sprinting ? 1.45f : 1f);
        if (_game.Level.InSlowZone(Position))
        {
            speed *= 0.72f;
        }

        if (SlowTimer > 0f)
        {
            speed *= 0.5f;
        }

        if (Model.ActionPlaying && Weapon.Weapon.Kind != WeaponKind.Gun)
        {
            speed *= 0.75f;
        }

        Stamina = sprinting ? MathF.Max(0f, Stamina - (dt * 22f)) : MathF.Min(100f, Stamina + (dt * 16f));

        Vector2 velocity;
        if (_dodgeTime > 0f)
        {
            _dodgeTime -= dt;
            float boost = Def.Id == "ade" ? 13f : 11f;
            velocity = _dodgeDir * boost * MathF.Max(0.3f, _dodgeTime / 0.42f);
        }
        else
        {
            velocity = Vector2.Lerp(Velocity, wish * speed, 1f - MathF.Exp(-14f * dt));
        }

        _knockback *= MathF.Exp(-8f * dt);
        Vector2 next = Position + ((velocity + _knockback) * dt);
        Position = _game.Level.Nav.Resolve(next, Radius);
        Velocity = velocity;

        if (velocity.LengthSquared() > 1f)
        {
            _stepTimer -= dt * velocity.Length();
            if (_stepTimer <= 0f)
            {
                _stepTimer = 1.7f;
                _game.Audio.Play("sfx_step", 0.35f, 0.9f + (Random.Shared.NextSingle() * 0.2f));
                if (sprinting)
                {
                    _game.Fx.Dust(World, 1, 0.3f);
                }
            }
        }
    }

    private void Actions(float dt, in InputState input)
    {
        WeaponDef w = Weapon.Weapon;
        float attackRate = Def.AttackSpeed;

        if (input.Dodge && _dodgeTime <= 0f && Stamina >= 20f)
        {
            Vector2 wish = (_game.Camera.GroundRight * input.Move.X) + (_game.Camera.GroundForward * input.Move.Y);
            _dodgeDir = wish.LengthSquared() > 0.05f ? Vector2.Normalize(wish) : Facing;
            Yaw = MathF.Atan2(_dodgeDir.X, _dodgeDir.Y);
            _dodgeTime = 0.42f;
            _invulnerable = 0.45f;
            Stamina -= 20f;
            Model.CancelAction();
            _pendingHit = -1f;
            Model.SetBase("dodge", 1.9f);
            _game.Audio.Play("sfx_dodge", 0.7f);
            _game.Fx.Dust(World, 4, 0.4f);
        }

        // reload
        if (w.Kind == WeaponKind.Gun)
        {
            if (_reloadTime > 0f)
            {
                _reloadTime -= dt;
                if (_reloadTime <= 0f)
                {
                    int need = w.Magazine - Weapon.Loaded;
                    int take = Math.Min(need, AmmoReserve);
                    Weapon.Loaded += take;
                    AmmoReserve -= take;
                }
            }
            else if ((input.Reload || Weapon.Loaded == 0) && Weapon.Loaded < w.Magazine && AmmoReserve > 0)
            {
                _reloadTime = w.ReloadTime;
                _game.Audio.Play("sfx_reload", 0.8f);
            }
        }

        if (input.Attack && _attackCooldown <= 0f && _dodgeTime <= 0f && _pendingThrow < 0f)
        {
            if (w.Kind == WeaponKind.Gun)
            {
                if (_reloadTime <= 0f && Weapon.Loaded > 0)
                {
                    Weapon.Loaded--;
                    _attackCooldown = w.Cooldown;
                    Model.PlayAction("shoot", 1.2f);
                    _game.Combat.Shoot(this, w);
                }
                else if (_reloadTime <= 0f)
                {
                    _attackCooldown = 0.3f;
                    _game.Audio.Play("sfx_empty", 0.8f);
                }
            }
            else
            {
                string clip = w.Kind == WeaponKind.Thrust ? "thrust" : "swing";
                float speed = attackRate * (Model.Duration(clip) / MathF.Max(w.Cooldown, 0.2f)) * 0.85f;
                Model.PlayAction(clip, speed);
                _attackCooldown = w.Cooldown / attackRate;
                // the blow lands a little before the middle of the animation
                _pendingHit = Model.Duration(clip) / speed * (w.Kind == WeaponKind.Thrust ? 0.45f : 0.42f);
                _game.Audio.Play("sfx_swing", 0.6f, 0.9f + (Random.Shared.NextSingle() * 0.25f));
            }
        }

        if (_pendingHit >= 0f)
        {
            _pendingHit -= dt;
            if (_pendingHit < 0f)
            {
                _game.Combat.Melee(this, w);
            }
        }

        if (input.Throw && Molotovs > 0 && _pendingThrow < 0f && _dodgeTime <= 0f)
        {
            Molotovs--;
            Model.ShowWeapon("molotov");
            Model.PlayAction("throw", 1.3f);
            _pendingThrow = Model.Duration("throw") / 1.3f * 0.5f;
            _attackCooldown = 0.5f;
        }

        if (_pendingThrow >= 0f)
        {
            _pendingThrow -= dt;
            if (_pendingThrow < 0f)
            {
                _game.Combat.ThrowMolotov(this, AimPoint);
                Model.ShowWeapon(Weapon.Weapon.Id);
                _pendingThrow = -1f;
            }
        }
    }

    private void UpdateModel(float dt)
    {
        if (Alive && _dodgeTime <= 0f)
        {
            float speed = Velocity.Length();
            if (speed > Def.Speed * 1.15f)
            {
                Model.SetBase("run", speed / 6.5f);
            }
            else if (speed > 0.4f)
            {
                Model.SetBase("walk", speed / 2.9f);
            }
            else
            {
                Model.SetBase("idle");
            }
        }

        Model.Root.Position = World;
        Model.Root.EulerAngles = new Vector3(0, Yaw, 0);
        Model.Update(dt);
    }
}
