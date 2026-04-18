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
    private const float TeleportHeightOffset = 0.35f;

    [SerializeField] private AudioMixer _audioMixer;

    [SerializeField] private Slider _masterSlider;
    [SerializeField] private TMP_Text _masterValue;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private TMP_Text _musicValue;
    [SerializeField] private Slider _effectsSlider;
    [SerializeField] private TMP_Text _effectsValue;

    [SerializeField] private Button _windowButton;
    [SerializeField] private Button _screenButton;
    [SerializeField] private Button _vSyncOffButton;
    [SerializeField] private Button _vSyncOnButton;
    [SerializeField] private Button _lowQualityButton;
    [SerializeField] private Button _mediumQualityButton;
    [SerializeField] private Button _highQualityButton;
    [SerializeField] private Button _healthOffButton;
    [SerializeField] private Button _healthOnButton;
    [SerializeField] private Button _damageOffButton;
    [SerializeField] private Button _damageOnButton;
    [SerializeField] private Button _teleportBossButton;
    [SerializeField] private Button _resetButton;

    [SerializeField] private Transform _playerRoot;
    [SerializeField] private Transform _playerBody;
    [SerializeField] private GameObject _levelRoot;

    private readonly int[] _qualityLevels = new int[3];

    private readonly Color _activeColor = new Color(0.44f, 0.58f, 0.35f, 1.0f);

    private Image _windowImage;
    private Image _screenImage;
    private Image _vSyncOffImage;
    private Image _vSyncOnImage;
    private Image _lowQualityImage;
    private Image _mediumQualityImage;
    private Image _highQualityImage;
    private Image _healthOffImage;
    private Image _healthOnImage;
    private Image _damageOffImage;
    private Image _damageOnImage;
    private Image _teleportBossImage;
    private Image _resetImage;

    private TMP_Text _windowText;
    private TMP_Text _screenText;
    private TMP_Text _vSyncOffText;
    private TMP_Text _vSyncOnText;
    private TMP_Text _lowQualityText;
    private TMP_Text _mediumQualityText;
    private TMP_Text _highQualityText;
    private TMP_Text _healthOffText;
    private TMP_Text _healthOnText;
    private TMP_Text _damageOffText;
    private TMP_Text _damageOnText;
    private TMP_Text _teleportBossText;
    private TMP_Text _resetText;

    private Color _buttonTextColor;
    private Color _inactiveColor;

    private bool _isSyncing;
    private bool _isBound;

    private SettingsPresenter _settingsPresenter;
    private LevelGenerator _levelGenerator;
    private Rigidbody _playerBodyRigidbody;

    public event Action<float> MasterChanged;
    public event Action<float> MusicChanged;
    public event Action<float> EffectsChanged;
    public event Action<bool> FullScreenChanged;
    public event Action<bool> VSyncChanged;
    public event Action<int> QualityChanged;
    public event Action<bool> InfiniteHealthChanged;
    public event Action<bool> InfiniteDamageChanged;
    public event Action ResetClicked;

    private void Awake()
    {
        ValidateReferences();
        CacheButtons();
        Bind();
        _isBound = true;

        _settingsPresenter = new SettingsPresenter(this, _audioMixer, new SettingsSave());
        _settingsPresenter.Initialize();
    }

    private void OnDestroy()
    {
        if (_isBound)
        {
            Unbind();
            _isBound = false;
        }

        if (_settingsPresenter != null)
        {
            _settingsPresenter.Dispose();
        }
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
        UpdateVSyncState(settingsData.IsVSyncEnabled);
        UpdateQualityState(settingsData.QualityLevel);
        UpdateHealthState(settingsData.IsInfiniteHealth);
        UpdateDamageState(settingsData.IsInfiniteDamage);
        UpdateButtonState(_teleportBossImage, _teleportBossText, false);

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
        ValidateReference(_vSyncOffButton, nameof(_vSyncOffButton));
        ValidateReference(_vSyncOnButton, nameof(_vSyncOnButton));
        ValidateReference(_lowQualityButton, nameof(_lowQualityButton));
        ValidateReference(_mediumQualityButton, nameof(_mediumQualityButton));
        ValidateReference(_highQualityButton, nameof(_highQualityButton));
        ValidateReference(_healthOffButton, nameof(_healthOffButton));
        ValidateReference(_healthOnButton, nameof(_healthOnButton));
        ValidateReference(_damageOffButton, nameof(_damageOffButton));
        ValidateReference(_damageOnButton, nameof(_damageOnButton));
        ValidateReference(_teleportBossButton, nameof(_teleportBossButton));
        ValidateReference(_resetButton, nameof(_resetButton));
        ValidateReference(_playerRoot, nameof(_playerRoot));
        ValidateReference(_playerBody, nameof(_playerBody));
        ValidateReference(_levelRoot, nameof(_levelRoot));
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
        _vSyncOffImage = GetButtonImage(_vSyncOffButton);
        _vSyncOnImage = GetButtonImage(_vSyncOnButton);
        _lowQualityImage = GetButtonImage(_lowQualityButton);
        _mediumQualityImage = GetButtonImage(_mediumQualityButton);
        _highQualityImage = GetButtonImage(_highQualityButton);
        _healthOffImage = GetButtonImage(_healthOffButton);
        _healthOnImage = GetButtonImage(_healthOnButton);
        _damageOffImage = GetButtonImage(_damageOffButton);
        _damageOnImage = GetButtonImage(_damageOnButton);
        _teleportBossImage = GetButtonImage(_teleportBossButton);
        _resetImage = GetButtonImage(_resetButton);

        _windowText = GetButtonText(_windowButton);
        _screenText = GetButtonText(_screenButton);
        _vSyncOffText = GetButtonText(_vSyncOffButton);
        _vSyncOnText = GetButtonText(_vSyncOnButton);
        _lowQualityText = GetButtonText(_lowQualityButton);
        _mediumQualityText = GetButtonText(_mediumQualityButton);
        _highQualityText = GetButtonText(_highQualityButton);
        _healthOffText = GetButtonText(_healthOffButton);
        _healthOnText = GetButtonText(_healthOnButton);
        _damageOffText = GetButtonText(_damageOffButton);
        _damageOnText = GetButtonText(_damageOnButton);
        _teleportBossText = GetButtonText(_teleportBossButton);
        _resetText = GetButtonText(_resetButton);

        _buttonTextColor = _windowText.color;
        _inactiveColor = _windowImage.color;

        _levelGenerator = _levelRoot.GetComponent<LevelGenerator>();

        if (_levelGenerator == null)
            throw new MissingComponentException(nameof(LevelGenerator));

        _playerBodyRigidbody = _playerBody.GetComponent<Rigidbody>();

        if (_playerBodyRigidbody == null)
            throw new MissingComponentException(nameof(Rigidbody));
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
        _vSyncOffButton.onClick.AddListener(OnVSyncOffClicked);
        _vSyncOnButton.onClick.AddListener(OnVSyncOnClicked);
        _lowQualityButton.onClick.AddListener(OnLowQualityClicked);
        _mediumQualityButton.onClick.AddListener(OnMediumQualityClicked);
        _highQualityButton.onClick.AddListener(OnHighQualityClicked);
        _healthOffButton.onClick.AddListener(OnHealthOffClicked);
        _healthOnButton.onClick.AddListener(OnHealthOnClicked);
        _damageOffButton.onClick.AddListener(OnDamageOffClicked);
        _damageOnButton.onClick.AddListener(OnDamageOnClicked);
        _teleportBossButton.onClick.AddListener(OnTeleportBossClicked);
        _resetButton.onClick.AddListener(OnResetClicked);
    }

    private void Unbind()
    {
        RemoveSliderListener(_masterSlider, OnMasterChanged);
        RemoveSliderListener(_musicSlider, OnMusicChanged);
        RemoveSliderListener(_effectsSlider, OnEffectsChanged);

        RemoveButtonListener(_windowButton, OnWindowClicked);
        RemoveButtonListener(_screenButton, OnScreenClicked);
        RemoveButtonListener(_vSyncOffButton, OnVSyncOffClicked);
        RemoveButtonListener(_vSyncOnButton, OnVSyncOnClicked);
        RemoveButtonListener(_lowQualityButton, OnLowQualityClicked);
        RemoveButtonListener(_mediumQualityButton, OnMediumQualityClicked);
        RemoveButtonListener(_highQualityButton, OnHighQualityClicked);
        RemoveButtonListener(_healthOffButton, OnHealthOffClicked);
        RemoveButtonListener(_healthOnButton, OnHealthOnClicked);
        RemoveButtonListener(_damageOffButton, OnDamageOffClicked);
        RemoveButtonListener(_damageOnButton, OnDamageOnClicked);
        RemoveButtonListener(_teleportBossButton, OnTeleportBossClicked);
        RemoveButtonListener(_resetButton, OnResetClicked);
    }

    private void RemoveSliderListener(Slider slider, UnityEngine.Events.UnityAction<float> listener)
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.RemoveListener(listener);
    }

    private void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(listener);
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

    private void UpdateVSyncState(bool isEnabled)
    {
        UpdateButtonState(_vSyncOffImage, _vSyncOffText, isEnabled == false);
        UpdateButtonState(_vSyncOnImage, _vSyncOnText, isEnabled);
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

    private void OnVSyncOffClicked()
    {
        if (_isSyncing == false)
            VSyncChanged?.Invoke(false);
    }

    private void OnVSyncOnClicked()
    {
        if (_isSyncing == false)
            VSyncChanged?.Invoke(true);
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

    private void OnTeleportBossClicked()
    {
        if (_isSyncing == false)
            TeleportPlayerToBossRoom();
    }

    private void OnResetClicked()
    {
        if (_isSyncing == false)
            ResetClicked?.Invoke();
    }

    private void TeleportPlayerToBossRoom()
    {
        Vector3 bossRoomEntryPosition = _levelGenerator.GetBossRoomEntry();
        bossRoomEntryPosition.y += TeleportHeightOffset;

        Vector3 rootToBodyOffset = _playerRoot.position - _playerBody.position;
        Vector3 targetRootPosition = bossRoomEntryPosition + rootToBodyOffset;

        _playerRoot.position = targetRootPosition;
        _playerBodyRigidbody.linearVelocity = Vector3.zero;
        _playerBodyRigidbody.angularVelocity = Vector3.zero;
        _playerBodyRigidbody.position = bossRoomEntryPosition;
    }
}
