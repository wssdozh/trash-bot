using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference _escapeAction;
    [SerializeField] private InputActionReference _clickAction;

    private bool _cursorLocked = true;

    private void OnEnable()
    {
        if (_escapeAction != null)
            _escapeAction.action.performed += OnEscapePressed;

        if (_clickAction != null)
            _clickAction.action.performed += OnClickPressed;

        _escapeAction?.action.Enable();
        _clickAction?.action.Enable();

        LockCursor(true);
    }

    private void OnDisable()
    {
        if (_escapeAction != null)
            _escapeAction.action.performed -= OnEscapePressed;

        if (_clickAction != null)
            _clickAction.action.performed -= OnClickPressed;

        _escapeAction?.action.Disable();
        _clickAction?.action.Disable();

        LockCursor(false);
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        LockCursor(false);
    }

    private void OnClickPressed(InputAction.CallbackContext context)
    {
        if (_cursorLocked == false)
            LockCursor(true);
    }

    public void LockCursor(bool isLocked)
    {
        _cursorLocked = isLocked;

        Cursor.visible = isLocked == false;

        Cursor.lockState = isLocked == true ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
