namespace Bertahan.Game;

public enum WeaponKind
{
    Swing,
    Thrust,
    Gun,
}

public sealed record WeaponDef(
    string Id,
    string Name,
    WeaponKind Kind,
    float Damage,
    float Range,
    float ArcDegrees,
    float Cooldown,
    float Knockback,
    string HitSound,
    float Stun = 0f,
    int Pierce = 1,
    int Magazine = 0,
    float ReloadTime = 0f,
    string Description = "")
{
    public static readonly WeaponDef[] All =
    [
        new("pentungan", "Pentungan", WeaponKind.Swing, 22, 1.7f, 110, 0.42f, 5f, "sfx_hit_blunt", Description: "Kayu keras andalan ronda malam."),
        new("sapu", "Sapu Lidi", WeaponKind.Swing, 14, 2.1f, 150, 0.34f, 8f, "sfx_hit_blunt", Description: "Menyapu zombi sekaligus banyak."),
        new("wajan", "Wajan", WeaponKind.Swing, 28, 1.6f, 100, 0.55f, 6.5f, "sfx_hit_pan", Stun: 0.9f, Description: "BONG! Membuat zombi pusing."),
        new("bambu", "Bambu Runcing", WeaponKind.Thrust, 32, 2.8f, 40, 0.58f, 4f, "sfx_hit_blade", Pierce: 3, Description: "Tusukan jauh menembus barisan."),
        new("linggis", "Linggis", WeaponKind.Swing, 36, 1.9f, 90, 0.6f, 6f, "sfx_hit_blunt", Stun: 0.3f, Description: "Berat dan tangguh."),
        new("kampak", "Kampak", WeaponKind.Swing, 48, 1.8f, 95, 0.75f, 5f, "sfx_hit_blade", Description: "Tebasan keras pembelah zombi."),
        new("pacul", "Pacul", WeaponKind.Swing, 60, 2.1f, 80, 0.95f, 9f, "sfx_hit_blade", Stun: 0.4f, Description: "Senjata petani paling mematikan."),
        new("senapan", "Senapan", WeaponKind.Gun, 65, 30f, 0, 0.55f, 7f, "sfx_hit_blade", Pierce: 2, Magazine: 6, ReloadTime: 1.6f, Description: "Senapan tua. Peluru terbatas!"),
    ];

    public static WeaponDef Get(string id) => All.First(w => w.Id == id);
}

public sealed record CharacterDef(
    string Id,
    string Name,
    string Role,
    float Health,
    float Speed,
    float DamageMultiplier,
    float AttackSpeed,
    string StartWeapon,
    float Regen,
    string Voice,
    string Perk)
{
    public static readonly CharacterDef[] All =
    [
        new("bapak", "Bapak", "Kepala keluarga", 130, 5.0f, 1.2f, 1.0f, "bambu", 0f, "sfx_hurt_low", "Darah paling tebal, tusukan bambu runcing jarak jauh."),
        new("ibu", "Ibu", "Jagoan dapur", 115, 5.2f, 1.1f, 1.05f, "wajan", 0f, "sfx_hurt_high", "Wajan membuat zombi pusing tujuh keliling."),
        new("kakak", "Kakak", "Anak SD pemberani", 100, 5.7f, 1.0f, 1.15f, "pentungan", 0f, "sfx_hurt_high", "Lincah, serangan cepat."),
        new("ade", "Ade", "Anggota Pramuka", 85, 6.3f, 0.9f, 1.3f, "pentungan", 0f, "sfx_hurt_high", "Paling gesit! Menghindar lebih jauh."),
        new("kake", "Kake", "Mantan jawara kampung", 100, 4.6f, 1.4f, 0.9f, "pentungan", 0f, "sfx_hurt_low", "Pukulan jurus silat paling sakit."),
        new("nene", "Nene", "Ratu sapu lidi", 95, 4.7f, 1.0f, 1.0f, "sapu", 2.0f, "sfx_hurt_high", "Jamu rahasia: darah pulih perlahan."),
    ];

    public static CharacterDef Get(string id) => All.First(c => c.Id == id);
}

public sealed record ZombieDef(
    string Id,
    string Name,
    float Health,
    float WalkSpeed,
    float RunSpeed,
    float Damage,
    float AttackRange,
    float AttackCooldown,
    float Radius,
    float Mass,
    int Score,
    string Sound,
    float Scale = 1f)
{
    public static readonly ZombieDef[] All =
    [
        new("warga", "Zombi Warga", 55, 1.3f, 2.4f, 10, 1.1f, 1.4f, 0.4f, 1f, 10, "sfx_groan_0"),
        new("tuyul", "Tuyul Zombi", 32, 3.2f, 4.6f, 6, 0.8f, 0.9f, 0.3f, 0.5f, 15, "sfx_tuyul"),
        new("pocong", "Pocong Zombi", 75, 1.9f, 2.9f, 12, 1.0f, 1.3f, 0.38f, 1.2f, 20, "sfx_groan_1"),
        new("satpam", "Satpam Zombi", 160, 1.4f, 2.3f, 18, 1.3f, 1.6f, 0.5f, 2.5f, 40, "sfx_groan_2"),
        new("kuntilanak", "Kuntilanak Zombi", 110, 2.0f, 3.2f, 12, 1.2f, 1.2f, 0.4f, 0.8f, 50, "sfx_kunti"),
        new("genderuwo", "Genderuwo Zombi", 480, 1.7f, 3.4f, 28, 2.0f, 2.2f, 0.85f, 8f, 150, "sfx_roar"),
        new("dukun", "Dukun Zombi", 1500, 1.4f, 2.0f, 20, 1.6f, 1.8f, 0.55f, 10f, 600, "sfx_cast"),
    ];

    public static ZombieDef Get(string id) => All.First(z => z.Id == id);
}

/// <summary>One group of a wave: count zombies of a type, spawned every interval seconds.</summary>
public sealed record SpawnGroup(string Zombie, int Count, float Interval = 1.2f, float Delay = 0f);

public sealed record WaveDef(SpawnGroup[] Groups, bool Boss = false)
{
    public int Total => Groups.Sum(g => g.Count);
}

public enum TimeOfDay
{
    Siang,
    Sore,
    Malam,
}

public sealed record LevelDef(
    int Number,
    string Id,
    string Name,
    string Zone,
    string Description,
    TimeOfDay Time,
    string Music,
    WaveDef[] Waves)
{
    public static readonly LevelDef[] All =
    [
        new(1, "gerbang", "Gerbang Kampung", "Zona Aman", "Zombi mulai berdatangan dari jalan desa. Pertahankan gerbang Kampung Damai!",
            TimeOfDay.Siang, "music_day",
            [
                new([new("warga", 5, 1.6f)]),
                new([new("warga", 7, 1.2f), new("tuyul", 2, 2.0f, 4f)]),
                new([new("warga", 8, 1.0f), new("tuyul", 4, 1.5f, 3f)]),
                new([new("warga", 8, 0.9f), new("tuyul", 3, 1.5f), new("satpam", 1, 1f, 8f)], Boss: true),
            ]),
        new(2, "sawah", "Sawah Berhantu", "Zona Bahaya", "Malam di sawah. Pocong melompat di pematang, kuntilanak melayang di bawah bulan.",
            TimeOfDay.Malam, "music_night",
            [
                new([new("warga", 6, 1.2f), new("pocong", 3, 1.8f, 3f)]),
                new([new("pocong", 5, 1.3f), new("tuyul", 4, 1.2f, 2f)]),
                new([new("warga", 8, 0.9f), new("kuntilanak", 2, 3f, 4f), new("pocong", 3, 1.5f, 6f)]),
                new([new("warga", 6, 1.0f), new("pocong", 4, 1.2f), new("kuntilanak", 2, 3f, 5f), new("genderuwo", 1, 1f, 10f)], Boss: true),
            ]),
        new(3, "pasar", "Pasar Lama", "Zona Menengah", "Pasar sudah sepi, tinggal zombi yang berbelanja. Satpam pasar pun ikut jadi zombi!",
            TimeOfDay.Sore, "music_day",
            [
                new([new("warga", 8, 0.9f), new("satpam", 2, 3f, 4f)]),
                new([new("tuyul", 6, 0.8f), new("pocong", 4, 1.2f, 2f), new("satpam", 2, 3f, 6f)]),
                new([new("warga", 10, 0.7f), new("kuntilanak", 3, 2.5f, 3f), new("satpam", 3, 2.5f, 6f)]),
                new([new("warga", 8, 0.8f), new("satpam", 3, 2f), new("genderuwo", 2, 8f, 6f), new("tuyul", 5, 1f, 10f)], Boss: true),
            ]),
        new(4, "kuburan", "Kuburan Terbengkalai", "Zona Berisiko", "Sumber wabah ada di sini: Dukun Zombi dan ritual gelapnya. Hentikan dia!",
            TimeOfDay.Malam, "music_night",
            [
                new([new("pocong", 6, 1.0f), new("warga", 6, 1.0f, 2f)]),
                new([new("kuntilanak", 4, 2f), new("tuyul", 6, 0.8f, 2f), new("satpam", 2, 3f, 5f)]),
                new([new("warga", 10, 0.6f), new("pocong", 6, 1f, 2f), new("genderuwo", 2, 8f, 8f)]),
                new([new("dukun", 1, 1f), new("warga", 8, 1.5f, 4f), new("pocong", 4, 2f, 8f), new("kuntilanak", 2, 4f, 12f), new("genderuwo", 1, 1f, 20f)], Boss: true),
            ]),
    ];
}
