using System.Numerics;
using ThreeNet;

namespace Bertahan.Game;

/// <summary>Weapon hits, projectiles and burning ground.</summary>
public sealed class Combat
{
    private sealed class Projectile
    {
        public Vector3 Pos;
        public Vector3 Vel;
        public float Life;
        public bool Molotov;
        public float Damage;
        public Node? Node;
        public Sprite Sprite = Sprite.MagicGreen;
        public bool Burn;
    }

    private sealed class PendingSlam
    {
        public Vector3 Pos;
        public float Time;
        public float Radius;
        public float Damage;
        public float Tick;
    }

    private sealed class FireZone
    {
        public Vector3 Pos;
        public float Radius;
        public float Time;
        public Node? Light;
        public SoundInstance? Sound;
        public float Tick;

        /// <summary>Enemy fire: burns the player, not the zombies.</summary>
        public bool Hostile;
    }

    private readonly GameSession _game;
    private readonly List<Projectile> _projectiles = [];
    private readonly List<FireZone> _fires = [];
    private readonly List<PendingSlam> _slams = [];
    private readonly Node _molotovPrototype;
    private readonly Node _muzzleLight;
    private float _muzzleTime;
    private readonly Stack<Node> _fireLights = new();

    public Combat(GameSession game, PropLibrary props, Node parent)
    {
        _game = game;
        _molotovPrototype = game.Scene.CreateNode(parent, "molotov-proto");
        props.Place("weapon_molotov", _molotovPrototype, Vector3.Zero);
        _molotovPrototype.Visible = false;
        _muzzleLight = game.Scene.AddLight(Light.Point(new Vector3(1f, 0.8f, 0.45f), 0f, 10f), parent, "muzzle");
        for (int i = 0; i < 4; i++)
        {
            Node light = game.Scene.AddLight(Light.Point(new Vector3(1f, 0.55f, 0.2f), 0f, 9f), parent, "fire");
            _fireLights.Push(light);
        }
    }

    private float Power(Player p) => p.Def.DamageMultiplier * _game.PowerBonus;

    /// <summary>Hits every zombie inside the weapon arc in front of the player.</summary>
    public void Melee(Player player, WeaponDef w)
    {
        Vector2 facing = player.Facing;
        float cosHalf = MathF.Cos(w.ArcDegrees * 0.5f * MathF.PI / 180f);
        int hits = 0;
        List<(Zombie Z, float D)> targets = [];
        foreach (Zombie z in _game.Zombies)
        {
            if (!z.Targetable && z.State != ZombieState.Spawning)
            {
                continue;
            }

            Vector2 to = z.Position - player.Position;
            float d = to.Length();
            if (d > w.Range + z.Def.Radius)
            {
                continue;
            }

            if (d > 0.3f && Vector2.Dot(to / d, facing) < cosHalf)
            {
                continue;
            }

            targets.Add((z, d));
        }

        foreach ((Zombie z, _) in targets.OrderBy(t => t.D).Take(w.Kind == WeaponKind.Thrust ? w.Pierce : 8))
        {
            float damage = w.Damage * Power(player) * (0.9f + (Random.Shared.NextSingle() * 0.2f));
            bool heavy = w.Damage >= 40 || w.Id == "wajan";
            z.Damage(damage, player.Position, w.Knockback, w.Stun, heavy);
            hits++;
        }

        if (hits > 0)
        {
            _game.Audio.PlayAt(w.HitSound, player.World, w.Id == "wajan" ? 0.9f : 1f, 0.9f + (Random.Shared.NextSingle() * 0.2f), 0.05);
            _game.Camera.AddShake(0.12f + (hits * 0.05f) + (w.Damage > 40 ? 0.15f : 0));
            _game.HitStop(w.Damage >= 40 ? 0.07f : 0.035f);
        }
    }

    /// <summary>Hitscan: the bullet passes through up to Pierce zombies.</summary>
    public void Shoot(Player player, WeaponDef w)
    {
        Vector3 aim = player.AimPoint;
        Vector2 dir2 = new Vector2(aim.X, aim.Z) - player.Position;
        dir2 = dir2.LengthSquared() > 0.01f ? Vector2.Normalize(dir2) : player.Facing;
        // snap the barrel to the aim for the next frames
        player.Yaw = MathF.Atan2(dir2.X, dir2.Y);
        Vector3 origin = player.World + new Vector3(0, 1.25f, 0) + (new Vector3(dir2.X, 0, dir2.Y) * 0.9f);
        List<(Zombie Z, float T)> hits = [];
        foreach (Zombie z in _game.Zombies)
        {
            if (!z.Targetable && z.State != ZombieState.Spawning)
            {
                continue;
            }

            Vector2 rel = z.Position - player.Position;
            float t = Vector2.Dot(rel, dir2);
            if (t < 0f || t > w.Range)
            {
                continue;
            }

            float perp = MathF.Abs((rel.X * dir2.Y) - (rel.Y * dir2.X));
            if (perp < z.Def.Radius + 0.25f)
            {
                hits.Add((z, t));
            }
        }

        float end = w.Range;
        foreach ((Zombie z, float t) in hits.OrderBy(h => h.T).Take(w.Pierce))
        {
            z.Damage(w.Damage * Power(player), player.Position, w.Knockback, 0.2f, true);
            end = t;
        }

        // stop the tracer at a wall
        for (float t = 1f; t < end; t += 0.5f)
        {
            if (_game.Level.Nav.IsBlocked(player.Position + (dir2 * t)))
            {
                end = t;
                break;
            }
        }

        Vector3 hitPoint = new Vector3(player.Position.X, 1.2f, player.Position.Y) + (new Vector3(dir2.X, 0, dir2.Y) * end);
        _game.Fx.Muzzle(origin, new Vector3(dir2.X, 0, dir2.Y));
        _game.Fx.Tracer(origin, hitPoint);
        if (hits.Count == 0)
        {
            _game.Fx.Dust(hitPoint with { Y = 0.2f }, 3, 0.3f);
        }

        _muzzleLight.Position = origin;
        _muzzleLight.Light = _muzzleLight.Light is { } l ? l with { Intensity = 25f } : null;
        _muzzleTime = 0.06f;
        _game.Audio.Play("sfx_gunshot", 0.8f, 0.95f + (Random.Shared.NextSingle() * 0.1f));
        _game.Camera.AddShake(0.25f);
    }

    public void ThrowMolotov(Player player, Vector3 target)
    {
        Vector3 from = player.World + new Vector3(0, 1.8f, 0);
        Vector2 flat = new Vector2(target.X, target.Z) - player.Position;
        float dist = Math.Clamp(flat.Length(), 3f, 14f);
        Vector2 dir = flat.LengthSquared() > 0.01f ? Vector2.Normalize(flat) : player.Facing;
        const float time = 0.75f;
        float gravity = 18f;
        Vector3 vel = new Vector3(dir.X, 0, dir.Y) * (dist / time);
        vel.Y = ((0f - from.Y) + (0.5f * gravity * time * time)) / time;
        Node node = _molotovPrototype.Clone(_molotovPrototype.Parent);
        node.Visible = true;
        _projectiles.Add(new Projectile { Pos = from, Vel = vel, Life = 3f, Molotov = true, Node = node });
        _game.Audio.Play("sfx_swing", 0.7f, 0.8f);
    }

    /// <summary>An enemy projectile: magic, water, bullets or fire (<paramref name="burn"/> leaves a burning patch).</summary>
    public void Fireball(Vector3 from, Vector2 dir, float damage, Sprite sprite = Sprite.MagicGreen, float speed = 9f, bool burn = false)
    {
        Vector3 vel = new Vector3(dir.X, 0, dir.Y) * speed;
        vel.Y = -0.4f * 9f * (from.Y > 1.5f ? 1f : 0.3f);
        _projectiles.Add(new Projectile { Pos = from, Vel = vel, Life = 3.5f, Damage = damage, Sprite = sprite, Burn = burn });
        _game.Audio.PlayAt("sfx_fireball", from, 0.6f, sprite == Sprite.Spark ? 1.6f : 1f, 0.15);
    }

    /// <summary>Marks a circle on the ground; after <paramref name="delay"/> seconds a shockwave hits it.</summary>
    public void Slam(Vector3 at, float delay, float radius, float damage)
    {
        _slams.Add(new PendingSlam { Pos = at with { Y = 0f }, Time = delay, Radius = radius, Damage = damage });
        _game.Fx.Emit(Sprite.Ring, at with { Y = 0.12f }, Vector3.Zero, delay, radius * 2.2f, radius * 2f, flat: true);
    }

    /// <summary>A patch of enemy fire (kuntilanak geni, demon king) that only hurts the player.</summary>
    public void HostileFire(Vector3 at, float radius, float time)
    {
        FireZone fire = new() { Pos = at with { Y = 0.05f }, Radius = radius, Time = time, Hostile = true };
        if (_fireLights.Count > 0 && _fires.Count(f => f.Light is not null) < 3)
        {
            fire.Light = _fireLights.Pop();
            fire.Light.Position = at + new Vector3(0, 1f, 0);
        }

        _fires.Add(fire);
    }

    /// <summary>Ground slam: damages the player (and knocks zombies back) inside the radius.</summary>
    public void Shockwave(Vector3 at, float radius, float damage, bool fromPlayer)
    {
        _game.Fx.Emit(Sprite.Ring, at + new Vector3(0, 0.15f, 0), Vector3.Zero, 0.4f, 0.4f, radius * 2.2f, flat: true);
        _game.Fx.Dust(at, 10, 0.8f);
        _game.Camera.AddShake(0.6f);
        _game.Audio.PlayAt("sfx_hit_blunt", at, 1f, 0.5f);
        Vector2 c = new(at.X, at.Z);
        if (!fromPlayer && Vector2.Distance(_game.Player.Position, c) < radius + _game.Player.Radius)
        {
            _game.Player.Damage(damage, c, 10f);
        }
    }

    private void Explode(Projectile p)
    {
        Vector3 at = p.Pos with { Y = 0.05f };
        if (p.Molotov)
        {
            _game.Fx.Explosion(at, 2.8f);
            _game.Audio.PlayAt("sfx_glass", at, 1f);
            _game.Audio.PlayAt("sfx_fire_whoosh", at, 1f);
            _game.Camera.AddShake(0.35f);
            FireZone fire = new() { Pos = at, Radius = 3.2f, Time = 6f, Sound = _game.Audio.Loop("sfx_fire_loop", at, 0.8f) };
            if (_fireLights.Count > 0)
            {
                fire.Light = _fireLights.Pop();
                fire.Light.Position = at + new Vector3(0, 1.2f, 0);
            }

            _fires.Add(fire);
            foreach (Zombie z in _game.Zombies)
            {
                if (z.Targetable && Vector2.Distance(z.Position, new Vector2(at.X, at.Z)) < 3.4f)
                {
                    z.Damage(35f * Power(_game.Player), new Vector2(at.X, at.Z), 6f, 0.4f, true);
                }
            }
        }
        else
        {
            if (p.Sprite == Sprite.Flame)
            {
                _game.Fx.Explosion(at, 1.4f);
            }
            else
            {
                _game.Fx.Magic(at + new Vector3(0, 0.5f, 0), p.Sprite != Sprite.MagicBlue, 12);
            }

            _game.Fx.Emit(Sprite.Ring, at + new Vector3(0, 0.3f, 0), Vector3.Zero, 0.3f, 0.3f, 3f);
            _game.Audio.PlayAt(p.Sprite == Sprite.Spark ? "sfx_hit_blunt" : "sfx_explosion", at, 0.5f, 1.4f, 0.1);
            // bullets and fireballs hit the player where they burst, not only at the landing spot
            Vector2 hitAt = new(p.Pos.X, p.Pos.Z);
            if (Vector2.Distance(_game.Player.Position, hitAt) < 1.6f)
            {
                _game.Player.Damage(p.Damage, hitAt, 6f);
            }

            if (p.Burn)
            {
                HostileFire(at, 1.6f, 3f);
            }
        }

        p.Node?.Remove();
        p.Node = null;
    }

    public void Update(float dt)
    {
        _muzzleTime -= dt;
        if (_muzzleTime <= 0f && _muzzleLight.Light is { Intensity: > 0f } ml)
        {
            _muzzleLight.Light = ml with { Intensity = 0f };
        }

        for (int i = _slams.Count - 1; i >= 0; i--)
        {
            PendingSlam s = _slams[i];
            s.Time -= dt;
            s.Tick -= dt;
            if (s.Tick <= 0f)
            {
                s.Tick = 0.15f;
                _game.Fx.Dust(s.Pos, 1, 0.4f);
            }

            if (s.Time <= 0f)
            {
                Shockwave(s.Pos, s.Radius, s.Damage, false);
                _slams.RemoveAt(i);
            }
        }

        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            Projectile p = _projectiles[i];
            p.Life -= dt;
            if (p.Molotov)
            {
                p.Vel.Y -= 18f * dt;
            }

            p.Pos += p.Vel * dt;
            bool hit = p.Pos.Y <= 0.05f || p.Life <= 0f || _game.Level.Nav.IsBlocked(new Vector2(p.Pos.X, p.Pos.Z));
            if (p.Molotov)
            {
                if (p.Node is not null)
                {
                    p.Node.Position = p.Pos;
                    p.Node.EulerAngles = new Vector3(p.Life * 12f, p.Life * 5f, 0);
                }

                _game.Fx.Emit(Sprite.Flame, p.Pos + new Vector3(0, 0.3f, 0), Vector3.UnitY * 0.5f, 0.2f, 0.25f, 0.05f);
            }
            else
            {
                _game.Fx.Emit(p.Sprite, p.Pos, -p.Vel * 0.05f, 0.35f, p.Sprite == Sprite.Spark ? 0.25f : 0.55f, 0.1f);
                if (Vector2.Distance(new Vector2(p.Pos.X, p.Pos.Z), _game.Player.Position) < 0.7f && p.Pos.Y < 2.2f)
                {
                    hit = true;
                }

                // gently fall to the ground over their flight
                if (p.Pos.Y < 0.9f)
                {
                    p.Vel.Y = 0f;
                }
            }

            if (hit)
            {
                Explode(p);
                _projectiles.RemoveAt(i);
            }
        }

        for (int i = _fires.Count - 1; i >= 0; i--)
        {
            FireZone f = _fires[i];
            f.Time -= dt;
            f.Tick -= dt;
            float strength = Math.Clamp(f.Time / 1.5f, 0f, 1f);
            int puffs = (int)(3 * strength) + 1;
            for (int k = 0; k < puffs; k++)
            {
                _game.Fx.Fire(f.Pos, f.Radius * 0.75f);
            }

            if (f.Light is not null)
            {
                float flicker = 0.75f + (0.25f * MathF.Sin(f.Time * 23f)) + (0.1f * MathF.Sin(f.Time * 57f));
                f.Light.Light = f.Light.Light is { } fl ? fl with { Intensity = 18f * strength * flicker } : null;
            }

            if (f.Tick <= 0f)
            {
                f.Tick = 0.33f;
                Vector2 c = new(f.Pos.X, f.Pos.Z);
                if (!f.Hostile)
                {
                    foreach (Zombie z in _game.Zombies)
                    {
                        if (z.Targetable && Vector2.Distance(z.Position, c) < f.Radius)
                        {
                            z.Damage(9f * Power(_game.Player), c, 0.5f, 0f, false);
                        }
                    }
                }

                if (Vector2.Distance(_game.Player.Position, c) < f.Radius * 0.8f)
                {
                    _game.Player.Damage(f.Hostile ? 6f * _game.DifficultyDamage : 3f, c, 1f);
                }
            }

            if (f.Time <= 0f)
            {
                f.Sound?.Stop();
                if (f.Light is not null)
                {
                    f.Light.Light = f.Light.Light is { } fl ? fl with { Intensity = 0f } : null;
                    _fireLights.Push(f.Light);
                }

                _fires.RemoveAt(i);
            }
        }
    }

    public void Clear()
    {
        foreach (FireZone f in _fires)
        {
            f.Sound?.Stop();
        }

        _fires.Clear();
        _slams.Clear();
        foreach (Projectile p in _projectiles)
        {
            p.Node?.Remove();
        }

        _projectiles.Clear();
    }
}
