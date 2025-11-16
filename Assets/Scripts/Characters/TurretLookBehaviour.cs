using System.Collections;
using UnityEngine;

public class TurretLookBehaviour : MonoBehaviour
{
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private Transform _rotationPivot;
    [SerializeField] private float _trackRotationSpeed = 10f;

    [SerializeField] private bool _useIdleRotation = true;
    [SerializeField] private float _idleRotationSpeed = 30f;
    [SerializeField] private float _idleMinAngle = -45f;
    [SerializeField] private float _idleMaxAngle = 45f;

    private Quaternion _baseLocalRotation;
    private Coroutine _trackCoroutine;
    private Coroutine _idleCoroutine;

    private void Awake()
    {
        _baseLocalRotation = _rotationPivot.localRotation;
    }

    private void OnEnable()
    {
        _targetVision.TargetFound += OnTargetFound;
        _targetVision.TargetLost += OnTargetLost;

        if (_useIdleRotation == true)
        {
            StartIdle();
        }
    }

    private void OnDisable()
    {
        _targetVision.TargetFound -= OnTargetFound;
        _targetVision.TargetLost -= OnTargetLost;

        StopTracking();
        StopIdle();
    }

    private void OnTargetFound()
    {
        StopIdle();
        StartTracking();
    }

    private void OnTargetLost()
    {
        StopTracking();

        if (_useIdleRotation == true)
        {
            StartIdle();
        }
    }

    private void StartTracking()
    {
        if (_trackCoroutine != null)
        {
            return;
        }

        _trackCoroutine = StartCoroutine(TrackLoop());
    }

    private void StopTracking()
    {
        if (_trackCoroutine == null)
        {
            return;
        }

        StopCoroutine(_trackCoroutine);
        _trackCoroutine = null;
    }

    private void StartIdle()
    {
        if (_idleCoroutine != null)
        {
            return;
        }

        _idleCoroutine = StartCoroutine(IdleLoop());
    }

    private void StopIdle()
    {
        if (_idleCoroutine == null)
        {
            return;
        }

        StopCoroutine(_idleCoroutine);
        _idleCoroutine = null;
    }

    private IEnumerator TrackLoop()
    {
        while (true)
        {
            Transform currentTarget = _targetVision.CurrentTarget;

            if (currentTarget != null)
            {
                Vector3 directionToTarget = (currentTarget.position - _rotationPivot.position).normalized;

                if (directionToTarget.sqrMagnitude > 0f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    float step = _trackRotationSpeed * Time.deltaTime;

                    _rotationPivot.rotation = Quaternion.Lerp(
                        _rotationPivot.rotation,
                        targetRotation,
                        step
                    );
                }
            }

            yield return null;
        }
    }

    private IEnumerator IdleLoop()
    {
        while (true)
        {
            float range = _idleMaxAngle - _idleMinAngle;

            if (range > 0f)
            {
                float timeValue = Time.time * _idleRotationSpeed;
                float pingPongValue = Mathf.PingPong(timeValue, range);
                float angle = _idleMinAngle + pingPongValue;

                Quaternion idleRotation = Quaternion.Euler(0f, angle, 0f);
                _rotationPivot.localRotation = _baseLocalRotation * idleRotation;
            }

            yield return null;
        }
    }
}
