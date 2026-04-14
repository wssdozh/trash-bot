using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public sealed class SettingsPanelView : MonoBehaviour
{
    private const int LowQualityIndex = 0;
    private const int MediumQualityIndex = 1;
    private const int HighQualityIndex = 2;

    [SerializeField] private AudioMixer _audioMixer;

    [SerializeField] private Slider _masterSlider;
    [SerializeField] private TMP_Text _masterValue;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private TMP_Text _musicValue;
    [SerializeField] private Slider _effectsSlider;
    [SerializeField] private TMP_Text _effectsValue;

    [SerializeField] private Button _windowButton;
    [SerializeField] private Button _screenButton;
    [SerializeField] private Button _lowQualityButton;
    [SerializeField] private Button _mediumQualityButton;
    [SerializeField] private Button _highQualityButton;
    [SerializeField] private Button _healthOffButton;
    [SerializeField] private Button _healthOnButton;
    [SerializeField] private Button _damageOffButton;
    [SerializeField] private Button _damageOnButton;
    [SerializeField] private Button _resetButton;

    private readonly int[] _qualityLevels = new int[3];

    private readonly Color _activeColor = new Color(0.44f, 0.58f, 0.35f, 1.0f);

    private Image _windowImage;
    private Image _screenImage;
    private Image _lowQualityImage;
    private Image _mediumQualityImage;
    private Image _highQualityImage;
    private Image _healthOffImage;
    private Image _healthOnImage;
    private Image _damageOffImage;
    private Image _damageOnImage;
    private Image _resetImage;

    private TMP_Text _windowText;
    private TMP_Text _screenText;
    private TMP_Text _lowQualityText;
    private TMP_Text _mediumQualityText;
    private TMP_Text _highQualityText;
    private TMP_Text _healthOffText;
    private TMP_Text _healthOnText;
    private TMP_Text _damageOffText;
    private TMP_Text _damageOnText;
    private TMP_Text _resetText;

    private Color _buttonTextColor;
    private Color _inactiveColor;

    private bool _isSyncing;

    private SettingsPresenter _settingsPresenter;

    public event Action<float> MasterChanged;
    public event Action<float> MusicChanged;
    public event Action<float> EffectsChanged;
    public event Action<bool> FullScreenChanged;
    public event Action<int> QualityChanged;
    public event Action<bool> InfiniteHealthChanged;
    public event Action<bool> InfiniteDamageChanged;
    public event Action ResetClicked;

    private void Awake()
    {
        ValidateReferences();
        CacheButtons();
        Bind();

        _settingsPresenter = new SettingsPresenter(this, _audioMixer, new SettingsSave());
        _settingsPresenter.Initialize();
    }

    private void OnDestroy()
    {
        Unbind();

        if (_settingsPresenter != null)
            _settingsPresenter.Dispose();
    }

    internal void SetQualityLevels(int lowQualityLevel, int mediumQualityLevel, int highQualityLevel)
    {
        _qualityLevels[LowQualityIndex] = lowQualityLevel;
        _qualityLevels[MediumQualityIndex] = mediumQualityLevel;
        _qualityLevels[HighQualityIndex] = highQualityLevel;
    }

    internal void SetData(SettingsData settingsData)
    {
        _isSyncing = true;

        _masterSlider.value = settingsData.MasterVolume;
        _musicSlider.value = settingsData.MusicVolume;
        _effectsSlider.value = settingsData.EffectsVolume;

        UpdateSliderValue(_masterValue, settingsData.MasterVolume);
        UpdateSliderValue(_musicValue, settingsData.MusicVolume);
        UpdateSliderValue(_effectsValue, settingsData.EffectsVolume);

        UpdateFullScreenState(settingsData.IsFullScreen);
        UpdateQualityState(settingsData.QualityLevel);
        UpdateHealthState(settingsData.IsInfiniteHealth);
        UpdateDamageState(settingsData.IsInfiniteDamage);

        _isSyncing = false;
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
        ValidateReference(_screenButton, nameof(_screenButton));
        ValidateReference(_lowQualityButton, nameof(_lowQualityButton));
        ValidateReference(_mediumQualityButton, nameof(_mediumQualityButton));
        ValidateReference(_highQualityButton, nameof(_highQualityButton));
        ValidateReference(_healthOffButton, nameof(_healthOffButton));
        ValidateReference(_healthOnButton, nameof(_healthOnButton));
        ValidateReference(_damageOffButton, nameof(_damageOffButton));
        ValidateReference(_damageOnButton, nameof(_damageOnButton));
        ValidateReference(_resetButton, nameof(_resetButton));
    }

    private void ValidateReference(UnityEngine.Object target, string fieldName)
    {
        if (target == null)
            throw new MissingReferenceException(fieldName);
    }

    private void CacheButtons()
    {
        _windowImage = GetButtonImage(_windowButton);
        _screenImage = GetButtonImage(_screenButton);
        _lowQualityImage = GetButtonImage(_lowQualityButton);
        _mediumQualityImage = GetButtonImage(_mediumQualityButton);
        _highQualityImage = GetButtonImage(_highQualityButton);
        _healthOffImage = GetButtonImage(_healthOffButton);
        _healthOnImage = GetButtonImage(_healthOnButton);
        _damageOffImage = GetButtonImage(_damageOffButton);
        _damageOnImage = GetButtonImage(_damageOnButton);
        _resetImage = GetButtonImage(_resetButton);

        _windowText = GetButtonText(_windowButton);
        _screenText = GetButtonText(_screenButton);
        _lowQualityText = GetButtonText(_lowQualityButton);
        _mediumQualityText = GetButtonText(_mediumQualityButton);
        _highQualityText = GetButtonText(_highQualityButton);
        _healthOffText = GetButtonText(_healthOffButton);
        _healthOnText = GetButtonText(_healthOnButton);
        _damageOffText = GetButtonText(_damageOffButton);
        _damageOnText = GetButtonText(_damageOnButton);
        _resetText = GetButtonText(_resetButton);

        _buttonTextColor = _windowText.color;
        _inactiveColor = _windowImage.color;
    }

    private Image GetButtonImage(Button button)
    {
        Image buttonImage = button.GetComponent<Image>();

        if (buttonImage == null)
            throw new MissingComponentException(nameof(Image));

        return buttonImage;
    }

    private TMP_Text GetButtonText(Button button)
    {
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText == null)
            throw new MissingComponentException(nameof(TMP_Text));

        return buttonText;
    }

    private void Bind()
    {
        _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        _effectsSlider.onValueChanged.AddListener(OnEffectsChanged);

        _windowButton.onClick.AddListener(OnWindowClicked);
        _screenButton.onClick.AddListener(OnScreenClicked);
        _lowQualityButton.onClick.AddListener(OnLowQualityClicked);
        _mediumQualityButton.onClick.AddListener(OnMediumQualityClicked);
        _highQualityButton.onClick.AddListener(OnHighQualityClicked);
        _healthOffButton.onClick.AddListener(OnHealthOffClicked);
        _healthOnButton.onClick.AddListener(OnHealthOnClicked);
        _damageOffButton.onClick.AddListener(OnDamageOffClicked);
        _damageOnButton.onClick.AddListener(OnDamageOnClicked);
        _resetButton.onClick.AddListener(OnResetClicked);
    }

    private void Unbind()
    {
        _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        _effectsSlider.onValueChanged.RemoveListener(OnEffectsChanged);

        _windowButton.onClick.RemoveListener(OnWindowClicked);
        _screenButton.onClick.RemoveListener(OnScreenClicked);
        _lowQualityButton.onClick.RemoveListener(OnLowQualityClicked);
        _mediumQualityButton.onClick.RemoveListener(OnMediumQualityClicked);
        _highQualityButton.onClick.RemoveListener(OnHighQualityClicked);
        _healthOffButton.onClick.RemoveListener(OnHealthOffClicked);
        _healthOnButton.onClick.RemoveListener(OnHealthOnClicked);
        _damageOffButton.onClick.RemoveListener(OnDamageOffClicked);
        _damageOnButton.onClick.RemoveListener(OnDamageOnClicked);
        _resetButton.onClick.RemoveListener(OnResetClicked);
    }

    private void UpdateSliderValue(TMP_Text valueText, float value)
    {
        int percentValue = Mathf.RoundToInt(value * 100.0f);

        valueText.text = percentValue + "%";
    }

    private void UpdateFullScreenState(bool isFullScreen)
    {
        UpdateButtonState(_windowImage, _windowText, isFullScreen == false);
        UpdateButtonState(_screenImage, _screenText, isFullScreen);
    }

    private void UpdateQualityState(int qualityLevel)
    {
        UpdateButtonState(_lowQualityImage, _lowQualityText, qualityLevel == _qualityLevels[LowQualityIndex]);
        UpdateButtonState(_mediumQualityImage, _mediumQualityText, qualityLevel == _qualityLevels[MediumQualityIndex]);
        UpdateButtonState(_highQualityImage, _highQualityText, qualityLevel == _qualityLevels[HighQualityIndex]);
        UpdateButtonState(_resetImage, _resetText, false);
    }

    private void UpdateHealthState(bool isEnabled)
    {
        UpdateButtonState(_healthOffImage, _healthOffText, isEnabled == false);
        UpdateButtonState(_healthOnImage, _healthOnText, isEnabled);
    }

    private void UpdateDamageState(bool isEnabled)
    {
        UpdateButtonState(_damageOffImage, _damageOffText, isEnabled == false);
        UpdateButtonState(_damageOnImage, _damageOnText, isEnabled);
    }

    private void UpdateButtonState(Image buttonImage, TMP_Text buttonText, bool isActive)
    {
        buttonImage.color = isActive ? _activeColor : _inactiveColor;
        buttonText.color = _buttonTextColor;
    }

    private void OnMasterChanged(float value)
    {
        UpdateSliderValue(_masterValue, value);

        if (_isSyncing == false)
            MasterChanged?.Invoke(value);
    }

    private void OnMusicChanged(float value)
    {
        UpdateSliderValue(_musicValue, value);

        if (_isSyncing == false)
            MusicChanged?.Invoke(value);
    }

    private void OnEffectsChanged(float value)
    {
        UpdateSliderValue(_effectsValue, value);

        if (_isSyncing == false)
            EffectsChanged?.Invoke(value);
    }

    private void OnWindowClicked()
    {
        if (_isSyncing == false)
            FullScreenChanged?.Invoke(false);
    }

    private void OnScreenClicked()
    {
        if (_isSyncing == false)
            FullScreenChanged?.Invoke(true);
    }

    private void OnLowQualityClicked()
    {
        if (_isSyncing == false)
            QualityChanged?.Invoke(_qualityLevels[LowQualityIndex]);
    }

    private void OnMediumQualityClicked()
    {
        if (_isSyncing == false)
            QualityChanged?.Invoke(_qualityLevels[MediumQualityIndex]);
    }

    private void OnHighQualityClicked()
    {
        if (_isSyncing == false)
            QualityChanged?.Invoke(_qualityLevels[HighQualityIndex]);
    }

    private void OnHealthOffClicked()
    {
        if (_isSyncing == false)
            InfiniteHealthChanged?.Invoke(false);
    }

    private void OnHealthOnClicked()
    {
        if (_isSyncing == false)
            InfiniteHealthChanged?.Invoke(true);
    }

    private void OnDamageOffClicked()
    {
        if (_isSyncing == false)
            InfiniteDamageChanged?.Invoke(false);
    }

    private void OnDamageOnClicked()
    {
        if (_isSyncing == false)
            InfiniteDamageChanged?.Invoke(true);
    }

    private void OnResetClicked()
    {
        if (_isSyncing == false)
            ResetClicked?.Invoke();
    }
}
