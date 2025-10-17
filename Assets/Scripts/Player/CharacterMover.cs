using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CharacterMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 2.0f;
    [SerializeField] private float _sprintSpeed = 4.5f;
    [SerializeField] private float _speedSmoothTime = 0.15f;
    [SerializeField] private float _jumpHeight = 1.0f;
    [SerializeField] private float _gravityValue = 9.81f;

    [Header("State (debug)")]
    [SerializeField] private float _verticalVelocity;
    [SerializeField] private float _groundedTimer;
    [SerializeField] private bool _isSprinting;

    [Header("References")]
    [SerializeField] private Transform _cameraTransform;

    private CharacterController _controller;
    private PlayerInputActions _inputs;

    private Vector2 _moveInput;
    private bool _jumpPressed;
    private bool _sprintPressed;

    private float _currentSpeed;
    private float _speedVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _inputs = new PlayerInputActions();

        // Movement
        _inputs.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputs.Player.Move.canceled += _ => _moveInput = Vector2.zero;

        // Jump
        _inputs.Player.Jump.performed += _ => _jumpPressed = true;

        // Sprint
        _inputs.Player.Sprint.performed += _ => _sprintPressed = true;
        _inputs.Player.Sprint.canceled += _ => _sprintPressed = false;
    }

    private void OnEnable() => _inputs.Enable();
    private void OnDisable() => _inputs.Disable();

    public void TickMovement()
    {
        HandleGrounding();
        ApplyGravity();
        HandleJump();

        Vector3 moveDirection = CalculateCameraRelativeMove();
        MoveCharacter(moveDirection);
    }

    private void HandleGrounding()
    {
        if (_controller.isGrounded == true)
            _groundedTimer = 0.2f;

        if (_groundedTimer > 0f)
            _groundedTimer -= Time.deltaTime;

        if (_controller.isGrounded == true && _verticalVelocity < 0f)
            _verticalVelocity = 0f;
    }

    private void ApplyGravity() => _verticalVelocity -= _gravityValue * Time.deltaTime;

    private void HandleJump()
    {
        if (_jumpPressed == true && _groundedTimer > 0f)
        {
            _jumpPressed = false;
            _groundedTimer = 0f;
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * 2f * _gravityValue);
        }
        else
        {
            _jumpPressed = false;
        }
    }

    private Vector3 CalculateCameraRelativeMove()
    {
        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

        Vector3 cameraForward = _cameraTransform.forward;
        Vector3 cameraRight = _cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // плавное изменение скорости между ходьбой и спринтом
        float targetSpeed = _sprintPressed == true ? _sprintSpeed : _walkSpeed;
        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, _speedSmoothTime);

        _isSprinting = _sprintPressed;

        return (cameraForward.normalized * inputDir.z + cameraRight.normalized * inputDir.x) * _currentSpeed;
    }

    private void MoveCharacter(Vector3 move)
    {
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);
    }
}
