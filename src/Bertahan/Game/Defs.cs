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
    float Scale = 1f,
    float Height = 1.7f,
    bool Boss = false,
    bool Stationary = false,
    string Title = "")
{
    public static readonly ZombieDef[] All =
    [
        new("warga", "Zombi Warga", 55, 1.3f, 2.4f, 10, 1.1f, 1.4f, 0.4f, 1f, 10, "sfx_groan_0"),
        new("tuyul", "Tuyul Zombi", 32, 3.2f, 4.6f, 6, 0.8f, 0.9f, 0.3f, 0.5f, 15, "sfx_tuyul", Height: 1.0f),
        new("pocong", "Pocong Zombi", 75, 1.9f, 2.9f, 12, 1.0f, 1.3f, 0.38f, 1.2f, 20, "sfx_groan_1"),
        new("satpam", "Satpam Zombi", 160, 1.4f, 2.3f, 18, 1.3f, 1.6f, 0.5f, 2.5f, 40, "sfx_groan_2", Height: 1.9f),
        new("kuntilanak", "Kuntilanak Zombi", 110, 2.0f, 3.2f, 12, 1.2f, 1.2f, 0.4f, 0.8f, 50, "sfx_kunti"),
        new("genderuwo", "Genderuwo Zombi", 480, 1.7f, 3.4f, 28, 2.0f, 2.2f, 0.85f, 8f, 150, "sfx_roar", Height: 2.4f, Boss: true,
            Title: "Hati-hati serudukannya!"),
        new("dukun", "Dukun Zombi", 1500, 1.4f, 2.0f, 20, 1.6f, 1.8f, 0.55f, 10f, 600, "sfx_cast", Boss: true, Title: "Hentikan ritualnya!"),

        // musuh tambahan (art/enemies-additional.png)
        new("genderuwo_raksasa", "Genderuwo Raksasa", 900, 1.6f, 3.2f, 34, 2.4f, 2.2f, 1.0f, 12f, 250, "sfx_roar", Height: 3.1f),
        new("siluman_harimau", "Siluman Harimau", 220, 2.2f, 4.4f, 18, 1.4f, 1.1f, 0.5f, 2f, 80, "sfx_roar", Height: 1.95f),
        new("tuyul_serdadu", "Tuyul Serdadu", 60, 3.0f, 4.2f, 8, 1.0f, 1.0f, 0.32f, 0.6f, 30, "sfx_tuyul", Height: 1.0f),
        new("kuntilanak_geni", "Kuntilanak Geni", 260, 2.0f, 3.4f, 16, 1.3f, 1.2f, 0.4f, 0.8f, 120, "sfx_kunti", Height: 1.8f),
        new("pocong_penjaga", "Pocong Penjaga", 320, 1.8f, 2.7f, 20, 1.2f, 1.4f, 0.45f, 3f, 100, "sfx_groan_1", Height: 2.0f),
        new("dukun_santet", "Dukun Santet", 900, 1.3f, 1.9f, 22, 1.6f, 1.8f, 0.55f, 8f, 400, "sfx_cast", Height: 1.8f),

        // bos level 5-10 (art/boss-level-5..10.png)
        new("jeng_roro", "Jeng Roro Kembang Malam", 2000, 0f, 0f, 30, 4.6f, 2.4f, 1.6f, 99f, 1500, "sfx_kunti", Height: 3.4f, Boss: true, Stationary: true,
            Title: "Penguasa sumur tua bangkit dari kedalaman!"),
        new("kunti_penguasa", "Kuntilanak Penguasa Kutukan", 2300, 1.6f, 2.6f, 26, 1.8f, 1.8f, 0.9f, 20f, 1800, "sfx_kunti", Height: 3.6f, Boss: true,
            Title: "Lima kepala, lima jeritan kutukan!"),
        new("genderuwo_raja", "Genderuwo Raja", 2800, 1.6f, 3.2f, 40, 2.8f, 2.4f, 1.3f, 40f, 2200, "sfx_roar", Height: 3.7f, Boss: true,
            Title: "Raja rimba kuburan kuno murka!"),
        new("kraken_raja", "Kraken Raja", 3000, 0f, 0f, 34, 5.0f, 2.6f, 3.2f, 99f, 2600, "sfx_roar", Height: 3.4f, Boss: true, Stationary: true,
            Title: "Tentakel raksasa muncul dari rawa!"),
        new("leviathan", "Leviathan Kuno", 3300, 0f, 0f, 36, 5.5f, 2.6f, 3.0f, 99f, 3000, "sfx_roar", Height: 4.5f, Boss: true, Stationary: true,
            Title: "Naga tiga kepala dari pusaran kutukan!"),
        new("demon_king", "Demon King Abyss", 3600, 1.5f, 3.0f, 44, 3.0f, 2.2f, 1.4f, 50f, 4000, "sfx_roar", Height: 4.2f, Boss: true,
            Title: "Penguasa neraka turun ke candi!"),
    ];

    public static ZombieDef Get(string id) => All.First(z => z.Id == id);

    /// <summary>Only moves while hopping.</summary>
    public bool Hopper => Id is "pocong" or "pocong_penjaga";

    /// <summary>Zig-zags towards the player.</summary>
    public bool Zigzag => Id is "tuyul" or "tuyul_serdadu";

    /// <summary>Keeps its distance and fights with magic.</summary>
    public bool Caster => Id is "dukun" or "dukun_santet";

    /// <summary>Big melee hits land as a ground-shaking shockwave.</summary>
    public bool Brute => Id is "genderuwo" or "genderuwo_raksasa" or "genderuwo_raja" or "demon_king" or "jeng_roro" or "kraken_raja" or "leviathan";

    /// <summary>Not slowed by paddy water or rivers.</summary>
    public bool IgnoresWater => Id is "kuntilanak" or "kuntilanak_geni" or "kunti_penguasa" or "pocong" or "pocong_penjaga" || Stationary;

    /// <summary>Who this zombie calls for help (bosses and casters).</summary>
    public string[] Summons => Id switch
    {
        "dukun" => ["warga", "warga", "tuyul"],
        "dukun_santet" => ["pocong_penjaga", "warga", "tuyul_serdadu"],
        "jeng_roro" => ["pocong", "warga", "pocong_penjaga"],
        "kunti_penguasa" => ["kuntilanak", "tuyul_serdadu"],
        "genderuwo_raja" => ["warga", "genderuwo_raksasa", "warga"],
        "kraken_raja" => ["siluman_harimau", "warga"],
        "leviathan" => ["kuntilanak_geni", "tuyul_serdadu", "warga"],
        "demon_king" => ["dukun_santet", "siluman_harimau", "kuntilanak_geni", "warga"],
        _ => [],
    };
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

    /// <summary>The cursed night of the last levels: a blood-red sky and a green vortex.</summary>
    Kutukan,
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

        // Latar lanjutan: Kampung Damai - Zona Terlarang (art/level 5-10.png)
        new(5, "jembatan", "Jembatan Bambu", "Zona Bahaya", "Kabut menyelimuti sungai. Seberangi jembatan bambu, sumur tua di seberang menyimpan kutukan!",
            TimeOfDay.Malam, "music_night",
            [
                new([new("warga", 8, 1.0f), new("pocong", 4, 1.5f, 3f)]),
                new([new("tuyul_serdadu", 6, 1.0f), new("warga", 6, 1.2f, 3f)]),
                new([new("pocong_penjaga", 3, 3f), new("kuntilanak", 3, 3f, 4f), new("tuyul_serdadu", 5, 1.2f, 6f)]),
                new([new("jeng_roro", 1, 1f), new("pocong", 5, 1.8f, 4f), new("warga", 8, 1.2f, 8f), new("pocong_penjaga", 2, 5f, 14f)], Boss: true),
            ]),
        new(6, "sekolah", "Sekolah Terbengkalai", "Zona Bahaya", "Kelas-kelas kosong, papan tulis penuh coretan... dan jeritan dari lorong sekolah.",
            TimeOfDay.Malam, "music_night",
            [
                new([new("tuyul", 8, 0.8f), new("tuyul_serdadu", 4, 1.5f, 3f)]),
                new([new("kuntilanak", 4, 2f), new("warga", 8, 1.0f, 2f)]),
                new([new("siluman_harimau", 3, 4f), new("tuyul_serdadu", 6, 1.0f, 2f), new("kuntilanak", 3, 3f, 6f)]),
                new([new("kunti_penguasa", 1, 1f), new("kuntilanak", 4, 3f, 5f), new("tuyul_serdadu", 6, 1.2f, 8f), new("siluman_harimau", 2, 6f, 14f)], Boss: true),
            ]),
        new(7, "kuburan_kuno", "Kuburan Kuno", "Zona Sangat Berisiko", "Makam-makam tua tertelan hutan. Arca batu berjaga, dan sesuatu yang besar sedang bangun.",
            TimeOfDay.Malam, "music_night",
            [
                new([new("pocong_penjaga", 4, 2f), new("pocong", 6, 1.2f, 2f)]),
                new([new("genderuwo", 1, 1f), new("warga", 10, 0.8f, 2f), new("siluman_harimau", 2, 4f, 6f)]),
                new([new("dukun_santet", 1, 1f), new("kuntilanak_geni", 2, 4f, 4f), new("pocong_penjaga", 4, 2f, 6f)]),
                new([new("genderuwo_raja", 1, 1f), new("genderuwo_raksasa", 1, 1f, 12f), new("warga", 10, 1.2f, 4f), new("siluman_harimau", 3, 4f, 8f)],
                    Boss: true),
            ]),
        new(8, "hutan", "Hutan Larangan", "Zona Sangat Berisiko", "Pohon-pohon raksasa menutup bulan. Jangan dekati rawa hitam di tengah hutan!",
            TimeOfDay.Malam, "music_night",
            [
                new([new("siluman_harimau", 4, 2f), new("tuyul", 8, 0.8f, 2f)]),
                new([new("genderuwo_raksasa", 1, 1f), new("warga", 10, 0.8f, 2f), new("kuntilanak_geni", 2, 4f, 6f)]),
                new([new("siluman_harimau", 5, 1.8f), new("pocong_penjaga", 4, 2.5f, 3f), new("kuntilanak_geni", 3, 3f, 6f)]),
                new([new("kraken_raja", 1, 1f), new("siluman_harimau", 4, 3f, 5f), new("pocong_penjaga", 3, 4f, 8f), new("genderuwo_raksasa", 1, 1f, 18f)],
                    Boss: true),
            ]),
        new(9, "masjid_rusak", "Masjid Rusak", "Zona Terlarang", "Kampung terbakar, masjid runtuh, dan pusaran kutukan berputar di langit. Bertahanlah!",
            TimeOfDay.Kutukan, "music_night",
            [
                new([new("tuyul_serdadu", 8, 0.9f), new("kuntilanak_geni", 2, 4f, 4f)]),
                new([new("genderuwo_raksasa", 2, 6f), new("warga", 12, 0.7f, 2f), new("siluman_harimau", 3, 3f, 5f)]),
                new([new("dukun_santet", 1, 1f), new("kuntilanak_geni", 4, 3f, 3f), new("pocong_penjaga", 4, 2.5f, 5f)]),
                new([new("leviathan", 1, 1f), new("kuntilanak_geni", 3, 4f, 6f), new("tuyul_serdadu", 8, 1.2f, 8f), new("genderuwo_raksasa", 1, 1f, 16f)],
                    Boss: true),
            ]),
        new(10, "candi", "Candi Terlarang", "Zona Terlarang", "Pusat ritual segala kutukan. Kalahkan penguasanya dan selamatkan Kampung Damai untuk selamanya!",
            TimeOfDay.Kutukan, "music_boss",
            [
                new([new("dukun_santet", 2, 6f), new("pocong_penjaga", 6, 1.5f, 2f)]),
                new([new("genderuwo_raksasa", 2, 8f), new("siluman_harimau", 5, 2f, 2f), new("kuntilanak_geni", 3, 3f, 6f)]),
                new([new("genderuwo_raja", 1, 1f), new("tuyul_serdadu", 10, 0.8f, 2f), new("dukun_santet", 1, 1f, 10f)]),
                new([new("demon_king", 1, 1f), new("dukun_santet", 1, 1f, 10f), new("genderuwo_raksasa", 2, 10f, 6f), new("kuntilanak_geni", 3, 5f, 12f)],
                    Boss: true),
            ]),
    ];
}
