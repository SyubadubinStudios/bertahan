using System.Numerics;

namespace Bertahan.Game;

/// <summary>
/// A simple bot that plays a level: walks to the closest zombie, attacks,
/// throws molotovs into crowds and collects pickups between waves. Used by the
/// headless screenshot runs (<c>--autoplay</c>) to capture real gameplay.
/// </summary>
public sealed class Autopilot
{
    private float _throwTimer = 4f;
    private float _dodgeTimer;
    private float _think;
    private Vector2 _wander;

    public InputState Next(GameSession s, float dt)
    {
        InputState input = new() { WeaponSlot = -1 };
        Player p = s.Player;
        if (!p.Alive)
        {
            return input;
        }

        _throwTimer -= dt;
        _dodgeTimer -= dt;
        _think -= dt;

        Zombie? target = null;
        float best = float.MaxValue;
        int crowd = 0;
        foreach (Zombie z in s.Zombies)
        {
            if (!z.Targetable)
            {
                continue;
            }

            float d = Vector2.Distance(z.Position, p.Position);
            if (d < 6f)
            {
                crowd++;
            }

            if (d < best)
            {
                best = d;
                target = z;
            }
        }

        // prefer the strongest weapon in the bag
        int strongest = 0;
        for (int i = 1; i < p.Inventory.Count; i++)
        {
            if (p.Inventory[i].Weapon.Damage > p.Inventory[strongest].Weapon.Damage && p.Inventory[i].Weapon.Kind != WeaponKind.Gun)
            {
                strongest = i;
            }
        }

        if (strongest != p.Current && p.Weapon.Weapon.Kind != WeaponKind.Gun)
        {
            input.WeaponSlot = strongest;
        }

        Vector2 goal;
        if (target is not null)
        {
            Vector2 to = target.Position - p.Position;
            float range = p.Weapon.Weapon.Range * 0.75f;
            goal = best > range ? to : Vector2.Zero;
            Aim(ref input, s, to);
            input.Attack = best < p.Weapon.Weapon.Range + 0.6f;
            if (crowd >= 3 && _throwTimer <= 0f && p.Molotovs > 0 && best > 3f)
            {
                _throwTimer = 6f;
                input.Throw = true;
                input.MouseValid = false;
            }

            if (p.Health / p.MaxHealth < 0.35f && best < 2f && _dodgeTimer <= 0f)
            {
                _dodgeTimer = 2f;
                input.Dodge = true;
                goal = -to;
            }
        }
        else
        {
            // collect whatever lies around, otherwise stroll
            (PickupKind Kind, Vector2 Position)? item = s.Pickups.All.OrderBy(i => Vector2.Distance(i.Position, p.Position)).Cast<(PickupKind, Vector2)?>().FirstOrDefault();
            if (item is { } it)
            {
                goal = it.Item2 - p.Position;
            }
            else
            {
                if (_think <= 0f)
                {
                    _think = 3f;
                    _wander = new Vector2(Random.Shared.NextSingle() - 0.5f, Random.Shared.NextSingle() - 0.5f) * 20f;
                }

                goal = _wander - p.Position;
            }
        }

        if (goal.LengthSquared() > 0.25f)
        {
            Vector2 dir = Vector2.Normalize(goal);
            input.Move = new Vector2(Vector2.Dot(dir, s.Camera.GroundRight), Vector2.Dot(dir, s.Camera.GroundForward));
        }

        return input;
    }

    private static void Aim(ref InputState input, GameSession s, Vector2 to)
    {
        if (to.LengthSquared() < 1e-4f)
        {
            return;
        }

        Vector2 dir = Vector2.Normalize(to);
        input.AimStick = new Vector2(Vector2.Dot(dir, s.Camera.GroundRight), Vector2.Dot(dir, s.Camera.GroundForward));
    }
}
