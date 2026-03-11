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
    private Vector3 _currentTargetPoint;
    private Transform _currentLookTarget;

    public event Action TargetDetected;
    public event Action TargetCleared;

    public bool IsTargetVisible { get; private set; }

    public float DistanceToTarget { get; private set; }
    public Vector3 CurrentTargetPoint { get { return _currentTargetPoint; } }

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
            _targetLayerMask,
            QueryTriggerInteraction.Ignore
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
        Transform hitTransform = GetHitTransform(candidate);
        Transform lookTransform = GetLookTransform(candidate);

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

        Vector3 candidatePoint = candidate.bounds.center;
        Vector3 directionToTarget = candidatePoint - originPosition;
        float distance = directionToTarget.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        directionToTarget.Normalize();

        bool hasObstacle = Physics.Raycast(
            originPosition,
            directionToTarget,
            distance,
            _obstacleLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hasObstacle)
        {
            return false;
        }

        IsTargetVisible = true;
        DistanceToTarget = Vector3.Distance(originPosition, lookTransform.position);
        _currentTarget = hitTransform;
        _currentTargetPoint = lookTransform.position;
        _currentLookTarget = lookTransform;

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

    private Vector3 GetOriginPosition()
    {
        if (_origin != null)
        {
            return _origin.position;
        }

        return transform.position;
    }
}
