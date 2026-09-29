using System.Text.Json;
using System.Text.Json.Serialization;
using Bertahan.Game;

namespace Bertahan.Core;

/// <summary>One line of the Top Score table of a level.</summary>
public sealed class ScoreEntry
{
    public string Name { get; set; } = "";

    public int Score { get; set; }

    public string Character { get; set; } = "bapak";

    public Difficulty Difficulty { get; set; }

    public bool Cleared { get; set; }

    public DateTime Date { get; set; }
}

/// <summary>Player preferences and progress, stored as JSON in %APPDATA%/Bertahan.</summary>
public sealed class GameSettings
{
    public const int TopScoreSize = 10;

    public float MusicVolume { get; set; } = 0.6f;

    public float SfxVolume { get; set; } = 0.9f;

    /// <summary>0 = rendah, 1 = sedang, 2 = tinggi.</summary>
    public int Quality { get; set; } = 1;

    public bool Fullscreen { get; set; }

    public bool ScreenShake { get; set; } = true;

    public bool ShowFps { get; set; }

    /// <summary>Multiplier for the Q/E camera turn speed.</summary>
    public float CameraSpeed { get; set; } = 1f;

    /// <summary>"auto", "dx12" or "vulkan".</summary>
    public string Backend { get; set; } = "auto";

    public string? ProbedBackend { get; set; }

    public int ProbeVersion { get; set; }

    public int UnlockedLevel { get; set; } = 1;

    public string LastCharacter { get; set; } = "bapak";

    public string LastName { get; set; } = "Si Otong";

    public Difficulty LastDifficulty { get; set; } = Difficulty.Pemberani;

    public bool SeenOpening { get; set; }

    /// <summary>Best runs per level id, highest first.</summary>
    public Dictionary<string, List<ScoreEntry>> TopScores { get; set; } = [];

    /// <summary>The run in progress, so "Lanjutkan" can pick it up after a restart.</summary>
    public Campaign? SavedRun { get; set; }

    /// <summary>%APPDATA%/Bertahan/settings.json, or the BERTAHAN_SETTINGS environment variable (used by the screenshot runs).</summary>
    [JsonIgnore]
    public static string FilePath => Environment.GetEnvironmentVariable("BERTAHAN_SETTINGS") is { Length: > 0 } custom
        ? custom
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bertahan", "settings.json");

    public static GameSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(FilePath), SettingsJson.Default.GameSettings) ?? new GameSettings();
            }
        }
        catch (Exception)
        {
            // A corrupt file just means default settings.
        }

        return new GameSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, SettingsJson.Default.GameSettings));
        }
        catch (Exception)
        {
            // Settings are a convenience; failing to save must not stop the game.
        }
    }

    /// <summary>Adds a result to the level table. Returns its rank (1 based) or 0 when it did not make the list.</summary>
    public int RecordScore(string level, ScoreEntry entry)
    {
        if (entry.Score <= 0)
        {
            return 0;
        }

        if (!TopScores.TryGetValue(level, out List<ScoreEntry>? list))
        {
            list = [];
            TopScores[level] = list;
        }

        list.Add(entry);
        list.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (list.Count > TopScoreSize)
        {
            list.RemoveRange(TopScoreSize, list.Count - TopScoreSize);
        }

        return list.IndexOf(entry) + 1;
    }

    public IReadOnlyList<ScoreEntry> Scores(string level) => TopScores.TryGetValue(level, out List<ScoreEntry>? list) ? list : [];
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(GameSettings))]
internal sealed partial class SettingsJson : JsonSerializerContext;
