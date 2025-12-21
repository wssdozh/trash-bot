using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuNavigation : MonoBehaviour
{
    [SerializeField] private SettingsMenuView _settingsView;
    [SerializeField] private Button _settingsButton;

    private bool _settingsOpen;

    private void OnEnable()
    {
        _settingsOpen = false;
        _settingsButton.onClick.AddListener(OnSettingsClicked);
    }

    private void OnDisable()
    {
        _settingsButton.onClick.RemoveListener(OnSettingsClicked);
    }

    public void CloseSettings()
    {
        if (_settingsOpen == false)
        {
            return;
        }

        if (_settingsView.Animating == true)
        {
            return;
        }

        _settingsOpen = false;
        _settingsView.Hide();
    }

    private void OnSettingsClicked()
    {
        if (_settingsView.Animating == true)
        {
            return;
        }

        if (_settingsOpen == false)
        {
            _settingsOpen = true;
            _settingsView.Show();
            return;
        }

        _settingsOpen = false;
        _settingsView.Hide();
    }
}
