using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyMeleeBrain : MonoBehaviour
{
    private const int SearchStepsCount = 4;
    private const int IdlePointTryCount = 16;
    private const float IdleProgressMin = 0.02f;
    private const float IdleStuckSeconds = 0.35f;
    private const float MoveStuckMin = 0.01f;
    private const float MoveStuckTime = 0.3f;
    private const float IdleFrontChance = 0.3f;
    private const float IdleFallbackDistance = 1.6f;
    private const float MinFightGap = 0.05f;
    private const float MinFightRadius = 0.1f;
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
    [SerializeField] private EnemyAnimation _animation;
    [SerializeField] private PlayerAnimationEvents _animationEvents;

    [Header("Combat")]
    [SerializeField] private float _attackDistance = 3.5f;
    [SerializeField] private float _attackGap = 0f;
    [SerializeField] private float _attackAngle = 20f;
    [SerializeField] private float _combatRadius = 0.55f;
    [SerializeField] private float _combatChaos = 0.04f;
    [SerializeField] private float _combatTolerance = 0.05f;
    [SerializeField] private float _fightExitGap = 0.12f;
    [SerializeField] private float _runStartDistance = 4.4f;
    [SerializeField] private float _runStopDistance = 3.1f;
    [SerializeField] private float _runLookBlend = 0.6f;
    [SerializeField] private float _slotAngle = 12f;
    [SerializeField] private float _slotRadius = 1.2f;
    [SerializeField] private int _slotCount = 7;

    [Header("Idle")]
    [SerializeField] private float _idleMoveMin = 4f;
    [SerializeField] private float _idleMoveMax = 7f;
    [SerializeField] private float _idleWaitMin = 1.4f;
    [SerializeField] private float _idleWaitMax = 2.6f;
    [SerializeField] private float _idleWaitScale = 0.35f;
    [SerializeField] private float _idleTurnMin = 14f;
    [SerializeField] private float _idleTurnMax = 38f;
    [SerializeField] private float _idleLookAngle = 38f;
    [SerializeField] private float _idleReachDistance = 0.2f;

    [Header("Search")]
    [SerializeField] private float _lostChaseDistance = 1.15f;
    [SerializeField] private float _lostStopDistance = 0.05f;
    [SerializeField] private float _searchPointDistance = 0.35f;
    [SerializeField] private float _chaseRetryDelay = 0.45f;
    [SerializeField] private float _chaseRetryGap = 0.8f;

    [Header("Steering")]
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _allyMask = ~0;
    [SerializeField] private float _probeRadius = 0.22f;
    [SerializeField] private float _probeHeight = 0.6f;
    [SerializeField] private float _probeDistance = 0.9f;
    [SerializeField] private float _probeAngle = 25f;
    [SerializeField] private float _avoidWeight = 1.05f;
    [SerializeField] private float _separationRadius = 1.35f;
    [SerializeField] private float _separationWeight = 2.2f;

    [Header("Gizmo")]
    [SerializeField] private bool _isAttackZoneVisible = true;
    [SerializeField] private bool _isMoveGizmoVisible = true;

    private EnemySteering _enemySteering;
    private System.Random _random;
    private EnemyState _state;
    private Vector3 _lastSeenPoint;
    private Vector3 _lastSeenDirection;
    private Vector3 _idleDirection;
    private Vector3 _idleTargetPoint;
    private Vector3 _idleLookPoint;
    private Vector3 _lastSeenMovePoint;
    private Vector3 _searchTargetPoint;
    private Vector3 _moveLastPoint;
    private bool _hasLastSeenPoint;
    private bool _hasLastSeenMovePoint;
    private bool _hasSearchPoint;
    private bool _hasMoveLastPoint;
    private bool _isIdleWalking;
    private float _fightRadius;
    private float _idleLastDistance;
    private float _idleStuckTimer;
    private float _idleTimer;
    private float _moveStuckTimer;
    private int _idleStep;
    private int _searchStep;
    private EnemyRoomLock _enemyRoomLock;
    private Vector3 _attackDirection;
    private bool _isAttackInProgress;
    private bool _isHitPending;
    private Vector3 _chaseBlockPoint;
    private float _chaseBlockTime;
    private bool _hasChaseBlock;

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

        if (_animation == null)
        {
            throw new InvalidOperationException(nameof(_animation));
        }

        if (_animationEvents == null)
        {
            throw new InvalidOperationException(nameof(_animationEvents));
        }

        if (_attackDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_attackDistance));
        }

        if (_attackGap < 0f)
        {
            throw new InvalidOperationException(nameof(_attackGap));
        }

        if (_attackGap >= _attackDistance)
        {
            throw new InvalidOperationException(nameof(_attackGap));
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

        if (_combatChaos < 0f)
        {
            throw new InvalidOperationException(nameof(_combatChaos));
        }

        if (_combatTolerance < 0f)
        {
            throw new InvalidOperationException(nameof(_combatTolerance));
        }

        if (_fightExitGap < 0f)
        {
            throw new InvalidOperationException(nameof(_fightExitGap));
        }

        if (_runStartDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_runStartDistance));
        }

        if (_runStopDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_runStopDistance));
        }

        if (_runStartDistance < _runStopDistance)
        {
            throw new InvalidOperationException(nameof(_runStartDistance));
        }

        if (_runLookBlend < 0f)
        {
            throw new InvalidOperationException(nameof(_runLookBlend));
        }

        if (_runLookBlend > 1f)
        {
            throw new InvalidOperationException(nameof(_runLookBlend));
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

        if (_idleWaitScale <= 0f)
        {
            throw new InvalidOperationException(nameof(_idleWaitScale));
        }

        if (_idleTurnMin < 0f)
        {
            throw new InvalidOperationException(nameof(_idleTurnMin));
        }

        if (_idleTurnMax < _idleTurnMin)
        {
            throw new InvalidOperationException(nameof(_idleTurnMax));
        }

        if (_idleLookAngle < 0f)
        {
            throw new InvalidOperationException(nameof(_idleLookAngle));
        }

        if (_idleReachDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_idleReachDistance));
        }

        if (_lostChaseDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_lostChaseDistance));
        }

        if (_lostStopDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_lostStopDistance));
        }

        if (_searchPointDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_searchPointDistance));
        }

        if (_chaseRetryDelay <= 0f)
        {
            throw new InvalidOperationException(nameof(_chaseRetryDelay));
        }

        if (_chaseRetryGap <= 0f)
        {
            throw new InvalidOperationException(nameof(_chaseRetryGap));
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

        _random = new System.Random(GetSeed());
        _enemySteering = new EnemySteering(transform, _enemyMove, _enemyRotator);
        _enemySteering.SetObstacle(_obstacleMask, _probeRadius, _probeHeight, _probeDistance, _probeAngle, _avoidWeight);
        _enemySteering.SetSpacing(_allyMask, _separationRadius, _separationWeight);
        _enemySteering.SetSlot(_slotAngle, _slotRadius, _slotCount);
    }

    private void OnEnable()
    {
        _enemy.Died += OnDied;
        _animationEvents.Attacking += OnAttackingFrame;
        _animationEvents.AttackEnded += OnAttackEnded;
        ResetState();
    }

    private void OnDisable()
    {
        _enemy.Died -= OnDied;
        _animationEvents.Attacking -= OnAttackingFrame;
        _animationEvents.AttackEnded -= OnAttackEnded;
        _enemyMove.SetRun(false);
        ClearChaseBlock();
        _enemySteering.Stop();
        ResetAttackState();
    }

    private void FixedUpdate()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        RefreshRoomLock();
        _targetVision.Refresh();
        Transform currentTarget = GetVisibleTarget();

        if (_enemySteering.ResolveOverlap())
        {
            RotateTarget(currentTarget);
            ResetMoveStuck();

            return;
        }

        UpdateLastSeenPoint();

        if (currentTarget != null)
        {
            ProcessVisibleTarget(currentTarget);

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

        Vector3 targetPoint = GetFlatPoint(_targetVision.CurrentTargetPoint);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawWireSphere(targetPoint, PointGizmoSize);
    }

    private void ProcessVisibleTarget(Transform currentTarget)
    {
        _isIdleWalking = false;
        _hasSearchPoint = false;
        _searchStep = 0;

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 targetPoint = GetTargetPoint(currentTarget);
        float distance = Vector3.Distance(currentPoint, targetPoint);

        if (_isAttackInProgress)
        {
            _state = EnemyState.Fight;
            StopAttackMove();
            _enemySteering.LookToPoint(targetPoint);

            return;
        }

        if (IsChaseNeeded(distance))
        {
            _state = EnemyState.Chase;

            if (IsChaseBlocked(targetPoint))
            {
                _enemyMove.SetRun(false);
                ResetMoveStuck();
                _enemySteering.LookToPoint(targetPoint);

                return;
            }

            bool isRunning = IsRunNeeded(distance);
            float lookBlend = 1f;

            if (isRunning)
            {
                lookBlend = _runLookBlend;
            }

            _enemyMove.SetRun(isRunning);

            if (TryVisibleMove(_enemySteering.ChaseTarget(targetPoint, _fightRadius, _combatTolerance, lookBlend), currentPoint, targetPoint))
            {
                return;
            }

            ResetMoveStuck();
            _enemySteering.LookToPoint(targetPoint);

            return;
        }

        ClearChaseBlock();
        _enemyMove.SetRun(false);

        if (_enemyMove.MoveAmount > 0f)
        {
            _state = EnemyState.Chase;
            _enemyMove.ForceStop();
            ResetMoveStuck();
            _enemySteering.LookToPoint(targetPoint);

            return;
        }

        ProcessFight(targetPoint, distance);
    }

    private void ProcessHiddenTarget()
    {
        if (_isAttackInProgress)
        {
            _state = EnemyState.Fight;
            StopAttackMove();

            return;
        }

        if (_hasLastSeenPoint == false)
        {
            _enemyMove.SetRun(false);
            ProcessIdle();

            return;
        }

        ClearChaseBlock();
        _isIdleWalking = false;

        Vector3 currentPoint = GetFlatPoint(transform.position);

        if (_state != EnemyState.Search)
        {
            if (TryLostPoint(currentPoint))
            {
                return;
            }
        }

        _state = EnemyState.Search;
        _enemyMove.SetRun(false);
        ProcessSearch(currentPoint);
    }

    private void ProcessIdle()
    {
        ClearChaseBlock();
        _enemyMove.SetRun(false);
        _hasSearchPoint = false;
        _searchStep = 0;

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
        float stopDistance = GetIdleStopDistance();

        TickIdleStuck(distance, stopDistance);

        if (_idleStuckTimer >= IdleStuckSeconds)
        {
            StartIdleWalk();

            return;
        }

        if (distance > stopDistance)
        {
            if (_enemySteering.MoveToPoint(_idleTargetPoint, stopDistance))
            {
                return;
            }

            StartIdleWalk();

            return;
        }

        StartIdleLook();
    }

    private void ProcessIdleLook()
    {
        ResetMoveStuck();
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
        _enemyMove.SetRun(false);

        if (_hasSearchPoint == false)
        {
            if (StartSearchPoint() == false)
            {
                FinishSearch();

                return;
            }
        }

        float distance = Vector3.Distance(currentPoint, _searchTargetPoint);

        if (distance > _searchPointDistance)
        {
            TrySearchMove(_enemySteering.MoveToPoint(_searchTargetPoint, _searchPointDistance), currentPoint);

            return;
        }

        AdvanceSearchPoint();
    }

    private void StartIdleWalk()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);
        int attemptIndex = 0;

        while (attemptIndex < IdlePointTryCount)
        {
            Vector3 nextDirection = _idleDirection;

            if (attemptIndex > 0)
            {
                nextDirection = GetNextIdleDirection();
            }

            float idleDistance = GetIdleDistance();

            if (TrySetIdlePoint(currentPoint, nextDirection, idleDistance))
            {
                return;
            }

            attemptIndex += 1;
        }

        if (TrySetIdlePoint(currentPoint, _idleDirection, IdleFallbackDistance))
        {
            return;
        }

        StartIdleLook();
    }

    private void StartIdleLook()
    {
        _isIdleWalking = false;
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        ResetMoveStuck();
        _enemyMove.SetRun(false);
        _idleTimer = GetIdleWait();
        _idleLookPoint = transform.position + (GetIdleLookDirection() * LookDistance);
        _enemySteering.Stop();
    }

    private bool StartSearchPoint()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);

        while (_searchStep < SearchStepsCount)
        {
            Vector3 candidatePoint = _enemySteering.GetSafePoint(GetFlatPoint(GetSearchPoint()), _probeRadius);
            float distance = Vector3.Distance(currentPoint, candidatePoint);

            if (distance <= _searchPointDistance)
            {
                _searchStep += 1;

                continue;
            }

            if (_enemySteering.HasPointClearance(candidatePoint) == false)
            {
                _searchStep += 1;

                continue;
            }

            _searchTargetPoint = candidatePoint;
            _hasSearchPoint = true;

            return true;
        }

        return false;
    }

    private void AdvanceSearchPoint()
    {
        _lastSeenMovePoint = _searchTargetPoint;
        _hasLastSeenMovePoint = true;
        _hasSearchPoint = false;
        _searchStep = 0;
        ResetMoveStuck();
    }

    private void FinishSearch()
    {
        Vector3 searchDirection = GetSearchDirection();

        ClearSearch();

        _state = EnemyState.Watch;
        _idleDirection = searchDirection;
        _isIdleWalking = false;
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        _idleTimer = GetIdleWait();
        _idleLookPoint = transform.position + (_idleDirection * LookDistance);
        _enemySteering.Stop();
    }

    private void ProcessFight(Vector3 targetPoint, float distance)
    {
        _state = EnemyState.Fight;
        StopAttackMove();

        if (TryAttack(targetPoint, distance))
        {
            ResetMoveStuck();

            return;
        }

        ResetMoveStuck();
        _enemySteering.LookToPoint(targetPoint);
    }

    private bool TryVisibleMove(bool isMoving, Vector3 currentPoint, Vector3 targetPoint)
    {
        if (isMoving == false)
        {
            SetChaseBlock(targetPoint);
            ResetMoveStuck();

            return false;
        }

        ClearChaseBlock();

        if (CanKeepMove(currentPoint))
        {
            return true;
        }

        SetChaseBlock(targetPoint);
        _enemyMove.ForceStop();
        ResetMoveStuck();

        return false;
    }

    private bool TrySearchMove(bool isMoving, Vector3 currentPoint)
    {
        if (isMoving == false)
        {
            _hasSearchPoint = false;
            _searchStep += 1;
            _enemyMove.ForceStop();
            ResetMoveStuck();

            return false;
        }

        if (CanKeepMove(currentPoint))
        {
            return true;
        }

        _hasSearchPoint = false;
        _searchStep += 1;
        _enemyMove.ForceStop();
        ResetMoveStuck();

        return false;
    }

    private bool TryAttack(Vector3 targetPoint, float distance)
    {
        if (_isAttackInProgress)
        {
            StopAttackMove();
            _enemySteering.LookToPoint(targetPoint);

            return true;
        }

        if (distance > GetAttackStartDistance())
        {
            return false;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 attackDirection = targetPoint - currentPoint;
        float angleToTarget = Vector3.Angle(GetStartDirection(), attackDirection);

        if (angleToTarget > _attackAngle)
        {
            _enemySteering.LookToPoint(targetPoint);

            return true;
        }

        StopAttackMove();
        _enemySteering.LookToPoint(targetPoint);

        if (_attacker.CanStartAttack() == false)
        {
            return true;
        }

        _attackDirection = attackDirection;
        _isAttackInProgress = true;
        _isHitPending = true;
        _animation.TriggerAttack();
        RefreshFight();

        return true;
    }

    private void OnAttackingFrame()
    {
        if (_isAttackInProgress == false)
        {
            return;
        }

        if (_isHitPending == false)
        {
            return;
        }

        StopAttackMove();
        _isHitPending = false;
        _attacker.PerformAttack(_attackDirection);
    }

    private void OnAttackEnded()
    {
        if (_isAttackInProgress == false)
        {
            return;
        }

        ResetAttackState();
    }

    private bool CanKeepMove(Vector3 currentPoint)
    {
        if (_hasMoveLastPoint == false)
        {
            _moveLastPoint = currentPoint;
            _moveStuckTimer = 0f;
            _hasMoveLastPoint = true;

            return true;
        }

        float moveDistance = Vector3.Distance(currentPoint, _moveLastPoint);

        if (moveDistance >= MoveStuckMin)
        {
            _moveLastPoint = currentPoint;
            _moveStuckTimer = 0f;

            return true;
        }

        _moveLastPoint = currentPoint;
        _moveStuckTimer += Time.fixedDeltaTime;

        if (_moveStuckTimer < MoveStuckTime)
        {
            return true;
        }

        _moveStuckTimer = 0f;

        return false;
    }

    private void ResetMoveStuck()
    {
        _hasMoveLastPoint = false;
        _moveLastPoint = Vector3.zero;
        _moveStuckTimer = 0f;
    }

    private void UpdateLastSeenPoint()
    {
        Transform currentTarget = GetVisibleTarget();

        if (currentTarget == null)
        {
            return;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 targetPoint = GetTargetPoint(currentTarget);
        Vector3 direction = targetPoint - currentPoint;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
            _lastSeenDirection = direction;
        }

        _lastSeenPoint = targetPoint;
        _hasLastSeenPoint = true;
        _hasLastSeenMovePoint = false;
    }

    private void ResetState()
    {
        _state = EnemyState.Idle;
        _idleTimer = 0f;
        _hasLastSeenPoint = false;
        _hasLastSeenMovePoint = false;
        _hasSearchPoint = false;
        _isIdleWalking = false;
        ClearChaseBlock();
        ResetAttackState();
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        ResetMoveStuck();
        _idleStep = 0;
        _searchStep = 0;
        RefreshFight();
        _lastSeenDirection = GetRandomDirection();
        _idleDirection = _lastSeenDirection;
        _idleLookPoint = transform.position + (_idleDirection * LookDistance);
        _enemyMove.SetRun(false);
        _enemyRotator.SnapToDirection(_idleDirection);
        _enemySteering.Stop();
        StartIdleWalk();

        if (_isIdleWalking)
        {
            _state = EnemyState.Patrol;
        }

        else
        {
            _state = EnemyState.Watch;
        }
    }

    private void OnDied()
    {
        _enemyMove.SetRun(false);
        ClearChaseBlock();
        ResetMoveStuck();
        ResetAttackState();
        _enemySteering.Stop();
        enabled = false;
    }

    private void RefreshRoomLock()
    {
        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();
        _enemySteering.SetRoomLock(enemyRoomLock);
    }

    private float GetIdleStopDistance()
    {
        return _idleReachDistance;
    }

    private bool TrySetIdlePoint(Vector3 currentPoint, Vector3 nextDirection, float idleDistance)
    {
        Vector3 nextPoint = currentPoint + (nextDirection * idleDistance);
        nextPoint.y = transform.position.y;

        Vector3 safePoint = _enemySteering.GetSafePoint(nextPoint, _probeRadius);
        float reachDistance = Vector3.Distance(currentPoint, safePoint);

        if (reachDistance < _idleReachDistance)
        {
            return false;
        }

        if (_enemySteering.HasPointClearance(safePoint) == false)
        {
            return false;
        }

        _idleTargetPoint = safePoint;
        _isIdleWalking = true;
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;

        return true;
    }

    private void TickIdleStuck(float distance, float stopDistance)
    {
        if (distance <= stopDistance)
        {
            _idleLastDistance = distance;
            _idleStuckTimer = 0f;

            return;
        }

        if (_idleLastDistance < 0f)
        {
            _idleLastDistance = distance;
            _idleStuckTimer = 0f;

            return;
        }

        if (_idleLastDistance - distance > IdleProgressMin)
        {
            _idleLastDistance = distance;
            _idleStuckTimer = 0f;

            return;
        }

        _idleStuckTimer += Time.fixedDeltaTime;
        _idleLastDistance = distance;
    }

    private float GetIdleDistance()
    {
        return GetRandomRange(_idleMoveMin, _idleMoveMax);
    }

    private float GetIdleWait()
    {
        return GetRandomRange(_idleWaitMin, _idleWaitMax) * _idleWaitScale;
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
        return GetRandomTurn(_idleTurnMin, _idleTurnMax);
    }

    private float GetIdleLookTurn()
    {
        if (Next01() < IdleFrontChance)
        {
            return 0f;
        }

        return GetRandomTurn(0f, _idleLookAngle);
    }

    private Vector3 GetSearchPoint()
    {
        Vector3 baseDirection = GetSearchDirection();
        Vector3 startPoint = GetSearchStartPoint();
        float searchDistance = GetSearchStride() * (_searchStep + 1);

        return startPoint + (baseDirection * searchDistance);
    }

    private Vector3 GetLostChasePoint()
    {
        Vector3 chaseDirection = GetSearchDirection();
        Vector3 chasePoint = _lastSeenPoint + (chaseDirection * _lostChaseDistance);

        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        if (enemyRoomLock != null)
        {
            chasePoint = enemyRoomLock.ClampMovePoint(chasePoint);
        }

        return GetFlatPoint(chasePoint);
    }

    private void RefreshFight()
    {
        _fightRadius = GetFightRadius();
    }

    private void ResetAttackState()
    {
        _attackDirection = Vector3.zero;
        _isAttackInProgress = false;
        _isHitPending = false;
    }

    private void StopAttackMove()
    {
        _enemyMove.SetRun(false);
        _enemyMove.ForceStop();
    }

    private bool IsRunNeeded(float distance)
    {
        if (_enemyMove.IsRunRequested)
        {
            if (distance > _runStopDistance)
            {
                return true;
            }

            return false;
        }

        if (distance >= _runStartDistance)
        {
            return true;
        }

        return false;
    }

    private bool IsChaseBlocked(Vector3 targetPoint)
    {
        if (_hasChaseBlock == false)
        {
            return false;
        }

        float pointDistance = Vector3.Distance(_chaseBlockPoint, targetPoint);

        if (pointDistance > _chaseRetryGap)
        {
            ClearChaseBlock();

            return false;
        }

        if (Time.time >= _chaseBlockTime)
        {
            ClearChaseBlock();

            return false;
        }

        return true;
    }

    private void SetChaseBlock(Vector3 targetPoint)
    {
        _chaseBlockPoint = targetPoint;
        _chaseBlockTime = Time.time + _chaseRetryDelay;
        _hasChaseBlock = true;
    }

    private void ClearChaseBlock()
    {
        _chaseBlockPoint = Vector3.zero;
        _chaseBlockTime = 0f;
        _hasChaseBlock = false;
    }

    private bool TryLostPoint(Vector3 currentPoint)
    {
        Vector3 lostPoint = GetLostChasePoint();
        float distance = Vector3.Distance(currentPoint, lostPoint);
        _lastSeenMovePoint = lostPoint;
        _hasLastSeenMovePoint = true;

        if (distance <= _lostStopDistance)
        {
            return false;
        }

        _state = EnemyState.Chase;
        _enemyMove.SetRun(IsRunNeeded(distance));

        return TryLostMove(_enemySteering.MoveToPoint(lostPoint, _lostStopDistance), currentPoint);
    }

    private bool TryLostMove(bool isMoving, Vector3 currentPoint)
    {
        if (isMoving == false)
        {
            _hasLastSeenMovePoint = false;
            _enemyMove.ForceStop();
            ResetMoveStuck();

            return false;
        }

        if (CanKeepMove(currentPoint))
        {
            return true;
        }

        _hasLastSeenMovePoint = false;
        _enemyMove.ForceStop();
        ResetMoveStuck();

        return false;
    }

    private bool IsChaseNeeded(float distance)
    {
        float chaseDistance = _fightRadius;

        if (_state == EnemyState.Fight)
        {
            chaseDistance += _fightExitGap;
        }

        if (chaseDistance > GetAttackStartDistance())
        {
            chaseDistance = GetAttackStartDistance();
        }

        if (distance > chaseDistance)
        {
            return true;
        }

        return false;
    }

    private float GetFightRadius()
    {
        float attackStartDistance = GetAttackStartDistance();
        float maxFightRadius = Mathf.Max(MinFightRadius, attackStartDistance - MinFightGap);
        float minRadius = Mathf.Max(MinFightRadius, _combatRadius - _combatChaos);
        float maxRadius = Mathf.Min(_combatRadius + _combatChaos, maxFightRadius);

        if (minRadius > maxFightRadius)
        {
            minRadius = maxFightRadius;
        }

        if (maxRadius < minRadius)
        {
            maxRadius = minRadius;
        }

        return GetRandomRange(minRadius, maxRadius);
    }

    private Vector3 GetSearchStartPoint()
    {
        if (_hasLastSeenMovePoint)
        {
            return _lastSeenMovePoint;
        }

        return _lastSeenPoint;
    }

    private Transform GetVisibleTarget()
    {
        if (_targetVision.IsTargetVisible == false)
        {
            return null;
        }

        Transform currentTarget = _targetVision.CurrentTarget;

        if (currentTarget == null)
        {
            return null;
        }

        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        if (enemyRoomLock == null)
        {
            return currentTarget;
        }

        Vector3 targetPoint = GetTargetPoint(currentTarget);

        if (enemyRoomLock.ContainsRoomPoint(currentTarget.position) == false && enemyRoomLock.ContainsRoomPoint(targetPoint) == false)
        {
            return null;
        }

        return currentTarget;
    }

    private Vector3 GetTargetPoint(Transform currentTarget)
    {
        if (currentTarget == null)
        {
            return GetFlatPoint(transform.position);
        }

        if (_targetVision != null)
        {
            if (currentTarget == _targetVision.CurrentTarget)
            {
                return GetFlatPoint(_targetVision.CurrentTargetPoint);
            }
        }

        return GetFlatPoint(currentTarget.position);
    }

    private EnemyRoomLock GetEnemyRoomLock()
    {
        if (_enemyRoomLock != null)
        {
            return _enemyRoomLock;
        }

        _enemyRoomLock = GetComponent<EnemyRoomLock>();

        return _enemyRoomLock;
    }

    private void RotateTarget(Transform currentTarget)
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 targetPoint = GetTargetPoint(currentTarget);
        _enemyRotator.RotateToPoint(targetPoint);
    }

    private float GetAttackStartDistance()
    {
        return _attackDistance - _attackGap;
    }

    private float GetSearchStride()
    {
        return Mathf.Max(_lostChaseDistance, Mathf.Max(_combatRadius, _probeDistance));
    }

    private void ClearSearch()
    {
        _hasLastSeenPoint = false;
        _hasLastSeenMovePoint = false;
        _hasSearchPoint = false;
        _searchStep = 0;
        ResetMoveStuck();
    }

    private float GetRandomRange(float minValue, float maxValue)
    {
        if (maxValue - minValue <= 0.0001f)
        {
            return minValue;
        }

        return Mathf.Lerp(minValue, maxValue, Next01());
    }

    private float GetRandomTurn(float minAngle, float maxAngle)
    {
        float angle = GetRandomRange(minAngle, maxAngle);

        if (GetRandomBool())
        {
            return angle;
        }

        return -angle;
    }

    private bool GetRandomBool()
    {
        return Next01() >= 0.5f;
    }

    private float Next01()
    {
        if (_random == null)
        {
            return 0.5f;
        }

        return (float)_random.NextDouble();
    }

    private int GetSeed()
    {
        int seed = gameObject.GetInstanceID();

        if (seed == int.MinValue)
        {
            return int.MaxValue;
        }

        if (seed < 0)
        {
            seed = -seed;
        }

        return seed;
    }

    private Vector3 GetRandomDirection()
    {
        float turnDegrees = GetRandomRange(0f, 360f);
        Quaternion rotation = Quaternion.Euler(0f, turnDegrees, 0f);
        Vector3 direction = rotation * Vector3.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        direction.Normalize();

        return direction;
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
        if (_enemyRotator != null)
        {
            return _enemyRotator.ForwardDirection;
        }

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
