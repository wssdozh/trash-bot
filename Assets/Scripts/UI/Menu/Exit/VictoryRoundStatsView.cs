using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VictoryRoundStatsView : MonoBehaviour
{
    [SerializeField] private TMP_Text _statsText;

    private void Awake()
    {
        if (_statsText == null)
        {
            throw new InvalidOperationException(nameof(_statsText));
        }
    }

    public void Render(PlayerRoundStatsSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new InvalidOperationException(nameof(snapshot));
        }

        _statsText.text =
            "ВРЕМЯ: " + FormatDuration(snapshot.DurationSeconds) + "\n" +
            "ВРАГОВ УБИТО: " + snapshot.DefeatedEnemies.ToString() + "\n" +
            "БОССОВ УБИТО: " + snapshot.DefeatedBosses.ToString() + "\n" +
            "МОНЕТ СОБРАНО: " + snapshot.CollectedCoins.ToString();
    }

    private string FormatDuration(float durationSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(durationSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}
