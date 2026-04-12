using UnityEngine;

public sealed class PlayerModifierMenuOpener : MonoBehaviour
{
    [SerializeField] private PlayerModifierShop _shop;
    [SerializeField] private PlayerModifierMenuView _menuView;

    private void OnEnable()
    {
        _shop.InteractionRequested += OnInteractionRequested;
    }

    private void OnDisable()
    {
        _shop.InteractionRequested -= OnInteractionRequested;
    }

    private void OnInteractionRequested(PlayerModifierShop shop, GameObject buyer)
    {
        _menuView.HandleInteractionRequest(shop, buyer);
    }
}
