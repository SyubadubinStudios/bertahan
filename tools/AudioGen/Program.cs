using AudioGen;

// Renders every sound effect and music loop of Bertahan into src/Bertahan/Assets/Audio.
string root = args.Length > 0 ? args[0] : FindProjectRoot();
string outDir = Path.Combine(root, "src", "Bertahan", "Assets", "Audio");

var jobs = new (string Name, Func<AudioGen.Buffer> Make)[]
{
    ("music_menu", Music.Menu),
    ("music_day", Music.Day),
    ("music_night", Music.Night),
    ("music_boss", Music.Boss),
    ("jingle_victory", Music.Victory),
    ("jingle_defeat", Music.Defeat),
    ("sfx_swing", Sfx.Swing),
    ("sfx_hit_blunt", Sfx.HitBlunt),
    ("sfx_hit_blade", Sfx.HitBlade),
    ("sfx_hit_pan", Sfx.HitPan),
    ("sfx_gunshot", Sfx.Gunshot),
    ("sfx_reload", Sfx.Reload),
    ("sfx_empty", Sfx.Empty),
    ("sfx_glass", Sfx.GlassBreak),
    ("sfx_fire_whoosh", Sfx.FireWhoosh),
    ("sfx_fire_loop", Sfx.FireLoop),
    ("sfx_explosion", Sfx.Explosion),
    ("sfx_groan_0", () => Sfx.Groan(0)),
    ("sfx_groan_1", () => Sfx.Groan(1)),
    ("sfx_groan_2", () => Sfx.Groan(2)),
    ("sfx_zombie_die", Sfx.ZombieDie),
    ("sfx_tuyul", Sfx.TuyulGiggle),
    ("sfx_kunti", Sfx.KuntiLaugh),
    ("sfx_pocong_hop", Sfx.PocongHop),
    ("sfx_roar", Sfx.Roar),
    ("sfx_cast", Sfx.DukunCast),
    ("sfx_fireball", Sfx.Fireball),
    ("sfx_hurt_low", () => Sfx.PlayerHurt(false)),
    ("sfx_hurt_high", () => Sfx.PlayerHurt(true)),
    ("sfx_player_die", Sfx.PlayerDie),
    ("sfx_pickup", Sfx.Pickup),
    ("sfx_heal", Sfx.Heal),
    ("sfx_wave_start", Sfx.WaveStart),
    ("sfx_wave_clear", Sfx.WaveClear),
    ("sfx_ui_click", Sfx.UiClick),
    ("sfx_ui_hover", Sfx.UiHover),
    ("sfx_step", Sfx.Step),
    ("sfx_dodge", Sfx.Dodge),
    ("sfx_boing", Sfx.Boing),
};

Parallel.ForEach(jobs, job =>
{
    AudioGen.Buffer buffer = job.Make();
    string path = Path.Combine(outDir, job.Name + ".wav");
    buffer.Save(path);
    Console.WriteLine($"{job.Name,-18} {buffer.Seconds,6:0.00} s");
});

static string FindProjectRoot()
{
    string? dir = AppContext.BaseDirectory;
    while (dir is not null && !Directory.Exists(Path.Combine(dir, "src", "Bertahan")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? Directory.GetCurrentDirectory();
}
