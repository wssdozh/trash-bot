using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineSightFader : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _target;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _sphereRadius = 0.4f;
    [SerializeField] private float _checkInterval = 0.3f;
    [SerializeField] private float _minDistanceFromCamera = 0.15f;
    [SerializeField] private float _minDistanceToTarget = 0.2f;

    private HashSet<IFadable> _current = new HashSet<IFadable>(32);
    private HashSet<IFadable> _previous = new HashSet<IFadable>(32);
    private RaycastHit[] _hitBuffer = new RaycastHit[64];
    private Coroutine _checkRoutine;

    private void OnEnable()
    {
        _checkRoutine = StartCoroutine(CheckRoutine());
    }

    private void OnDisable()
    {
        if (_checkRoutine != null)
        {
            StopCoroutine(_checkRoutine);
            _checkRoutine = null;
        }
    }

    private IEnumerator CheckRoutine()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(_checkInterval);

        while (true)
        {
            UpdateVisibility();
            
            yield return waitForSeconds;
        }
    }

    private void UpdateVisibility()
    {
        SwapCollections();
        _current.Clear();

        Vector3 origin = _cameraTransform.position;
        Vector3 vectorToTarget = _target.position - origin;
        float distanceToTarget = vectorToTarget.magnitude;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _sphereRadius,
            vectorToTarget.normalized,
            _hitBuffer,
            distanceToTarget,
            _obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            ProcessHit(_hitBuffer[i], distanceToTarget);
        }

        RestoreVisibility();
    }

    private void ProcessHit(RaycastHit hit, float distanceToTarget)
    {
        float hitDistance = hit.distance;

        if (hitDistance < _minDistanceFromCamera) 
            return;
        
        if (distanceToTarget - hitDistance < _minDistanceToTarget) 
            return;

        Transform hitTransform = hit.transform;

        if (hitTransform == _target) 
            return;
        
        if (hitTransform.IsChildOf(_target) == true) 
            return;

        IFadable fadable;
        
        if (hitTransform.TryGetComponent<IFadable>(out fadable) == false) 
            return;

        if (_current.Add(fadable) == false)     
            return;

        fadable.OnOccluded();
    }

    private void RestoreVisibility()
    {
        foreach (IFadable fadable in _previous)
        {
            if (_current.Contains(fadable) == false)
            {
                fadable.OnVisible();
            }
        }
    }

    private void SwapCollections()
    {
        HashSet<IFadable> temporarySet = _previous;
        _previous = _current;
        _current = temporarySet;
    }
}
