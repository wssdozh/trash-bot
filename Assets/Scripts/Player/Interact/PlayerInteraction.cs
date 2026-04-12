using System.Collections;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private CharacterInteractor _interactor;
    [SerializeField] private CharacterMover _movement;
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private CursorManager _cursor;
    [SerializeField] private PlayerCombat _combat;
    [SerializeField] private ModifierVendingMachineMenuView _modifierVendingMachineMenuView;
    [SerializeField] private PlayerModifierMenuView _playerModifierMenuView;

    [Header("Настройки")]
    [SerializeField] private float _hoverTickSeconds = 0.15f;

    private Coroutine _hoverCoroutine;

    private void OnEnable()
    {
        _hoverCoroutine = StartCoroutine(HoverTickRoutine());
    }

    private void OnDisable()
    {
        if (_hoverCoroutine != null)
        {
            StopCoroutine(_hoverCoroutine);
            _hoverCoroutine = null;
        }
    }

    private void Update()
    {
        Vector3 aimPoint = GetAimPoint();

        _combat.SetAimPoint(aimPoint);
    }

    public void Interact()
    {
        if (_playerModifierMenuView != null)
        {
            if (_playerModifierMenuView.TryClose())
            {
                return;
            }
        }

        if (_modifierVendingMachineMenuView != null)
        {
            if (_modifierVendingMachineMenuView.TryClose())
            {
                return;
            }
        }

        _interactor.TryInteract(_movement.gameObject);
        _animator.TriggerPoint();
    }

    private Vector3 GetAimPoint()
    {
        if (_cursor.HasHit)
        {
            return _cursor.MouseHitPos;
        }

        return _cursor.MouseWorldPos;
    }

    private IEnumerator HoverTickRoutine()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(_hoverTickSeconds);

        while (enabled)
        {
            _interactor.TickHover();

            yield return waitForSeconds;
        }
    }
}
