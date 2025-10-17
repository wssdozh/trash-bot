using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMover _characterMover;
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private Cursor _cursor;

    private PlayerInputActions _inputs;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpPressed;
    private bool _sprintPressed;

    private void Awake()
    {
        _inputs = new PlayerInputActions();

        _inputs.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputs.Player.Move.canceled += _ => _moveInput = Vector2.zero;

        _inputs.Player.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        _inputs.Player.Look.canceled += _ => _lookInput = Vector2.zero;

        _inputs.Player.Jump.performed += _ => _jumpPressed = true;
        _inputs.Player.Sprint.performed += _ => _sprintPressed = true;
        _inputs.Player.Sprint.canceled += _ => _sprintPressed = false;

        _inputs.UI.Cancel.performed += _ => ToggleCursorLock(false);
        _inputs.UI.Click.performed += _ =>
        {
            if (_cursor != null && _cursor.IsCursorLocked() == false)
                ToggleCursorLock(true);
        };
    }

    private void OnEnable()
    {
        _inputs.Enable();
        ToggleCursorLock(true);
    }

    private void OnDisable()
    {
        _inputs.Disable();
        ToggleCursorLock(false);
    }

    private void Update()
    {
        if (_characterMover != null)
        {
            _characterMover.TickMovement(_moveInput, _jumpPressed, _sprintPressed);
            _jumpPressed = false;
        }
    }

    private void LateUpdate()
    {
        if (_cameraMover != null && _cursor != null && _cursor.IsCursorLocked() == true)
            _cameraMover.TickCamera(_lookInput);
    }

    private void ToggleCursorLock(bool isLocked)
    {
        if (_cursor != null)
            _cursor.LockCursor(isLocked);
    }
}
