using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Camera Settings")]
    [SerializeField] private float _distance = 4f;
    [SerializeField] private float _sensitivityX = 2f;
    [SerializeField] private float _sensitivityY = 2f;
    [SerializeField] private float _smoothTime = 0.05f;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 70f;

    [Header("Character Rotation")]
    [SerializeField] private bool _rotateTarget = true;
    [SerializeField] private float _rotationLerpSpeed = 10f;

    private Vector2 _targetRotation;
    private Vector2 _currentRotation;
    private Vector2 _rotationVelocity;

    public void TickCamera(Vector2 lookInput)
    {
        if (_target == null)
            return;

        _targetRotation.y += lookInput.x * _sensitivityX;
        _targetRotation.x -= lookInput.y * _sensitivityY;
        _targetRotation.x = Mathf.Clamp(_targetRotation.x, _minPitch, _maxPitch);

        _currentRotation = Vector2.SmoothDamp(
            _currentRotation,
            _targetRotation,
            ref _rotationVelocity,
            _smoothTime
        );

        Quaternion rotation = Quaternion.Euler(_currentRotation.x, _currentRotation.y, 0f);
        Vector3 position = _target.position - rotation * Vector3.forward * _distance;
        transform.SetPositionAndRotation(position, rotation);

        if (_rotateTarget == true)
        {
            Quaternion targetYawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            _target.rotation = Quaternion.Lerp(
                _target.rotation,
                targetYawRotation,
                Time.deltaTime * _rotationLerpSpeed
            );
        }
    }
}
