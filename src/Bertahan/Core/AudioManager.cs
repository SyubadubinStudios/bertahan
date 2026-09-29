using System.Numerics;
using ThreeNet;

namespace Bertahan.Core;

/// <summary>
/// Loads every WAV in Assets/Audio and plays them through the ThreeNet spatial
/// mixer. Music cross-fades between tracks; sound effects are rate limited so a
/// crowd of zombies cannot flood the mixer.
/// </summary>
public sealed class AudioManager : IDisposable
{
    private readonly AudioEngine? _engine;
    private readonly Dictionary<string, AudioClip> _clips = [];
    private readonly Dictionary<string, double> _lastPlayed = [];
    private readonly GameSettings _settings;
    private SoundInstance? _music;
    private SoundInstance? _fadingOut;
    private string? _musicName;
    private float _musicFade = 1f;
    private float _fadeOutGain;
    private double _clock;

    public AudioManager(GameSettings settings)
    {
        _settings = settings;
        try
        {
            _engine = AudioEngine.Open();
            _engine.DopplerFactor = 0f;
            string dir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
            foreach (string file in Directory.EnumerateFiles(dir, "*.wav"))
            {
                _clips[Path.GetFileNameWithoutExtension(file)] = _engine.LoadClip(file);
            }
        }
        catch (Exception exception)
        {
            Unavailable = exception.Message;
            _engine = null;
        }
    }

    public string? Unavailable { get; }

    public bool Available => _engine is not null;

    public string? CurrentMusic => _musicName;

    public void PlayMusic(string name, bool restart = false)
    {
        if (_engine is null || (_musicName == name && !restart) || !_clips.TryGetValue(name, out AudioClip? clip))
        {
            return;
        }

        _fadingOut?.Stop();
        _fadingOut = _music;
        _fadeOutGain = _settings.MusicVolume * _musicFade;
        _music = _engine.Play(clip, SoundOptions.Flat(0f, loop: true));
        _musicName = name;
        _musicFade = 0f;
    }

    public void StopMusic()
    {
        _fadingOut?.Stop();
        _fadingOut = _music;
        _fadeOutGain = _settings.MusicVolume * _musicFade;
        _music = null;
        _musicName = null;
    }

    /// <summary>Plays a flat (non spatial) sound, e.g. UI and the player's own actions.</summary>
    public void Play(string name, float gain = 1f, float pitch = 1f, double minInterval = 0.03)
    {
        if (_engine is null || !_clips.TryGetValue(name, out AudioClip? clip) || !Allow(name, minInterval))
        {
            return;
        }

        _engine.Play(clip, SoundOptions.Flat(gain * _settings.SfxVolume) with { Pitch = pitch });
    }

    /// <summary>Plays a positional sound in the world.</summary>
    public void PlayAt(string name, Vector3 position, float gain = 1f, float pitch = 1f, double minInterval = 0.05)
    {
        if (_engine is null || !_clips.TryGetValue(name, out AudioClip? clip) || !Allow(name, minInterval))
        {
            return;
        }

        _engine.Play(clip, SoundOptions.At(position, gain * _settings.SfxVolume) with
        {
            Pitch = pitch,
            MinDistance = 3f,
            MaxDistance = 45f,
            Rolloff = 1f,
        });
    }

    /// <summary>A looping positional sound (fire) the caller stops later.</summary>
    public SoundInstance? Loop(string name, Vector3 position, float gain = 1f)
    {
        if (_engine is null || !_clips.TryGetValue(name, out AudioClip? clip))
        {
            return null;
        }

        return _engine.Play(clip, SoundOptions.At(position, gain * _settings.SfxVolume, loop: true) with { MinDistance = 3f, MaxDistance = 30f });
    }

    private bool Allow(string name, double minInterval)
    {
        if (_lastPlayed.TryGetValue(name, out double last) && _clock - last < minInterval)
        {
            return false;
        }

        _lastPlayed[name] = _clock;
        return true;
    }

    public void Update(float dt, Vector3 listener, Vector3 forward)
    {
        _clock += dt;
        if (_engine is null)
        {
            return;
        }

        _engine.SetListener(listener, forward, Vector3.UnitY);
        _musicFade = MathF.Min(1f, _musicFade + (dt / 1.5f));
        _music?.Update(o => o with { Gain = _settings.MusicVolume * _musicFade });
        if (_fadingOut is not null)
        {
            _fadeOutGain -= dt * _settings.MusicVolume / 1.2f;
            if (_fadeOutGain <= 0f)
            {
                _fadingOut.Stop();
                _fadingOut = null;
            }
            else
            {
                _fadingOut.Update(o => o with { Gain = _fadeOutGain });
            }
        }
    }

    public void Dispose() => _engine?.Dispose();
}
