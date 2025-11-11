using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSightFader : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _target;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _sphereRadius = 0.4f;
    [SerializeField] private float _checkInterval = 0.3f;

    private readonly List<IFadable> _current = new List<IFadable>(32);
    private readonly List<IFadable> _previous = new List<IFadable>(32);
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
        }
    }

    private IEnumerator CheckRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_checkInterval);
        while (true)
        {
            UpdateVisibility();
            yield return wait;
        }
    }

    private void UpdateVisibility()
    {
        _previous.Clear();
        _previous.AddRange(_current);
        _current.Clear();

        Vector3 origin = _cameraTransform.position;
        Vector3 direction = _target.position - origin;
        float distance = direction.magnitude;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            _sphereRadius,
            direction.normalized,
            distance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        int count = hits.Length;
        for (int i = 0; i < count; i++)
        {
            IFadable fadable;
            if (hits[i].transform.TryGetComponent<IFadable>(out fadable) == true)
            {
                _current.Add(fadable);
                fadable.OnOccluded();
            }
        }

        int prevCount = _previous.Count;
        for (int i = 0; i < prevCount; i++)
        {
            IFadable fadable = _previous[i];
            if (_current.Contains(fadable) == false)
            {
                fadable.OnVisible();
            }
        }
    }
}
