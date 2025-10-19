using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private CharacterMover _movement;
    [SerializeField] private CharacterRotator _rotator;
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private CursorManager _cursor;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private float _timeBattle = 3f;

    private PlayerInputActions _inputs;

    private bool _isBattle = false;
    private Coroutine _waitCoroutine;
    private Vector2 _moveInput; 

    private void Awake()
    {
        _inputs = new PlayerInputActions();

        _inputs.Player.Move.performed += OnMovePerformed;
        _inputs.Player.Move.canceled += OnMoveCanceled;

        _inputs.Player.Jump.performed += OnJumpPerformed;
        _inputs.Player.Sprint.performed += OnSprintPerformed;
        _inputs.Player.Sprint.canceled += OnSprintCanceled;

        _inputs.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnEnable() => _inputs.Enable();
    private void OnDisable() => _inputs.Disable();

    private void FixedUpdate()
    {
        if (_isBattle)
        {
            _rotator.Rotate();
        }
        else
        {
            _rotator.RotateTowardsMovement(_moveInput);
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (_waitCoroutine != null)
            StopCoroutine(_waitCoroutine);

        _waitCoroutine = StartCoroutine(Wait());
    }

    private IEnumerator Wait()
    {
        _isBattle = true;

        yield return new WaitForSeconds(_timeBattle);

        _isBattle = false;
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
        _movement.OnMove(_moveInput);
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _moveInput = Vector2.zero;
        _movement.OnMove(Vector2.zero);
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        _movement.OnJump();
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        _movement.OnSprint(true);
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        _movement.OnSprint(false);
    }
}
