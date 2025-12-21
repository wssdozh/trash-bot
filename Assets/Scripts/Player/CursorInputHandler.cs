using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class CursorInputHandler : MonoBehaviour
{
    [SerializeField] private CursorAnimator _cursorAnimator;
    [SerializeField] private float _holdThresholdSeconds = 0.18f;

    private PlayerInputActions _inputs;

    private bool _isAttackPressed;
    private bool _isHoldConfirmed;
    private float _attackPressedAtTime;

    private void Awake()
    {
        _inputs = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputs.Enable();

        _inputs.Player.Attack.performed += OnAttackPerformed;
        _inputs.Player.Attack.canceled += OnAttackCanceled;

        _inputs.Player.UseItem.performed += OnUseItemPerformed;
        _inputs.Player.Scroll.performed += OnScrollPerformed;
    }

    private void OnDisable()
    {
        _inputs.Player.Attack.performed -= OnAttackPerformed;
        _inputs.Player.Attack.canceled -= OnAttackCanceled;

        _inputs.Player.UseItem.performed -= OnUseItemPerformed;
        _inputs.Player.Scroll.performed -= OnScrollPerformed;

        _inputs.Disable();

        _isAttackPressed = false;
        _isHoldConfirmed = false;

        _cursorAnimator.ResetToBase();
    }

    private void Update()
    {
        if (_isAttackPressed == true && _isHoldConfirmed == false)
        {
            float elapsedTime = Time.unscaledTime - _attackPressedAtTime;
            if (elapsedTime >= _holdThresholdSeconds)
            {
                _isHoldConfirmed = true;
                _cursorAnimator.ConfirmHold();
            }
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext callbackContext)
    {
        _isAttackPressed = true;
        _isHoldConfirmed = false;
        _attackPressedAtTime = Time.unscaledTime;

        _cursorAnimator.BeginHoldCandidate(_holdThresholdSeconds);
    }

    private void OnAttackCanceled(InputAction.CallbackContext callbackContext)
    {
        if (_isHoldConfirmed == true)
        {
            _cursorAnimator.EndHold();
        }
        else
        {
            _cursorAnimator.CancelHoldCandidate();
            _cursorAnimator.PlayClick();
        }

        _isAttackPressed = false;
        _isHoldConfirmed = false;
    }

    private void OnUseItemPerformed(InputAction.CallbackContext callbackContext)
    {
        _cursorAnimator.PlaySecondaryClick();
    }

    private void OnScrollPerformed(InputAction.CallbackContext callbackContext)
    {
        Vector2 scrollValue = callbackContext.ReadValue<Vector2>();
        float direction = scrollValue.y;

        if (direction == 0f)
        {
            direction = scrollValue.x;
        }

        _cursorAnimator.PlayScroll(direction);
    }
}
