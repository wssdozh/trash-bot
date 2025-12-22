using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuNavigation : MonoBehaviour
{
    [SerializeField] private List<ButtonMenu> _buttonMenus;

    private void OnEnable()
    {
        _buttonMenus.ForEach(buttonMenu =>
        {
                buttonMenu.Button.onClick.AddListener(() => ToggleMenu(buttonMenu.MenuView));
        });
    }

    private void OnDisable()
    {
        _buttonMenus.ForEach(buttonMenu =>
        {
            buttonMenu.Button.onClick.RemoveListener(() => ToggleMenu(buttonMenu.MenuView));
        });
    }

    private void ToggleMenu(BaseMenuView menuView)
    {
        if (menuView.IsOpen == true)
        {
            CloseMenu(menuView);

            return;
        }

        OpenMenu(menuView);
    }

    private void OpenMenu(BaseMenuView menuView)
    {
        if (menuView.IsAnimating == true)
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
        if (menuView.IsAnimating == true)
        {
            return;
        }

        if (menuView.IsOpen == true)
        {
            menuView.Hide();

            return;
        }
    }
}
