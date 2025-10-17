using UnityEngine;

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
    private float _currentSpeed;
    private float _speedVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public void TickMovement(Vector2 moveInput, bool jumpPressed, bool sprintPressed)
    {
        HandleGrounding();
        ApplyGravity();
        HandleJump(jumpPressed);

        Vector3 moveDirection = CalculateCameraRelativeMove(moveInput, sprintPressed);
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

    private void HandleJump(bool jumpPressed)
    {
        if (jumpPressed == true && _groundedTimer > 0f)
        {
            _groundedTimer = 0f;
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * 2f * _gravityValue);
        }
    }

    private Vector3 CalculateCameraRelativeMove(Vector2 moveInput, bool sprintPressed)
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 cameraForward = _cameraTransform.forward;
        Vector3 cameraRight = _cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        float targetSpeed = sprintPressed == true ? _sprintSpeed : _walkSpeed;
        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, _speedSmoothTime);
        _isSprinting = sprintPressed;

        return (cameraForward.normalized * inputDir.z + cameraRight.normalized * inputDir.x) * _currentSpeed;
    }

    private void MoveCharacter(Vector3 move)
    {
        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);
    }
}
