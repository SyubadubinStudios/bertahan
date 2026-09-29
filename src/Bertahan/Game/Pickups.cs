using System.Numerics;
using ThreeNet;

namespace Bertahan.Game;

public enum PickupKind
{
    Nasi,
    Jamu,
    Ammo,
    Molotov,
    Weapon,
}

/// <summary>Items lying on the ground: food heals, crates refill, weapons join the inventory.</summary>
public sealed class Pickups
{
    private sealed class Item
    {
        public PickupKind Kind;
        public string? Weapon;
        public Vector2 Pos;
        public Node Node = null!;
        public float Time;
        public float Life;
    }

    private readonly GameSession _game;
    private readonly PropLibrary _props;
    private readonly Node _parent;
    private readonly List<Item> _items = [];

    public Pickups(GameSession game, PropLibrary props, Node parent)
    {
        _game = game;
        _props = props;
        _parent = parent;
    }

    public IEnumerable<(PickupKind Kind, Vector2 Position)> All => _items.Select(i => (i.Kind, i.Pos));

    public void Spawn(PickupKind kind, Vector2 at, string? weapon = null, float life = 0f)
    {
        string model = kind switch
        {
            PickupKind.Nasi => "prop_pickup_nasi",
            PickupKind.Jamu => "prop_pickup_jamu",
            PickupKind.Ammo => "prop_pickup_ammo",
            PickupKind.Molotov => "prop_pickup_molotov",
            _ => "weapon_" + weapon,
        };
        float scale = kind == PickupKind.Weapon ? 1.1f : 1.6f;
        Node? node = _props.Place(model, _parent, new Vector3(at.X, 0.35f, at.Y), 0f, scale);
        if (node is null)
        {
            return;
        }

        node.SetShadowsRecursive(true, false);
        _items.Add(new Item { Kind = kind, Weapon = weapon, Pos = at, Node = node, Time = Random.Shared.NextSingle() * 6f, Life = life });
        _game.Fx.Emit(Sprite.Ring, new Vector3(at.X, 0.1f, at.Y), Vector3.Zero, 0.5f, 0.2f, 1.6f, flat: true);
    }

    public void Update(float dt)
    {
        Player player = _game.Player;
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            Item item = _items[i];
            item.Time += dt;
            float bob = 0.35f + (MathF.Sin(item.Time * 3f) * 0.12f);
            item.Node.Position = new Vector3(item.Pos.X, bob, item.Pos.Y);
            // weapons lie tilted and spin slowly, food spins faster
            item.Node.EulerAngles = item.Kind == PickupKind.Weapon
                ? new Vector3(0.9f, item.Time * 1.2f, 0)
                : new Vector3(0, item.Time * 2f, 0);
            if (((int)(item.Time * 10)) % 7 == 0 && Random.Shared.Next(4) == 0)
            {
                _game.Fx.Emit(Sprite.Spark, new Vector3(item.Pos.X + Random.Shared.NextSingle() - 0.5f, 0.3f, item.Pos.Y + Random.Shared.NextSingle() - 0.5f),
                    new Vector3(0, 1.2f, 0), 0.6f, 0.15f, 0.02f);
            }

            if (item.Life > 0f && item.Time > item.Life)
            {
                item.Node.Remove();
                _items.RemoveAt(i);
                continue;
            }

            if (!player.Alive || Vector2.Distance(player.Position, item.Pos) > 1.1f)
            {
                continue;
            }

            if (Collect(item, player))
            {
                item.Node.Remove();
                _items.RemoveAt(i);
            }
        }
    }

    private bool Collect(Item item, Player player)
    {
        Vector3 at = new(item.Pos.X, 0.5f, item.Pos.Y);
        switch (item.Kind)
        {
            case PickupKind.Nasi:
            case PickupKind.Jamu:
                if (player.Health >= player.MaxHealth - 0.5f)
                {
                    return false;
                }

                float amount = item.Kind == PickupKind.Nasi ? 35f : 70f;
                player.Heal(amount);
                _game.Fx.Hearts(player.World);
                _game.Audio.Play("sfx_heal", 0.9f);
                _game.ShowMessage(item.Kind == PickupKind.Nasi ? $"Nasi bungkus! +{amount:0} darah" : $"Jamu kuat! +{amount:0} darah", 1.5f);
                return true;
            case PickupKind.Ammo:
                player.AmmoReserve += 12;
                _game.Audio.Play("sfx_pickup", 0.9f);
                _game.ShowMessage("Peluru +12", 1.5f);
                return true;
            case PickupKind.Molotov:
                player.Molotovs += 2;
                _game.Audio.Play("sfx_pickup", 0.9f);
                _game.ShowMessage("Bom molotov +2  (tekan G untuk melempar)", 2f);
                return true;
            default:
                WeaponDef weapon = WeaponDef.Get(item.Weapon!);
                if (!player.Give(weapon))
                {
                    return false;
                }

                _game.Fx.Confetti(at);
                _game.Audio.Play("sfx_pickup", 1f);
                _game.ShowMessage($"Dapat {weapon.Name}! {weapon.Description}", 2.5f);
                return true;
        }
    }

    public void Clear()
    {
        foreach (Item item in _items)
        {
            item.Node.Remove();
        }

        _items.Clear();
    }
}
