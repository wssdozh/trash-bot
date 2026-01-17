using System.Collections;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private CharacterInteractor _interactor;
    [SerializeField] private CharacterMover _movement;
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private CursorManager _cursor;
    [SerializeField] private PlayerCombat _combat;

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

    public void Interact()
    {
        _interactor.TryInteract(_movement.gameObject);
        _animator.TriggerPoint();
    }

    private IEnumerator HoverTickRoutine()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(_hoverTickSeconds);

        while (enabled == true)
        {
            _interactor.TickHover();
            _combat.SetAimPoint(_cursor.MouseHitPos);

            yield return waitForSeconds;
        }
    }
}
