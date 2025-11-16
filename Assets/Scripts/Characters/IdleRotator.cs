using UnityEngine;

public class IdleRotator : MonoBehaviour
{
    [SerializeField] private Transform _rotationPivot;
    [SerializeField] private float _idleRotationSpeed = 30f;
    [SerializeField] private float _idleMinAngle = -45f;
    [SerializeField] private float _idleMaxAngle = 45f;

    private Quaternion _baseLocalRotation;
    private float _currentAngle;
    private int _direction = 1;

    public void ResetBaseRotation()
    {
        _baseLocalRotation = _rotationPivot.localRotation;
        _currentAngle = 0f;
        _direction = 1;
    }

    private void Update()
    {
        float range = _idleMaxAngle - _idleMinAngle;

        if (range <= 0f)
        {
            return;
        }

        float deltaAngle = _idleRotationSpeed * Time.deltaTime * _direction;
        _currentAngle += deltaAngle;

        if (_currentAngle > _idleMaxAngle)
        {
            _currentAngle = _idleMaxAngle;
            _direction = -1;
        }

        if (_currentAngle < _idleMinAngle)
        {
            _currentAngle = _idleMinAngle;
            _direction = 1;
        }

        Quaternion idleRotation = Quaternion.Euler(0f, _currentAngle, 0f);
        _rotationPivot.localRotation = _baseLocalRotation * idleRotation;
    }
}
