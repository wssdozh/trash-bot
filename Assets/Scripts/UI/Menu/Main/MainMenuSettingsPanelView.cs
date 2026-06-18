using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public sealed class MainMenuSettingsPanelView : MonoBehaviour
{
    private const string MasterParameterName = "Master";
    private const string MusicParameterName = "Music";
    private const string EffectsParameterName = "Effects";

    private const string HighQualityName = "HighQuality";
    private const string MediumQualityName = "MediumQuality";
    private const string LowQualityName = "LowQuality";

    private const int LowQualityIndex = 0;
    private const int MediumQualityIndex = 1;
    private const int HighQualityIndex = 2;

    private const float MinDb = -80.0f;
    private const float MuteThreshold = 0.0001f;
    private const float DbMultiplier = 20.0f;

    [SerializeField] private AudioMixer _audioMixer;

    [SerializeField] private Slider _masterSlider;
    [SerializeField] private TMP_Text _masterValue;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private TMP_Text _musicValue;
    [SerializeField] private Slider _effectsSlider;
    [SerializeField] private TMP_Text _effectsValue;

    [SerializeField] private Button _windowButton;
    [SerializeField] private Image _windowButtonImage;
    [SerializeField] private TMP_Text _windowButtonText;
    [SerializeField] private Button _screenButton;
    [SerializeField] private Image _screenButtonImage;
    [SerializeField] private TMP_Text _screenButtonText;
    [SerializeField] private Button _vSyncOffButton;
    [SerializeField] private Image _vSyncOffButtonImage;
    [SerializeField] private TMP_Text _vSyncOffButtonText;
    [SerializeField] private Button _vSyncOnButton;
    [SerializeField] private Image _vSyncOnButtonImage;
    [SerializeField] private TMP_Text _vSyncOnButtonText;
    [SerializeField] private Button _lowQualityButton;
    [SerializeField] private Image _lowQualityButtonImage;
    [SerializeField] private TMP_Text _lowQualityButtonText;
    [SerializeField] private Button _mediumQualityButton;
    [SerializeField] private Image _mediumQualityButtonImage;
    [SerializeField] private TMP_Text _mediumQualityButtonText;
    [SerializeField] private Button _highQualityButton;
    [SerializeField] private Image _highQualityButtonImage;
    [SerializeField] private TMP_Text _highQualityButtonText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Image _resetButtonImage;
    [SerializeField] private TMP_Text _resetButtonText;

    private readonly SettingsSave _settingsSave = new SettingsSave();
    private readonly int[] _qualityLevels = new int[3];
    private readonly Color _activeColor = new Color(0.44f, 0.58f, 0.35f, 1.0f);

    private SettingsData _settingsData;

    private Color _buttonTextColor;
    private Color _inactiveColor;

    private bool _defaultFullScreen;
    private bool _defaultVSyncEnabled;
    private bool _isBound;
    private bool _isSyncing;

    private void Awake()
    {
        ValidateReferences();
        CacheQualityLevels();

        _buttonTextColor = _windowButtonText.color;
        _inactiveColor = _windowButtonImage.color;
        _defaultFullScreen = Screen.fullScreen;
        _defaultVSyncEnabled = QualitySettings.vSyncCount > 0;

        Bind();
        _isBound = true;

        _settingsData = _settingsSave.Load(_qualityLevels[HighQualityIndex], _defaultFullScreen, _defaultVSyncEnabled);
        _settingsData = NormalizeData(_settingsData);

        ApplyData();
        RefreshView();
    }

    private void OnDestroy()
    {
        if (_isBound)
        {
            Unbind();
            _isBound = false;
        }
    }

    private void ValidateReferences()
    {
        ValidateReference(_audioMixer, nameof(_audioMixer));
        ValidateReference(_masterSlider, nameof(_masterSlider));
        ValidateReference(_masterValue, nameof(_masterValue));
        ValidateReference(_musicSlider, nameof(_musicSlider));
        ValidateReference(_musicValue, nameof(_musicValue));
        ValidateReference(_effectsSlider, nameof(_effectsSlider));
        ValidateReference(_effectsValue, nameof(_effectsValue));
        ValidateReference(_windowButton, nameof(_windowButton));
        ValidateReference(_windowButtonImage, nameof(_windowButtonImage));
        ValidateReference(_windowButtonText, nameof(_windowButtonText));
        ValidateReference(_screenButton, nameof(_screenButton));
        ValidateReference(_screenButtonImage, nameof(_screenButtonImage));
        ValidateReference(_screenButtonText, nameof(_screenButtonText));
        ValidateReference(_vSyncOffButton, nameof(_vSyncOffButton));
        ValidateReference(_vSyncOffButtonImage, nameof(_vSyncOffButtonImage));
        ValidateReference(_vSyncOffButtonText, nameof(_vSyncOffButtonText));
        ValidateReference(_vSyncOnButton, nameof(_vSyncOnButton));
        ValidateReference(_vSyncOnButtonImage, nameof(_vSyncOnButtonImage));
        ValidateReference(_vSyncOnButtonText, nameof(_vSyncOnButtonText));
        ValidateReference(_lowQualityButton, nameof(_lowQualityButton));
        ValidateReference(_lowQualityButtonImage, nameof(_lowQualityButtonImage));
        ValidateReference(_lowQualityButtonText, nameof(_lowQualityButtonText));
        ValidateReference(_mediumQualityButton, nameof(_mediumQualityButton));
        ValidateReference(_mediumQualityButtonImage, nameof(_mediumQualityButtonImage));
        ValidateReference(_mediumQualityButtonText, nameof(_mediumQualityButtonText));
        ValidateReference(_highQualityButton, nameof(_highQualityButton));
        ValidateReference(_highQualityButtonImage, nameof(_highQualityButtonImage));
        ValidateReference(_highQualityButtonText, nameof(_highQualityButtonText));
        ValidateReference(_resetButton, nameof(_resetButton));
        ValidateReference(_resetButtonImage, nameof(_resetButtonImage));
        ValidateReference(_resetButtonText, nameof(_resetButtonText));
    }

    private void ValidateReference(UnityEngine.Object target, string fieldName)
    {
        if (target == null)
        {
            throw new MissingReferenceException(fieldName);
        }
    }

    private void Bind()
    {
        _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        _effectsSlider.onValueChanged.AddListener(OnEffectsChanged);

        _windowButton.onClick.AddListener(OnWindowClicked);
        _screenButton.onClick.AddListener(OnScreenClicked);
        _vSyncOffButton.onClick.AddListener(OnVSyncOffClicked);
        _vSyncOnButton.onClick.AddListener(OnVSyncOnClicked);
        _lowQualityButton.onClick.AddListener(OnLowQualityClicked);
        _mediumQualityButton.onClick.AddListener(OnMediumQualityClicked);
        _highQualityButton.onClick.AddListener(OnHighQualityClicked);
        _resetButton.onClick.AddListener(OnResetClicked);
    }

    private void Unbind()
    {
        _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        _effectsSlider.onValueChanged.RemoveListener(OnEffectsChanged);

        _windowButton.onClick.RemoveListener(OnWindowClicked);
        _screenButton.onClick.RemoveListener(OnScreenClicked);
        _vSyncOffButton.onClick.RemoveListener(OnVSyncOffClicked);
        _vSyncOnButton.onClick.RemoveListener(OnVSyncOnClicked);
        _lowQualityButton.onClick.RemoveListener(OnLowQualityClicked);
        _mediumQualityButton.onClick.RemoveListener(OnMediumQualityClicked);
        _highQualityButton.onClick.RemoveListener(OnHighQualityClicked);
        _resetButton.onClick.RemoveListener(OnResetClicked);
    }

    private void CacheQualityLevels()
    {
        _qualityLevels[LowQualityIndex] = FindQualityLevel(LowQualityName);
        _qualityLevels[MediumQualityIndex] = FindQualityLevel(MediumQualityName);
        _qualityLevels[HighQualityIndex] = FindQualityLevel(HighQualityName);

        if (_qualityLevels[LowQualityIndex] < 0 ||
            _qualityLevels[MediumQualityIndex] < 0 ||
            _qualityLevels[HighQualityIndex] < 0)
        {
            throw new InvalidOperationException(nameof(QualitySettings));
        }
    }

    private int FindQualityLevel(string qualityName)
    {
        string[] qualityNames = QualitySettings.names;

        for (int index = 0; index < qualityNames.Length; index++)
        {
            if (qualityNames[index] == qualityName)
            {
                return index;
            }
        }

        return -1;
    }

    private SettingsData NormalizeData(SettingsData settingsData)
    {
        settingsData.MasterVolume = Mathf.Clamp01(settingsData.MasterVolume);
        settingsData.MusicVolume = Mathf.Clamp01(settingsData.MusicVolume);
        settingsData.EffectsVolume = Mathf.Clamp01(settingsData.EffectsVolume);
        settingsData.QualityLevel = NormalizeQualityLevel(settingsData.QualityLevel);

        return settingsData;
    }

    private int NormalizeQualityLevel(int qualityLevel)
    {
        if (qualityLevel == _qualityLevels[LowQualityIndex])
        {
            return qualityLevel;
        }

        if (qualityLevel == _qualityLevels[MediumQualityIndex])
        {
            return qualityLevel;
        }

        if (qualityLevel == _qualityLevels[HighQualityIndex])
        {
            return qualityLevel;
        }

        return _qualityLevels[HighQualityIndex];
    }

    private void RefreshView()
    {
        _isSyncing = true;

        _masterSlider.value = _settingsData.MasterVolume;
        _musicSlider.value = _settingsData.MusicVolume;
        _effectsSlider.value = _settingsData.EffectsVolume;

        UpdateSliderValue(_masterValue, _settingsData.MasterVolume);
        UpdateSliderValue(_musicValue, _settingsData.MusicVolume);
        UpdateSliderValue(_effectsValue, _settingsData.EffectsVolume);

        UpdateFullScreenState(_settingsData.IsFullScreen);
        UpdateVSyncState(_settingsData.IsVSyncEnabled);
        UpdateQualityState(_settingsData.QualityLevel);

        _isSyncing = false;
    }

    private void UpdateSliderValue(TMP_Text valueText, float value)
    {
        int percentValue = Mathf.RoundToInt(value * 100.0f);
        valueText.text = percentValue + "%";
    }

    private void UpdateFullScreenState(bool isFullScreen)
    {
        UpdateButtonState(_windowButtonImage, _windowButtonText, isFullScreen == false);
        UpdateButtonState(_screenButtonImage, _screenButtonText, isFullScreen);
    }

    private void UpdateVSyncState(bool isEnabled)
    {
        UpdateButtonState(_vSyncOffButtonImage, _vSyncOffButtonText, isEnabled == false);
        UpdateButtonState(_vSyncOnButtonImage, _vSyncOnButtonText, isEnabled);
    }

    private void UpdateQualityState(int qualityLevel)
    {
        UpdateButtonState(_lowQualityButtonImage, _lowQualityButtonText, qualityLevel == _qualityLevels[LowQualityIndex]);
        UpdateButtonState(_mediumQualityButtonImage, _mediumQualityButtonText, qualityLevel == _qualityLevels[MediumQualityIndex]);
        UpdateButtonState(_highQualityButtonImage, _highQualityButtonText, qualityLevel == _qualityLevels[HighQualityIndex]);
        UpdateButtonState(_resetButtonImage, _resetButtonText, false);
    }

    private void UpdateButtonState(Image buttonImage, TMP_Text buttonText, bool isActive)
    {
        buttonImage.color = isActive ? _activeColor : _inactiveColor;
        buttonText.color = _buttonTextColor;
    }

    private void OnMasterChanged(float value)
    {
        UpdateSliderValue(_masterValue, value);

        if (_isSyncing)
        {
            return;
        }

        _settingsData.MasterVolume = Mathf.Clamp01(value);
        ApplyVolume(MasterParameterName, _settingsData.MasterVolume);
        Save();
    }

    private void OnMusicChanged(float value)
    {
        UpdateSliderValue(_musicValue, value);

        if (_isSyncing)
        {
            return;
        }

        _settingsData.MusicVolume = Mathf.Clamp01(value);
        ApplyVolume(MusicParameterName, _settingsData.MusicVolume);
        Save();
    }

    private void OnEffectsChanged(float value)
    {
        UpdateSliderValue(_effectsValue, value);

        if (_isSyncing)
        {
            return;
        }

        _settingsData.EffectsVolume = Mathf.Clamp01(value);
        ApplyVolume(EffectsParameterName, _settingsData.EffectsVolume);
        Save();
    }

    private void OnWindowClicked()
    {
        if (_isSyncing)
        {
            return;
        }

        _settingsData.IsFullScreen = false;
        ApplyFullScreen(_settingsData.IsFullScreen);
        Save();
        RefreshView();
    }

    private void OnScreenClicked()
    {
        if (_isSyncing)
        {
            return;
        }

        _settingsData.IsFullScreen = true;
        ApplyFullScreen(_settingsData.IsFullScreen);
        Save();
        RefreshView();
    }

    private void OnVSyncOffClicked()
    {
        if (_isSyncing)
        {
            return;
        }

        _settingsData.IsVSyncEnabled = false;
        ApplyVSync(_settingsData.IsVSyncEnabled);
        Save();
        RefreshView();
    }

    private void OnVSyncOnClicked()
    {
        if (_isSyncing)
        {
            return;
        }

        _settingsData.IsVSyncEnabled = true;
        ApplyVSync(_settingsData.IsVSyncEnabled);
        Save();
        RefreshView();
    }

    private void OnLowQualityClicked()
    {
        ApplyQualitySelection(_qualityLevels[LowQualityIndex]);
    }

    private void OnMediumQualityClicked()
    {
        ApplyQualitySelection(_qualityLevels[MediumQualityIndex]);
    }

    private void OnHighQualityClicked()
    {
        ApplyQualitySelection(_qualityLevels[HighQualityIndex]);
    }

    private void OnResetClicked()
    {
        if (_isSyncing)
        {
            return;
        }

        _settingsData = CreateDefaultData();
        ApplyData();
        Save();
        RefreshView();
    }

    private void ApplyQualitySelection(int qualityLevel)
    {
        if (_isSyncing)
        {
            return;
        }

        _settingsData.QualityLevel = NormalizeQualityLevel(qualityLevel);
        ApplyQuality(_settingsData.QualityLevel);
        ApplyVSync(_settingsData.IsVSyncEnabled);
        Save();
        RefreshView();
    }

    private void Save()
    {
        _settingsSave.Save(_settingsData);
    }

    private SettingsData CreateDefaultData()
    {
        return new SettingsData(
            1.0f,
            1.0f,
            1.0f,
            _defaultFullScreen,
            _defaultVSyncEnabled,
            _qualityLevels[HighQualityIndex],
            false,
            false);
    }

    private void ApplyData()
    {
        ApplyVolume(MasterParameterName, _settingsData.MasterVolume);
        ApplyVolume(MusicParameterName, _settingsData.MusicVolume);
        ApplyVolume(EffectsParameterName, _settingsData.EffectsVolume);
        ApplyFullScreen(_settingsData.IsFullScreen);
        ApplyQuality(_settingsData.QualityLevel);
        ApplyVSync(_settingsData.IsVSyncEnabled);
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

    private void ApplyVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
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
