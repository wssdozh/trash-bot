using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyBomberBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert
{
    private const int IdlePointTryCount = 10;
    private const int HitBufferSize = 32;
    private const float ZeroThreshold = 0.0001f;
    private const int ObstacleMaskBits = 385;
    private const int AllyMaskBits = -1;
    private const float ProbeRadius = 0.22f;
    private const float ProbeHeight = 0.6f;
    private const float ProbeDistance = 0.9f;
    private const float ProbeAngle = 25f;
    private const float AvoidWeight = 1.05f;
    private const float SeparationRadius = 1.35f;
    private const float SeparationWeight = 2.2f;
    private const float IdleWallGap = 0.6f;

    private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];
    private readonly EnemyPatrolPicker _idlePatrolPicker = new EnemyPatrolPicker();

    [Header("Dependencies")]
    [SerializeField] private Enemy _enemy;
    [SerializeField] private Health _health;
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private EnemyMove _enemyMove;
    [SerializeField] private EnemyRotator _enemyRotator;
    [SerializeField] private GameObject _explosionPrefab;

    [Header("Move")]
    [SerializeField] private float _searchTime = 1.2f;
    [SerializeField] private float _explodeDistance = 1.25f;
    [SerializeField] private float _explodeDelay = 0.6f;
    [SerializeField] private float _stopDistance = 0.2f;

    [Header("Idle")]
    [SerializeField] private float _idleWaitMin = 0.45f;
    [SerializeField] private float _idleWaitMax = 1.1f;
    [SerializeField] private float _idleRadius = 1.6f;
    [SerializeField] private float _idleReachDistance = 0.35f;

    [Header("Explosion")]
    [SerializeField] private LayerMask _damageMask;
    [SerializeField] private float _damage = 24f;
    [SerializeField] private float _radius = 2.25f;
    [SerializeField] private float _impulse = 3.5f;
    [SerializeField] private float _up = 0.35f;
    [SerializeField] private float _effectLife = 5f;

    private System.Random _random;
    private EnemyRoomLock _enemyRoomLock;
    private EnemySteering _enemySteering;
    private EnemyState _state;
    private Vector3 _lastPoint;
    private Vector3 _idlePoint;
    private bool _hasLastPoint;
    private bool _hasIdlePoint;
    private bool _isExploding;
    private float _searchTimer;
    private float _idleTimer;
    private float _explodeTimer;

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

        if (_isExploding)
        {
            return false;
        }

        _idlePatrolPicker.Clear();
        ClearIdle();
        _lastPoint = ClampPoint(point);
        _hasLastPoint = true;
        _searchTimer = _searchTime;

        return true;
    }

    private void Awake()
    {
        if (_enemy == null)
        {
            throw new InvalidOperationException(nameof(_enemy));
        }

        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_targetVision == null)
        {
            throw new InvalidOperationException(nameof(_targetVision));
        }

        if (_enemyMove == null)
        {
            throw new InvalidOperationException(nameof(_enemyMove));
        }

        if (_enemyRotator == null)
        {
            throw new InvalidOperationException(nameof(_enemyRotator));
        }

        if (_searchTime < 0f)
        {
            throw new InvalidOperationException(nameof(_searchTime));
        }

        if (_explodeDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_explodeDistance));
        }

        if (_explodeDelay < 0f)
        {
            throw new InvalidOperationException(nameof(_explodeDelay));
        }

        if (_stopDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_stopDistance));
        }

        if (_idleWaitMin < 0f)
        {
            throw new InvalidOperationException(nameof(_idleWaitMin));
        }

        if (_idleWaitMax < _idleWaitMin)
        {
            throw new InvalidOperationException(nameof(_idleWaitMax));
        }

        if (_idleRadius < 0f)
        {
            throw new InvalidOperationException(nameof(_idleRadius));
        }

        if (_idleReachDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_idleReachDistance));
        }

        if (_damageMask.value == 0)
        {
            throw new InvalidOperationException(nameof(_damageMask));
        }

        if (_damage <= 0f)
        {
            throw new InvalidOperationException(nameof(_damage));
        }

        if (_radius <= 0f)
        {
            throw new InvalidOperationException(nameof(_radius));
        }

        if (_impulse < 0f)
        {
            throw new InvalidOperationException(nameof(_impulse));
        }

        if (_up < 0f)
        {
            throw new InvalidOperationException(nameof(_up));
        }

        if (_effectLife < 0f)
        {
            throw new InvalidOperationException(nameof(_effectLife));
        }

        _random = new System.Random(GetInstanceID());
        _enemySteering = new EnemySteering(transform, _enemyMove, _enemyRotator);
        _enemySteering.SetObstacle(ObstacleMaskBits, ProbeRadius, ProbeHeight, ProbeDistance, ProbeAngle, AvoidWeight);
        _enemySteering.SetSpacing(AllyMaskBits, SeparationRadius, SeparationWeight);
    }

    private void OnEnable()
    {
        _enemy.Died += OnEnemyDied;
        _targetVision.TargetDetected += OnTargetFound;
        _targetVision.TargetCleared += OnTargetLost;
        ResetState();
    }

    private void OnDisable()
    {
        _enemy.Died -= OnEnemyDied;
        _targetVision.TargetDetected -= OnTargetFound;
        _targetVision.TargetCleared -= OnTargetLost;
        _enemySteering.ForceStop();
    }

    private void FixedUpdate()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        RefreshRoomLock();
        _targetVision.Refresh();

        if (_isExploding)
        {
            UpdateExplode();

            return;
        }

        if (_enemySteering.ResolveOverlap())
        {
            _enemySteering.ResetMoveStuck();

            return;
        }

        if (_targetVision.IsTargetVisible)
        {
            UpdateChase(_targetVision.CurrentTargetPoint);

            return;
        }

        if (_hasLastPoint)
        {
            UpdateSearch();

            return;
        }

        UpdateIdle();
    }

    private void UpdateChase(Vector3 targetPoint)
    {
        _lastPoint = ClampPoint(targetPoint);
        _hasLastPoint = true;
        _searchTimer = _searchTime;

        float distance = GetFlatDistance(transform.position, _lastPoint);

        if (distance <= _explodeDistance)
        {
            BeginExplode(_lastPoint);

            return;
        }

        _state = EnemyState.Chase;
        MoveToPoint(_lastPoint, true);
    }

    private void UpdateSearch()
    {
        if (_searchTimer <= 0f)
        {
            ClearLastPoint();
            StartIdleWait();

            return;
        }

        _searchTimer -= Time.fixedDeltaTime;
        Vector3 searchPoint = ClampPoint(_lastPoint);
        float distance = GetFlatDistance(transform.position, searchPoint);

        if (distance <= _stopDistance)
        {
            if (TryPickSearchPoint())
            {
                searchPoint = _lastPoint;
            }
            else
            {
                ClearLastPoint();
                StartIdleWait();

                return;
            }
        }

        _state = EnemyState.Search;

        if (MoveToPoint(searchPoint, false))
        {
            return;
        }

        if (TryPickSearchPoint())
        {
            return;
        }

        ClearLastPoint();
        StartIdleWait();
    }

    private void UpdateIdle()
    {
        _enemyMove.SetRun(false);

        if (_idleTimer > 0f)
        {
            _state = EnemyState.Idle;
            _idleTimer -= Time.fixedDeltaTime;
            _enemySteering.Stop();

            return;
        }

        if (_hasIdlePoint == false)
        {
            PickIdlePoint();
        }

        if (_hasIdlePoint == false)
        {
            _state = EnemyState.Idle;
            _enemySteering.Stop();

            return;
        }

        float distance = GetFlatDistance(transform.position, _idlePoint);

        if (distance <= _idleReachDistance)
        {
            StartIdleWait();

            return;
        }

        _state = EnemyState.Patrol;

        if (MoveToPoint(_idlePoint, false))
        {
            return;
        }

        PickIdlePoint();

        if (_hasIdlePoint)
        {
            return;
        }

        StartIdleWait();
    }

    private void UpdateExplode()
    {
        _state = EnemyState.Fight;

        if (_targetVision.IsTargetVisible)
        {
            _lastPoint = ClampPoint(_targetVision.CurrentTargetPoint);
            _hasLastPoint = true;
        }

        if (_hasLastPoint)
        {
            MoveToPoint(_lastPoint, true);
        }
        else
        {
            _enemySteering.ForceStop();
        }

        if (_explodeTimer > 0f)
        {
            _explodeTimer -= Time.fixedDeltaTime;
        }

        if (_explodeTimer > 0f)
        {
            return;
        }

        Explode();
    }

    private void BeginExplode(Vector3 targetPoint)
    {
        if (_isExploding)
        {
            return;
        }

        _isExploding = true;
        _explodeTimer = _explodeDelay;
        _lastPoint = ClampPoint(targetPoint);
        _hasLastPoint = true;
        _state = EnemyState.Fight;
    }

    private void Explode()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        Vector3 explosionPoint = transform.position;
        SpawnExplosion(explosionPoint);

        int hitCount = Physics.OverlapSphereNonAlloc(
            explosionPoint,
            _radius,
            _hitBuffer,
            _damageMask,
            QueryTriggerInteraction.Ignore
        );

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider hitCollider = _hitBuffer[hitIndex];
            _hitBuffer[hitIndex] = null;

            if (CanHit(hitCollider) == false)
            {
                continue;
            }

            ApplyExplosion(hitCollider, explosionPoint);
        }

        _health.Decrease(_health.MaxValue);
    }

    private void ApplyExplosion(Collider hitCollider, Vector3 explosionPoint)
    {
        Vector3 hitPoint = hitCollider.ClosestPoint(explosionPoint);
        float distance = Vector3.Distance(explosionPoint, hitPoint);
        float normalizedDistance = Mathf.Clamp01(distance / _radius);
        float damageScale = 1f - normalizedDistance;

        if (damageScale > 0f)
        {
            Health targetHealth = hitCollider.GetComponentInParent<Health>();

            if (targetHealth != null)
            {
                float finalDamage = _damage * damageScale;

                if (finalDamage > 0f)
                {
                    targetHealth.Decrease(finalDamage);
                }
            }
        }

        Rigidbody targetRigidbody = hitCollider.attachedRigidbody;

        if (targetRigidbody == null)
        {
            return;
        }

        targetRigidbody.AddExplosionForce(_impulse, explosionPoint, _radius, _up, ForceMode.Impulse);
    }

    private bool CanHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.transform.IsChildOf(transform))
        {
            return false;
        }

        Rigidbody attachedRigidbody = hitCollider.attachedRigidbody;

        if (attachedRigidbody == null)
        {
            return true;
        }

        if (attachedRigidbody.transform.IsChildOf(transform))
        {
            return false;
        }

        return true;
    }

    private void SpawnExplosion(Vector3 explosionPoint)
    {
        if (_explosionPrefab == null)
        {
            return;
        }

        GameObject explosionObject = Instantiate(_explosionPrefab, explosionPoint, Quaternion.identity);

        if (_effectLife > 0f)
        {
            Destroy(explosionObject, _effectLife);
        }
    }

    private void StartIdleWait()
    {
        ClearIdle();
        _state = EnemyState.Idle;
        _idleTimer = GetRandomValue(_idleWaitMin, _idleWaitMax);
        _enemySteering.ForceStop();
    }

    private void PickIdlePoint()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);

        if (TryPickGuardIdlePoint(currentPoint))
        {
            return;
        }

        if (TryPickNavIdlePoint(currentPoint))
        {
            return;
        }

        _hasIdlePoint = false;
    }

    private bool TryPickGuardIdlePoint(Vector3 currentPoint)
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();

        if (enemyRoomLock == null)
        {
            return false;
        }

        if (enemyRoomLock.GetPatrolCount() <= 0)
        {
            return false;
        }

        Vector3 fallbackPoint = ClampPoint(currentPoint);
        fallbackPoint.y = transform.position.y;
        Vector3 patrolPoint;
        float idleMinDistance = GetIdleMinDistance();

        if (_idlePatrolPicker.TryPickPoint(enemyRoomLock, currentPoint, GetPatrolForward(), fallbackPoint, transform.position.y, idleMinDistance, IdlePointTryCount, GetRandomDirection, out patrolPoint) == false)
        {
            return false;
        }

        return TrySetIdlePoint(currentPoint, patrolPoint, IdleWallGap);
    }

    private bool TryPickNavIdlePoint(Vector3 currentPoint)
    {
        float idleMinDistance = GetIdleMinDistance();
        float idleMaxDistance = Mathf.Max(idleMinDistance, _idleRadius);
        Vector3 navPoint;

        if (_enemySteering.TryPickNavPoint(currentPoint, GetPatrolForward(), idleMinDistance, idleMaxDistance, IdleWallGap, IdlePointTryCount, Next01, out navPoint))
        {
            return TrySetIdlePoint(currentPoint, navPoint, IdleWallGap);
        }

        if (_enemySteering.TryPickNavPoint(currentPoint, GetPatrolForward(), idleMinDistance, idleMaxDistance, ProbeRadius, IdlePointTryCount, Next01, out navPoint))
        {
            return TrySetIdlePoint(currentPoint, navPoint, ProbeRadius);
        }

        return false;
    }

    private bool TrySetIdlePoint(Vector3 currentPoint, Vector3 targetPoint, float wallGap)
    {
        Vector3 safePoint = _enemySteering.GetSafePoint(targetPoint, wallGap);

        if (GetFlatDistance(currentPoint, safePoint) < _idleReachDistance)
        {
            return false;
        }

        if (_enemySteering.HasPointClearance(safePoint) == false)
        {
            return false;
        }

        if (_enemySteering.HasWallGap(safePoint, wallGap) == false)
        {
            return false;
        }

        _idlePoint = safePoint;
        _idlePoint.y = transform.position.y;
        _hasIdlePoint = true;
        _enemySteering.ResetMoveStuck();

        return true;
    }

    private float GetIdleMinDistance()
    {
        return Mathf.Max(ProbeDistance, _idleReachDistance * 2f);
    }

    private float Next01()
    {
        if (_random == null)
        {
            return 0.5f;
        }

        return (float)_random.NextDouble();
    }

    private bool TryPickSearchPoint()
    {
        float searchDistance = Mathf.Max(_idleReachDistance, _idleRadius * 0.5f);
        EnemyRoomLock enemyRoomLock = GetRoomLock();
        Vector3 fallbackPoint = ClampPoint(transform.position);
        fallbackPoint.y = transform.position.y;

        if (_idlePatrolPicker.TryPickPoint(enemyRoomLock, transform.position, GetPatrolForward(), fallbackPoint, transform.position.y, searchDistance, IdlePointTryCount, GetRandomDirection, out _lastPoint) == false)
        {
            return false;
        }

        _hasLastPoint = true;

        return true;
    }

    private bool MoveToPoint(Vector3 targetPoint, bool isRunning)
    {
        _enemyMove.SetRun(isRunning);
        Vector3 currentPoint = GetFlatPoint(transform.position);
        bool isMoving = _enemySteering.MoveToPoint(targetPoint, _stopDistance, targetPoint);

        if (isMoving)
        {
            if (_enemySteering.CanKeepMove(currentPoint, Time.fixedDeltaTime))
            {
                return true;
            }
        }

        _enemySteering.ForceStop();

        return false;
    }

    private void ClearLastPoint()
    {
        _hasLastPoint = false;
        _searchTimer = 0f;
        _enemySteering.ResetMoveStuck();
    }

    private void ClearIdle()
    {
        _hasIdlePoint = false;
        _idleTimer = 0f;
        _enemySteering.ResetMoveStuck();
    }

    private Vector3 ClampPoint(Vector3 point)
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();
        float wallGap = Mathf.Max(IdleWallGap, ProbeRadius);

        if (enemyRoomLock == null)
        {
            return _enemySteering.GetSafePoint(point, wallGap);
        }

        Vector3 clampedPoint = enemyRoomLock.ClampMovePoint(point);

        return _enemySteering.GetSafePoint(clampedPoint, wallGap);
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

    private float GetFlatDistance(Vector3 firstPoint, Vector3 secondPoint)
    {
        firstPoint.y = 0f;
        secondPoint.y = 0f;

        return Vector3.Distance(firstPoint, secondPoint);
    }

    private Vector3 GetFlatPoint(Vector3 point)
    {
        point.y = 0f;

        return point;
    }

    private void ResetState()
    {
        _idlePatrolPicker.Clear();
        ClearIdle();
        _hasLastPoint = false;
        _isExploding = false;
        _searchTimer = 0f;
        _explodeTimer = 0f;
        _enemySteering.ResetMoveStuck();
        StartIdleWait();
    }

    private void OnTargetFound()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        _idlePatrolPicker.Clear();
        ClearIdle();
        _lastPoint = ClampPoint(_targetVision.CurrentTargetPoint);
        _hasLastPoint = true;
        _searchTimer = _searchTime;
    }

    private void OnTargetLost()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        if (_hasLastPoint == false)
        {
            return;
        }

        _searchTimer = _searchTime;
    }

    private void OnEnemyDied()
    {
        _enemySteering.ForceStop();
        enabled = false;
    }

    private void RefreshRoomLock()
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();
        _enemySteering.SetRoomLock(enemyRoomLock);
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
        Vector3 forwardDirection = _enemyMove.MoveDirection;
        forwardDirection.y = 0f;

        if (forwardDirection.sqrMagnitude <= ZeroThreshold)
        {
            forwardDirection = transform.forward;
            forwardDirection.y = 0f;
        }

        if (forwardDirection.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.zero;
        }

        forwardDirection.Normalize();

        return forwardDirection;
    }

}
