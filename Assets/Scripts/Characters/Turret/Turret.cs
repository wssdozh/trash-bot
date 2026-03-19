using System.Collections;
using UnityEngine;

public class Turret : MonoBehaviour, IEnemyAlert
{
    private const float AlertTime = 2.5f;

    [Header("Зависимости")]
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private TargetRotator _targetRotator;
    [SerializeField] private IdleRotator _idleRotator;
    [SerializeField] private FireExecutor _fireExecutor;

    [Header("Настройки")]
    [SerializeField] private float _fireDelaySeconds = 0.25f;

    private Vector3 _alertPoint;
    private bool _hasAlertPoint;
    private float _alertTimer;
    private Coroutine _fireDelayCoroutine;

    public void ApplyAlert(Vector3 point)
    {
        if (_targetVision.IsTargetVisible)
        {
            return;
        }

        _alertPoint = point;
        _hasAlertPoint = true;
        _alertTimer = AlertTime;
        SetAlertState();
    }

    private void OnEnable()
    {
        _targetVision.TargetDetected += OnTargetFound;
        _targetVision.TargetCleared += OnTargetLost;
        SetIdleState();
    }

    private void OnDisable()
    {
        _targetVision.TargetDetected -= OnTargetFound;
        _targetVision.TargetCleared -= OnTargetLost;

        _hasAlertPoint = false;
        _alertTimer = 0f;
        _fireExecutor.StopFiring();
        _fireExecutor.ClearAimPoint();
        _targetRotator.ClearAimPoint();
        StopFireDelay();
    }

    private void Start()
    {
        SetIdleState();
    }

    private void Update()
    {
        if (_targetVision.IsTargetVisible)
        {
            _hasAlertPoint = false;
            _targetRotator.ClearAimPoint();
            Transform currentTarget = _targetVision.CurrentTarget;

            if (currentTarget == null)
            {
                _fireExecutor.ClearAimPoint();

                return;
            }

            _fireExecutor.SetAimPoint(_targetVision.CurrentTargetPoint);

            return;
        }

        if (_hasAlertPoint)
        {
            _alertTimer -= Time.deltaTime;
            _targetRotator.SetAimPoint(_alertPoint);
            _fireExecutor.StopFiring();
            _fireExecutor.SetAimPoint(_alertPoint);

            if (_alertTimer > 0f)
            {
                return;
            }

            SetIdleState();

            return;
        }

        if (_targetRotator.enabled == false)
        {
            _fireExecutor.ClearAimPoint();

            return;
        }

        _fireExecutor.ClearAimPoint();
    }

    private void OnTargetFound()
    {
        _hasAlertPoint = false;
        _alertTimer = 0f;
        _targetRotator.ClearAimPoint();
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
        _targetRotator.ClearAimPoint();

        _fireExecutor.StopFiring();

        StopFireDelay();

        _fireDelayCoroutine = StartCoroutine(FireWithDelayCoroutine());
    }

    private void SetIdleState()
    {
        StopFireDelay();

        _hasAlertPoint = false;
        _alertTimer = 0f;
        _fireExecutor.StopFiring();
        _fireExecutor.ClearAimPoint();

        _targetRotator.enabled = false;
        _targetRotator.ClearAimPoint();
        _idleRotator.ResetBaseRotation();
        _idleRotator.enabled = true;
    }

    private void SetAlertState()
    {
        StopFireDelay();

        _fireExecutor.StopFiring();
        _targetRotator.SetAimPoint(_alertPoint);
        _idleRotator.enabled = false;
        _targetRotator.enabled = true;
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
