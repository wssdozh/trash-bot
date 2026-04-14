using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class EnemyAimCollider : MonoBehaviour
{
    [SerializeField] private Collider _collider;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private Turret _turret;

    public Vector3 AimPoint => _collider.bounds.center;

    private void Awake()
    {
        if (_collider == null)
        {
            throw new InvalidOperationException(nameof(_collider));
        }

        if (_enemy == null && _turret == null)
        {
            throw new InvalidOperationException(nameof(_enemy));
        }
    }

    private void OnEnable()
    {
        _collider.enabled = true;

        if (_enemy != null)
        {
            _enemy.Died += OnDied;
        }

        if (_turret != null)
        {
            _turret.Died += OnDied;
        }
    }

    private void OnDisable()
    {
        if (_enemy != null)
        {
            _enemy.Died -= OnDied;
        }

        if (_turret != null)
        {
            _turret.Died -= OnDied;
        }
    }

    private void OnDied()
    {
        _collider.enabled = false;
    }
}
