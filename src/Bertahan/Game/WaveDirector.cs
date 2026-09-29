using System.Numerics;

namespace Bertahan.Game;

public enum WavePhase
{
    Intro,
    Countdown,
    Fighting,
    Cleared,
    Victory,
}

/// <summary>Runs the waves of a level: countdown, spawning, clearing and the breaks between.</summary>
public sealed class WaveDirector
{
    private sealed class Queue
    {
        public required string Zombie;
        public int Remaining;
        public float Interval;
        public float Timer;
    }

    private readonly GameSession _game;
    private readonly List<Queue> _queues = [];
    private readonly Random _rng = new(7);
    private float _timer;

    public WaveDirector(GameSession game)
    {
        _game = game;
        Phase = WavePhase.Intro;
        _timer = 3.5f;
    }

    public WavePhase Phase { get; private set; }

    /// <summary>Zero based index of the current wave.</summary>
    public int Wave { get; private set; }

    public int WaveCount => _game.Level.Def.Waves.Length;

    public float Timer => _timer;

    public int Pending => _queues.Sum(q => q.Remaining);

    public int Alive => _game.Zombies.Count(z => z.Active && z.State != ZombieState.Dying);

    public int Remaining => Pending + Alive;

    public bool IsBossWave => _game.Level.Def.Waves[Math.Min(Wave, WaveCount - 1)].Boss;

    public void Update(float dt)
    {
        _timer -= dt;
        switch (Phase)
        {
            case WavePhase.Intro when _timer <= 0f:
                StartCountdown();
                break;
            case WavePhase.Countdown when _timer <= 0f:
                StartWave();
                break;
            case WavePhase.Fighting:
                Spawning(dt);
                if (Pending == 0 && Alive == 0)
                {
                    WaveCleared();
                }

                break;
            case WavePhase.Cleared when _timer <= 0f:
                if (Wave + 1 >= WaveCount)
                {
                    Phase = WavePhase.Victory;
                    _game.OnVictory();
                }
                else
                {
                    Wave++;
                    StartCountdown();
                }

                break;
        }
    }

    private void StartCountdown()
    {
        Phase = WavePhase.Countdown;
        _timer = 4f;
        _game.Audio.Play("sfx_wave_start", 0.9f);
        bool boss = _game.Level.Def.Waves[Wave].Boss;
        _game.ShowBanner(boss ? $"GELOMBANG TERAKHIR!" : $"GELOMBANG {Wave + 1}", boss ? "Bos zombi datang... bersiaplah!" : "Kentongan berbunyi, zombi datang!");
    }

    private void StartWave()
    {
        Phase = WavePhase.Fighting;
        _queues.Clear();
        WaveDef wave = _game.Level.Def.Waves[Wave];
        foreach (SpawnGroup g in wave.Groups)
        {
            _queues.Add(new Queue { Zombie = g.Zombie, Remaining = g.Count, Interval = g.Interval, Timer = g.Delay });
        }

        if (wave.Boss)
        {
            _game.Audio.PlayMusic("music_boss");
        }
    }

    private void Spawning(float dt)
    {
        foreach (Queue q in _queues)
        {
            if (q.Remaining <= 0)
            {
                continue;
            }

            q.Timer -= dt;
            if (q.Timer > 0f)
            {
                continue;
            }

            bool boss = q.Zombie is "dukun" or "genderuwo";
            Vector2 at = boss ? _game.Level.BossSpot : PickSpawnPoint();
            if (_game.SpawnZombie(q.Zombie, at))
            {
                q.Remaining--;
                q.Timer = q.Interval;
                if (boss)
                {
                    _game.ShowBanner(q.Zombie == "dukun" ? "DUKUN ZOMBI!" : "GENDERUWO ZOMBI!", q.Zombie == "dukun" ? "Hentikan ritualnya!" : "Hati-hati serudukannya!");
                }
            }
            else
            {
                // pool exhausted: try again shortly
                q.Timer = 0.5f;
            }
        }
    }

    /// <summary>A spawn point that is not right on top of the player.</summary>
    private Vector2 PickSpawnPoint()
    {
        List<Vector2> points = _game.Level.SpawnPoints;
        Vector2 player = _game.Player.Position;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 p = points[_rng.Next(points.Count)] + new Vector2((float)_rng.NextDouble() * 3 - 1.5f, (float)_rng.NextDouble() * 3 - 1.5f);
            if (Vector2.Distance(p, player) > 9f && !_game.Level.Nav.IsBlocked(p))
            {
                return p;
            }
        }

        return points.OrderByDescending(p => Vector2.Distance(p, player)).First();
    }

    /// <summary>The dukun calls helpers around himself.</summary>
    public void Summon(Vector2 around, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count * MathF.Tau) + (float)_rng.NextDouble();
            Vector2 p = _game.Level.Nav.Resolve(around + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * 3f), 0.4f);
            _game.SpawnZombie(_rng.Next(3) == 0 ? "tuyul" : "warga", p);
        }
    }

    private void WaveCleared()
    {
        Phase = WavePhase.Cleared;
        _timer = Wave + 1 >= WaveCount ? 3f : 7f;
        _game.Audio.Play("sfx_wave_clear", 0.9f);
        _game.OnWaveCleared(Wave);
        if (IsBossWave)
        {
            _game.Audio.PlayMusic(_game.Level.Def.Music);
        }
    }
}
