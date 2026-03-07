using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyMeleeBrain : MonoBehaviour
{
    private const int SearchStepsCount = 4;
    private const int IdlePointTryCount = 8;
    private const float LookDistance = 2f;
    private const float ForwardGizmoLength = 1.1f;
    private const float MoveGizmoLength = 1.35f;
    private const float PointGizmoSize = 0.18f;

    [Header("Dependencies")]
    [SerializeField] private Enemy _enemy;
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private EnemyMove _enemyMove;
    [SerializeField] private EnemyRotator _enemyRotator;
    [SerializeField] private Attacker _attacker;

    [Header("Combat")]
    [SerializeField] private float _attackDistance = 1.5f;
    [SerializeField] private float _attackAngle = 20f;
    [SerializeField] private float _combatRadius = 1.2f;
    [SerializeField] private float _combatTolerance = 0.2f;
    [SerializeField] private float _orbitMin = 1.15f;
    [SerializeField] private float _orbitMax = 1.85f;
    [SerializeField] private float _recoverMin = 0.25f;
    [SerializeField] private float _recoverMax = 0.45f;

    [Header("Idle")]
    [SerializeField] private float _idleMoveMin = 4f;
    [SerializeField] private float _idleMoveMax = 7f;
    [SerializeField] private float _idleWaitMin = 1.4f;
    [SerializeField] private float _idleWaitMax = 2.6f;
    [SerializeField] private float _idleLookAngle = 38f;
    [SerializeField] private float _idleReachDistance = 0.2f;

    [Header("Search")]
    [SerializeField] private float _searchPointDistance = 0.35f;
    [SerializeField] private float _searchWaitSeconds = 1.25f;

    [Header("Steering")]
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _allyMask = ~0;
    [SerializeField] private float _probeRadius = 0.3f;
    [SerializeField] private float _probeHeight = 0.6f;
    [SerializeField] private float _probeDistance = 1.5f;
    [SerializeField] private float _probeAngle = 35f;
    [SerializeField] private float _avoidWeight = 1.6f;
    [SerializeField] private float _separationRadius = 1.05f;
    [SerializeField] private float _separationWeight = 1.1f;
    [SerializeField] private float _orbitWeight = 0.95f;
    [SerializeField] private float _ringWeight = 1.35f;
    [SerializeField] private float _slotWeight = 0.75f;
    [SerializeField] private float _slotAngle = 22f;
    [SerializeField] private float _slotRadius = 3f;
    [SerializeField] private int _slotCount = 5;
    [SerializeField] private float _recoverBack = 1.1f;
    [SerializeField] private float _recoverSide = 0.8f;

    [Header("Gizmo")]
    [SerializeField] private bool _isAttackZoneVisible = true;
    [SerializeField] private bool _isMoveGizmoVisible = true;

    private EnemySteering _enemySteering;
    private EnemyState _state;
    private Vector3 _lastSeenPoint;
    private Vector3 _lastSeenDirection;
    private Vector3 _idleDirection;
    private Vector3 _idleTargetPoint;
    private Vector3 _idleLookPoint;
    private Vector3 _searchTargetPoint;
    private bool _hasLastSeenPoint;
    private bool _hasSearchPoint;
    private bool _isIdleWalking;
    private bool _isOrbitClockwise;
    private float _searchTimer;
    private float _idleTimer;
    private float _orbitTimer;
    private float _recoverTimer;
    private int _idleStep;
    private int _searchStep;
    private int _orbitStep;
    private int _recoverStep;

    public EnemyState State => _state;

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

        if (_enemyRotator == null)
        {
            throw new InvalidOperationException(nameof(_enemyRotator));
        }

        if (_attacker == null)
        {
            throw new InvalidOperationException(nameof(_attacker));
        }

        if (_attackDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_attackDistance));
        }

        if (_attackAngle < 0f)
        {
            throw new InvalidOperationException(nameof(_attackAngle));
        }

        if (_combatRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_combatRadius));
        }

        if (_combatRadius > _attackDistance)
        {
            throw new InvalidOperationException(nameof(_combatRadius));
        }

        if (_combatTolerance < 0f)
        {
            throw new InvalidOperationException(nameof(_combatTolerance));
        }

        if (_orbitMin <= 0f)
        {
            throw new InvalidOperationException(nameof(_orbitMin));
        }

        if (_orbitMax < _orbitMin)
        {
            throw new InvalidOperationException(nameof(_orbitMax));
        }

        if (_recoverMin <= 0f)
        {
            throw new InvalidOperationException(nameof(_recoverMin));
        }

        if (_recoverMax < _recoverMin)
        {
            throw new InvalidOperationException(nameof(_recoverMax));
        }

        if (_idleMoveMin <= 0f)
        {
            throw new InvalidOperationException(nameof(_idleMoveMin));
        }

        if (_idleMoveMax < _idleMoveMin)
        {
            throw new InvalidOperationException(nameof(_idleMoveMax));
        }

        if (_idleWaitMin < 0f)
        {
            throw new InvalidOperationException(nameof(_idleWaitMin));
        }

        if (_idleWaitMax < _idleWaitMin)
        {
            throw new InvalidOperationException(nameof(_idleWaitMax));
        }

        if (_idleLookAngle < 0f)
        {
            throw new InvalidOperationException(nameof(_idleLookAngle));
        }

        if (_idleReachDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_idleReachDistance));
        }

        if (_searchPointDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_searchPointDistance));
        }

        if (_searchWaitSeconds < 0f)
        {
            throw new InvalidOperationException(nameof(_searchWaitSeconds));
        }

        if (_probeRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_probeRadius));
        }

        if (_probeHeight <= 0f)
        {
            throw new InvalidOperationException(nameof(_probeHeight));
        }

        if (_probeDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_probeDistance));
        }

        if (_probeAngle <= 0f)
        {
            throw new InvalidOperationException(nameof(_probeAngle));
        }

        if (_avoidWeight < 0f)
        {
            throw new InvalidOperationException(nameof(_avoidWeight));
        }

        if (_separationRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_separationRadius));
        }

        if (_separationWeight < 0f)
        {
            throw new InvalidOperationException(nameof(_separationWeight));
        }

        if (_orbitWeight < 0f)
        {
            throw new InvalidOperationException(nameof(_orbitWeight));
        }

        if (_ringWeight < 0f)
        {
            throw new InvalidOperationException(nameof(_ringWeight));
        }

        if (_slotWeight < 0f)
        {
            throw new InvalidOperationException(nameof(_slotWeight));
        }

        if (_slotAngle < 0f)
        {
            throw new InvalidOperationException(nameof(_slotAngle));
        }

        if (_slotRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_slotRadius));
        }

        if (_slotCount <= 0)
        {
            throw new InvalidOperationException(nameof(_slotCount));
        }

        if (_recoverBack <= 0f)
        {
            throw new InvalidOperationException(nameof(_recoverBack));
        }

        if (_recoverSide < 0f)
        {
            throw new InvalidOperationException(nameof(_recoverSide));
        }

        _enemySteering = new EnemySteering(transform, _enemyMove, _enemyRotator);
        _enemySteering.SetObstacle(_obstacleMask, _probeRadius, _probeHeight, _probeDistance, _probeAngle, _avoidWeight);
        _enemySteering.SetSpacing(_allyMask, _separationRadius, _separationWeight);
        _enemySteering.SetCombat(_orbitWeight, _ringWeight, _slotWeight, _slotAngle, _slotRadius, _slotCount, _recoverBack, _recoverSide);
    }

    private void OnEnable()
    {
        _enemy.Died += OnDied;
        ResetState();
    }

    private void OnDisable()
    {
        _enemy.Died -= OnDied;
        _enemySteering.Stop();
    }

    private void FixedUpdate()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        UpdateLastSeenPoint();

        if (_targetVision.IsTargetVisible)
        {
            ProcessVisibleTarget();

            return;
        }

        ProcessHiddenTarget();
    }

    private void OnDrawGizmos()
    {
        if (_isAttackZoneVisible)
        {
            DrawAttackGizmo();
        }

        if (_isMoveGizmoVisible == false)
        {
            return;
        }

        DrawMoveGizmo();
        DrawStateGizmo();
        DrawTargetGizmo();
    }

    private void DrawAttackGizmo()
    {
        Vector3 currentPoint = transform.position;
        Vector3 forwardDirection = GetStartDirection();
        Vector3 leftDirection = RotateDirection(forwardDirection, -_attackAngle);
        Vector3 rightDirection = RotateDirection(forwardDirection, _attackAngle);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentPoint, _attackDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(currentPoint, _combatRadius);

        Gizmos.color = new Color(1f, 0.25f, 0.1f);
        Gizmos.DrawLine(currentPoint, currentPoint + (forwardDirection * _attackDistance));
        Gizmos.DrawLine(currentPoint, currentPoint + (leftDirection * _attackDistance));
        Gizmos.DrawLine(currentPoint, currentPoint + (rightDirection * _attackDistance));
    }

    private void DrawMoveGizmo()
    {
        Vector3 currentPoint = transform.position;
        Vector3 forwardPoint = currentPoint + (GetStartDirection() * ForwardGizmoLength);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(currentPoint, forwardPoint);
        Gizmos.DrawWireSphere(forwardPoint, PointGizmoSize);

        if (_enemyMove == null)
        {
            return;
        }

        if (_enemyMove.MoveAmount <= 0f)
        {
            return;
        }

        Vector3 movePoint = currentPoint + (_enemyMove.MoveDirection * MoveGizmoLength);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(currentPoint, movePoint);
        Gizmos.DrawWireSphere(movePoint, PointGizmoSize);
    }

    private void DrawStateGizmo()
    {
        if (_isIdleWalking)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _idleTargetPoint);
            Gizmos.DrawWireSphere(_idleTargetPoint, _idleReachDistance);
        }

        else if (_hasLastSeenPoint == false)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _idleLookPoint);
            Gizmos.DrawWireSphere(_idleLookPoint, 0.15f);
        }

        if (_hasLastSeenPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_lastSeenPoint, _searchPointDistance);
        }

        if (_hasSearchPoint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, _searchTargetPoint);
            Gizmos.DrawWireSphere(_searchTargetPoint, _searchPointDistance);
        }
    }

    private void DrawTargetGizmo()
    {
        if (_targetVision == null)
        {
            return;
        }

        if (_targetVision.IsTargetVisible == false)
        {
            return;
        }

        if (_targetVision.CurrentTarget == null)
        {
            return;
        }

        Vector3 targetPoint = GetFlatPoint(_targetVision.CurrentTarget.position);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawWireSphere(targetPoint, PointGizmoSize);
    }

    private void ProcessVisibleTarget()
    {
        Transform currentTarget = _targetVision.CurrentTarget;

        if (currentTarget == null)
        {
            ProcessHiddenTarget();

            return;
        }

        _isIdleWalking = false;
        _hasSearchPoint = false;
        _searchStep = 0;
        _searchTimer = 0f;

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 targetPoint = GetFlatPoint(currentTarget.position);
        float distance = Vector3.Distance(currentPoint, targetPoint);

        if (distance > _combatRadius)
        {
            _state = EnemyState.Chase;

            if (_enemySteering.MoveToPoint(targetPoint, _combatRadius))
            {
                return;
            }

            _enemySteering.LookToPoint(targetPoint);

            return;
        }

        _state = EnemyState.Fight;
        TryAttack(currentPoint, targetPoint, distance);
    }

    private void ProcessHiddenTarget()
    {
        _recoverTimer = 0f;
        _orbitTimer = 0f;

        if (_hasLastSeenPoint == false)
        {
            ProcessIdle();

            return;
        }

        _state = EnemyState.Search;
        _isIdleWalking = false;

        Vector3 currentPoint = GetFlatPoint(transform.position);
        float distanceToPoint = Vector3.Distance(currentPoint, _lastSeenPoint);

        if (distanceToPoint > _searchPointDistance)
        {
            if (_enemySteering.MoveToPoint(_lastSeenPoint, _searchPointDistance))
            {
                return;
            }

            ClearSearch();
            StartIdleLook();

            return;
        }

        ProcessSearch(currentPoint);
    }

    private void ProcessIdle()
    {
        _hasSearchPoint = false;
        _searchStep = 0;
        _searchTimer = 0f;

        if (_isIdleWalking)
        {
            _state = EnemyState.Patrol;
            ProcessIdleWalk();

            return;
        }

        _state = EnemyState.Watch;
        ProcessIdleLook();
    }

    private void ProcessIdleWalk()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);
        float distance = Vector3.Distance(currentPoint, _idleTargetPoint);

        if (distance > _idleReachDistance)
        {
            if (_enemySteering.MoveToPoint(_idleTargetPoint, _idleReachDistance))
            {
                return;
            }
        }

        StartIdleLook();
    }

    private void ProcessIdleLook()
    {
        _enemySteering.LookToPoint(_idleLookPoint);
        _idleTimer -= Time.fixedDeltaTime;

        if (_idleTimer > 0f)
        {
            return;
        }

        StartIdleWalk();
    }

    private void ProcessSearch(Vector3 currentPoint)
    {
        if (_hasSearchPoint == false)
        {
            if (StartSearchPoint() == false)
            {
                ClearSearch();
                StartIdleLook();

                return;
            }
        }

        float distance = Vector3.Distance(currentPoint, _searchTargetPoint);

        if (distance > _searchPointDistance)
        {
            if (_enemySteering.MoveToPoint(_searchTargetPoint, _searchPointDistance))
            {
                return;
            }

            _hasSearchPoint = false;
            _searchStep += 1;

            return;
        }

        HoldSearchLook();
        _searchTimer += Time.fixedDeltaTime;

        if (_searchTimer < _searchWaitSeconds)
        {
            return;
        }

        _searchTimer = 0f;
        _searchStep += 1;
        _hasSearchPoint = false;

        if (_searchStep >= SearchStepsCount)
        {
            ClearSearch();
            StartIdleLook();
        }
    }

    private void StartIdleWalk()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);
        int attemptIndex = 0;

        while (attemptIndex < IdlePointTryCount)
        {
            Vector3 nextDirection = GetNextIdleDirection();
            float idleDistance = GetIdleDistance();
            Vector3 nextPoint = currentPoint + (nextDirection * idleDistance);
            nextPoint.y = transform.position.y;

            if (CanUsePoint(nextPoint))
            {
                _idleTargetPoint = nextPoint;
                _isIdleWalking = true;

                return;
            }

            attemptIndex += 1;
        }

        StartIdleLook();
    }

    private void StartIdleLook()
    {
        _isIdleWalking = false;
        _idleTimer = GetIdleWait();
        _idleLookPoint = transform.position + (GetIdleLookDirection() * LookDistance);
        _enemySteering.Stop();
    }

    private bool StartSearchPoint()
    {
        while (_searchStep < SearchStepsCount)
        {
            Vector3 candidatePoint = GetSearchPoint();

            if (CanUsePoint(candidatePoint))
            {
                _searchTargetPoint = candidatePoint;
                _hasSearchPoint = true;
                _searchTimer = 0f;

                return true;
            }

            _searchStep += 1;
        }

        return false;
    }

    private void HoldSearchLook()
    {
        _enemySteering.Stop();
        _enemyRotator.RotateToDirection(GetSearchDirection());
    }

    private bool TryAttack(Vector3 currentPoint, Vector3 targetPoint, float distance)
    {
        if (distance > _attackDistance)
        {
            return false;
        }

        Vector3 attackDirection = targetPoint - currentPoint;
        float angleToTarget = Vector3.Angle(transform.forward, attackDirection);

        if (angleToTarget > _attackAngle)
        {
            _enemySteering.LookToPoint(targetPoint);

            return true;
        }

        _enemySteering.LookToPoint(targetPoint);

        if (_attacker.PerformAttack() == false)
        {
            return false;
        }

        return true;
    }

    private void StartRecover()
    {
        _recoverTimer = GetRecoverSeconds();
        FlipOrbit();
        _orbitTimer = GetOrbitSeconds();
    }

    private void TickRecover()
    {
        if (_recoverTimer <= 0f)
        {
            return;
        }

        _recoverTimer -= Time.fixedDeltaTime;

        if (_recoverTimer < 0f)
        {
            _recoverTimer = 0f;
        }
    }

    private void TickOrbit()
    {
        if (_orbitTimer <= 0f)
        {
            _orbitTimer = GetOrbitSeconds();

            return;
        }

        _orbitTimer -= Time.fixedDeltaTime;

        if (_orbitTimer > 0f)
        {
            return;
        }

        FlipOrbit();
        _orbitTimer = GetOrbitSeconds();
    }

    private void FlipOrbit()
    {
        _isOrbitClockwise = _isOrbitClockwise == false;
    }

    private void UpdateLastSeenPoint()
    {
        if (_targetVision.IsTargetVisible == false)
        {
            return;
        }

        Transform currentTarget = _targetVision.CurrentTarget;

        if (currentTarget == null)
        {
            return;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 targetPoint = GetFlatPoint(currentTarget.position);
        Vector3 direction = targetPoint - currentPoint;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
            _lastSeenDirection = direction;
        }

        _lastSeenPoint = targetPoint;
        _hasLastSeenPoint = true;
    }

    private void ResetState()
    {
        _state = EnemyState.Idle;
        _searchTimer = 0f;
        _idleTimer = 0f;
        _orbitTimer = 0f;
        _recoverTimer = 0f;
        _hasLastSeenPoint = false;
        _hasSearchPoint = false;
        _isIdleWalking = false;
        _isOrbitClockwise = true;
        _idleStep = 0;
        _searchStep = 0;
        _orbitStep = 0;
        _recoverStep = 0;
        _lastSeenDirection = GetStartDirection();
        _idleDirection = _lastSeenDirection;
        _idleLookPoint = transform.position + (_idleDirection * LookDistance);
        _enemySteering.Stop();
        StartIdleLook();
    }

    private void ClearSearch()
    {
        _hasLastSeenPoint = false;
        _hasSearchPoint = false;
        _searchStep = 0;
        _searchTimer = 0f;
    }

    private void OnDied()
    {
        _enemySteering.Stop();
        enabled = false;
    }

    private bool CanUsePoint(Vector3 point)
    {
        if (_enemySteering.HasPointClearance(point) == false)
        {
            return false;
        }

        if (_enemySteering.IsLineBlocked(point))
        {
            return false;
        }

        return true;
    }

    private float GetIdleDistance()
    {
        int patternIndex = _idleStep % 4;
        float pattern01 = (float)patternIndex / 3f;

        return Mathf.Lerp(_idleMoveMin, _idleMoveMax, pattern01);
    }

    private float GetIdleWait()
    {
        int patternIndex = _idleStep % 4;
        float pattern01 = 1f - ((float)patternIndex / 3f);

        return Mathf.Lerp(_idleWaitMin, _idleWaitMax, pattern01);
    }

    private float GetOrbitSeconds()
    {
        int patternIndex = _orbitStep % 4;
        float pattern01 = (float)patternIndex / 3f;
        _orbitStep += 1;

        return Mathf.Lerp(_orbitMin, _orbitMax, pattern01);
    }

    private float GetRecoverSeconds()
    {
        int patternIndex = _recoverStep % 3;
        float pattern01 = (float)patternIndex / 2f;
        _recoverStep += 1;

        return Mathf.Lerp(_recoverMin, _recoverMax, pattern01);
    }

    private Vector3 GetNextIdleDirection()
    {
        float turnDegrees = GetIdleTurn();
        Quaternion rotation = Quaternion.Euler(0f, turnDegrees, 0f);
        Vector3 nextDirection = rotation * _idleDirection;
        nextDirection.y = 0f;

        if (nextDirection.sqrMagnitude <= 0.0001f)
        {
            nextDirection = Vector3.forward;
        }

        nextDirection.Normalize();
        _idleDirection = nextDirection;
        _idleStep += 1;

        return nextDirection;
    }

    private Vector3 GetIdleLookDirection()
    {
        float turnDegrees = GetIdleLookTurn();
        Vector3 lookDirection = RotateDirection(_idleDirection, turnDegrees);

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return GetStartDirection();
        }

        return lookDirection;
    }

    private float GetIdleTurn()
    {
        int patternIndex = _idleStep % 6;

        if (patternIndex == 0)
        {
            return 18f;
        }

        if (patternIndex == 1)
        {
            return -34f;
        }

        if (patternIndex == 2)
        {
            return 22f;
        }

        if (patternIndex == 3)
        {
            return 38f;
        }

        if (patternIndex == 4)
        {
            return -26f;
        }

        return 14f;
    }

    private float GetIdleLookTurn()
    {
        int patternIndex = _idleStep % 5;

        if (patternIndex == 0)
        {
            return 0f;
        }

        if (patternIndex == 1)
        {
            return -_idleLookAngle;
        }

        if (patternIndex == 2)
        {
            return _idleLookAngle * 0.65f;
        }

        if (patternIndex == 3)
        {
            return _idleLookAngle;
        }

        return -_idleLookAngle * 0.5f;
    }

    private Vector3 GetSearchPoint()
    {
        Vector3 baseDirection = GetSearchDirection();
        float searchDistance = Mathf.Max(_combatRadius, _probeDistance * 0.85f);

        if (_searchStep == 0)
        {
            return _lastSeenPoint;
        }

        if (_searchStep == 1)
        {
            return _lastSeenPoint + (baseDirection * (searchDistance * 0.75f));
        }

        if (_searchStep == 2)
        {
            return _lastSeenPoint + (baseDirection * (searchDistance * 1.35f));
        }

        return _lastSeenPoint + (baseDirection * (searchDistance * 1.85f));
    }

    private Vector3 GetSearchDirection()
    {
        if (_lastSeenDirection.sqrMagnitude <= 0.0001f)
        {
            return GetStartDirection();
        }

        return _lastSeenDirection;
    }

    private Vector3 GetStartDirection()
    {
        Vector3 direction = transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        return direction;
    }

    private Vector3 GetFlatPoint(Vector3 point)
    {
        point.y = transform.position.y;

        return point;
    }

    private Vector3 RotateDirection(Vector3 direction, float angle)
    {
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        Vector3 rotatedDirection = rotation * direction;
        rotatedDirection.y = 0f;

        if (rotatedDirection.sqrMagnitude <= 0.0001f)
        {
            return direction;
        }

        rotatedDirection.Normalize();

        return rotatedDirection;
    }
}
