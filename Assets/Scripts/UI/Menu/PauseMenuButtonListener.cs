public sealed class PauseMenuButtonListener
{
    private readonly PauseMenuNavigation _navigation;
    private readonly BaseMenuView _menuView;

    public PauseMenuButtonListener(PauseMenuNavigation navigation, BaseMenuView menuView)
    {
        _navigation = navigation;
        _menuView = menuView;
    }

    public void OnClicked()
    {
        _navigation.ToggleMenu(_menuView);
    }
}
