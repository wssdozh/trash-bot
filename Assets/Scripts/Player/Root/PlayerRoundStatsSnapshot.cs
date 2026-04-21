public sealed class PlayerRoundStatsSnapshot
{
    public PlayerRoundStatsSnapshot(
        float durationSeconds,
        int defeatedEnemies,
        int defeatedBosses,
        int collectedCoins)
    {
        DurationSeconds = durationSeconds;
        DefeatedEnemies = defeatedEnemies;
        DefeatedBosses = defeatedBosses;
        CollectedCoins = collectedCoins;
    }

    public float DurationSeconds { get; }

    public int DefeatedEnemies { get; }

    public int DefeatedBosses { get; }

    public int CollectedCoins { get; }
}
