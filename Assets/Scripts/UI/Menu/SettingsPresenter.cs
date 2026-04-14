using System;
using UnityEngine;
using UnityEngine.Audio;

internal sealed class SettingsPresenter
{
    private const string MasterParameterName = "Master";
    private const string MusicParameterName = "Music";
    private const string EffectsParameterName = "Effects";

    private const string HighQualityName = "HighQuality";
    private const string MediumQualityName = "MediumQuality";
    private const string LowQualityName = "LowQuality";

    private const float MinDb = -80.0f;
    private const float MuteThreshold = 0.0001f;
    private const float DbMultiplier = 20.0f;

    private readonly SettingsPanelView _view;
    private readonly AudioMixer _audioMixer;
    private readonly SettingsSave _settingsSave;

    private SettingsData _settingsData;

    private int _lowQualityLevel;
    private int _mediumQualityLevel;
    private int _highQualityLevel;

    private bool _defaultFullScreen;

    public SettingsPresenter(SettingsPanelView view, AudioMixer audioMixer, SettingsSave settingsSave)
    {
        _view = view;
        _audioMixer = audioMixer;
        _settingsSave = settingsSave;
    }

    public void Initialize()
    {
        CacheQualityLevels();

        _defaultFullScreen = Screen.fullScreen;

        _view.MasterChanged += OnMasterChanged;
        _view.MusicChanged += OnMusicChanged;
        _view.EffectsChanged += OnEffectsChanged;
        _view.FullScreenChanged += OnFullScreenChanged;
        _view.QualityChanged += OnQualityChanged;
        _view.InfiniteHealthChanged += OnInfiniteHealthChanged;
        _view.InfiniteDamageChanged += OnInfiniteDamageChanged;
        _view.ResetClicked += OnResetClicked;

        _view.SetQualityLevels(_lowQualityLevel, _mediumQualityLevel, _highQualityLevel);

        _settingsData = _settingsSave.Load(_highQualityLevel, _defaultFullScreen);
        _settingsData = NormalizeData(_settingsData);

        ApplyData();
        _view.SetData(_settingsData);
    }

    public void Dispose()
    {
        _view.MasterChanged -= OnMasterChanged;
        _view.MusicChanged -= OnMusicChanged;
        _view.EffectsChanged -= OnEffectsChanged;
        _view.FullScreenChanged -= OnFullScreenChanged;
        _view.QualityChanged -= OnQualityChanged;
        _view.InfiniteHealthChanged -= OnInfiniteHealthChanged;
        _view.InfiniteDamageChanged -= OnInfiniteDamageChanged;
        _view.ResetClicked -= OnResetClicked;
    }

    private void OnMasterChanged(float value)
    {
        _settingsData.MasterVolume = Mathf.Clamp01(value);

        ApplyVolume(MasterParameterName, _settingsData.MasterVolume);
        SaveAndRefresh();
    }

    private void OnMusicChanged(float value)
    {
        _settingsData.MusicVolume = Mathf.Clamp01(value);

        ApplyVolume(MusicParameterName, _settingsData.MusicVolume);
        SaveAndRefresh();
    }

    private void OnEffectsChanged(float value)
    {
        _settingsData.EffectsVolume = Mathf.Clamp01(value);

        ApplyVolume(EffectsParameterName, _settingsData.EffectsVolume);
        SaveAndRefresh();
    }

    private void OnFullScreenChanged(bool isFullScreen)
    {
        _settingsData.IsFullScreen = isFullScreen;

        ApplyFullScreen(isFullScreen);
        SaveAndRefresh();
    }

    private void OnQualityChanged(int qualityLevel)
    {
        _settingsData.QualityLevel = NormalizeQualityLevel(qualityLevel);

        ApplyQuality(_settingsData.QualityLevel);
        SaveAndRefresh();
    }

    private void OnInfiniteHealthChanged(bool isEnabled)
    {
        _settingsData.IsInfiniteHealth = isEnabled;

        SaveAndRefresh();
    }

    private void OnInfiniteDamageChanged(bool isEnabled)
    {
        _settingsData.IsInfiniteDamage = isEnabled;

        SaveAndRefresh();
    }

    private void OnResetClicked()
    {
        _settingsData = CreateDefaultData();

        ApplyData();
        SaveAndRefresh();
    }

    private void SaveAndRefresh()
    {
        _settingsSave.Save(_settingsData);
        _view.SetData(_settingsData);
    }

    private void ApplyData()
    {
        ApplyVolume(MasterParameterName, _settingsData.MasterVolume);
        ApplyVolume(MusicParameterName, _settingsData.MusicVolume);
        ApplyVolume(EffectsParameterName, _settingsData.EffectsVolume);
        ApplyFullScreen(_settingsData.IsFullScreen);
        ApplyQuality(_settingsData.QualityLevel);
    }

    private void ApplyVolume(string parameterName, float linearValue)
    {
        float dbValue = LinearToDb(linearValue);
        bool isApplied = _audioMixer.SetFloat(parameterName, dbValue);

        if (isApplied == false)
        {
            throw new InvalidOperationException(nameof(_audioMixer));
        }
    }

    private void ApplyFullScreen(bool isFullScreen)
    {
        Screen.fullScreenMode = isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = isFullScreen;
    }

    private void ApplyQuality(int qualityLevel)
    {
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }

    private SettingsData NormalizeData(SettingsData settingsData)
    {
        settingsData.MasterVolume = Mathf.Clamp01(settingsData.MasterVolume);
        settingsData.MusicVolume = Mathf.Clamp01(settingsData.MusicVolume);
        settingsData.EffectsVolume = Mathf.Clamp01(settingsData.EffectsVolume);
        settingsData.QualityLevel = NormalizeQualityLevel(settingsData.QualityLevel);

        return settingsData;
    }

    private SettingsData CreateDefaultData()
    {
        return new SettingsData(1.0f, 1.0f, 1.0f, _defaultFullScreen, _highQualityLevel, false, false);
    }

    private int NormalizeQualityLevel(int qualityLevel)
    {
        if (qualityLevel == _lowQualityLevel)
        {
            return qualityLevel;
        }

        if (qualityLevel == _mediumQualityLevel)
        {
            return qualityLevel;
        }

        if (qualityLevel == _highQualityLevel)
        {
            return qualityLevel;
        }

        return _highQualityLevel;
    }

    private void CacheQualityLevels()
    {
        _lowQualityLevel = FindQualityLevel(LowQualityName);
        _mediumQualityLevel = FindQualityLevel(MediumQualityName);
        _highQualityLevel = FindQualityLevel(HighQualityName);

        if (_lowQualityLevel < 0 || _mediumQualityLevel < 0 || _highQualityLevel < 0)
        {
            throw new InvalidOperationException(nameof(QualitySettings));
        }
    }

    private int FindQualityLevel(string qualityName)
    {
        string[] qualityNames = QualitySettings.names;

        for (int i = 0; i < qualityNames.Length; i++)
        {
            if (qualityNames[i] == qualityName)
            {
                return i;
            }
        }

        return -1;
    }

    private float LinearToDb(float linearValue)
    {
        if (linearValue <= MuteThreshold)
        {
            return MinDb;
        }

        return Mathf.Log10(linearValue) * DbMultiplier;
    }
}
