using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyBomberBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert
{
    private const int HitBufferSize = 32;
    private const float ZeroThreshold = 0.0001f;

    private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];

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

    [Header("Explosion")]
    [SerializeField] private LayerMask _damageMask;
    [SerializeField] private float _damage = 24f;
    [SerializeField] private float _radius = 2.25f;
    [SerializeField] private float _impulse = 3.5f;
    [SerializeField] private float _up = 0.35f;
    [SerializeField] private float _effectLife = 5f;

    private EnemyRoomLock _enemyRoomLock;
    private EnemyState _state;
    private Vector3 _lastPoint;
    private bool _hasLastPoint;
    private bool _isExploding;
    private float _searchTimer;
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
        _enemyMove.ForceStop();
    }

    private void Update()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        if (_isExploding)
        {
            UpdateExplode();

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

        ApplyIdle();
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
        RotateToPoint(_lastPoint);
        MoveToPoint(_lastPoint);
    }

    private void UpdateSearch()
    {
        if (_searchTimer <= 0f)
        {
            ClearLastPoint();
            ApplyIdle();

            return;
        }

        _searchTimer -= Time.deltaTime;

        Vector3 searchPoint = ClampPoint(_lastPoint);
        float distance = GetFlatDistance(transform.position, searchPoint);

        if (distance <= _explodeDistance)
        {
            BeginExplode(searchPoint);

            return;
        }

        if (distance <= _stopDistance)
        {
            ClearLastPoint();
            ApplyIdle();

            return;
        }

        _state = EnemyState.Search;
        RotateToPoint(searchPoint);
        MoveToPoint(searchPoint);
    }

    private void UpdateExplode()
    {
        _state = EnemyState.Fight;
        _enemyMove.ForceStop();

        if (_hasLastPoint)
        {
            RotateToPoint(_lastPoint);
        }

        if (_explodeTimer > 0f)
        {
            _explodeTimer -= Time.deltaTime;
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
        _enemyMove.ForceStop();
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

    private void ApplyIdle()
    {
        _state = EnemyState.Idle;
        _enemyMove.StopMove();
    }

    private void MoveToPoint(Vector3 targetPoint)
    {
        Vector3 moveDirection = targetPoint - transform.position;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            _enemyMove.StopMove();

            return;
        }

        _enemyMove.SetRun(true);
        _enemyMove.SetDirection(moveDirection);
    }

    private void RotateToPoint(Vector3 targetPoint)
    {
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= ZeroThreshold)
        {
            return;
        }

        _enemyRotator.RotateToDirection(direction);
    }

    private void ClearLastPoint()
    {
        _hasLastPoint = false;
        _searchTimer = 0f;
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

    private float GetFlatDistance(Vector3 firstPoint, Vector3 secondPoint)
    {
        firstPoint.y = 0f;
        secondPoint.y = 0f;

        return Vector3.Distance(firstPoint, secondPoint);
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

    private void ResetState()
    {
        _state = EnemyState.Idle;
        _hasLastPoint = false;
        _isExploding = false;
        _searchTimer = 0f;
        _explodeTimer = 0f;
        _enemyMove.ForceStop();
    }

    private void OnTargetFound()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        _lastPoint = ClampPoint(_targetVision.CurrentTargetPoint);
        _hasLastPoint = true;
        _searchTimer = _searchTime;
        AlertRoom(_lastPoint);
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
        _enemyMove.ForceStop();
        enabled = false;
    }
}
