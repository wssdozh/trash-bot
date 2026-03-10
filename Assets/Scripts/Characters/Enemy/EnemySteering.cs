using System;
using UnityEngine;
using UnityEngine.AI;

public sealed class EnemySteering
{
    private const int AllyBufferSize = 24;
    private const int PointBufferSize = 24;
    private const int ProbeBufferSize = 24;
    private const float MinDistance = 0.0001f;
    private const float NavSampleGap = 2f;
    private const float PathRefreshGap = 0.3f;
    private const float PathRefreshTime = 0.2f;
    private const float ResolveMinStep = 0.03f;
    private const float ResolveMaxStep = 0.18f;
    private const float AgentSpeed = 3.5f;
    private const float NavRecoverGap = 6f;
    private const float NavSnapGap = 0.35f;
    private const float SlotOffsetMin = 0.01f;

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
    private readonly NavMeshAgent _navMeshAgent;

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
    private float _pathTime;
    private Vector3 _lastNavPoint;
    private bool _hasPathTarget;
    private bool _hasLastNavPoint;
    private EnemyRoomLock _enemyRoomLock;

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
        _obstacleMask = obstacleMask;
        _probeRadius = probeRadius;
        _probeHeight = probeHeight;
        _probeDistance = probeDistance;
        _probeAngle = probeAngle;
        _avoidWeight = avoidWeight;

        if (_navMeshAgent == null)
        {
            return;
        }

        _navMeshAgent.radius = Mathf.Max(_probeRadius, 0.1f);
        _navMeshAgent.height = Mathf.Max(_probeHeight * 2f, 0.5f);
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

    private bool MoveToPoint(Vector3 targetPoint, float stopDistance, float lookBlend, Vector3 lookPoint)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = ClampMovePoint(targetPoint);
        float safeStopDistance = Mathf.Max(stopDistance, 0.01f);
        float targetDistance = Vector3.Distance(currentPoint, flatTargetPoint);

        if (targetDistance <= safeStopDistance)
        {
            _enemyMove.StopMove();
            ClearPath();

            return false;
        }

        if (SyncAgent(currentPoint) == false)
        {
            ClearPath();

            return TryDirectMove(currentPoint, flatTargetPoint, safeStopDistance, lookBlend, lookPoint);
        }

        if (TryRefreshPath(flatTargetPoint, safeStopDistance) == false)
        {
            ClearPath();

            return TryDirectMove(currentPoint, flatTargetPoint, safeStopDistance, lookBlend, lookPoint);
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
        float targetDistance = Vector3.Distance(currentPoint, targetPoint);

        if (targetDistance <= stopDistance)
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

    public bool ChaseTarget(Vector3 targetPoint, float ringDistance, float ringTolerance, float lookBlend)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = GetFlatPoint(targetPoint);
        Vector3 toTarget = flatTargetPoint - currentPoint;

        if (toTarget.sqrMagnitude <= MinDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = toTarget.normalized;
        Vector3 chasePoint = GetChasePoint(currentPoint, flatTargetPoint, targetDirection, ringDistance, toTarget.magnitude);
        Vector3 safePoint = GetSafePoint(chasePoint, _probeRadius);
        float stopDistance = Mathf.Max(ringTolerance, 0.05f);

        if (lookBlend > 0f)
        {
            return MoveToPoint(safePoint, stopDistance, lookBlend, flatTargetPoint);
        }

        return MoveToPoint(safePoint, stopDistance);
    }

    public bool OrbitTarget(Transform target, float ringDistance, float ringTolerance, bool isClockwise)
    {
        if (target == null)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 targetPoint = GetFlatPoint(target.position);
        Vector3 toTarget = targetPoint - currentPoint;

        if (toTarget.sqrMagnitude <= MinDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = toTarget.normalized;
        Vector3 tangentDirection = GetOrbitDirection(targetDirection, isClockwise);
        Vector3 ringDirection = Vector3.zero;
        float targetDistance = toTarget.magnitude;

        if (targetDistance > ringDistance + ringTolerance)
        {
            ringDirection = targetDirection;
        }

        else if (targetDistance < ringDistance - ringTolerance)
        {
            ringDirection = -targetDirection;
        }

        Vector3 desiredDirection = (tangentDirection * _orbitWeight) + (ringDirection * _ringWeight);
        desiredDirection = GetFlatDirection(desiredDirection);

        if (desiredDirection.sqrMagnitude <= MinDistance)
        {
            desiredDirection = tangentDirection;
        }

        Vector3 movePoint = currentPoint + (desiredDirection * Mathf.Max(_probeDistance, 0.6f));

        return MoveToPoint(movePoint, 0.05f);
    }

    public bool RecoverTarget(Transform target, bool isClockwise)
    {
        if (target == null)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 targetPoint = GetFlatPoint(target.position);
        Vector3 toTarget = targetPoint - currentPoint;

        if (toTarget.sqrMagnitude <= MinDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = toTarget.normalized;
        Vector3 sideDirection = GetOrbitDirection(targetDirection, isClockwise);
        Vector3 desiredDirection = (-targetDirection * _recoverBack) + (sideDirection * _recoverSide);
        desiredDirection = GetFlatDirection(desiredDirection);

        if (desiredDirection.sqrMagnitude <= MinDistance)
        {
            desiredDirection = -targetDirection;
        }

        Vector3 movePoint = currentPoint + (desiredDirection * Mathf.Max(_probeDistance, 0.6f));

        return MoveToPoint(movePoint, 0.05f);
    }

    public void LookToPoint(Vector3 targetPoint)
    {
        _enemyMove.StopMove();
        _enemyRotator.RotateToPoint(GetFlatPoint(targetPoint));
    }

    public void Stop()
    {
        ClearPath();
        _enemyMove.StopMove();
    }

    public bool ResolveOverlap()
    {
        Vector3 overlapPush = GetOverlapPush();

        if (overlapPush.sqrMagnitude <= MinDistance)
        {
            return false;
        }

        float pushDistance = overlapPush.magnitude;
        float resolveDistance = Mathf.Clamp(pushDistance, ResolveMinStep, ResolveMaxStep);
        Vector3 resolveVector = (overlapPush / pushDistance) * resolveDistance;

        _enemyMove.ForceStop();
        ApplyResolve(resolveVector);
        SyncAgent(GetFlatPoint(_root.position));

        return true;
    }

    public bool HasPointClearance(Vector3 point)
    {
        if (ContainsMovePoint(point) == false)
        {
            return false;
        }

        Vector3 navPoint;

        if (TryGetNavPoint(point, out navPoint) == false)
        {
            return false;
        }

        if (ContainsMovePoint(navPoint) == false)
        {
            return false;
        }

        if (HasObstaclePoint(navPoint))
        {
            return false;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 navStartPoint;

        if (TryGetNavPoint(currentPoint, out navStartPoint) == false)
        {
            return false;
        }

        bool hasPath = NavMesh.CalculatePath(navStartPoint, navPoint, NavMesh.AllAreas, _navMeshPath);

        if (hasPath == false)
        {
            return false;
        }

        if (_navMeshPath.status == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }

        if (_navMeshPath.status == NavMeshPathStatus.PathPartial)
        {
            return false;
        }

        return true;
    }

    public Vector3 GetSafePoint(Vector3 point, float wallGap)
    {
        Vector3 flatPoint = ClampMovePoint(point);
        float sampleDistance = Mathf.Max(GetNavSampleGap(), wallGap * 4f);
        Vector3 navPoint;

        if (TryGetNavPoint(flatPoint, sampleDistance, out navPoint) == false)
        {
            return flatPoint;
        }

        if (wallGap <= MinDistance)
        {
            return ClampMovePoint(navPoint);
        }

        NavMeshHit edgeHit;
        bool hasEdge = NavMesh.FindClosestEdge(navPoint, out edgeHit, NavMesh.AllAreas);

        if (hasEdge == false)
        {
            return ClampMovePoint(navPoint);
        }

        if (edgeHit.distance >= wallGap)
        {
            return ClampMovePoint(navPoint);
        }

        Vector3 edgeDirection = GetFlatDirection(edgeHit.normal);

        if (edgeDirection.sqrMagnitude <= MinDistance)
        {
            return ClampMovePoint(navPoint);
        }

        float pushDistance = wallGap - edgeHit.distance;
        Vector3 pushedPoint = navPoint + (edgeDirection * pushDistance);
        Vector3 safePoint;

        if (TryGetNavPoint(pushedPoint, sampleDistance, out safePoint))
        {
            return ClampMovePoint(safePoint);
        }

        return ClampMovePoint(navPoint);
    }

    public Vector3 GetReachPoint(Vector3 targetPoint, float wallGap)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = ClampMovePoint(targetPoint);
        Vector3 navStartPoint;
        Vector3 navTargetPoint;

        if (TryGetNavPoint(currentPoint, out navStartPoint) && TryGetNavPoint(flatTargetPoint, out navTargetPoint))
        {
            NavMeshHit navMeshHit;
            bool isBlocked = NavMesh.Raycast(navStartPoint, navTargetPoint, out navMeshHit, NavMesh.AllAreas);

            if (isBlocked == false)
            {
                return navTargetPoint;
            }

            return GetFlatPoint(navMeshHit.position);
        }

        return GetSafePoint(currentPoint, wallGap);
    }

    public bool IsLineBlocked(Vector3 targetPoint)
    {
        if (ContainsMovePoint(targetPoint) == false)
        {
            return true;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = ClampMovePoint(targetPoint);
        Vector3 navStartPoint;
        Vector3 navTargetPoint;

        if (TryGetNavPoint(currentPoint, out navStartPoint) == false)
        {
            return true;
        }

        if (TryGetNavPoint(flatTargetPoint, out navTargetPoint) == false)
        {
            return true;
        }

        NavMeshHit navMeshHit;

        return NavMesh.Raycast(navStartPoint, navTargetPoint, out navMeshHit, NavMesh.AllAreas);
    }

    private NavMeshAgent EnsureAgent()
    {
        NavMeshAgent navMeshAgent = _root.GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            navMeshAgent = _root.gameObject.AddComponent<NavMeshAgent>();
        }

        navMeshAgent.enabled = true;
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        navMeshAgent.autoBraking = true;
        navMeshAgent.autoRepath = true;
        navMeshAgent.acceleration = 120f;
        navMeshAgent.angularSpeed = 0f;
        navMeshAgent.speed = AgentSpeed;
        navMeshAgent.avoidancePriority = GetAvoidPriority();
        navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        return navMeshAgent;
    }

    private int GetAvoidPriority()
    {
        int priorityId = _root.gameObject.GetInstanceID();

        if (priorityId == int.MinValue)
        {
            priorityId = int.MaxValue;
        }

        if (priorityId < 0)
        {
            priorityId = -priorityId;
        }

        return 20 + (priorityId % 60);
    }

    private bool SyncAgent(Vector3 currentPoint)
    {
        if (_navMeshAgent.enabled == false)
        {
            _navMeshAgent.enabled = true;
        }

        if (_navMeshAgent.isOnNavMesh == false)
        {
            Vector3 navPoint;

            if (TryGetRecoverPoint(currentPoint, out navPoint) == false)
            {
                return false;
            }

            if (Vector3.Distance(currentPoint, navPoint) > NavSnapGap)
            {
                SnapToPoint(navPoint);
                currentPoint = navPoint;
            }

            bool isWarped = _navMeshAgent.Warp(navPoint);

            if (isWarped == false)
            {
                return false;
            }

            CacheNavPoint(navPoint);

            return true;
        }

        _navMeshAgent.nextPosition = currentPoint;

        Vector3 currentNavPoint;

        if (TryGetNavPoint(currentPoint, NavRecoverGap, out currentNavPoint))
        {
            CacheNavPoint(currentNavPoint);
        }

        return true;
    }

    private bool TryGetRecoverPoint(Vector3 currentPoint, out Vector3 navPoint)
    {
        if (TryGetNavPoint(currentPoint, out navPoint))
        {
            return true;
        }

        if (TryGetNavPoint(currentPoint, NavRecoverGap, out navPoint))
        {
            return true;
        }

        if (_hasLastNavPoint)
        {
            if (TryGetNavPoint(_lastNavPoint, NavRecoverGap, out navPoint))
            {
                return true;
            }
        }

        navPoint = GetFlatPoint(currentPoint);

        return false;
    }

    private void SnapToPoint(Vector3 navPoint)
    {
        Vector3 nextPosition = _root.position;
        nextPosition.x = navPoint.x;
        nextPosition.z = navPoint.z;

        if (_rigidbody != null)
        {
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            _rigidbody.position = nextPosition;
            _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);

            return;
        }

        _root.position = nextPosition;
    }

    private void CacheNavPoint(Vector3 navPoint)
    {
        _lastNavPoint = navPoint;
        _hasLastNavPoint = true;
    }

    private bool TryRefreshPath(Vector3 targetPoint, float stopDistance)
    {
        if (NeedPathRefresh(targetPoint, stopDistance))
        {
            _navMeshAgent.stoppingDistance = stopDistance;
            bool isSet = _navMeshAgent.SetDestination(targetPoint);

            if (isSet == false)
            {
                return false;
            }

            _pathTargetPoint = targetPoint;
            _pathStopDistance = stopDistance;
            _pathTime = Time.time;
            _hasPathTarget = true;
        }

        if (_navMeshAgent.pathPending)
        {
            return true;
        }

        if (_navMeshAgent.hasPath == false)
        {
            return false;
        }

        if (_navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }

        return true;
    }

    private bool NeedPathRefresh(Vector3 targetPoint, float stopDistance)
    {
        if (_hasPathTarget == false)
        {
            return true;
        }

        if (Time.time - _pathTime >= PathRefreshTime)
        {
            return true;
        }

        if (Vector3.Distance(_pathTargetPoint, targetPoint) >= PathRefreshGap)
        {
            return true;
        }

        if (Mathf.Abs(_pathStopDistance - stopDistance) > 0.05f)
        {
            return true;
        }

        if (_navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return true;
        }

        if (_navMeshAgent.isPathStale)
        {
            return true;
        }

        return false;
    }

    private Vector3 GetMoveDirection(Vector3 currentPoint)
    {
        Vector3 moveDirection = GetFlatDirection(_navMeshAgent.desiredVelocity);

        if (moveDirection.sqrMagnitude > MinDistance)
        {
            return moveDirection;
        }

        if (_navMeshAgent.hasPath)
        {
            Vector3 steeringPoint = GetFlatPoint(_navMeshAgent.steeringTarget);
            Vector3 steeringDirection = GetFlatDirection(steeringPoint - currentPoint);

            if (steeringDirection.sqrMagnitude > MinDistance)
            {
                return steeringDirection;
            }
        }

        if (_navMeshAgent.pathPending == false)
        {
            Vector3 targetPoint = GetFlatPoint(_navMeshAgent.destination);

            return GetFlatDirection(targetPoint - currentPoint);
        }

        return Vector3.zero;
    }

    private Vector3 GetSteerDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        Vector3 baseDirection = GetFlatDirection(moveDirection);

        if (baseDirection.sqrMagnitude <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 desiredDirection = baseDirection;
        Vector3 separationDirection = GetSeparationDirection(currentPoint, baseDirection);

        if (separationDirection.sqrMagnitude > MinDistance)
        {
            desiredDirection += separationDirection * _separationWeight;
        }

        Vector3 avoidDirection = GetAvoidDirection(currentPoint, baseDirection);

        if (avoidDirection.sqrMagnitude > MinDistance)
        {
            desiredDirection += avoidDirection * _avoidWeight;
        }

        Vector3 steerDirection = GetFlatDirection(desiredDirection);

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
            return baseDirection;
        }

        return steerDirection;
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

    private Vector3 GetAvoidDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        if (_obstacleMask.value == 0)
        {
            return Vector3.zero;
        }

        if (_probeDistance <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 baseDirection = GetFlatDirection(moveDirection);

        if (baseDirection.sqrMagnitude <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 avoidDirection = GetProbePush(currentPoint, baseDirection, _probeDistance);
        Vector3 leftDirection = RotateDirection(baseDirection, -_probeAngle);
        Vector3 rightDirection = RotateDirection(baseDirection, _probeAngle);

        avoidDirection += GetProbePush(currentPoint, leftDirection, _probeDistance * 0.85f) * 0.75f;
        avoidDirection += GetProbePush(currentPoint, rightDirection, _probeDistance * 0.85f) * 0.75f;

        return GetFlatVector(avoidDirection);
    }

    private Vector3 GetProbePush(Vector3 currentPoint, Vector3 probeDirection, float probeDistance)
    {
        if (probeDistance <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 origin = currentPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _probeRadius,
            probeDirection,
            _probeBuffer,
            probeDistance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        Vector3 nearestNormal = Vector3.zero;
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            RaycastHit hit = _probeBuffer[hitIndex];
            Collider hitCollider = hit.collider;

            if (hitCollider != null)
            {
                if (hitCollider.transform.IsChildOf(_root) == false)
                {
                    if (IsEnemyCollider(hitCollider) == false)
                    {
                        if (hit.distance < nearestDistance)
                        {
                            nearestDistance = hit.distance;
                            nearestNormal = hit.normal;
                        }
                    }
                }
            }

            hitIndex += 1;
        }

        if (nearestDistance == float.MaxValue)
        {
            return Vector3.zero;
        }

        Vector3 pushDirection = GetFlatDirection(nearestNormal);

        if (pushDirection.sqrMagnitude <= MinDistance)
        {
            pushDirection = -probeDirection;
        }

        float hitFactor = 1f - Mathf.Clamp01(nearestDistance / probeDistance);

        return pushDirection * hitFactor;
    }

    private Vector3 GetSeparationDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        if (_allyMask.value == 0)
        {
            return Vector3.zero;
        }

        if (_separationRadius <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 origin = currentPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            _separationRadius,
            _allyBuffer,
            _allyMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return Vector3.zero;
        }

        Vector3 separationDirection = Vector3.zero;
        int uniqueCount = 0;
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _allyBuffer[hitIndex];

            if (hitCollider != null)
            {
                Enemy otherEnemy = hitCollider.GetComponentInParent<Enemy>();

                if (otherEnemy != null)
                {
                    if (otherEnemy.gameObject != _root.gameObject && otherEnemy.IsDead == false)
                    {
                        int otherId = otherEnemy.gameObject.GetInstanceID();

                        if (ContainsAlly(otherId, uniqueCount) == false)
                        {
                            if (uniqueCount < _allyIdBuffer.Length)
                            {
                                _allyIdBuffer[uniqueCount] = otherId;
                                uniqueCount += 1;
                            }

                            Vector3 otherPoint = GetFlatPoint(otherEnemy.transform.position);
                            Vector3 awayDirection = currentPoint - otherPoint;
                            float distance = awayDirection.magnitude;

                            if (distance > MinDistance)
                            {
                                Vector3 lateralDirection = awayDirection / distance;
                                float frontWeight = 1f;

                                if (moveDirection.sqrMagnitude > MinDistance)
                                {
                                    Vector3 toOtherDirection = (otherPoint - currentPoint) / distance;
                                    frontWeight = Mathf.Clamp01(Vector3.Dot(moveDirection, toOtherDirection));

                                    if (frontWeight <= MinDistance)
                                    {
                                        hitIndex += 1;

                                        continue;
                                    }

                                    lateralDirection = Vector3.ProjectOnPlane(lateralDirection, moveDirection);
                                    lateralDirection = GetFlatDirection(lateralDirection);

                                    if (lateralDirection.sqrMagnitude <= MinDistance)
                                    {
                                        hitIndex += 1;
                                        continue;
                                    }
                                }

                                float weight = 1f - Mathf.Clamp01(distance / _separationRadius);
                                weight *= frontWeight;
                                separationDirection += lateralDirection * weight;
                            }
                        }
                    }
                }
            }

            hitIndex += 1;
        }

        return GetFlatVector(separationDirection);
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

    private bool ContainsAlly(int allyId, int uniqueCount)
    {
        int allyIndex = 0;

        while (allyIndex < uniqueCount)
        {
            if (_allyIdBuffer[allyIndex] == allyId)
            {
                return true;
            }

            allyIndex += 1;
        }

        return false;
    }

    private Vector3 GetSlotPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 targetDirection, float ringDistance, float slotOffset)
    {
        Vector3 fromTargetDirection = -targetDirection;
        Vector3 slotDirection = RotateDirection(fromTargetDirection, slotOffset);
        Vector3 slotPoint = targetPoint + (slotDirection * ringDistance);
        slotPoint.y = currentPoint.y;

        return slotPoint;
    }

    private float GetChaseDistance(float targetDistance, float ringDistance)
    {
        float maxDistance = Mathf.Max(_slotRadius, ringDistance);
        float blendMaxDistance = maxDistance + ringDistance;

        if (targetDistance >= blendMaxDistance)
        {
            return maxDistance;
        }

        if (targetDistance <= ringDistance)
        {
            return ringDistance;
        }

        float blend = Mathf.InverseLerp(ringDistance, blendMaxDistance, targetDistance);

        return Mathf.Lerp(ringDistance, maxDistance, blend);
    }

    private Vector3 GetChasePoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 targetDirection, float ringDistance, float targetDistance)
    {
        Vector3 chasePoint = targetPoint - (targetDirection * ringDistance);
        chasePoint.y = currentPoint.y;

        if (CanUseSlot(targetDistance, ringDistance) == false)
        {
            return chasePoint;
        }

        float slotOffset = GetSlotOffset(targetPoint);

        if (Mathf.Abs(slotOffset) <= SlotOffsetMin)
        {
            return chasePoint;
        }

        float chaseDistance = GetChaseDistance(targetDistance, ringDistance);

        return GetSlotPoint(currentPoint, targetPoint, targetDirection, chaseDistance, slotOffset);
    }

    private bool CanUseSlot(float targetDistance, float ringDistance)
    {
        float slotUseDistance = Mathf.Max(_slotRadius, ringDistance) + _separationRadius;

        if (targetDistance > slotUseDistance)
        {
            return false;
        }

        return true;
    }

    private float GetSlotOffset(Vector3 targetPoint)
    {
        if (_slotCount <= 1)
        {
            return 0f;
        }

        if (_allyMask.value == 0)
        {
            return 0f;
        }

        if (_slotRadius <= MinDistance)
        {
            return 0f;
        }

        if (_slotAngle <= 0f)
        {
            return 0f;
        }

        float searchRadius = Mathf.Max(_slotRadius * 2f, _separationRadius * 2f);
        Vector3 origin = targetPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            searchRadius,
            _allyBuffer,
            _allyMask,
            QueryTriggerInteraction.Ignore);
        int uniqueCount = 0;
        int rank = 0;
        int currentId = _root.gameObject.GetInstanceID();
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _allyBuffer[hitIndex];

            if (hitCollider != null)
            {
                Enemy otherEnemy = hitCollider.GetComponentInParent<Enemy>();

                if (otherEnemy != null)
                {
                    if (otherEnemy.gameObject != _root.gameObject && otherEnemy.IsDead == false)
                    {
                        int otherId = otherEnemy.gameObject.GetInstanceID();

                        if (ContainsAlly(otherId, uniqueCount) == false)
                        {
                            if (uniqueCount < _allyIdBuffer.Length)
                            {
                                _allyIdBuffer[uniqueCount] = otherId;
                                uniqueCount += 1;
                            }

                            if (otherId < currentId)
                            {
                                rank += 1;
                            }
                        }
                    }
                }
            }

            hitIndex += 1;
        }

        int slotIndex = rank % _slotCount;

        if (slotIndex == 0)
        {
            return 0f;
        }

        int offsetStep = ((slotIndex - 1) / 2) + 1;
        float angle = _slotAngle * offsetStep;

        if (slotIndex % 2 == 0)
        {
            angle = -angle;
        }

        return angle;
    }

    private bool HasObstaclePoint(Vector3 point)
    {
        if (_obstacleMask.value == 0)
        {
            return false;
        }

        Vector3 origin = GetFlatPoint(point) + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            _probeRadius,
            _pointBuffer,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _pointBuffer[hitIndex];

            if (hitCollider != null)
            {
                if (hitCollider.transform.IsChildOf(_root) == false && IsEnemyCollider(hitCollider) == false)
                {
                    return true;
                }
            }

            hitIndex += 1;
        }

        return false;
    }

    private Vector3 GetOverlapPush()
    {
        if (_obstacleMask.value == 0)
        {
            return Vector3.zero;
        }

        if (_bodyBuffer == null)
        {
            return Vector3.zero;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 origin = currentPoint + (Vector3.up * _probeHeight);
        float probeRadius = Mathf.Max(_probeRadius, 0.2f);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            probeRadius,
            _pointBuffer,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return Vector3.zero;
        }

        Vector3 pushDirection = Vector3.zero;
        int bodyIndex = 0;

        while (bodyIndex < _bodyBuffer.Length)
        {
            Collider bodyCollider = _bodyBuffer[bodyIndex];

            if (CanUseBodyCollider(bodyCollider))
            {
                int hitIndex = 0;

                while (hitIndex < hitCount)
                {
                    Collider hitCollider = _pointBuffer[hitIndex];

                    if (hitCollider != null)
                    {
                        if (hitCollider.transform.IsChildOf(_root) == false && hitCollider.isTrigger == false)
                        {
                            if (IsEnemyCollider(hitCollider) == false)
                            {
                                Vector3 overlapDirection;
                                float overlapDistance;
                                bool hasOverlap = Physics.ComputePenetration(
                                    bodyCollider,
                                    bodyCollider.transform.position,
                                    bodyCollider.transform.rotation,
                                    hitCollider,
                                    hitCollider.transform.position,
                                    hitCollider.transform.rotation,
                                    out overlapDirection,
                                    out overlapDistance);

                                if (hasOverlap)
                                {
                                    if (overlapDistance > 0f)
                                    {
                                        overlapDirection.y = 0f;

                                        if (overlapDirection.sqrMagnitude > MinDistance)
                                        {
                                            overlapDirection.Normalize();
                                            pushDirection += overlapDirection * overlapDistance;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    hitIndex += 1;
                }
            }

            bodyIndex += 1;
        }

        pushDirection.y = 0f;

        return pushDirection;
    }

    private bool CanUseBodyCollider(Collider bodyCollider)
    {
        if (bodyCollider == null)
        {
            return false;
        }

        if (bodyCollider.enabled == false)
        {
            return false;
        }

        if (bodyCollider.isTrigger)
        {
            return false;
        }

        Rigidbody bodyRigidbody = bodyCollider.attachedRigidbody;

        if (bodyRigidbody != null && bodyRigidbody != _rigidbody)
        {
            return false;
        }

        return true;
    }

    private bool IsEnemyCollider(Collider hitCollider)
    {
        Enemy hitEnemy = hitCollider.GetComponentInParent<Enemy>();

        if (hitEnemy == null)
        {
            return false;
        }

        return hitEnemy.gameObject != _root.gameObject;
    }

    private void ApplyResolve(Vector3 resolveVector)
    {
        Vector3 nextPosition = _root.position + resolveVector;

        if (_rigidbody != null)
        {
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            _rigidbody.position = nextPosition;
            _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);

            return;
        }

        _root.position = nextPosition;
    }

    private void ClearPath()
    {
        _pathTargetPoint = Vector3.zero;
        _pathStopDistance = 0f;
        _pathTime = 0f;
        _hasPathTarget = false;

        if (_navMeshAgent.enabled == false)
        {
            return;
        }

        if (_navMeshAgent.isOnNavMesh == false)
        {
            return;
        }

        _navMeshAgent.ResetPath();
        _navMeshAgent.nextPosition = GetFlatPoint(_root.position);
    }

    private bool TryGetNavPoint(Vector3 point, out Vector3 navPoint)
    {
        return TryGetNavPoint(point, GetNavSampleGap(), out navPoint);
    }

    private bool TryGetNavPoint(Vector3 point, float sampleGap, out Vector3 navPoint)
    {
        NavMeshHit navMeshHit;
        bool hasNavPoint = NavMesh.SamplePosition(point, out navMeshHit, sampleGap, NavMesh.AllAreas);

        if (hasNavPoint == false)
        {
            navPoint = GetFlatPoint(point);

            return false;
        }

        navPoint = GetFlatPoint(navMeshHit.position);

        return true;
    }

    private float GetNavSampleGap()
    {
        float minSampleGap = Mathf.Max(NavSampleGap, _probeRadius * 4f);

        return Mathf.Max(minSampleGap, _probeHeight * 4f);
    }

    private bool ContainsMovePoint(Vector3 point)
    {
        if (_enemyRoomLock == null)
        {
            return true;
        }

        return _enemyRoomLock.ContainsMovePoint(point);
    }

    private Vector3 ClampMovePoint(Vector3 point)
    {
        if (_enemyRoomLock == null)
        {
            return GetFlatPoint(point);
        }

        return GetFlatPoint(_enemyRoomLock.ClampMovePoint(point));
    }

    private Vector3 GetOrbitDirection(Vector3 targetDirection, bool isClockwise)
    {
        if (isClockwise)
        {
            return Vector3.Cross(targetDirection, Vector3.up);
        }

        return Vector3.Cross(Vector3.up, targetDirection);
    }

    private Vector3 GetFlatPoint(Vector3 point)
    {
        point.y = _root.position.y;

        return point;
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
