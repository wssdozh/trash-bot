using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuSceneController : MonoBehaviour
{
    [SerializeField] private PauseMenuView _mainMenuView;
    [SerializeField] private SettingsMenuView _settingsMenuView;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private string _levelSceneName = "SampleSceneLevelGen";

    private void Awake()
    {
        ValidateReference(_mainMenuView, nameof(_mainMenuView));
        ValidateReference(_settingsMenuView, nameof(_settingsMenuView));
        ValidateReference(_startButton, nameof(_startButton));
        ValidateReference(_settingsButton, nameof(_settingsButton));
        ValidateReference(_exitButton, nameof(_exitButton));
        ValidateReference(_backButton, nameof(_backButton));

        if (string.IsNullOrWhiteSpace(_levelSceneName))
        {
            throw new MissingReferenceException(nameof(_levelSceneName));
        }
    }

    private void OnEnable()
    {
        _startButton.onClick.AddListener(OnStartClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
        _exitButton.onClick.AddListener(OnExitClicked);
        _backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        _startButton.onClick.RemoveListener(OnStartClicked);
        _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        _exitButton.onClick.RemoveListener(OnExitClicked);
        _backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (_mainMenuView.IsOpen == false)
        {
            _mainMenuView.Show();
        }

        if (_settingsMenuView.IsOpen)
        {
            _settingsMenuView.Hide();
        }
    }

    private void OnStartClicked()
    {
        SceneManager.LoadScene(_levelSceneName);
    }

    private void OnSettingsClicked()
    {
        if (_mainMenuView.IsAnimating || _settingsMenuView.IsAnimating)
        {
            return;
        }

        if (_mainMenuView.IsOpen)
        {
            _mainMenuView.Hide();
        }

        if (_settingsMenuView.IsOpen == false)
        {
            _settingsMenuView.Show();
        }
    }

    private void OnBackClicked()
    {
        if (_mainMenuView.IsAnimating || _settingsMenuView.IsAnimating)
        {
            return;
        }

        if (_settingsMenuView.IsOpen)
        {
            _settingsMenuView.Hide();
        }

        if (_mainMenuView.IsOpen == false)
        {
            _mainMenuView.Show();
        }
    }

    private void OnExitClicked()
    {
        Application.Quit();
    }

    private void ValidateReference(Object target, string fieldName)
    {
        if (target == null)
        {
            throw new MissingReferenceException(fieldName);
        }
    }
}
