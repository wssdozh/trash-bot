using UnityEngine;

public sealed class ModifierVendingMachineMenuOpener : MonoBehaviour
{
    [SerializeField] private ModifierVendingMachine _vendingMachine;
    [SerializeField] private ModifierVendingMachineMenuView _menuView;

    private void OnEnable()
    {
        _vendingMachine.InteractionRequested += OnInteractionRequested;
    }

    private void OnDisable()
    {
        _vendingMachine.InteractionRequested -= OnInteractionRequested;
    }

    private void OnInteractionRequested(ModifierVendingMachine machine, GameObject buyer)
    {
        _menuView.HandleInteractionRequest(machine, buyer);
    }
}
