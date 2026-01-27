using System.Collections;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private TargetRotator _targetRotator;
    [SerializeField] private IdleRotator _idleRotator;
    [SerializeField] private FireExecutor _fireExecutor;

    [Header("Настройки")]
    [SerializeField] private float _fireDelaySeconds = 0.25f;

    private Coroutine _fireDelayCoroutine;

    private void OnEnable()
    {
        _targetVision.TargetFound += OnTargetFound;
        _targetVision.TargetLost += OnTargetLost;
    }

    private void OnDisable()
    {
        _targetVision.TargetFound -= OnTargetFound;
        _targetVision.TargetLost -= OnTargetLost;

        StopFireDelay();
    }

    private void Start()
    {
        SetIdleState();
    }

    private void OnTargetFound()
    {
        SetTrackingState();
    }

    private void OnTargetLost()
    {
        SetIdleState();
    }

    private void SetTrackingState()
    {
        _idleRotator.enabled = false;
        _targetRotator.enabled = true;

        _fireExecutor.StopFiring();

        StopFireDelay();

        _fireDelayCoroutine = StartCoroutine(FireWithDelayCoroutine());
    }

    private void SetIdleState()
    {
        StopFireDelay();

        _fireExecutor.StopFiring();
        _targetRotator.enabled = false;
        _idleRotator.ResetBaseRotation();
        _idleRotator.enabled = true;
    }

    private void StopFireDelay()
    {

        if (_fireDelayCoroutine == null)
        {
            return;
        }

        StopCoroutine(_fireDelayCoroutine);
        _fireDelayCoroutine = null;
    }

    private IEnumerator FireWithDelayCoroutine()
    {

        if (_fireDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(_fireDelaySeconds);
        }

        _fireDelayCoroutine = null;
        _fireExecutor.StartFiring();
    }
}
