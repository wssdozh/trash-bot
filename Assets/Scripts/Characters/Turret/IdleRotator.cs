using System;
using UnityEngine;

public class IdleRotator : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Transform _rotationPivot;
    [Header("Настройки")]
    [SerializeField] private float _idleRotationSpeed = 30f;
    [SerializeField] private float _idleMinAngle = -45f;
    [SerializeField] private float _idleMaxAngle = 45f;

    private Quaternion _baseLocalRotation;
    private Quaternion _startLocalRotation;
    private float _currentAngle;
    private int _direction = 1;

    private void Awake()
    {
        if (_rotationPivot == null)
        {
            throw new InvalidOperationException(nameof(_rotationPivot));
        }

        _startLocalRotation = _rotationPivot.localRotation;
        _baseLocalRotation = _startLocalRotation;
    }

    public void ResetBaseRotation()
    {
        _baseLocalRotation = _startLocalRotation;
        _rotationPivot.localRotation = _startLocalRotation;
        _currentAngle = 0f;
        _direction = 1;
    }

    public void CaptureBaseRotation()
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
