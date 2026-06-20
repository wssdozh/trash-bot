using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuNavigation : MonoBehaviour
{
    [SerializeField] private List<ButtonMenu> _buttonMenus;
    [SerializeField] private Button _restartButton;

    private readonly List<PauseMenuButtonListener> _buttonListeners = new List<PauseMenuButtonListener>();

    private void OnEnable()
    {
        _buttonListeners.Clear();

        for (int buttonIndex = 0; buttonIndex < _buttonMenus.Count; buttonIndex++)
        {
            ButtonMenu buttonMenu = _buttonMenus[buttonIndex];
            PauseMenuButtonListener buttonListener = new PauseMenuButtonListener(this, buttonMenu.MenuView);

            _buttonListeners.Add(buttonListener);
            buttonMenu.Button.onClick.AddListener(buttonListener.OnClicked);
        }

        _restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnDisable()
    {
        int buttonCount = Mathf.Min(_buttonMenus.Count, _buttonListeners.Count);

        for (int buttonIndex = 0; buttonIndex < buttonCount; buttonIndex++)
        {
            ButtonMenu buttonMenu = _buttonMenus[buttonIndex];
            PauseMenuButtonListener buttonListener = _buttonListeners[buttonIndex];
            buttonMenu.Button.onClick.RemoveListener(buttonListener.OnClicked);
        }

        _restartButton.onClick.RemoveListener(OnRestartClicked);
        _buttonListeners.Clear();
    }

    internal void ToggleMenu(BaseMenuView menuView)
    {
        if (menuView.IsOpen)
        {
            CloseMenu(menuView);

            return;
        }

        OpenMenu(menuView);
    }

    private void OpenMenu(BaseMenuView menuView)
    {
        if (menuView.IsAnimating)
        {
            return;
        }

        if (menuView.IsOpen == false)
        {
            menuView.Show();

            return;
        }
    }

    private void CloseMenu(BaseMenuView menuView)
    {
        if (menuView.IsAnimating)
        {
            return;
        }

        if (menuView.IsOpen)
        {
            menuView.Hide();

            return;
        }
    }

    private void OnRestartClicked()
    {
        PauseController.Instance.Resume();
        DOTween.KillAll(true);
        SceneLoadingScreen.ReloadCurrentScene();
    }
}
