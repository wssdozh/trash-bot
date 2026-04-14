using UnityEngine;

public sealed class DeveloperCheatSave
{
    private const string InfiniteHealthKey = "settings.dev.health";
    private const string InfiniteDamageKey = "settings.dev.damage";

    public bool LoadInfiniteHealth()
    {
        return PlayerPrefs.GetInt(InfiniteHealthKey, 0) == 1;
    }

    public bool LoadInfiniteDamage()
    {
        return PlayerPrefs.GetInt(InfiniteDamageKey, 0) == 1;
    }

    public void SaveInfiniteHealth(bool isEnabled)
    {
        PlayerPrefs.SetInt(InfiniteHealthKey, isEnabled ? 1 : 0);
    }

    public void SaveInfiniteDamage(bool isEnabled)
    {
        PlayerPrefs.SetInt(InfiniteDamageKey, isEnabled ? 1 : 0);
    }
}
