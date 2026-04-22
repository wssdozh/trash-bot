using System;
using UnityEngine;
using UnityEngine.AI;

public sealed partial class EnemySteering
{
    private const int AvoidDirectionCount = 4;
    private const int AllyBufferSize = 24;
    private const int PointBufferSize = 24;
    private const int ProbeBufferSize = 24;
    private const string EnemyLayerName = "Enemies";
    private const float MinDistance = 0.0001f;
    private const float NavSampleGap = 2f;
    private const float PathRefreshGap = 0.3f;
    private const float ResolveMinStep = 0.03f;
    private const float ResolveMaxStep = 0.18f;
    private const float AgentSpeed = 3.5f;
    private const float NavRecoverGap = 6f;
    private const float NavSnapGap = 0.35f;
    private const float PathLookAheadDistance = 0.45f;
    private const float ReachGap = 0.05f;
    private const float SafePointPushGap = 0.02f;
    private const int SafePointPushCount = 4;
    private const float SlotOffsetMin = 0.01f;
    private const float ProbeSkin = 0.05f;
    private const float NavAgentRadiusScale = 0.45f;
    private const float NearProbeDistanceScale = 1f;
    private const float NearProbeSkinScale = 0f;
    private const float LowProbeHeightScale = 0.45f;
    private const float MinProbeHeight = 0.18f;
    private const float MoveStuckMin = 0.01f;
    private const float MoveStuckTime = 0.3f;
    private const float MoveWallBlockedTime = 0.12f;
    private const float NavPickAngle = 180f;
    private const float PathFallbackGap = 0.1f;
    private const float SteerFlipDot = 0.2f;
    private const float SteerBlendKeepWeight = 0.65f;

    private readonly Transform _root;
    private readonly EnemyMove _enemyMove;
    private readonly EnemyRotator _enemyRotator;
    private readonly Rigidbody _rigidbody;
    private readonly Collider[] _bodyBuffer;
    private readonly Collider[] _allyBuffer = new Collider[AllyBufferSize];
    private readonly Collider[] _pointBuffer = new Collider[PointBufferSize];
    private readonly RaycastHit[] _probeBuffer = new RaycastHit[ProbeBufferSize];
    private readonly int[] _allyIdBuffer = new int[AllyBufferSize];
    private readonly NavMeshPath _navMeshPath = new NavMeshPath();
    private NavMeshAgent _navMeshAgent;

    private LayerMask _obstacleMask;
    private float _probeRadius;
    private float _probeHeight;
    private float _probeDistance;
    private float _probeAngle;
    private float _avoidWeight;
    private LayerMask _allyMask;
    private float _separationRadius;
    private float _separationWeight;
    private float _orbitWeight;
    private float _ringWeight;
    private float _slotWeight;
    private float _slotAngle;
    private float _slotRadius;
    private int _slotCount;
    private float _recoverBack;
    private float _recoverSide;
    private Vector3 _pathTargetPoint;
    private float _pathStopDistance;
    private Vector3 _lastNavPoint;
    private Vector3 _moveLastPoint;
    private Vector3 _debugRequestedTargetPoint;
    private Vector3 _debugResolvedTargetPoint;
    private Vector3 _debugLookPoint;
    private Vector3 _debugMoveDirection;
    private Vector3 _debugSteerDirection;
    private Vector3 _lastSteerMoveDirection;
    private bool _hasPathTarget;
    private bool _hasLastNavPoint;
    private bool _hasMoveLastPoint;
    private bool _hasLastSteerMoveDirection;
    private bool _hasDebugRequestedTargetPoint;
    private bool _hasDebugResolvedTargetPoint;
    private bool _hasDebugLookPoint;
    private EnemyRoomLock _enemyRoomLock;
    private float _moveStuckTimer;
    private string _debugStatus = "Init";

    public string DebugStatus => _debugStatus;
    public Vector3 DebugRequestedTargetPoint => _debugRequestedTargetPoint;
    public Vector3 DebugResolvedTargetPoint => _debugResolvedTargetPoint;
    public Vector3 DebugLookPoint => _debugLookPoint;
    public Vector3 DebugMoveDirection => _debugMoveDirection;
    public Vector3 DebugSteerDirection => _debugSteerDirection;
    public Vector3 DebugPathTargetPoint => _pathTargetPoint;
    public Vector3 DebugLastNavPoint => _lastNavPoint;
    public float DebugPathStopDistance => _pathStopDistance;
    public float DebugMoveStuckTimer => _moveStuckTimer;
    public bool HasDebugRequestedTargetPoint => _hasDebugRequestedTargetPoint;
    public bool HasDebugResolvedTargetPoint => _hasDebugResolvedTargetPoint;
    public bool HasDebugLookPoint => _hasDebugLookPoint;
    public bool DebugHasPathTarget => _hasPathTarget;
    public bool DebugHasLastNavPoint => _hasLastNavPoint;
    public bool DebugHasNavAgent => _navMeshAgent != null;
    public bool DebugNavAgentEnabled => _navMeshAgent != null && _navMeshAgent.enabled;
    public bool DebugNavAgentOnNavMesh => _navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh;
    public bool DebugNavPathPending => _navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.pathPending;
    public bool DebugNavHasPath => _navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.hasPath;
    public NavMeshPathStatus DebugNavPathStatus => GetDebugNavPathStatus();

    public EnemySteering(Transform root, EnemyMove enemyMove, EnemyRotator enemyRotator)
    {
        if (root == null)
        {
            throw new InvalidOperationException(nameof(root));
        }

        if (enemyMove == null)
        {
            throw new InvalidOperationException(nameof(enemyMove));
        }

        if (enemyRotator == null)
        {
            throw new InvalidOperationException(nameof(enemyRotator));
        }

        _root = root;
        _enemyMove = enemyMove;
        _enemyRotator = enemyRotator;
        _rigidbody = _root.GetComponent<Rigidbody>();
        _bodyBuffer = _root.GetComponentsInChildren<Collider>();
        _slotCount = 1;
        _navMeshAgent = EnsureAgent();
    }

    public void SetObstacle(LayerMask obstacleMask, float probeRadius, float probeHeight, float probeDistance, float probeAngle, float avoidWeight)
    {
        _obstacleMask = GetObstacleMask(obstacleMask);
        _probeRadius = probeRadius;
        _probeHeight = probeHeight;
        _probeDistance = probeDistance;
        _probeAngle = probeAngle;
        _avoidWeight = avoidWeight;

        ApplyAgentShape(_navMeshAgent);
    }

    public void SetSpacing(LayerMask allyMask, float separationRadius, float separationWeight)
    {
        _allyMask = allyMask;
        _separationRadius = separationRadius;
        _separationWeight = separationWeight;
    }

    public void SetSlot(float slotAngle, float slotRadius, int slotCount)
    {
        _slotAngle = slotAngle;
        _slotRadius = slotRadius;
        _slotCount = Mathf.Max(slotCount, 1);
    }

    public void SetCombat(float orbitWeight, float ringWeight, float slotWeight, float slotAngle, float slotRadius, int slotCount, float recoverBack, float recoverSide)
    {
        _orbitWeight = orbitWeight;
        _ringWeight = ringWeight;
        _slotWeight = slotWeight;
        _slotAngle = slotAngle;
        _slotRadius = slotRadius;
        _slotCount = Mathf.Max(slotCount, 1);
        _recoverBack = recoverBack;
        _recoverSide = recoverSide;
    }

    public void SetRoomLock(EnemyRoomLock enemyRoomLock)
    {
        _enemyRoomLock = enemyRoomLock;
    }

    public bool MoveToPoint(Vector3 targetPoint, float stopDistance)
    {
        return MoveToPoint(targetPoint, stopDistance, 0f, Vector3.zero);
    }

    public bool MoveToPoint(Vector3 targetPoint, float stopDistance, Vector3 lookPoint)
    {
        return MoveToPoint(targetPoint, stopDistance, 1f, lookPoint);
    }

    public bool MoveDirect(Vector3 targetPoint, float stopDistance, float moveSpeed, Vector3 lookPoint)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = ClampMovePoint(targetPoint);
        SetDebugMoveRequest(flatTargetPoint, true, lookPoint);
        float safeStopDistance = Mathf.Max(stopDistance, 0.01f);
        float safeMoveSpeed = Mathf.Max(moveSpeed, 0.01f);
        float targetDistanceSqr = GetFlatDistanceSqr(currentPoint, flatTargetPoint);
        float safeStopDistanceSqr = safeStopDistance * safeStopDistance;

        if (targetDistanceSqr <= safeStopDistanceSqr)
        {
            SetDebugStatus("MoveDirect.Reached");
            _enemyMove.StopMove();
            SyncAgent(currentPoint);

            return false;
        }

        Vector3 moveDirection = GetFlatDirection(flatTargetPoint - currentPoint);

        if (moveDirection.sqrMagnitude <= MinDistance)
        {
            SetDebugStatus("MoveDirect.NoDirection");
            _enemyMove.StopMove();
            SyncAgent(currentPoint);

            return false;
        }

        float targetDistance = Mathf.Sqrt(targetDistanceSqr);
        float maxStepDistance = targetDistance - safeStopDistance;
        float stepDistance = safeMoveSpeed * Time.fixedDeltaTime;

        if (stepDistance > maxStepDistance)
        {
            stepDistance = maxStepDistance;
        }

        if (stepDistance <= MinDistance)
        {
            SetDebugStatus("MoveDirect.NoStep");
            _enemyMove.StopMove();
            SyncAgent(currentPoint);

            return false;
        }

        Vector3 nextPoint = currentPoint + (moveDirection * stepDistance);
        nextPoint = ClampMovePoint(nextPoint);
        Vector3 lookDirection = GetLookDirection(currentPoint, moveDirection, lookPoint, 1f);

        ClearPath();
        _enemyMove.ForceStop();
        SnapToPoint(nextPoint);
        SyncAgent(nextPoint);
        _enemyRotator.RotateToDirection(lookDirection);
        SetDebugMoveResult(nextPoint, moveDirection, moveDirection);
        SetDebugStatus("MoveDirect.Step");

        return true;
    }

    public void LookToPoint(Vector3 targetPoint)
    {
        SetDebugMoveRequest(GetFlatPoint(_root.position), true, targetPoint);
        SetDebugStatus("LookToPoint");
        _enemyMove.StopMove();
        ResetMoveStuck();
        _enemyRotator.RotateToPoint(GetFlatPoint(targetPoint));
    }

    public void Stop()
    {
        SetDebugStatus("Stop");
        ClearPath();
        ResetSteerDirection();
        _enemyMove.StopMove();
        ResetMoveStuck();
    }

    public void ForceStop()
    {
        SetDebugStatus("ForceStop");
        ClearPath();
        ResetSteerDirection();
        _enemyMove.ForceStop();
        ResetMoveStuck();
    }

    public bool CanKeepMove(Vector3 currentPoint, float deltaTime)
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
        _moveStuckTimer += deltaTime;
        float moveStuckTime = MoveStuckTime;

        if (_enemyMove.IsWallBlocked)
        {
            moveStuckTime = MoveWallBlockedTime;
        }

        if (_moveStuckTimer < moveStuckTime)
        {
            return true;
        }

        if (_enemyMove.IsWallBlocked)
        {
            SetDebugStatus("MoveWallBlocked");
        }

        else
        {
            SetDebugStatus("MoveStuck");
        }

        _moveStuckTimer = 0f;

        return false;
    }

    public void ResetMoveStuck()
    {
        _hasMoveLastPoint = false;
        _moveLastPoint = Vector3.zero;
        _moveStuckTimer = 0f;
    }

    private LayerMask GetObstacleMask(LayerMask obstacleMask)
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);

        if (enemyLayer < 0)
        {
            return obstacleMask;
        }

        int obstacleMaskBits = obstacleMask.value | (1 << enemyLayer);
        LayerMask nextObstacleMask = obstacleMaskBits;

        return nextObstacleMask;
    }

    private bool MoveToPoint(Vector3 targetPoint, float stopDistance, float lookBlend, Vector3 lookPoint)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = ClampMovePoint(targetPoint);
        SetDebugMoveRequest(flatTargetPoint, lookBlend > 0f, lookPoint);
        float safeStopDistance = Mathf.Max(stopDistance, 0.01f);
        float safeStopDistanceSqr = safeStopDistance * safeStopDistance;

        if (GetFlatDistanceSqr(currentPoint, flatTargetPoint) <= safeStopDistanceSqr)
        {
            SetDebugStatus("MoveToPoint.Reached");
            _enemyMove.StopMove();
            ClearPath();

            return false;
        }

        if (SyncAgent(currentPoint) == false)
        {
            SetDebugStatus("MoveToPoint.SyncFailed");
            ClearPath();

            return TryReachMove(currentPoint, flatTargetPoint, safeStopDistance, lookBlend, lookPoint);
        }

        if (TryRefreshPath(flatTargetPoint, safeStopDistance) == false)
        {
            SetDebugStatus("MoveToPoint.PathRefreshFailed");
            ClearPath();

            return TryReachMove(currentPoint, flatTargetPoint, safeStopDistance, lookBlend, lookPoint);
        }

        Vector3 moveDirection = GetMoveDirection(currentPoint);
        Vector3 steerDirection = GetSteerDirection(currentPoint, moveDirection);
        SetDebugMoveResult(_pathTargetPoint, moveDirection, steerDirection);

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
            Vector3 fallbackDirection;

            if (TryGetPathFallbackDirection(currentPoint, moveDirection, out fallbackDirection))
            {
                Vector3 fallbackLookDirection = GetLookDirection(currentPoint, fallbackDirection, lookPoint, lookBlend);

                _enemyRotator.RotateToDirection(fallbackLookDirection);
                _enemyMove.SetDirection(fallbackDirection);
                SetDebugMoveResult(_pathTargetPoint, moveDirection, fallbackDirection);
                SetDebugStatus("MoveToPoint.PathFallback");

                return true;
            }

            SetDebugStatus("MoveToPoint.NoSteer");
            _enemyMove.StopMove();

            if (lookBlend > 0f)
            {
                _enemyRotator.RotateToPoint(GetFlatPoint(lookPoint));
            }

            if (_navMeshAgent.pathPending)
            {
                return true;
            }

            return false;
        }

        Vector3 lookDirection = GetLookDirection(currentPoint, steerDirection, lookPoint, lookBlend);

        _enemyRotator.RotateToDirection(lookDirection);
        _enemyMove.SetDirection(steerDirection);
        SetDebugStatus("MoveToPoint.Active");

        return true;
    }

    private bool TryDirectMove(Vector3 currentPoint, Vector3 targetPoint, float stopDistance, float lookBlend, Vector3 lookPoint)
    {
        if (GetFlatDistanceSqr(currentPoint, targetPoint) <= stopDistance * stopDistance)
        {
            SetDebugStatus("DirectMove.Reached");
            _enemyMove.StopMove();

            return false;
        }

        Vector3 moveDirection = GetFlatDirection(targetPoint - currentPoint);
        Vector3 steerDirection = GetSteerDirection(currentPoint, moveDirection);
        SetDebugMoveResult(targetPoint, moveDirection, steerDirection);

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
            steerDirection = moveDirection;
            SetDebugMoveResult(targetPoint, moveDirection, steerDirection);
        }

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
            SetDebugStatus("DirectMove.NoSteer");
            _enemyMove.StopMove();

            if (lookBlend > 0f)
            {
                _enemyRotator.RotateToPoint(GetFlatPoint(lookPoint));
            }

            return false;
        }

        Vector3 lookDirection = GetLookDirection(currentPoint, steerDirection, lookPoint, lookBlend);

        _enemyRotator.RotateToDirection(lookDirection);
        _enemyMove.SetDirection(steerDirection);
        SetDebugStatus("DirectMove.Active");

        return true;
    }

    private bool TryReachMove(Vector3 currentPoint, Vector3 targetPoint, float stopDistance, float lookBlend, Vector3 lookPoint)
    {
        Vector3 reachPoint;

        if (TryGetReachMovePoint(currentPoint, targetPoint, stopDistance, out reachPoint))
        {
            SetDebugStatus("ReachMove");
            return TryDirectMove(currentPoint, reachPoint, stopDistance, lookBlend, lookPoint);
        }

        SetDebugStatus("ReachFailed");
        _enemyMove.ForceStop();

        if (lookBlend > 0f)
        {
            _enemyRotator.RotateToPoint(GetFlatPoint(lookPoint));
        }

        return false;
    }

    private Vector3 GetLookDirection(Vector3 currentPoint, Vector3 steerDirection, Vector3 lookPoint, float lookBlend)
    {
        Vector3 baseDirection = GetFlatDirection(steerDirection);

        if (lookBlend <= 0f)
        {
            return baseDirection;
        }

        Vector3 lookDirection = GetFlatDirection(GetFlatPoint(lookPoint) - currentPoint);

        if (lookDirection.sqrMagnitude <= MinDistance)
        {
            return baseDirection;
        }

        if (lookBlend >= 1f)
        {
            return lookDirection;
        }

        Vector3 blendedDirection = (baseDirection * (1f - lookBlend)) + (lookDirection * lookBlend);
        blendedDirection = GetFlatDirection(blendedDirection);

        if (blendedDirection.sqrMagnitude <= MinDistance)
        {
            return lookDirection;
        }

        return blendedDirection;
    }

    private Vector3 RotateDirection(Vector3 direction, float angle)
    {
        Vector3 flatDirection = GetFlatDirection(direction);

        if (flatDirection.sqrMagnitude <= MinDistance)
        {
            return Vector3.zero;
        }

        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        Vector3 rotatedDirection = rotation * flatDirection;

        return GetFlatDirection(rotatedDirection);
    }

    private Vector3 GetFlatPoint(Vector3 point)
    {
        point.y = _root.position.y;

        return point;
    }

    private Vector3 GetProbeOrigin(Vector3 currentPoint, float probeHeight)
    {
        return currentPoint + (Vector3.up * probeHeight);
    }

    private Vector3 GetClosestPoint(Collider hitCollider, Vector3 point)
    {
        if (hitCollider is BoxCollider
            || hitCollider is SphereCollider
            || hitCollider is CapsuleCollider)
        {
            return hitCollider.ClosestPoint(point);
        }

        MeshCollider meshCollider = hitCollider as MeshCollider;

        if (meshCollider != null && meshCollider.convex)
        {
            return hitCollider.ClosestPoint(point);
        }

        return hitCollider.bounds.ClosestPoint(point);
    }

    private bool CanUseProbeObstacle(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.transform.IsChildOf(_root))
        {
            return false;
        }

        if (IsEnemyCollider(hitCollider))
        {
            return false;
        }

        return true;
    }

    private bool CanUseStaticObstacle(Collider hitCollider)
    {
        if (CanUseProbeObstacle(hitCollider) == false)
        {
            return false;
        }

        if (IsDynamicObstacleCollider(hitCollider))
        {
            return false;
        }

        return true;
    }

    private bool IsDynamicObstacleCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        Rigidbody obstacleRigidbody = hitCollider.attachedRigidbody;

        if (obstacleRigidbody == null)
        {
            return false;
        }

        if (obstacleRigidbody == _rigidbody)
        {
            return false;
        }

        if (obstacleRigidbody.isKinematic)
        {
            return false;
        }

        return true;
    }

    private float GetFlatDistanceSqr(Vector3 firstPoint, Vector3 secondPoint)
    {
        firstPoint.y = _root.position.y;
        secondPoint.y = _root.position.y;

        Vector3 delta = firstPoint - secondPoint;

        return delta.sqrMagnitude;
    }

    private Vector3 GetFlatVector(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= MinDistance)
        {
            return Vector3.zero;
        }

        return Vector3.ClampMagnitude(direction, 1f);
    }

    private Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= MinDistance)
        {
            return Vector3.zero;
        }

        direction.Normalize();

        return direction;
    }

    private NavMeshPathStatus GetDebugNavPathStatus()
    {
        if (_navMeshAgent == null)
        {
            return NavMeshPathStatus.PathInvalid;
        }

        if (_navMeshAgent.enabled == false)
        {
            return NavMeshPathStatus.PathInvalid;
        }

        return _navMeshAgent.pathStatus;
    }

    private void SetDebugStatus(string debugStatus)
    {
        _debugStatus = debugStatus;
    }

    private void SetDebugMoveRequest(Vector3 requestedTargetPoint, bool hasLookPoint, Vector3 lookPoint)
    {
        _debugRequestedTargetPoint = GetFlatPoint(requestedTargetPoint);
        _hasDebugRequestedTargetPoint = true;

        if (hasLookPoint)
        {
            _debugLookPoint = GetFlatPoint(lookPoint);
            _hasDebugLookPoint = true;

            return;
        }

        _debugLookPoint = Vector3.zero;
        _hasDebugLookPoint = false;
    }

    private void SetDebugMoveResult(Vector3 resolvedTargetPoint, Vector3 moveDirection, Vector3 steerDirection)
    {
        _debugResolvedTargetPoint = GetFlatPoint(resolvedTargetPoint);
        _hasDebugResolvedTargetPoint = true;
        _debugMoveDirection = GetFlatDirection(moveDirection);
        _debugSteerDirection = GetFlatDirection(steerDirection);
    }

    private void ResetSteerDirection()
    {
        _lastSteerMoveDirection = Vector3.zero;
        _hasLastSteerMoveDirection = false;
    }

    private Vector3 StabilizeSteerDirection(Vector3 currentPoint, Vector3 baseDirection, Vector3 steerDirection)
    {
        Vector3 flatSteerDirection = GetFlatDirection(steerDirection);

        if (flatSteerDirection.sqrMagnitude <= MinDistance)
        {
            ResetSteerDirection();

            return Vector3.zero;
        }

        if (_hasLastSteerMoveDirection == false)
        {
            _lastSteerMoveDirection = flatSteerDirection;
            _hasLastSteerMoveDirection = true;

            return flatSteerDirection;
        }

        Vector3 lastSteerDirection = _lastSteerMoveDirection;
        float steerDot = Vector3.Dot(lastSteerDirection, flatSteerDirection);

        if (steerDot < SteerFlipDot)
        {
            float baseDot = Vector3.Dot(baseDirection, flatSteerDirection);
            float lastBaseDot = Vector3.Dot(baseDirection, lastSteerDirection);

            if (lastBaseDot > baseDot)
            {
                if (IsBlocked(currentPoint, lastSteerDirection, GetResolveProbeDistance()) == false)
                {
                    flatSteerDirection = lastSteerDirection;
                }
            }
        }

        else
        {
            Vector3 blendedDirection = (lastSteerDirection * SteerBlendKeepWeight) + (flatSteerDirection * (1f - SteerBlendKeepWeight));
            blendedDirection = GetFlatDirection(blendedDirection);

            if (blendedDirection.sqrMagnitude > MinDistance)
            {
                flatSteerDirection = blendedDirection;
            }
        }

        _lastSteerMoveDirection = flatSteerDirection;
        _hasLastSteerMoveDirection = true;

        return flatSteerDirection;
    }
}
