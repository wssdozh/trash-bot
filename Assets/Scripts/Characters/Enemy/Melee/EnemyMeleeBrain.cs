using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed partial class EnemyMeleeBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert
{
    private const float AlertGap = 1f;
    private const int SearchStepsCount = 4;
    private const int IdlePointTryCount = 16;
    private const int IdleChainMin = 2;
    private const int IdleChainMax = 4;
    private const float IdleProgressMin = 0.02f;
    private const float IdleStuckSeconds = 0.35f;
    private const float IdleFrontChance = 0.3f;
    private const float IdleFallbackDistance = 1.6f;
    private const float IdleStrideMinScale = 0.35f;
    private const float IdleStrideMaxScale = 0.55f;
    private const float IdleSoftTurnScale = 0.45f;
    private const float IdleWideTurnChance = 0.22f;
    private const float MinFightRadius = 0.1f;
    private const float RangeHoldScale = 0.2f;
    private const float RangeModeHoldTime = 1f;
    private const float SearchSideAngle = 55f;
    private const float SearchBackScale = 0.75f;
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
    [SerializeField] private EnemySuicideAttack _suicideAttack;

    [Header("Combat")]
    [SerializeField] private float _runStartDistance = 4.4f;
    [SerializeField] private float _runStopDistance = 3.1f;
    [SerializeField, Range(0f, 1f)] private float _chaseLookBlend = 0.7f;
    [SerializeField, Min(0f)] private float _attackWindupTime = 0.22f;
    [SerializeField, Range(0.1f, 1f)] private float _attackStartNearScale = 0.5f;
    [SerializeField, Range(0.1f, 1f)] private float _attackKeepNearScale = 0.6f;
    [SerializeField, Min(0f)] private float _attackStartExtraScale = 0.12f;
    [SerializeField, Min(0f)] private float _attackKeepExtraScale = 0.22f;
    [SerializeField, Range(-1f, 1f)] private float _attackMinDot = -0.15f;

    [Header("Range")]
    [SerializeField] private float _fightMinDistance = 2.75f;
    [SerializeField] private float _fightMaxDistance = 5.75f;
    [SerializeField] private float _fireMaxDistance = 7.25f;
    [SerializeField] private float _fightGapDistance = 0.45f;
    [SerializeField] private float _rangeRunStart = 7f;
    [SerializeField] private float _rangeRunStop = 5.5f;

    [Header("Role")]
    [SerializeField, Min(0f)] private float _meleeHealthBonus = 3f;
    [SerializeField, Min(0.1f)] private float _meleeSpeedScale = 1.08f;
    [SerializeField, Min(0f)] private float _rangeNearExtra = 0.35f;

    [Header("Combat Steering")]
    [SerializeField] private float _orbitWeight = 1.15f;
    [SerializeField] private float _ringWeight = 0.9f;
    [SerializeField] private float _slotWeight = 1f;
    [SerializeField] private float _slotAngle = 24f;
    [SerializeField] private float _slotRadius = 1.4f;
    [SerializeField] private int _slotCount = 3;
    [SerializeField] private float _recoverBack = 1.2f;
    [SerializeField] private float _recoverSide = 0.8f;

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
    [SerializeField, Min(0.1f)] private float _safeMoveSpeed = 3.5f;

    [Header("Steering")]
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _allyMask = ~0;
    [SerializeField] private float _probeRadius = 0.2f;
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
    private Vector3 _searchTargetPoint;
    private bool _hasLastSeenPoint;
    private bool _hasSearchPoint;
    private bool _isSafeMove;
    private bool _isIdleWalking;
    private bool _isSearchIdle;
    private float _idleLastDistance;
    private float _idleStuckTimer;
    private float _idleTimer;
    private float _baseHealthMax;
    private float _rangeModeTimer;
    private int _idleChain;
    private int _searchStep;
    private EnemyRoomLock _enemyRoomLock;
    private int _rangeMode;
    private Vector3 _attackDirection;
    private bool _isAttackInProgress;
    private bool _isHitPending;
    private bool _isCombatClockwise;
    private bool _isAttackWindup;
    private float _attackWindupTimer;

    public EnemyState State => _state;

    private void Awake()
    {
        ValidateDependencies();
        ValidateCombat();
        ValidateRange();
        ValidateRole();
        ValidateCombatSteering();
        ValidateIdle();
        ValidateSearch();
        ValidateSteering();
        InitializeSteering();
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

        if (_suicideAttack != null)
        {
            _suicideAttack.ResetState();
        }
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

        UpdateLastSeenPoint(currentTarget);

        if (currentTarget != null)
        {
            ProcessVisibleTarget(currentTarget);

            return;
        }

        ProcessHiddenTarget();
    }

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

        if (IsSuicideActive())
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
        _hasSearchPoint = false;
        _searchStep = 0;
        _isIdleWalking = false;
        _isSearchIdle = false;
        _idleTimer = 0f;
        StopFire();
        ResetMoveStuck();
        _enemySteering.Stop();

        return true;
    }

    public void ApplyRole()
    {
        Health health = _enemy.Health;
        float maxHealth = _baseHealthMax;
        float speedScale = 1f;

        if (_suicideAttack == null)
        {
            if (GetFireExecutor() == null)
            {
                maxHealth += _meleeHealthBonus;
                speedScale = _meleeSpeedScale;
            }
        }

        health.SetMaxValue(maxHealth);
        health.Fill();
        _enemyMove.SetSpeedScale(speedScale);
    }

    private void ValidateDependencies()
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
    }

    private void ValidateCombat()
    {
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

        if (_chaseLookBlend < 0f || _chaseLookBlend > 1f)
        {
            throw new InvalidOperationException(nameof(_chaseLookBlend));
        }

        if (_attackWindupTime < 0f)
        {
            throw new InvalidOperationException(nameof(_attackWindupTime));
        }

        if (_attackStartNearScale <= 0f || _attackStartNearScale > 1f)
        {
            throw new InvalidOperationException(nameof(_attackStartNearScale));
        }

        if (_attackKeepNearScale < _attackStartNearScale || _attackKeepNearScale > 1f)
        {
            throw new InvalidOperationException(nameof(_attackKeepNearScale));
        }

        if (_attackStartExtraScale < 0f)
        {
            throw new InvalidOperationException(nameof(_attackStartExtraScale));
        }

        if (_attackKeepExtraScale < 0f)
        {
            throw new InvalidOperationException(nameof(_attackKeepExtraScale));
        }

        if (_attackMinDot < -1f || _attackMinDot > 1f)
        {
            throw new InvalidOperationException(nameof(_attackMinDot));
        }
    }

    private void ValidateRange()
    {
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
    }

    private void ValidateRole()
    {
        if (_meleeHealthBonus < 0f)
        {
            throw new InvalidOperationException(nameof(_meleeHealthBonus));
        }

        if (_meleeSpeedScale <= 0f)
        {
            throw new InvalidOperationException(nameof(_meleeSpeedScale));
        }

        if (_rangeNearExtra < 0f)
        {
            throw new InvalidOperationException(nameof(_rangeNearExtra));
        }
    }

    private void ValidateCombatSteering()
    {
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

        if (_recoverBack < 0f)
        {
            throw new InvalidOperationException(nameof(_recoverBack));
        }

        if (_recoverSide < 0f)
        {
            throw new InvalidOperationException(nameof(_recoverSide));
        }
    }

    private void ValidateIdle()
    {
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
    }

    private void ValidateSearch()
    {
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

        if (_safeMoveSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_safeMoveSpeed));
        }
    }

    private void ValidateSteering()
    {
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
    }

    private void InitializeSteering()
    {
        _random = new System.Random(GetSeed());
        _enemySteering = new EnemySteering(transform, _enemyMove, _enemyRotator);
        _enemySteering.SetObstacle(_obstacleMask, _probeRadius, _probeHeight, _probeDistance, _probeAngle, _avoidWeight);
        _enemySteering.SetSpacing(_allyMask, _separationRadius, _separationWeight);
        _enemySteering.SetCombat(_orbitWeight, _ringWeight, _slotWeight, _slotAngle, _slotRadius, _slotCount, _recoverBack, _recoverSide);
        _baseHealthMax = _enemy.Health.MaxValue;
    }
}
