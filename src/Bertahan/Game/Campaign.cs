using System.Text.Json.Serialization;

namespace Bertahan.Game;

public enum LevelOutcome
{
    /// <summary>The level was lost but a retry is left: play it again.</summary>
    Retry,

    /// <summary>No retries left: the run starts over from level 1.</summary>
    GameOver,
}

/// <summary>
/// One run through the story: who plays, how hard, which level and the score
/// so far. A lost level can be retried <see cref="MaxRetries"/> times; after
/// that the run falls back to level 1.
/// </summary>
public sealed class Campaign
{
    public const int MaxRetries = 3;

    /// <summary>Levels that are finished; 5-10 are announced as "segera hadir".</summary>
    public static int PlayableLevels => LevelDef.All.Length;

    public const int AnnouncedLevels = 10;

    public string PlayerName { get; set; } = "Si Otong";

    public string Character { get; set; } = "bapak";

    public Difficulty Difficulty { get; set; } = Difficulty.Pemberani;

    public int Level { get; set; } = 1;

    public int TotalScore { get; set; }

    public int RetriesLeft { get; set; } = MaxRetries;

    public int TotalKills { get; set; }

    [JsonIgnore]
    public DifficultyDef DifficultyDef => DifficultyDef.Get(Difficulty);

    [JsonIgnore]
    public LevelDef LevelDef => LevelDef.All[Level - 1];

    [JsonIgnore]
    public CharacterDef CharacterDef => CharacterDef.Get(Character);

    [JsonIgnore]
    public bool IsLastLevel => Level >= PlayableLevels;

    public void LevelWon(int score, int kills)
    {
        TotalScore += score;
        TotalKills += kills;
        RetriesLeft = MaxRetries;
        if (!IsLastLevel)
        {
            Level++;
        }
    }

    public LevelOutcome LevelLost()
    {
        if (RetriesLeft > 0)
        {
            RetriesLeft--;
            return LevelOutcome.Retry;
        }

        Level = 1;
        TotalScore = 0;
        TotalKills = 0;
        RetriesLeft = MaxRetries;
        return LevelOutcome.GameOver;
    }
}
