namespace Bertahan.Game;

public enum Difficulty
{
    Bayi,
    Pemberani,
    MimpiBuruk,
}

/// <summary>How hard the zombies push. Applied on top of the per level scaling.</summary>
public sealed record DifficultyDef(
    Difficulty Id,
    string Name,
    string Tagline,
    string Description,
    float EnemyHealth,
    float EnemyDamage,
    float EnemySpeed,
    int Lives,
    float ScoreMultiplier,
    float DropChance,
    uint Color)
{
    public static readonly DifficultyDef[] All =
    [
        new(Difficulty.Bayi, "Bayi", "Santai saja, Dik", "Zombi lemah dan lambat. 5 nyawa per level. Cocok buat yang baru belajar.",
            0.7f, 0.55f, 0.88f, 5, 0.75f, 1.5f, 0x7BD65A),
        new(Difficulty.Pemberani, "Pemberani", "Seimbang dan seru", "Tantangan pas untuk pahlawan kampung. 3 nyawa per level.",
            1f, 1f, 1f, 3, 1f, 1f, 0xFFB43A),
        new(Difficulty.MimpiBuruk, "Mimpi Buruk", "Hanya untuk jawara sejati", "Zombi ganas, cepat dan tebal. 2 nyawa per level. Skor x1.5!",
            1.35f, 1.5f, 1.12f, 2, 1.5f, 0.7f, 0xE8443A),
    ];

    public static DifficultyDef Get(Difficulty id) => All[(int)id];
}
