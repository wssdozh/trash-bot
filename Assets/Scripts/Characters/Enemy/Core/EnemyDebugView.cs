using System;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class EnemyDebugView : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnemyMeleeBrain _brain;
    [SerializeField] private EnemyMove _enemyMove;
    [SerializeField] private EnemyRotator _enemyRotator;
    [SerializeField] private TargetVision _targetVision;

    [Header("Runtime State")]
    [SerializeField] private EnemyState _state;
    [SerializeField] private bool _isTargetVisible;
    [SerializeField] private bool _hasTarget;
    [SerializeField] private float _targetDistance;
    [SerializeField] private bool _isSafeMove;
    [SerializeField] private bool _isIdleWalking;
    [SerializeField] private bool _isSearchIdle;
    [SerializeField] private bool _isCombatClockwise;
    [SerializeField] private bool _hasLastSeenPoint;
    [SerializeField] private bool _hasSearchPoint;
    [SerializeField] private int _searchStep;
    [SerializeField] private int _rangeMode;

    [Header("Runtime Attack")]
    [SerializeField] private bool _isAttackWindup;
    [SerializeField] private float _attackWindupTimer;
    [SerializeField] private bool _isAttackInProgress;
    [SerializeField] private bool _isHitPending;

    [Header("Runtime Move")]
    [SerializeField] private float _moveAmount;
    [SerializeField] private bool _isRunRequested;
    [SerializeField] private bool _isRunning;
    [SerializeField] private float _idleStuckTimer;
    [SerializeField] private float _steeringStuckTimer;
    [SerializeField] private string _steeringStatus;

    [Header("Runtime Nav")]
    [SerializeField] private bool _hasNavAgent;
    [SerializeField] private bool _isNavAgentEnabled;
    [SerializeField] private bool _isOnNavMesh;
    [SerializeField] private bool _isPathPending;
    [SerializeField] private bool _hasPath;
    [SerializeField] private bool _hasPathTarget;
    [SerializeField] private bool _hasLastNavPoint;
    [SerializeField] private string _pathStatus;
    [SerializeField] private float _pathStopDistance;

    [Header("Runtime Directions")]
    [SerializeField] private Vector3 _forwardDirection;
    [SerializeField] private Vector3 _moveDirection;
    [SerializeField] private Vector3 _steerDirection;
    [SerializeField] private Vector3 _attackDirection;

    [Header("Runtime Points")]
    [SerializeField] private bool _hasRequestedTargetPoint;
    [SerializeField] private bool _hasResolvedTargetPoint;
    [SerializeField] private bool _hasLookPoint;
    [SerializeField] private Vector3 _targetPoint;
    [SerializeField] private Vector3 _requestedTargetPoint;
    [SerializeField] private Vector3 _resolvedTargetPoint;
    [SerializeField] private Vector3 _lookPoint;
    [SerializeField] private Vector3 _pathTargetPoint;
    [SerializeField] private Vector3 _lastSeenPoint;
    [SerializeField] private Vector3 _searchTargetPoint;
    [SerializeField] private Vector3 _idleTargetPoint;
    [SerializeField] private Vector3 _idleLookPoint;
    [SerializeField] private Vector3 _lastNavPoint;

    private void Reset()
    {
        _brain = GetComponent<EnemyMeleeBrain>();
        _enemyMove = GetComponent<EnemyMove>();
        _enemyRotator = GetComponent<EnemyRotator>();
        _targetVision = GetComponent<TargetVision>();
    }

    private void Awake()
    {
        if (_brain == null)
        {
            throw new InvalidOperationException(nameof(_brain));
        }

        if (_enemyMove == null)
        {
            throw new InvalidOperationException(nameof(_enemyMove));
        }

        if (_enemyRotator == null)
        {
            throw new InvalidOperationException(nameof(_enemyRotator));
        }

        if (_targetVision == null)
        {
            throw new InvalidOperationException(nameof(_targetVision));
        }
    }

    private void LateUpdate()
    {
        if (Application.isPlaying == false)
        {
            return;
        }

        SyncRuntimeData();
    }

    public void RefreshDebugData()
    {
        if (Application.isPlaying == false)
        {
            return;
        }

        SyncRuntimeData();
    }

    public string BuildDebugSnapshot()
    {
        RefreshDebugData();

        StringBuilder stringBuilder = new StringBuilder(1024);
        stringBuilder.AppendLine("EnemyDebugView");
        stringBuilder.Append("State: ").AppendLine(_state.ToString());
        stringBuilder.Append("TargetVisible: ").Append(_isTargetVisible).Append(" | HasTarget: ").Append(_hasTarget).Append(" | Distance: ").AppendLine(_targetDistance.ToString("F2"));
        stringBuilder.Append("SafeMove: ").Append(_isSafeMove).Append(" | IdleWalking: ").Append(_isIdleWalking).Append(" | SearchIdle: ").Append(_isSearchIdle).Append(" | Clockwise: ").AppendLine(_isCombatClockwise.ToString());
        stringBuilder.Append("HasLastSeen: ").Append(_hasLastSeenPoint).Append(" | HasSearchPoint: ").Append(_hasSearchPoint).Append(" | SearchStep: ").Append(_searchStep).Append(" | RangeMode: ").AppendLine(_rangeMode.ToString());
        stringBuilder.Append("AttackWindup: ").Append(_isAttackWindup).Append(" | WindupTimer: ").Append(_attackWindupTimer.ToString("F2")).Append(" | AttackInProgress: ").Append(_isAttackInProgress).Append(" | HitPending: ").AppendLine(_isHitPending.ToString());
        stringBuilder.Append("MoveAmount: ").Append(_moveAmount.ToString("F2")).Append(" | RunRequested: ").Append(_isRunRequested).Append(" | Running: ").Append(_isRunning).Append(" | IdleStuck: ").Append(_idleStuckTimer.ToString("F2")).Append(" | SteeringStuck: ").AppendLine(_steeringStuckTimer.ToString("F2"));
        stringBuilder.Append("SteeringStatus: ").AppendLine(_steeringStatus);
        stringBuilder.Append("NavAgent: ").Append(_hasNavAgent).Append(" | Enabled: ").Append(_isNavAgentEnabled).Append(" | OnNavMesh: ").Append(_isOnNavMesh).Append(" | PathPending: ").Append(_isPathPending).Append(" | HasPath: ").Append(_hasPath).AppendLine();
        stringBuilder.Append("HasPathTarget: ").Append(_hasPathTarget).Append(" | HasLastNavPoint: ").Append(_hasLastNavPoint).Append(" | PathStatus: ").Append(_pathStatus).Append(" | StopDistance: ").AppendLine(_pathStopDistance.ToString("F2"));
        AppendVector(stringBuilder, "Forward", _forwardDirection);
        AppendVector(stringBuilder, "Move", _moveDirection);
        AppendVector(stringBuilder, "Steer", _steerDirection);
        AppendVector(stringBuilder, "Attack", _attackDirection);
        stringBuilder.Append("HasRequestedTarget: ").Append(_hasRequestedTargetPoint).Append(" | HasResolvedTarget: ").Append(_hasResolvedTargetPoint).Append(" | HasLookPoint: ").AppendLine(_hasLookPoint.ToString());
        AppendVector(stringBuilder, "TargetPoint", _targetPoint);
        AppendVector(stringBuilder, "RequestedTargetPoint", _requestedTargetPoint);
        AppendVector(stringBuilder, "ResolvedTargetPoint", _resolvedTargetPoint);
        AppendVector(stringBuilder, "LookPoint", _lookPoint);
        AppendVector(stringBuilder, "PathTargetPoint", _pathTargetPoint);
        AppendVector(stringBuilder, "LastSeenPoint", _lastSeenPoint);
        AppendVector(stringBuilder, "SearchTargetPoint", _searchTargetPoint);
        AppendVector(stringBuilder, "IdleTargetPoint", _idleTargetPoint);
        AppendVector(stringBuilder, "IdleLookPoint", _idleLookPoint);
        AppendVector(stringBuilder, "LastNavPoint", _lastNavPoint);

        return stringBuilder.ToString();
    }

    private void SyncRuntimeData()
    {
        _state = _brain.State;
        _isSafeMove = _brain.DebugIsSafeMove;
        _isIdleWalking = _brain.DebugIsIdleWalking;
        _isSearchIdle = _brain.DebugIsSearchIdle;
        _isCombatClockwise = _brain.DebugIsCombatClockwise;
        _hasLastSeenPoint = _brain.DebugHasLastSeenPoint;
        _hasSearchPoint = _brain.DebugHasSearchPoint;
        _searchStep = _brain.DebugSearchStep;
        _rangeMode = _brain.DebugRangeMode;

        _isAttackWindup = _brain.DebugIsAttackWindup;
        _attackWindupTimer = _brain.DebugAttackWindupTimer;
        _isAttackInProgress = _brain.DebugIsAttackInProgress;
        _isHitPending = _brain.DebugIsHitPending;

        _moveAmount = _enemyMove.MoveAmount;
        _isRunRequested = _enemyMove.IsRunRequested;
        _isRunning = _enemyMove.IsRunning;
        _idleStuckTimer = _brain.DebugIdleStuckTimer;

        _forwardDirection = _enemyRotator.ForwardDirection;
        _moveDirection = _enemyMove.MoveDirection;
        _attackDirection = _brain.DebugAttackDirection;

        SyncTargetData();
        SyncSteeringData();
        SyncPointData();
    }

    private void SyncTargetData()
    {
        _isTargetVisible = _targetVision.IsTargetVisible;
        _hasTarget = _targetVision.CurrentTarget != null;

        if (_isTargetVisible)
        {
            _targetDistance = _targetVision.DistanceToTarget;
            _targetPoint = _targetVision.CurrentTargetPoint;

            return;
        }

        _targetDistance = 0f;
        _targetPoint = Vector3.zero;
    }

    private void SyncSteeringData()
    {
        EnemySteering steering = _brain.Steering;

        if (steering == null)
        {
            ClearSteeringData();

            return;
        }

        _steeringStuckTimer = steering.DebugMoveStuckTimer;
        _steeringStatus = steering.DebugStatus;
        _steerDirection = steering.DebugSteerDirection;

        _hasNavAgent = steering.DebugHasNavAgent;
        _isNavAgentEnabled = steering.DebugNavAgentEnabled;
        _isOnNavMesh = steering.DebugNavAgentOnNavMesh;
        _isPathPending = steering.DebugNavPathPending;
        _hasPath = steering.DebugNavHasPath;
        _hasPathTarget = steering.DebugHasPathTarget;
        _hasLastNavPoint = steering.DebugHasLastNavPoint;
        _pathStatus = GetPathStatusText(steering.DebugNavPathStatus);
        _pathStopDistance = steering.DebugPathStopDistance;

        _hasRequestedTargetPoint = steering.HasDebugRequestedTargetPoint;
        _hasResolvedTargetPoint = steering.HasDebugResolvedTargetPoint;
        _hasLookPoint = steering.HasDebugLookPoint;
        _requestedTargetPoint = steering.DebugRequestedTargetPoint;
        _resolvedTargetPoint = steering.DebugResolvedTargetPoint;
        _lookPoint = steering.DebugLookPoint;
        _pathTargetPoint = steering.DebugPathTargetPoint;
        _lastNavPoint = steering.DebugLastNavPoint;
    }

    private void SyncPointData()
    {
        _lastSeenPoint = _brain.DebugLastSeenPoint;
        _searchTargetPoint = _brain.DebugSearchTargetPoint;
        _idleTargetPoint = _brain.DebugIdleTargetPoint;
        _idleLookPoint = _brain.DebugIdleLookPoint;
    }

    private void ClearSteeringData()
    {
        _steeringStuckTimer = 0f;
        _steeringStatus = string.Empty;
        _steerDirection = Vector3.zero;

        _hasNavAgent = false;
        _isNavAgentEnabled = false;
        _isOnNavMesh = false;
        _isPathPending = false;
        _hasPath = false;
        _hasPathTarget = false;
        _hasLastNavPoint = false;
        _pathStatus = string.Empty;
        _pathStopDistance = 0f;

        _hasRequestedTargetPoint = false;
        _hasResolvedTargetPoint = false;
        _hasLookPoint = false;
        _requestedTargetPoint = Vector3.zero;
        _resolvedTargetPoint = Vector3.zero;
        _lookPoint = Vector3.zero;
        _pathTargetPoint = Vector3.zero;
        _lastNavPoint = Vector3.zero;
    }

    private string GetPathStatusText(NavMeshPathStatus pathStatus)
    {
        if (pathStatus == NavMeshPathStatus.PathComplete)
        {
            return "Complete";
        }

        if (pathStatus == NavMeshPathStatus.PathPartial)
        {
            return "Partial";
        }

        return "Invalid";
    }

    private void AppendVector(StringBuilder stringBuilder, string name, Vector3 point)
    {
        stringBuilder
            .Append(name)
            .Append(": ")
            .Append(point.x.ToString("F2"))
            .Append(", ")
            .Append(point.y.ToString("F2"))
            .Append(", ")
            .Append(point.z.ToString("F2"))
            .AppendLine();
    }
}
