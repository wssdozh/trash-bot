internal struct SettingsData
{
    public float MasterVolume;
    public float MusicVolume;
    public float EffectsVolume;
    public bool IsFullScreen;
    public int QualityLevel;

    public SettingsData(float masterVolume, float musicVolume, float effectsVolume, bool isFullScreen, int qualityLevel)
    {
        MasterVolume = masterVolume;
        MusicVolume = musicVolume;
        EffectsVolume = effectsVolume;
        IsFullScreen = isFullScreen;
        QualityLevel = qualityLevel;
    }
}
