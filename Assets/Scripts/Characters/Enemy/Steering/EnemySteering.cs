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
    private const float LowProbeHeightScale = 0.45f;
    private const float MinProbeHeight = 0.18f;
    private const float MoveStuckMin = 0.01f;
    private const float MoveStuckTime = 0.3f;
    private const float NavPickAngle = 180f;

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
    private bool _hasPathTarget;
    private bool _hasLastNavPoint;
    private bool _hasMoveLastPoint;
    private EnemyRoomLock _enemyRoomLock;
    private float _moveStuckTimer;

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
        float safeStopDistance = Mathf.Max(stopDistance, 0.01f);
        float safeMoveSpeed = Mathf.Max(moveSpeed, 0.01f);
        float targetDistanceSqr = GetFlatDistanceSqr(currentPoint, flatTargetPoint);
        float safeStopDistanceSqr = safeStopDistance * safeStopDistance;

        if (targetDistanceSqr <= safeStopDistanceSqr)
        {
            _enemyMove.StopMove();
            SyncAgent(currentPoint);

            return false;
        }

        Vector3 moveDirection = GetFlatDirection(flatTargetPoint - currentPoint);

        if (moveDirection.sqrMagnitude <= MinDistance)
        {
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

        return true;
    }

    public void LookToPoint(Vector3 targetPoint)
    {
        _enemyMove.StopMove();
        ResetMoveStuck();
        _enemyRotator.RotateToPoint(GetFlatPoint(targetPoint));
    }

    public void Stop()
    {
        ClearPath();
        _enemyMove.StopMove();
        ResetMoveStuck();
    }

    public void ForceStop()
    {
        ClearPath();
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

        if (_moveStuckTimer < MoveStuckTime)
        {
            return true;
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
        float safeStopDistance = Mathf.Max(stopDistance, 0.01f);
        float safeStopDistanceSqr = safeStopDistance * safeStopDistance;

        if (GetFlatDistanceSqr(currentPoint, flatTargetPoint) <= safeStopDistanceSqr)
        {
            _enemyMove.StopMove();
            ClearPath();

            return false;
        }

        if (SyncAgent(currentPoint) == false)
        {
            ClearPath();

            return TryReachMove(currentPoint, flatTargetPoint, safeStopDistance, lookBlend, lookPoint);
        }

        if (TryRefreshPath(flatTargetPoint, safeStopDistance) == false)
        {
            ClearPath();

            return TryReachMove(currentPoint, flatTargetPoint, safeStopDistance, lookBlend, lookPoint);
        }

        Vector3 moveDirection = GetMoveDirection(currentPoint);
        Vector3 steerDirection = GetSteerDirection(currentPoint, moveDirection);

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
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

        return true;
    }

    private bool TryDirectMove(Vector3 currentPoint, Vector3 targetPoint, float stopDistance, float lookBlend, Vector3 lookPoint)
    {
        if (GetFlatDistanceSqr(currentPoint, targetPoint) <= stopDistance * stopDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 moveDirection = GetFlatDirection(targetPoint - currentPoint);
        Vector3 steerDirection = GetSteerDirection(currentPoint, moveDirection);

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
            steerDirection = moveDirection;
        }

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
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

        return true;
    }

    private bool TryReachMove(Vector3 currentPoint, Vector3 targetPoint, float stopDistance, float lookBlend, Vector3 lookPoint)
    {
        Vector3 reachPoint;

        if (TryGetReachMovePoint(currentPoint, targetPoint, stopDistance, out reachPoint))
        {
            return TryDirectMove(currentPoint, reachPoint, stopDistance, lookBlend, lookPoint);
        }

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
}
