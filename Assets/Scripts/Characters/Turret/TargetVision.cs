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
    [SerializeField] private Transform _currentTarget;
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
        if (_scanCoroutine == null == false)
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
        bool wasTargetVisible = _isTargetVisible;

        EvaluateTargets();

        if (wasTargetVisible == false && _isTargetVisible == true)
        {
            TargetFound?.Invoke();
        }

        if (wasTargetVisible == true && _isTargetVisible == false)
        {
            TargetLost?.Invoke();
        }
    }

    private void EvaluateTargets()
    {
        Vector3 originPosition = GetOriginPosition();

        Collider[] hits = Physics.OverlapSphere(
            originPosition,
            _viewDistance,
            _targetLayerMask
        );

        int hitsCount = hits.Length;

        for (int i = 0; i < hitsCount; i++)
        {
            Collider candidate = hits[i];

            bool isValid = TryValidateTarget(candidate, originPosition);

            if (isValid == true)
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

        if (hasObstacle == true)
        {
            return false;
        }

        _isTargetVisible = true;
        _distanceToTarget = distance;
        _currentTarget = candidateTransform;

        return true;
    }

    private void ResetState()
    {
        _isTargetVisible = false;
        _distanceToTarget = 0f;
        _currentTarget = null;
    }

    private Vector3 GetOriginPosition()
    {
        if (_origin == null == false)
        {
            return _origin.position;
        }

        return transform.position;
    }
}
