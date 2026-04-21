using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Death")]
    [SerializeField, Min(0f)] private float _sinkDelay = 1f;
    [SerializeField, Min(0.01f)] private float _sinkDuration = 7f;
    [SerializeField, Min(0f)] private float _sinkDistance = 2.2f;

    private Vector3 _alertPoint;
    private Collider[] _corpseColliders;
    private Rigidbody[] _corpseRigidbodies;
    private BoxCollider _bodyCollider;
    private NavMeshObstacle _navMeshObstacle;
    private bool _hasAlertPoint;
    private bool _isDead;
    private bool _isSinkStarted;
    private float _alertTimer;
    private Coroutine _fireDelayCoroutine;
    private WaitForSeconds _fireDelayWait;

    public static event Action<Turret> AnyDied;

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

        _bodyCollider = GetComponent<BoxCollider>();

        if (_bodyCollider == null)
        {
            throw new InvalidOperationException(nameof(_bodyCollider));
        }

        if (_sinkDelay < 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDelay));
        }

        if (_sinkDuration <= 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDuration));
        }

        if (_sinkDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDistance));
        }

        if (_fireDelaySeconds > 0f)
        {
            _fireDelayWait = new WaitForSeconds(_fireDelaySeconds);
        }

        EnsureNavMeshObstacle();
        _corpseColliders = GetComponentsInChildren<Collider>(true);
        _corpseRigidbodies = GetComponentsInChildren<Rigidbody>(true);
    }

    private void OnEnable()
    {
        _health.Ended += OnDied;
        _targetVision.TargetDetected += OnTargetFound;
        _targetVision.TargetCleared += OnTargetLost;
        _isDead = false;
        ApplyNavMeshObstacle();
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

        Action<Turret> anyDied = AnyDied;

        if (anyDied != null)
        {
            anyDied.Invoke(this);
        }

        _headCrash.BeginSink(_sinkDelay, _sinkDuration, _sinkDistance);
        StartSink();
    }

    private void StartSink()
    {
        if (_isSinkStarted)
        {
            return;
        }

        _isSinkStarted = true;
        StartCoroutine(SinkCoroutine());
    }

    private IEnumerator SinkCoroutine()
    {
        float delayTimer = 0f;

        while (delayTimer < _sinkDelay)
        {
            delayTimer += Time.deltaTime;

            yield return null;
        }

        DisableCorpsePhysics();

        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + (Vector3.down * _sinkDistance);
        float sinkTimer = 0f;

        while (sinkTimer < _sinkDuration)
        {
            sinkTimer += Time.deltaTime;
            float sinkProgress = Mathf.Clamp01(sinkTimer / _sinkDuration);
            transform.position = Vector3.Lerp(startPoint, endPoint, sinkProgress);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void DisableCorpsePhysics()
    {
        _navMeshObstacle.enabled = false;

        int rigidbodyIndex = 0;

        while (rigidbodyIndex < _corpseRigidbodies.Length)
        {
            Rigidbody corpseRigidbody = _corpseRigidbodies[rigidbodyIndex];

            if (corpseRigidbody != null)
            {
                if (IsHeadPart(corpseRigidbody.transform) == false)
                {
                    corpseRigidbody.linearVelocity = Vector3.zero;
                    corpseRigidbody.angularVelocity = Vector3.zero;
                    corpseRigidbody.useGravity = false;
                    corpseRigidbody.isKinematic = true;
                    corpseRigidbody.detectCollisions = false;
                }
            }

            rigidbodyIndex += 1;
        }

        int colliderIndex = 0;

        while (colliderIndex < _corpseColliders.Length)
        {
            Collider corpseCollider = _corpseColliders[colliderIndex];

            if (corpseCollider != null)
            {
                if (IsHeadPart(corpseCollider.transform) == false)
                {
                    corpseCollider.enabled = false;
                }
            }

            colliderIndex += 1;
        }
    }

    private bool IsHeadPart(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return false;
        }

        return targetTransform.IsChildOf(_headCrash.transform);
    }

    private void EnsureNavMeshObstacle()
    {
        _navMeshObstacle = GetComponent<NavMeshObstacle>();

        if (_navMeshObstacle == null)
        {
            _navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
        }

        ApplyNavMeshObstacle();
    }

    private void ApplyNavMeshObstacle()
    {
        _navMeshObstacle.shape = NavMeshObstacleShape.Box;
        _navMeshObstacle.center = _bodyCollider.center;
        _navMeshObstacle.size = _bodyCollider.size;
        _navMeshObstacle.carving = true;
        _navMeshObstacle.carveOnlyStationary = true;
        _navMeshObstacle.enabled = true;
    }
}
