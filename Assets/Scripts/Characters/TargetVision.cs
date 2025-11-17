using System;
using System.Collections;
using UnityEngine;

public class TargetVision : MonoBehaviour
{
    [SerializeField] private Transform _origin;
    [SerializeField] private float _viewDistance = 10f;
    [SerializeField] private float _scanInterval = 0.5f;
    [SerializeField] private LayerMask _targetLayerMask;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private string _targetTag = "Player";

    private bool _isTargetVisible;
    private float _distanceToTarget;
    private Transform _currentTarget;
    private Coroutine _scanCoroutine;

    public event Action TargetFound;
    public event Action TargetLost;

    public bool IsTargetVisible
    {
        get { return _isTargetVisible; }
    }

    public float DistanceToTarget
    {
        get { return _distanceToTarget; }
    }

    public Transform CurrentTarget
    {
        get { return _currentTarget; }
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
        bool previousIsTargetVisible = _isTargetVisible;

        EvaluateTargets();

        if (previousIsTargetVisible == false && _isTargetVisible == true)
        {
            TargetFound?.Invoke();
        }

        if (previousIsTargetVisible == true && _isTargetVisible == false)
        {
            TargetLost?.Invoke();
        }
    }

    private void EvaluateTargets()
    {
        ResetState();

        Vector3 originPosition = GetOriginPosition();

        Collider[] hits = Physics.OverlapSphere(originPosition, _viewDistance, _targetLayerMask);
        int hitsCount = hits.Length;

        for (int i = 0; i < hitsCount; i++)
        {
            Collider candidate = hits[i];

            bool isValidTarget = IsValidTarget(candidate, originPosition);

            if (isValidTarget == true)
            {
                break;
            }
        }
    }

    private void ResetState()
    {
        _isTargetVisible = false;
        _distanceToTarget = 0f;
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

    private bool IsValidTarget(Collider candidate, Vector3 originPosition)
    {
        bool hasValidTag = candidate.CompareTag(_targetTag);

        if (hasValidTag == false)
        {
            return false;
        }

        Transform candidateTransform = candidate.transform;
        Vector3 directionToTarget = (candidateTransform.position - originPosition).normalized;
        float distanceToCandidate = Vector3.Distance(originPosition, candidateTransform.position);

        bool hasLineOfSight = HasLineOfSight(originPosition, directionToTarget, distanceToCandidate, candidateTransform);

        if (hasLineOfSight == false)
        {
            return false;
        }

        _isTargetVisible = true;
        _distanceToTarget = distanceToCandidate;
        _currentTarget = candidateTransform;

        return true;
    }

    private bool HasLineOfSight(Vector3 originPosition, Vector3 directionToTarget, float distanceToCandidate, Transform candidateTransform)
    {
        Ray ray = new Ray(originPosition, directionToTarget);
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, distanceToCandidate, ~0);

        if (hasHit == false)
        {
            return false;
        }

        bool isObstacle = (_obstacleLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0;

        if (isObstacle)
        {
            return false;
        }

        if (hit.transform == candidateTransform)
        {
            return true;
        }

        return false;
    }
}
