using System;
using System.Collections;
using UnityEngine;

public class Turret : MonoBehaviour, IEnemyAlert
{
    private const float AlertTime = 2.5f;
    private const float AlertGap = 1f;

    [Header("Р—Р°РІРёСЃРёРјРѕСЃС‚Рё")]
    [SerializeField] private Health _health;
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private TargetRotator _targetRotator;
    [SerializeField] private IdleRotator _idleRotator;
    [SerializeField] private FireExecutor _fireExecutor;
    [SerializeField] private TurretHeadCrash _headCrash;

    [Header("РќР°СЃС‚СЂРѕР№РєРё")]
    [SerializeField] private float _fireDelaySeconds = 0.25f;

    private Vector3 _alertPoint;
    private bool _hasAlertPoint;
    private bool _isDead;
    private float _alertTimer;
    private Coroutine _fireDelayCoroutine;
    private WaitForSeconds _fireDelayWait;

    public event Action Died;

    public bool IsDead => _isDead;

    public bool ApplyAlert(Vector3 point)
    {
        if (_isDead)
        {
            return false;
        }

        if (_targetVision.IsTargetVisible)
        {
            return false;
        }

        if (_hasAlertPoint)
        {
            Vector3 alertDelta = _alertPoint - point;
            float alertGapSqr = AlertGap * AlertGap;

            if (alertDelta.sqrMagnitude <= alertGapSqr)
            {
                return false;
            }
        }

        _alertPoint = point;
        _hasAlertPoint = true;
        _alertTimer = AlertTime;
        SetAlertState();

        return true;
    }

    private void Awake()
    {
        if (_health == null)
        {
            _health = GetComponent<Health>();
        }

        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_headCrash == null)
        {
            throw new InvalidOperationException(nameof(_headCrash));
        }

        if (_fireDelaySeconds > 0f)
        {
            _fireDelayWait = new WaitForSeconds(_fireDelaySeconds);
        }
    }

    private void OnEnable()
    {
        _health.Ended += OnDied;
        _targetVision.TargetDetected += OnTargetFound;
        _targetVision.TargetCleared += OnTargetLost;
        _isDead = false;
        SetIdleState(true);
    }

    private void OnDisable()
    {
        _health.Ended -= OnDied;
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
        SetIdleState(true);
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        if (_targetVision.IsTargetVisible)
        {
            _hasAlertPoint = false;
            _targetRotator.ClearAimPoint();
            Transform currentTarget = _targetVision.CurrentTarget;

            if (currentTarget == null)
            {
                _fireExecutor.StopFiring();
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

            SetIdleState(false);

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
        SetIdleState(false);
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

    private void SetIdleState(bool isResetNeeded)
    {
        StopFireDelay();

        _hasAlertPoint = false;
        _alertTimer = 0f;
        _fireExecutor.StopFiring();
        _fireExecutor.ClearAimPoint();

        _targetRotator.enabled = false;
        _targetRotator.ClearAimPoint();

        if (isResetNeeded)
        {
            _idleRotator.ResetBaseRotation();
        }

        else
        {
            _idleRotator.CaptureBaseRotation();
        }

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
            yield return _fireDelayWait;
        }

        _fireDelayCoroutine = null;
        _fireExecutor.StartFiring();
    }

    private void OnDied()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        StopFireDelay();
        _hasAlertPoint = false;
        _alertTimer = 0f;
        _fireExecutor.StopFiring();
        _fireExecutor.ClearAimPoint();
        _targetRotator.ClearAimPoint();
        _targetRotator.enabled = false;
        _idleRotator.enabled = false;
        _headCrash.Crash();

        Action died = Died;

        if (died != null)
        {
            died.Invoke();
        }

        enabled = false;
    }
}
