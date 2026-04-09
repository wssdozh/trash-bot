using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDroneBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert
{
    private const float AlertDelay = 3f;
    private const float AlertGap = 2.25f;
    private const int IdlePointTryCount = 10;
    private const float ZeroThreshold = 0.0001f;
    private const float OrbitTurnAngle = 90f;

    private readonly EnemyPatrolPicker _idlePatrolPicker = new EnemyPatrolPicker();

    [Header("Dependencies")]
    [SerializeField] private Enemy _enemy;
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private EnemyDroneMove _enemyMove;
    [SerializeField] private EnemyDroneCrash _enemyCrash;
    [SerializeField] private TargetRotator _targetRotator;
    [SerializeField] private IdleRotator _idleRotator;
    [SerializeField] private FireExecutor _fireExecutor;

    [Header("Fight")]
    [SerializeField] private float _fightMinDistance = 2.9f;
    [SerializeField] private float _fightMaxDistance = 4.7f;
    [SerializeField] private float _pursuitDistance = 3.15f;
    [SerializeField] private float _fireDistance = 8.5f;
    [SerializeField] private float _strafeDistance = 0.7f;
    [SerializeField] private float _strafeTimeMin = 1f;
    [SerializeField] private float _strafeTimeMax = 1.45f;
    [SerializeField] private float _strafeWaitMin = 0.35f;
    [SerializeField] private float _strafeWaitMax = 0.7f;

    [Header("Search")]
    [SerializeField] private float _searchTime = 4.5f;
    [SerializeField] private float _searchReachDistance = 0.35f;

    [Header("Idle")]
    [SerializeField] private float _idleRadius = 4.6f;
    [SerializeField] private float _idleWaitMin = 0.12f;
    [SerializeField] private float _idleWaitMax = 0.28f;
    [SerializeField] private float _idleReachDistance = 0.5f;

    private System.Random _random;
    private EnemyRoomLock _enemyRoomLock;
    private EnemyState _state;
    private Vector3 _spawnPoint;
    private Vector3 _lastSeenPoint;
    private Vector3 _idlePoint;
    private bool _hasLastSeenPoint;
    private bool _hasIdlePoint;
    private float _searchTimer;
    private float _idleTimer;
    private float _alertTimer;
    private float _strafeTimer;
    private bool _isStrafeMove;
    private int _strafeDirection = 1;

    public EnemyState State => _state;

    public bool ApplyAlert(Vector3 point)
    {
        if (_enemy.IsDead)
        {
            return false;
        }

        if (_targetVision.IsTargetVisible)
        {
            return false;
        }

        Vector3 alertPoint = ClampPoint(point);
        alertPoint.y = _spawnPoint.y;

        if (_hasLastSeenPoint)
        {
            if (GetFlatDistance(_lastSeenPoint, alertPoint) <= AlertGap)
            {
                return false;
            }
        }

        _lastSeenPoint = alertPoint;
        _hasLastSeenPoint = true;
        _searchTimer = _searchTime;
        _idleTimer = 0f;
        _hasIdlePoint = false;
        _alertTimer = 0f;
        ResetStrafe();
        ApplyTrackMode();

        return true;
    }

    private void Awake()
    {
        if (_enemy == null)
        {
            throw new InvalidOperationException(nameof(_enemy));
        }

        if (_targetVision == null)
        {
            throw new InvalidOperationException(nameof(_targetVision));
        }

        if (_enemyMove == null)
        {
            throw new InvalidOperationException(nameof(_enemyMove));
        }

        if (_enemyCrash == null)
        {
            throw new InvalidOperationException(nameof(_enemyCrash));
        }

        if (_targetRotator == null)
        {
            throw new InvalidOperationException(nameof(_targetRotator));
        }

        if (_idleRotator == null)
        {
            throw new InvalidOperationException(nameof(_idleRotator));
        }

        if (_fireExecutor == null)
        {
            throw new InvalidOperationException(nameof(_fireExecutor));
        }

        if (_fightMinDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_fightMinDistance));
        }

        if (_fightMaxDistance < _fightMinDistance)
        {
            throw new InvalidOperationException(nameof(_fightMaxDistance));
        }

        if (_fireDistance < _fightMaxDistance)
        {
            throw new InvalidOperationException(nameof(_fireDistance));
        }

        if (_pursuitDistance < _fightMinDistance)
        {
            throw new InvalidOperationException(nameof(_pursuitDistance));
        }

        if (_pursuitDistance > _fightMaxDistance)
        {
            throw new InvalidOperationException(nameof(_pursuitDistance));
        }

        if (_strafeDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_strafeDistance));
        }

        if (_strafeTimeMin <= 0f)
        {
            throw new InvalidOperationException(nameof(_strafeTimeMin));
        }

        if (_strafeTimeMax < _strafeTimeMin)
        {
            throw new InvalidOperationException(nameof(_strafeTimeMax));
        }

        if (_searchTime < 0f)
        {
            throw new InvalidOperationException(nameof(_searchTime));
        }

        if (_strafeWaitMin < 0f)
        {
            throw new InvalidOperationException(nameof(_strafeWaitMin));
        }

        if (_strafeWaitMax < _strafeWaitMin)
        {
            throw new InvalidOperationException(nameof(_strafeWaitMax));
        }

        if (_searchReachDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_searchReachDistance));
        }

        if (_idleRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_idleRadius));
        }

        if (_idleWaitMin < 0f)
        {
            throw new InvalidOperationException(nameof(_idleWaitMin));
        }

        if (_idleWaitMax < _idleWaitMin)
        {
            throw new InvalidOperationException(nameof(_idleWaitMax));
        }

        if (_idleReachDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_idleReachDistance));
        }

        _random = new System.Random(GetInstanceID());
    }

    private void OnEnable()
    {
        _enemy.Died += OnEnemyDied;
        _targetVision.TargetDetected += OnTargetFound;
        _targetVision.TargetCleared += OnTargetLost;

        _alertTimer = 0f;
        _idlePatrolPicker.Clear();
        ApplyIdleMode();
        _state = EnemyState.Idle;
    }

    private void Start()
    {
        _enemyMove.ResetAnchor();
        _spawnPoint = _enemyMove.GetAnchorPoint();
    }

    private void OnDisable()
    {
        _enemy.Died -= OnEnemyDied;
        _targetVision.TargetDetected -= OnTargetFound;
        _targetVision.TargetCleared -= OnTargetLost;

        _alertTimer = 0f;
        StopAll();
    }

    private void Update()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        if (_targetVision.IsTargetVisible)
        {
            UpdateFight();

            return;
        }

        if (_hasLastSeenPoint)
        {
            UpdateSearch();

            return;
        }

        UpdateIdle();
    }

    private void UpdateFight()
    {
        Vector3 targetPoint = _targetVision.CurrentTargetPoint;
        _lastSeenPoint = targetPoint;
        _hasLastSeenPoint = true;
        _searchTimer = _searchTime;

        ApplyTrackMode();
        TickAlert(targetPoint);

        float targetDistance = GetFlatDistance(transform.position, targetPoint);

        if (targetDistance > _fightMaxDistance)
        {
            _state = EnemyState.Chase;
            ResetStrafe();
            _enemyMove.SetMovePoint(ClampPoint(targetPoint));
        }
        else
        {
            if (targetDistance < _fightMinDistance)
            {
                _state = EnemyState.Fight;
                ResetStrafe();
                MoveToStand(targetPoint, _pursuitDistance);
            }
            else
            {
                _state = EnemyState.Fight;
                MoveAroundTarget(targetPoint);
            }
        }

        _fireExecutor.SetAimPoint(targetPoint);

        if (targetDistance <= _fireDistance)
        {
            if (_fireExecutor.IsAimReady())
            {
                _fireExecutor.StartFiring();
            }
            else
            {
                _fireExecutor.StopFiring();
            }
        }
        else
        {
            _fireExecutor.StopFiring();
        }
    }

    private void UpdateSearch()
    {
        ApplyTrackMode();
        _fireExecutor.StopFiring();
        _fireExecutor.ClearAimPoint();

        if (_searchTimer <= 0f)
        {
            ClearSearch();

            return;
        }

        _state = EnemyState.Search;
        _searchTimer -= Time.deltaTime;

        Vector3 searchPoint = ClampPoint(_lastSeenPoint);
        searchPoint.y = _spawnPoint.y;
        _targetRotator.SetAimPoint(searchPoint);
        _enemyMove.SetMovePoint(searchPoint);

        if (GetFlatDistance(transform.position, searchPoint) <= _searchReachDistance)
        {
            ClearSearch();
        }
    }

    private void UpdateIdle()
    {
        ApplyIdleMode();
        StopFire();

        if (_idleTimer > 0f)
        {
            _state = EnemyState.Idle;
            _idleTimer -= Time.deltaTime;
            _enemyMove.StopMove();

            return;
        }

        if (_hasIdlePoint == false)
        {
            PickIdlePoint();
        }

        if (_hasIdlePoint == false)
        {
            _state = EnemyState.Idle;
            _enemyMove.StopMove();

            return;
        }

        _state = EnemyState.Patrol;
        _enemyMove.SetMovePoint(_idlePoint);

        if (GetFlatDistance(transform.position, _idlePoint) <= _idleReachDistance)
        {
            _hasIdlePoint = false;
            _idleTimer = GetRandomValue(_idleWaitMin, _idleWaitMax);
            _enemyMove.StopMove();
        }
    }

    private void MoveToStand(Vector3 targetPoint, float standDistance)
    {
        Vector3 awayDirection = GetFlatDirection(transform.position - targetPoint);

        if (awayDirection.sqrMagnitude <= ZeroThreshold)
        {
            awayDirection = GetFlatDirection(transform.forward);
        }

        Vector3 standPoint = targetPoint + (awayDirection * standDistance);
        _enemyMove.SetMovePoint(ClampPoint(standPoint));
    }

    private void MoveAroundTarget(Vector3 targetPoint)
    {
        UpdateStrafe();

        if (_isStrafeMove == false)
        {
            MoveToStand(targetPoint, _pursuitDistance);

            return;
        }

        Vector3 awayDirection = GetFlatDirection(transform.position - targetPoint);

        if (awayDirection.sqrMagnitude <= ZeroThreshold)
        {
            awayDirection = GetFlatDirection(transform.forward);
        }

        Vector3 basePoint = targetPoint + (awayDirection * _pursuitDistance);
        Quaternion orbitRotation = Quaternion.Euler(0f, OrbitTurnAngle * _strafeDirection, 0f);
        Vector3 orbitDirection = orbitRotation * awayDirection;
        Vector3 movePoint = basePoint + (orbitDirection * _strafeDistance);
        _enemyMove.SetMovePoint(ClampPoint(movePoint));
    }

    private void UpdateStrafe()
    {
        if (_strafeTimer > 0f)
        {
            _strafeTimer -= Time.deltaTime;

            return;
        }

        if (_isStrafeMove)
        {
            _isStrafeMove = false;
            _strafeTimer = GetRandomValue(_strafeWaitMin, _strafeWaitMax);

            return;
        }

        _isStrafeMove = true;
        _strafeTimer = GetRandomValue(_strafeTimeMin, _strafeTimeMax);
        _strafeDirection = GetRandomDirection();
    }

    private void PickIdlePoint()
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();

        if (enemyRoomLock == null)
        {
            _idlePoint = _spawnPoint;
            _hasIdlePoint = true;

            return;
        }

        int patrolCount = enemyRoomLock.GetPatrolCount();

        if (patrolCount <= 0)
        {
            _idlePoint = ClampPoint(_spawnPoint);
            _idlePoint.y = _spawnPoint.y;
            _hasIdlePoint = true;

            return;
        }

        Vector3 currentPoint = transform.position;
        currentPoint.y = _spawnPoint.y;
        Vector3 fallbackPoint = ClampPoint(_spawnPoint);
        fallbackPoint.y = _spawnPoint.y;
        _hasIdlePoint = _idlePatrolPicker.TryPickPoint(enemyRoomLock, currentPoint, GetPatrolForward(), fallbackPoint, _spawnPoint.y, _idleRadius, IdlePointTryCount, GetRandomDirection, out _idlePoint);
    }

    private void ClearSearch()
    {
        _hasLastSeenPoint = false;
        _searchTimer = 0f;
        _hasIdlePoint = false;
        _idlePatrolPicker.Clear();
        _idleTimer = 0f;
        ResetStrafe();
    }

    private void StopFire()
    {
        _fireExecutor.StopFiring();
        _fireExecutor.ClearAimPoint();
    }

    private void StopAll()
    {
        StopFire();
        ResetStrafe();
        _enemyMove.ForceStop();
    }

    private void ResetStrafe()
    {
        _isStrafeMove = false;
        _strafeTimer = 0f;
    }

    private void TickAlert(Vector3 point)
    {
        _alertTimer -= Time.deltaTime;

        if (_alertTimer > 0f)
        {
            return;
        }

        AlertRoom(point);
        _alertTimer = AlertDelay;
    }

    private void AlertRoom(Vector3 point)
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();

        if (enemyRoomLock == null)
        {
            return;
        }

        enemyRoomLock.AlertPoint(point, this);
    }

    private void ApplyTrackMode()
    {
        if (_idleRotator.enabled)
        {
            _idleRotator.enabled = false;
        }

        if (_targetRotator.enabled == false)
        {
            _targetRotator.enabled = true;
        }
    }

    private void ApplyIdleMode()
    {
        _targetRotator.ClearAimPoint();

        if (_targetRotator.enabled)
        {
            _targetRotator.enabled = false;
        }

        if (_idleRotator.enabled == false)
        {
            _idleRotator.ResetBaseRotation();
            _idleRotator.enabled = true;
        }
    }

    private void ApplyDeadMode()
    {
        _targetRotator.ClearAimPoint();
        _targetRotator.enabled = false;
        _idleRotator.enabled = false;
    }

    private float GetRandomValue(float minValue, float maxValue)
    {
        if (Mathf.Abs(maxValue - minValue) <= ZeroThreshold)
        {
            return minValue;
        }

        double range = maxValue - minValue;
        double randomValue = _random.NextDouble();

        return minValue + (float)(range * randomValue);
    }

    private int GetRandomDirection()
    {
        int direction = _random.Next(0, 2);

        if (direction == 0)
        {
            return -1;
        }

        return 1;
    }

    private Vector3 GetPatrolForward()
    {
        Vector3 forwardDirection = GetFlatDirection(_enemyMove.ForwardDirection);

        if (forwardDirection.sqrMagnitude <= ZeroThreshold)
        {
            forwardDirection = GetFlatDirection(transform.forward);
        }

        return forwardDirection;
    }

    private float GetFlatDistance(Vector3 firstPoint, Vector3 secondPoint)
    {
        firstPoint.y = 0f;
        secondPoint.y = 0f;

        return Vector3.Distance(firstPoint, secondPoint);
    }

    private Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.zero;
        }

        direction.Normalize();

        return direction;
    }

    private Vector3 ClampPoint(Vector3 point)
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();

        if (enemyRoomLock == null)
        {
            return point;
        }

        return enemyRoomLock.ClampMovePoint(point);
    }

    private EnemyRoomLock GetRoomLock()
    {
        if (_enemyRoomLock != null)
        {
            return _enemyRoomLock;
        }

        _enemyRoomLock = GetComponent<EnemyRoomLock>();

        return _enemyRoomLock;
    }

    private void OnTargetFound()
    {
        _hasIdlePoint = false;
        _idleTimer = 0f;
        _alertTimer = AlertDelay;
        ResetStrafe();
        _strafeTimer = GetRandomValue(0.05f, 0.2f);
        AlertRoom(_targetVision.CurrentTargetPoint);
        ApplyTrackMode();
    }

    private void OnTargetLost()
    {
        _searchTimer = _searchTime;
        _alertTimer = 0f;
        ResetStrafe();
        ApplyIdleMode();
    }

    private void OnEnemyDied()
    {
        Vector3 moveVelocity = _enemyMove.MoveVelocity;
        Vector3 forwardDirection = _enemyMove.ForwardDirection;
        ApplyDeadMode();
        StopAll();
        _enemyMove.enabled = false;
        _enemyCrash.Crash(moveVelocity, forwardDirection);
        enabled = false;
    }
}
