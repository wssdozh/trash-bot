using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMover : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Camera Settings")]
    [SerializeField] private float _distance = 4f;
    [SerializeField] private float _sensitivity = 2f;
    [SerializeField] private float _smoothTime = 0.05f;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 70f;

    [Header("Character Rotation")]
    [SerializeField] private bool _rotateTarget = true;
    [SerializeField] private float _rotationLerpSpeed = 10f;

    [Header("Input")]
    [SerializeField] private InputActionReference _lookAction;

    private Vector2 _targetRotation;
    private Vector2 _currentRotation;
    private Vector2 _rotationVelocity;

    private void OnEnable() => _lookAction.action.Enable();
    private void OnDisable() => _lookAction.action.Disable();

    public void TickCamera()
    {
        if (_target == null)
            return;

        HandleLookInput();
        UpdateCameraRotation();
        UpdateCameraPosition();
        RotateCharacterToCamera();
    }

    private void HandleLookInput()
    {
        Vector2 lookInput = _lookAction.action.ReadValue<Vector2>() * _sensitivity;

        _targetRotation.y += lookInput.x;
        _targetRotation.x -= lookInput.y;
        _targetRotation.x = Mathf.Clamp(_targetRotation.x, _minPitch, _maxPitch);
    }

    private void UpdateCameraRotation()
    {
        _currentRotation = Vector2.SmoothDamp(
            _currentRotation,
            _targetRotation,
            ref _rotationVelocity,
            _smoothTime
        );
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(_currentRotation.x, _currentRotation.y, 0f);
        Vector3 position = _target.position - rotation * Vector3.forward * _distance;
        transform.SetPositionAndRotation(position, rotation);
    }

    private void RotateCharacterToCamera()
    {
        if (!_rotateTarget)
            return;

        Quaternion targetYawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        _target.rotation = Quaternion.Lerp(
            _target.rotation,
            targetYawRotation,
            Time.deltaTime * _rotationLerpSpeed
        );
    }
}
