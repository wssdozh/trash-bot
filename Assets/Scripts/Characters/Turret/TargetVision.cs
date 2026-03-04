using System;
using System.Collections;
using UnityEngine;

public class TargetVision : MonoBehaviour
{
    private const int TargetsBufferSize = 32;

    [Header("Зависимости")]
    [SerializeField] private Transform _origin;
    [SerializeField] private Transform _currentTarget;
    [Header("Настройки")]
    [SerializeField] private float _viewDistance = 10f;
    [SerializeField] private float _scanInterval = 0.5f;
    [SerializeField] private LayerMask _targetLayerMask;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private string _targetTag = "Player";

    private Coroutine _scanCoroutine;
    private readonly Collider[] _targetsBuffer = new Collider[TargetsBufferSize];

    public event Action TargetDetected;
    public event Action TargetCleared;

    public bool IsTargetVisible { get; private set; }

    public float DistanceToTarget { get; private set; }

    public Transform CurrentTarget { get { return _currentTarget; } }

    private void OnEnable()
    {
        _scanCoroutine = StartCoroutine(ScanLoop());
    }

    private void OnDisable()
    {
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }
    }

    private IEnumerator ScanLoop()
    {
        while (enabled)
        {
            Scan();

            yield return new WaitForSeconds(_scanInterval);
        }
    }

    private void Scan()
    {
        bool wasTargetVisible = IsTargetVisible;

        EvaluateTargets();

        if (wasTargetVisible == false && IsTargetVisible)
        {
            TargetDetected?.Invoke();
        }

        if (wasTargetVisible && IsTargetVisible == false)
        {
            TargetCleared?.Invoke();
        }
    }

    private void EvaluateTargets()
    {
        Vector3 originPosition = GetOriginPosition();

        int hitsCount = Physics.OverlapSphereNonAlloc(
            originPosition,
            _viewDistance,
            _targetsBuffer,
            _targetLayerMask
        );

        for (int i = 0; i < hitsCount; i++)
        {
            Collider candidate = _targetsBuffer[i];

            bool isValid = TryValidateTarget(candidate, originPosition);

            if (isValid)
            {
                return;
            }
        }

        ResetState();
    }

    private bool TryValidateTarget(Collider candidate, Vector3 originPosition)
    {
        if (candidate.CompareTag(_targetTag) == false)
        {
            return false;
        }

        Transform candidateTransform = candidate.transform;
        Vector3 directionToTarget = candidateTransform.position - originPosition;
        float distance = directionToTarget.magnitude;

        directionToTarget.Normalize();

        bool hasObstacle = Physics.Raycast(
            originPosition,
            directionToTarget,
            distance,
            _obstacleLayerMask
        );

        if (hasObstacle)
        {
            return false;
        }

        IsTargetVisible = true;
        DistanceToTarget = distance;
        _currentTarget = candidateTransform;

        return true;
    }

    private void ResetState()
    {
        IsTargetVisible = false;
        DistanceToTarget = 0f;
        _currentTarget = null;
    }

    private Vector3 GetOriginPosition()
    {
        if (_origin != null)
        {
            return _origin.position;
        }

        return transform.position;
    }
}
