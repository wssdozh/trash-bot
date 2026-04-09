using System;
using System.Collections;
using UnityEngine;

public class TargetVision : MonoBehaviour
{
    private const int TargetsBufferSize = 32;
    private const float MinScanInterval = 0.02f;

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
    private WaitForSeconds _scanDelay;
    private readonly Collider[] _targetsBuffer = new Collider[TargetsBufferSize];
    private Vector3 _currentTargetPoint;
    private Transform _currentLookTarget;

    public event Action TargetDetected;
    public event Action TargetCleared;

    public bool IsTargetVisible { get; private set; }

    public float DistanceToTarget { get; private set; }
    public Vector3 CurrentTargetPoint { get { return _currentTargetPoint; } }

    public Transform CurrentTarget { get { return _currentTarget; } }

    private void Awake()
    {
        _scanDelay = new WaitForSeconds(GetScanInterval());
    }

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

    private void Update()
    {
        RefreshTargetPoint();
    }

    public void Refresh()
    {
        Scan();
    }

    private IEnumerator ScanLoop()
    {
        while (enabled)
        {
            Scan();

            yield return _scanDelay;
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
        Transform bestTarget = null;
        Transform bestLookTarget = null;
        Vector3 bestPoint = Vector3.zero;
        float bestDistance = float.MaxValue;

        int hitsCount = Physics.OverlapSphereNonAlloc(
            originPosition,
            _viewDistance,
            _targetsBuffer,
            _targetLayerMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitsCount; i++)
        {
            Collider candidate = _targetsBuffer[i];
            Transform hitTransform;
            Transform lookTransform;
            Vector3 targetPoint;
            float distance;

            if (TryValidateTarget(candidate, originPosition, out hitTransform, out lookTransform, out targetPoint, out distance))
            {
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = hitTransform;
                    bestLookTarget = lookTransform;
                    bestPoint = targetPoint;
                }
            }
        }

        if (bestTarget == null)
        {
            ResetState();

            return;
        }

        IsTargetVisible = true;
        DistanceToTarget = bestDistance;
        _currentTarget = bestTarget;
        _currentTargetPoint = bestPoint;
        _currentLookTarget = bestLookTarget;
    }

    private bool TryValidateTarget(Collider candidate, Vector3 originPosition, out Transform hitTransform, out Transform lookTransform, out Vector3 targetPoint, out float distance)
    {
        hitTransform = GetHitTransform(candidate);
        lookTransform = GetLookTransform(candidate);
        targetPoint = Vector3.zero;
        distance = 0f;

        if (hitTransform == null)
        {
            return false;
        }

        if (lookTransform == null)
        {
            return false;
        }

        if (lookTransform.CompareTag(_targetTag) == false)
        {
            return false;
        }

        if (TryGetVisiblePoint(candidate, lookTransform, originPosition, out targetPoint) == false)
        {
            return false;
        }

        distance = Vector3.Distance(originPosition, lookTransform.position);

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        return true;
    }

    private Transform GetHitTransform(Collider candidate)
    {
        Rigidbody targetRigidbody = candidate.attachedRigidbody;

        if (targetRigidbody != null)
        {
            return targetRigidbody.transform;
        }

        return candidate.transform;
    }

    private Transform GetLookTransform(Collider candidate)
    {
        Health targetHealth = candidate.GetComponentInParent<Health>();

        if (targetHealth != null)
        {
            return targetHealth.transform;
        }

        return GetHitTransform(candidate);
    }

    private void ResetState()
    {
        IsTargetVisible = false;
        DistanceToTarget = 0f;
        _currentTargetPoint = Vector3.zero;
        _currentTarget = null;
        _currentLookTarget = null;
    }

    private void RefreshTargetPoint()
    {
        if (IsTargetVisible == false)
        {
            return;
        }

        if (_currentLookTarget == null)
        {
            return;
        }

        Vector3 originPosition = GetOriginPosition();
        DistanceToTarget = Vector3.Distance(originPosition, _currentLookTarget.position);
        _currentTargetPoint = _currentLookTarget.position;
    }

    private bool TryGetVisiblePoint(Collider candidate, Transform lookTransform, Vector3 originPosition, out Vector3 visiblePoint)
    {
        Vector3 lookPoint = lookTransform.position;

        if (HasLineOfSight(originPosition, lookPoint))
        {
            visiblePoint = lookPoint;

            return true;
        }

        Vector3 closestPoint = candidate.ClosestPoint(originPosition);

        if (HasLineOfSight(originPosition, closestPoint))
        {
            visiblePoint = closestPoint;

            return true;
        }

        Bounds targetBounds = candidate.bounds;
        Vector3 centerPoint = targetBounds.center;

        if (HasLineOfSight(originPosition, centerPoint))
        {
            visiblePoint = centerPoint;

            return true;
        }

        Vector3 topPoint = new Vector3(targetBounds.center.x, targetBounds.max.y, targetBounds.center.z);

        if (HasLineOfSight(originPosition, topPoint))
        {
            visiblePoint = topPoint;

            return true;
        }

        visiblePoint = Vector3.zero;

        return false;
    }

    private bool HasLineOfSight(Vector3 originPosition, Vector3 targetPoint)
    {
        Vector3 directionToTarget = targetPoint - originPosition;
        float distance = directionToTarget.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        directionToTarget /= distance;

        return Physics.Raycast(
            originPosition,
            directionToTarget,
            distance,
            _obstacleLayerMask,
            QueryTriggerInteraction.Ignore
        ) == false;
    }

    private float GetScanInterval()
    {
        return Mathf.Max(MinScanInterval, _scanInterval);
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
