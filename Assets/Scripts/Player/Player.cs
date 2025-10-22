using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private CharacterMover _movement;
    [SerializeField] private CharacterJump _jump;
    [SerializeField] private CharacterRotator _rotator;
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private CursorManager _cursor;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Health _health;
    [SerializeField] private Stamina _stamina;

    [Header("Настройки")]
    [SerializeField] private float _timeBattle = 3f;
    [SerializeField] private float _jumpStaminaCost = 10f;
    [SerializeField] private float _sprintStaminaCostPerSecond = 5f;

    private PlayerInputActions _inputs;
    private bool _isBattle = false;
    private bool _isSprinting = false;
    private Coroutine _waitCoroutine;
    private Coroutine _sprintCoroutine;
    private Vector2 _moveInput;

    public event Action Died;

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

    private void OnEnable() 
    {
        _inputs.Enable();

        _health.Ended += Die;
    }

    private void OnDisable()
    {
        _inputs.Disable();

        _health.Ended -= Die;
    }

    private void FixedUpdate()
    {
        if (_isBattle)
            _rotator.Rotate();
        else
            _rotator.RotateTowardsMovement(_moveInput);
    }

    private void Die()
    {
        Died?.Invoke();
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
        if (_stamina.Value > _jumpStaminaCost)
        {
            _jump.OnJump();
            _stamina.Decrease(_jumpStaminaCost);
        }
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        if (_stamina.Value > 0f && !_isSprinting)
        {
            _isSprinting = true;
            _movement.OnSprint(true);
            _sprintCoroutine = StartCoroutine(SprintConsume());
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        StopSprinting();
    }

    private IEnumerator SprintConsume()
    {
        while (_isSprinting)
        {
            if (_stamina.Value <= _stamina.MinValue)
            {
                StopSprinting();
                yield break;
            }

            _stamina.Decrease(_sprintStaminaCostPerSecond * Time.deltaTime);

            yield return null;
        }
    }

    private void StopSprinting()
    {
        if (_isSprinting == false)
            return;

        _isSprinting = false;
        _movement.OnSprint(false);

        if (_sprintCoroutine != null)
            StopCoroutine(_sprintCoroutine);
    }
}
