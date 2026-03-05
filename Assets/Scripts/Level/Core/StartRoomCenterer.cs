using System;
using UnityEngine;

public sealed class StartRoomCenterer : MonoBehaviour
{
    [SerializeField] private LevelGenerator _levelGenerator;
    [SerializeField] private Transform _playerRoot;
    [SerializeField] private Transform _playerBody;
    [SerializeField] private float _heightOffset = 0.35f;

    private Rigidbody _playerBodyRigidbody;

    private void Awake()
    {
        if (_levelGenerator == null)
            throw new InvalidOperationException(nameof(_levelGenerator));

        if (_playerRoot == null)
            throw new InvalidOperationException(nameof(_playerRoot));

        if (_playerBody == null)
            throw new InvalidOperationException(nameof(_playerBody));

        _playerBodyRigidbody = _playerBody.GetComponent<Rigidbody>();

        if (_playerBodyRigidbody == null)
            throw new InvalidOperationException(nameof(_playerBodyRigidbody));
    }

    private void OnEnable()
    {
        _levelGenerator.GenerationCompleted += OnGenerationCompleted;

        if (_levelGenerator.HasGeneratedLevel)
            OnGenerationCompleted();
    }

    private void OnDisable()
    {
        _levelGenerator.GenerationCompleted -= OnGenerationCompleted;
    }

    private void OnGenerationCompleted()
    {
        Vector3 startRoomCenterPosition = _levelGenerator.GetStartRoomCenter();
        startRoomCenterPosition.y += _heightOffset;

        Vector3 rootToBodyOffset = _playerRoot.position - _playerBody.position;
        Vector3 targetRootPosition = startRoomCenterPosition + rootToBodyOffset;

        _playerRoot.position = targetRootPosition;

        _playerBodyRigidbody.linearVelocity = Vector3.zero;
        _playerBodyRigidbody.angularVelocity = Vector3.zero;
        _playerBodyRigidbody.position = startRoomCenterPosition;
    }
}
