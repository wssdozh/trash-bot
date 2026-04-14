using UnityEngine;

internal sealed class SettingsSave
{
    private const string MasterVolumeKey = "settings.master";
    private const string MusicVolumeKey = "settings.music";
    private const string EffectsVolumeKey = "settings.effects";
    private const string FullScreenKey = "settings.fullscreen";
    private const string QualityKey = "settings.quality";

    public SettingsData Load(int defaultQualityLevel, bool defaultFullScreen)
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1.0f);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1.0f);
        float effectsVolume = PlayerPrefs.GetFloat(EffectsVolumeKey, 1.0f);
        bool isFullScreen = PlayerPrefs.GetInt(FullScreenKey, defaultFullScreen ? 1 : 0) == 1;
        int qualityLevel = PlayerPrefs.GetInt(QualityKey, defaultQualityLevel);

        return new SettingsData(masterVolume, musicVolume, effectsVolume, isFullScreen, qualityLevel);
    }

    public void Save(SettingsData settingsData)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, settingsData.MasterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, settingsData.MusicVolume);
        PlayerPrefs.SetFloat(EffectsVolumeKey, settingsData.EffectsVolume);
        PlayerPrefs.SetInt(FullScreenKey, settingsData.IsFullScreen ? 1 : 0);
        PlayerPrefs.SetInt(QualityKey, settingsData.QualityLevel);
        PlayerPrefs.Save();
    }
}
