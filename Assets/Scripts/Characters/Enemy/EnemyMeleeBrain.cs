using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyMeleeBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert
{
    private const float AlertGap = 1f;
    private const int SearchStepsCount = 4;
    private const int IdlePointTryCount = 16;
    private const int IdleChainMin = 2;
    private const int IdleChainMax = 4;
    private const float IdleProgressMin = 0.02f;
    private const float IdleStuckSeconds = 0.35f;
    private const float MoveStuckMin = 0.01f;
    private const float MoveStuckTime = 0.3f;
    private const float IdleFrontChance = 0.3f;
    private const float IdleFallbackDistance = 1.6f;
    private const float IdleStrideMinScale = 0.35f;
    private const float IdleStrideMaxScale = 0.55f;
    private const float IdleSoftTurnScale = 0.45f;
    private const float IdleWideTurnChance = 0.22f;
    private const float MinFightRadius = 0.1f;
    private const float RangeHoldScale = 0.2f;
    private const float LookDistance = 2f;
    private const float ForwardGizmoLength = 1.1f;
    private const float MoveGizmoLength = 1.35f;
    private const float PointGizmoSize = 0.18f;
    private const float ZeroThreshold = 0.0001f;
    private const int RangeFight = 0;
    private const int RangeChase = 1;
    private const int RangeBack = -1;

    private readonly EnemyPatrolPicker _idlePatrolPicker = new EnemyPatrolPicker();

    [Header("Dependencies")]
    [SerializeField] private Enemy _enemy;
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private EnemyMove _enemyMove;
    [SerializeField] private EnemyRotator _enemyRotator;
    [SerializeField] private Attacker _attacker;
    [SerializeField] private EnemyAnimation _animation;
    [SerializeField] private PlayerAnimationEvents _animationEvents;
    [SerializeField] private WeaponHolder _weaponHolder;

    [Header("Combat")]
    [SerializeField] private float _runStartDistance = 4.4f;
    [SerializeField] private float _runStopDistance = 3.1f;

    [Header("Range")]
    [SerializeField] private float _fightMinDistance = 2.75f;
    [SerializeField] private float _fightMaxDistance = 5.75f;
    [SerializeField] private float _fireMaxDistance = 7.25f;
    [SerializeField] private float _fightGapDistance = 0.45f;
    [SerializeField] private float _rangeRunStart = 7f;
    [SerializeField] private float _rangeRunStop = 5.5f;

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
    [SerializeField] private float _idleWallGap = 0.6f;

    [Header("Search")]
    [SerializeField] private float _lostChaseDistance = 1.15f;
    [SerializeField] private float _lostStopDistance = 0.05f;
    [SerializeField] private float _searchPointDistance = 0.35f;

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
    private float _idleLastDistance;
    private float _idleStuckTimer;
    private float _idleTimer;
    private float _moveStuckTimer;
    private int _idleChain;
    private int _searchStep;
    private EnemyRoomLock _enemyRoomLock;
    private int _rangeMode;
    private Vector3 _attackDirection;
    private bool _isAttackInProgress;
    private bool _isHitPending;

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

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 alertPoint = GetFlatPoint(point);
        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        if (enemyRoomLock != null)
        {
            alertPoint = GetFlatPoint(enemyRoomLock.ClampMovePoint(alertPoint));
        }

        alertPoint = GetMovePoint(alertPoint);

        if (_hasLastSeenPoint)
        {
            if (Vector3.Distance(_lastSeenPoint, alertPoint) <= AlertGap)
            {
                return false;
            }
        }

        Vector3 alertDirection = alertPoint - currentPoint;
        alertDirection.y = 0f;

        if (alertDirection.sqrMagnitude > ZeroThreshold)
        {
            alertDirection.Normalize();
            _lastSeenDirection = alertDirection;
        }

        _lastSeenPoint = alertPoint;
        _hasLastSeenPoint = true;
        _hasLastSeenMovePoint = false;
        _hasSearchPoint = false;
        _searchStep = 0;
        _isIdleWalking = false;
        _idleTimer = 0f;
        StopFire();
        ResetMoveStuck();
        _enemySteering.Stop();

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

        if (_weaponHolder == null)
        {
            throw new InvalidOperationException(nameof(_weaponHolder));
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

        if (_fightMinDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_fightMinDistance));
        }

        if (_fightMaxDistance < _fightMinDistance)
        {
            throw new InvalidOperationException(nameof(_fightMaxDistance));
        }

        if (_fireMaxDistance < _fightMaxDistance)
        {
            throw new InvalidOperationException(nameof(_fireMaxDistance));
        }

        if (_fightGapDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_fightGapDistance));
        }

        if (_rangeRunStart <= 0f)
        {
            throw new InvalidOperationException(nameof(_rangeRunStart));
        }

        if (_rangeRunStop <= 0f)
        {
            throw new InvalidOperationException(nameof(_rangeRunStop));
        }

        if (_rangeRunStart < _rangeRunStop)
        {
            throw new InvalidOperationException(nameof(_rangeRunStart));
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

        if (_idleWallGap <= 0f)
        {
            throw new InvalidOperationException(nameof(_idleWallGap));
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
        StopFire();
        _enemyMove.SetRun(false);
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
        if (_isMoveGizmoVisible == false)
        {
            return;
        }

        DrawMoveGizmo();
        DrawStateGizmo();
        DrawTargetGizmo();
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
        FireExecutor fireExecutor = GetFireExecutor();

        if (_isAttackInProgress)
        {
            _state = EnemyState.Fight;
            StopAttackMove();
            _enemySteering.LookToPoint(targetPoint);

            return;
        }

        if (fireExecutor != null)
        {
            ProcessRangeTarget(currentPoint, targetPoint, distance, fireExecutor);

            return;
        }

        if (TryAttack(currentTarget, targetPoint))
        {
            _state = EnemyState.Fight;
            ResetMoveStuck();

            return;
        }

        ProcessCombatMove(currentPoint, targetPoint, distance);
    }

    private void ProcessHiddenTarget()
    {
        StopFire();

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

        if (_idleChain > 0)
        {
            _idleChain -= 1;
        }

        if (_idleChain > 0)
        {
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
        bool isNewChain = _idleChain <= 0;

        if (isNewChain)
        {
            _idleChain = GetIdleChainCount();
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);

        if (TryStartPatrolWalk(currentPoint))
        {
            return;
        }

        int attemptIndex = 0;

        while (attemptIndex < IdlePointTryCount)
        {
            Vector3 nextDirection = _idleDirection;

            if (attemptIndex > 0 || isNewChain == false)
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
        _idleChain = 0;
        ResetMoveStuck();
        _enemyMove.SetRun(false);
        _idleTimer = GetIdleWait();
        _idleLookPoint = transform.position + (GetIdleLookDirection() * LookDistance);
        _enemySteering.Stop();
    }

    private bool StartSearchPoint()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);
        float wallGap = GetMoveWallGap();

        while (_searchStep < SearchStepsCount)
        {
            Vector3 candidatePoint = _enemySteering.GetSafePoint(GetFlatPoint(GetSearchPoint()), wallGap);
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
        _idleChain = 0;
        _idleTimer = GetIdleWait();
        _idleLookPoint = transform.position + (_idleDirection * LookDistance);
        _enemySteering.Stop();
    }

    private void ProcessCombatMove(Vector3 currentPoint, Vector3 targetPoint, float distance)
    {
        _state = EnemyState.Chase;
        _enemyMove.SetRun(IsRunNeeded(distance));
        Vector3 chasePoint = GetMovePoint(targetPoint);

        if (TryCombatMove(currentPoint, chasePoint, targetPoint))
        {
            return;
        }

        _enemyMove.SetRun(false);
        ResetMoveStuck();
        _enemySteering.LookToPoint(targetPoint);
    }

    private void ProcessRangeTarget(Vector3 currentPoint, Vector3 targetPoint, float distance, FireExecutor fireExecutor)
    {
        bool hasFireLine = HasFireLine(targetPoint);
        fireExecutor.SetAimPoint(targetPoint);

        if (_state != EnemyState.Chase && _state != EnemyState.Fight)
        {
            _rangeMode = GetInitialRangeMode(distance);
        }

        UpdateRangeMode(distance);

        if (_rangeMode == RangeChase)
        {
            ProcessRangeChase(currentPoint, targetPoint, distance);
        }

        else
        {
            if (_rangeMode == RangeBack)
            {
                ProcessRangeBack(currentPoint, targetPoint);
            }

            else
            {
                ProcessRangeFight(currentPoint, targetPoint, hasFireLine);
            }
        }

        TryShoot(fireExecutor, targetPoint, distance, hasFireLine);
    }

    private void ProcessRangeChase(Vector3 currentPoint, Vector3 targetPoint, float distance)
    {
        _state = EnemyState.Chase;
        _enemyMove.SetRun(IsRangeRunNeeded(distance));

        if (TryRangeHold(currentPoint, targetPoint))
        {
            return;
        }

        _enemyMove.SetRun(false);
        _enemySteering.LookToPoint(targetPoint);
    }

    private void ProcessRangeBack(Vector3 currentPoint, Vector3 targetPoint)
    {
        _state = EnemyState.Fight;
        _enemyMove.SetRun(false);

        if (TryRangeHold(currentPoint, targetPoint))
        {
            return;
        }

        _enemySteering.LookToPoint(targetPoint);
    }

    private void ProcessRangeFight(Vector3 currentPoint, Vector3 targetPoint, bool hasFireLine)
    {
        _state = EnemyState.Fight;
        _enemyMove.SetRun(false);

        if (hasFireLine)
        {
            if (TryRangeHold(currentPoint, targetPoint))
            {
                return;
            }

            ResetMoveStuck();
            _enemySteering.Stop();
            _enemyRotator.RotateToPoint(targetPoint);

            return;
        }

        if (TryRangeMove(GetMovePoint(targetPoint), _fightMinDistance, targetPoint, currentPoint))
        {
            return;
        }

        _enemySteering.LookToPoint(targetPoint);
    }

    private bool TryRangeHold(Vector3 currentPoint, Vector3 targetPoint)
    {
        float holdDistance = GetRangeHoldDistance();
        float holdGap = GetRangeHoldGap();
        Vector3 holdPoint = GetRangeHoldPoint(currentPoint, targetPoint, holdDistance);

        if (Vector3.Distance(currentPoint, holdPoint) <= holdGap)
        {
            return false;
        }

        return TryRangeMove(GetMovePoint(holdPoint), holdGap, targetPoint, currentPoint);
    }

    private bool TryRangeMove(Vector3 movePoint, float stopDistance, Vector3 lookPoint, Vector3 currentPoint)
    {
        bool isMoving = _enemySteering.MoveToPoint(movePoint, stopDistance, lookPoint);

        if (isMoving == false)
        {
            _enemyMove.ForceStop();
            ResetMoveStuck();

            return false;
        }

        if (CanKeepMove(currentPoint))
        {
            return true;
        }

        _enemyMove.ForceStop();
        ResetMoveStuck();

        return false;
    }

    private int GetInitialRangeMode(float distance)
    {
        if (distance > _fightMaxDistance)
        {
            return RangeChase;
        }

        if (distance < _fightMinDistance)
        {
            return RangeBack;
        }

        return RangeFight;
    }

    private void UpdateRangeMode(float distance)
    {
        if (_rangeMode == RangeChase)
        {
            if (distance > GetRangeEnterMax())
            {
                return;
            }

            _rangeMode = RangeFight;

            return;
        }

        if (_rangeMode == RangeBack)
        {
            if (distance < GetRangeEnterMin())
            {
                return;
            }

            _rangeMode = RangeFight;

            return;
        }

        if (distance > GetRangeExitMax())
        {
            _rangeMode = RangeChase;

            return;
        }

        if (distance < GetRangeExitMin())
        {
            _rangeMode = RangeBack;
        }
    }

    private void TryShoot(FireExecutor fireExecutor, Vector3 targetPoint, float distance, bool hasFireLine)
    {
        if (distance > _fireMaxDistance)
        {
            return;
        }

        if (hasFireLine == false)
        {
            return;
        }

        fireExecutor.SetAimPoint(targetPoint);
        fireExecutor.TryFire();
    }

    private bool TryCombatMove(Vector3 currentPoint, Vector3 movePoint, Vector3 lookPoint)
    {
        float stopDistance = GetCombatStopDistance();
        bool isMoving = _enemySteering.MoveToPoint(movePoint, stopDistance, lookPoint);

        if (isMoving == false)
        {
            _enemyMove.ForceStop();
            ResetMoveStuck();

            return false;
        }

        if (CanKeepMove(currentPoint))
        {
            _state = EnemyState.Chase;

            return true;
        }

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

    private bool TryAttack(Transform currentTarget, Vector3 targetPoint)
    {
        if (_isAttackInProgress)
        {
            StopAttackMove();
            _enemySteering.LookToPoint(targetPoint);

            return true;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 attackDirection = targetPoint - currentPoint;

        if (CanHitTarget(currentTarget, attackDirection) == false)
        {
            return false;
        }

        StopAttackMove();
        _enemyRotator.SnapToDirection(attackDirection);

        if (_attacker.CanStartAttack() == false)
        {
            return true;
        }

        _attackDirection = attackDirection;
        _isAttackInProgress = true;
        _isHitPending = true;
        _animation.TriggerAttack();

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
        _rangeMode = RangeFight;
        _idleTimer = 0f;
        _hasLastSeenPoint = false;
        _hasLastSeenMovePoint = false;
        _hasSearchPoint = false;
        _isIdleWalking = false;
        ResetAttackState();
        StopFire();
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        ResetMoveStuck();
        _idleChain = 0;
        _searchStep = 0;
        _idlePatrolPicker.Clear();
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
        StopFire();
        _enemyMove.SetRun(false);
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

    private bool TryStartPatrolWalk(Vector3 currentPoint)
    {
        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        return _idlePatrolPicker.TryPickNextPoint(enemyRoomLock, currentPoint, GetPatrolForward(), transform.position.y, IdlePointTryCount, GetRandomPatrolDirection, patrolPoint => TrySetPatrolPoint(currentPoint, patrolPoint));
    }

    private bool TrySetIdlePoint(Vector3 currentPoint, Vector3 nextDirection, float idleDistance)
    {
        if (TryApplyIdlePoint(currentPoint, nextDirection, idleDistance, _idleWallGap))
        {
            return true;
        }

        if (_idleWallGap <= _probeRadius)
        {
            return false;
        }

        return TryApplyIdlePoint(currentPoint, nextDirection, idleDistance, _probeRadius);
    }

    private bool TrySetPatrolPoint(Vector3 currentPoint, Vector3 patrolPoint)
    {
        if (TryApplyPatrolPoint(currentPoint, patrolPoint, _idleWallGap))
        {
            return true;
        }

        if (_idleWallGap <= _probeRadius)
        {
            return false;
        }

        return TryApplyPatrolPoint(currentPoint, patrolPoint, _probeRadius);
    }

    private bool TryApplyIdlePoint(Vector3 currentPoint, Vector3 nextDirection, float idleDistance, float wallGap)
    {
        Vector3 nextPoint = currentPoint + (nextDirection * idleDistance);
        nextPoint.y = transform.position.y;

        return TrySetIdleTarget(currentPoint, nextPoint, wallGap);
    }

    private bool TryApplyPatrolPoint(Vector3 currentPoint, Vector3 patrolPoint, float wallGap)
    {
        Vector3 nextPoint = GetFlatPoint(patrolPoint);

        return TrySetIdleTarget(currentPoint, nextPoint, wallGap);
    }

    private bool TrySetIdleTarget(Vector3 currentPoint, Vector3 nextPoint, float wallGap)
    {
        Vector3 safePoint = _enemySteering.GetSafePoint(nextPoint, wallGap);
        float reachDistance = Vector3.Distance(currentPoint, safePoint);

        if (reachDistance < _idleReachDistance)
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

        Vector3 nextDirection = safePoint - currentPoint;
        nextDirection.y = 0f;

        if (nextDirection.sqrMagnitude > ZeroThreshold)
        {
            nextDirection.Normalize();
            _idleDirection = nextDirection;
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
        float idleDistance = GetRandomRange(_idleMoveMin, _idleMoveMax);
        float strideScale = GetRandomRange(IdleStrideMinScale, IdleStrideMaxScale);

        return idleDistance * strideScale;
    }

    private int GetIdleChainCount()
    {
        return Mathf.FloorToInt(GetRandomRange(IdleChainMin, IdleChainMax + 1f));
    }

    private float GetIdleWait()
    {
        return GetRandomRange(_idleWaitMin, _idleWaitMax) * _idleWaitScale;
    }

    private int GetRandomPatrolDirection()
    {
        if (GetRandomBool())
        {
            return 1;
        }

        return -1;
    }

    private Vector3 GetPatrolForward()
    {
        if (_idleDirection.sqrMagnitude > ZeroThreshold)
        {
            return _idleDirection;
        }

        return GetStartDirection();
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
        float minTurn = _idleTurnMin;
        float maxTurn = _idleTurnMax;

        if (Next01() >= IdleWideTurnChance)
        {
            minTurn *= IdleSoftTurnScale;
            maxTurn *= IdleSoftTurnScale;
        }

        return GetRandomTurn(minTurn, maxTurn);
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

        return GetMovePoint(GetFlatPoint(chasePoint));
    }

    private float GetCombatStopDistance()
    {
        return Mathf.Max(MinFightRadius, _fightGapDistance * 0.5f);
    }

    private float GetMoveWallGap()
    {
        return Mathf.Max(_idleWallGap, _probeRadius * 2f);
    }

    private Vector3 GetMovePoint(Vector3 point)
    {
        return _enemySteering.GetSafePoint(point, GetMoveWallGap());
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

    private void StopFire()
    {
        FireExecutor fireExecutor = GetFireExecutor();

        if (fireExecutor == null)
        {
            return;
        }

        fireExecutor.StopFiring();
        fireExecutor.ClearAimPoint();
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

    private bool IsRangeRunNeeded(float distance)
    {
        if (_enemyMove.IsRunRequested)
        {
            if (distance > _rangeRunStop)
            {
                return true;
            }

            return false;
        }

        if (distance >= _rangeRunStart)
        {
            return true;
        }

        return false;
    }

    private bool TryLostPoint(Vector3 currentPoint)
    {
        Vector3 lostPoint = GetLostChasePoint();
        float distance = Vector3.Distance(currentPoint, lostPoint);
        _lastSeenMovePoint = lostPoint;
        _hasLastSeenMovePoint = true;
        float stopDistance = Mathf.Max(_lostStopDistance, _searchPointDistance);

        if (distance <= stopDistance)
        {
            return false;
        }

        _state = EnemyState.Chase;
        _enemyMove.SetRun(IsRunNeeded(distance));

        return TryLostMove(_enemySteering.MoveToPoint(lostPoint, stopDistance), currentPoint);
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

    private bool CanHitTarget(Transform currentTarget, Vector3 attackDirection)
    {
        if (currentTarget == null)
        {
            return false;
        }

        return _attacker.CanHitTarget(currentTarget, attackDirection);
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

    private FireExecutor GetFireExecutor()
    {
        return _weaponHolder.FireExecutor;
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

    private bool HasFireLine(Vector3 targetPoint)
    {
        if (_enemySteering.IsLineBlocked(targetPoint))
        {
            return false;
        }

        return true;
    }

    private float GetSearchStride()
    {
        return Mathf.Max(_lostChaseDistance, _probeDistance);
    }

    private void ClearSearch()
    {
        _hasLastSeenPoint = false;
        _hasLastSeenMovePoint = false;
        _hasSearchPoint = false;
        _searchStep = 0;
        _idlePatrolPicker.Clear();
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

    private float GetRangeHoldDistance()
    {
        return (_fightMinDistance + _fightMaxDistance) * 0.5f;
    }

    private float GetRangeEnterMax()
    {
        float rangeEnterMax = _fightMaxDistance - _fightGapDistance;

        return Mathf.Max(_fightMinDistance, rangeEnterMax);
    }

    private float GetRangeEnterMin()
    {
        float rangeEnterMin = _fightMinDistance + _fightGapDistance;

        return Mathf.Min(_fightMaxDistance, rangeEnterMin);
    }

    private float GetRangeExitMax()
    {
        float rangeExitMax = _fightMaxDistance + _fightGapDistance;

        return Mathf.Max(GetRangeEnterMax(), rangeExitMax);
    }

    private float GetRangeExitMin()
    {
        float rangeExitMin = _fightMinDistance - _fightGapDistance;

        return Mathf.Max(MinFightRadius, rangeExitMin);
    }

    private float GetRangeHoldGap()
    {
        float holdGap = (_fightMaxDistance - _fightMinDistance) * RangeHoldScale;

        if (holdGap < _fightGapDistance)
        {
            holdGap = _fightGapDistance;
        }

        return holdGap;
    }

    private Vector3 GetRangeHoldPoint(Vector3 currentPoint, Vector3 targetPoint, float holdDistance)
    {
        Vector3 holdDirection = currentPoint - targetPoint;
        holdDirection.y = 0f;

        if (holdDirection.sqrMagnitude <= ZeroThreshold)
        {
            holdDirection = -GetStartDirection();
        }

        holdDirection.Normalize();

        Vector3 holdPoint = targetPoint + (holdDirection * holdDistance);
        holdPoint.y = transform.position.y;

        return holdPoint;
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
