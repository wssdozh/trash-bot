using UnityEngine;
using UnityEngine.AI;

public sealed partial class EnemySteering
{
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

    private bool TryGetReachMovePoint(Vector3 currentPoint, Vector3 targetPoint, float stopDistance, out Vector3 reachPoint)
    {
        Vector3 nextReachPoint = GetReachPoint(targetPoint, _probeRadius);
        nextReachPoint = GetSafePoint(nextReachPoint, _probeRadius);
        float reachDistance = Vector3.Distance(currentPoint, nextReachPoint);

        if (reachDistance <= stopDistance + ReachGap)
        {
            reachPoint = Vector3.zero;

            return false;
        }

        if (ContainsMovePoint(nextReachPoint) == false)
        {
            reachPoint = Vector3.zero;

            return false;
        }

        if (HasObstaclePoint(nextReachPoint))
        {
            reachPoint = Vector3.zero;

            return false;
        }

        reachPoint = ClampMovePoint(nextReachPoint);

        return true;
    }

    private NavMeshAgent EnsureAgent()
    {
        NavMeshAgent navMeshAgent = _root.GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            if (HasAnyNavMesh() == false)
            {
                return null;
            }

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
        navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        ApplyAgentShape(navMeshAgent);
        navMeshAgent.enabled = false;

        return navMeshAgent;
    }

    private void ApplyAgentShape(NavMeshAgent navMeshAgent)
    {
        if (navMeshAgent == null)
        {
            return;
        }

        if (_probeRadius > 0f)
        {
            navMeshAgent.radius = Mathf.Max(_probeRadius, 0.1f);
        }

        if (_probeHeight > 0f)
        {
            navMeshAgent.height = Mathf.Max(_probeHeight * 2f, 0.5f);
        }
    }

    private bool HasAnyNavMesh()
    {
        NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();

        if (navMeshTriangulation.vertices == null)
        {
            return false;
        }

        return navMeshTriangulation.vertices.Length > 0;
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
        if (_navMeshAgent == null)
        {
            _navMeshAgent = EnsureAgent();

            if (_navMeshAgent == null)
            {
                return false;
            }
        }

        if (_navMeshAgent.enabled == false)
        {
            return TryActivateAgent(currentPoint);
        }

        if (_navMeshAgent.isOnNavMesh == false)
        {
            Vector3 navPoint;

            if (TryGetRecoverPoint(currentPoint, out navPoint) == false)
            {
                _navMeshAgent.enabled = false;

                return false;
            }

            if (GetFlatDistanceSqr(currentPoint, navPoint) > NavSnapGap * NavSnapGap)
            {
                SnapToPoint(navPoint);
                currentPoint = navPoint;
            }

            bool isWarped = _navMeshAgent.Warp(navPoint);

            if (isWarped == false)
            {
                _navMeshAgent.enabled = false;

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

        PullAgent(currentPoint);

        return true;
    }

    private bool TryActivateAgent(Vector3 currentPoint)
    {
        Vector3 navPoint;

        if (TryGetNavPoint(currentPoint, out navPoint) == false)
        {
            return false;
        }

        if (GetFlatDistanceSqr(currentPoint, navPoint) > NavSnapGap * NavSnapGap)
        {
            SnapToPoint(navPoint);
            currentPoint = navPoint;
        }

        _navMeshAgent.enabled = true;

        if (_navMeshAgent.isOnNavMesh == false)
        {
            bool isWarped = _navMeshAgent.Warp(navPoint);

            if (isWarped == false)
            {
                _navMeshAgent.enabled = false;

                return false;
            }
        }

        _navMeshAgent.nextPosition = currentPoint;
        CacheNavPoint(navPoint);

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

    private void PullAgent(Vector3 currentPoint)
    {
        Vector3 worldDeltaPosition = _navMeshAgent.nextPosition - currentPoint;
        worldDeltaPosition.y = 0f;
        float agentRadius = Mathf.Max(_navMeshAgent.radius, _probeRadius);

        if (worldDeltaPosition.sqrMagnitude <= agentRadius * agentRadius)
        {
            return;
        }

        _navMeshAgent.nextPosition = currentPoint + (worldDeltaPosition * 0.9f);
    }

    private bool TryRefreshPath(Vector3 targetPoint, float stopDistance)
    {
        Vector3 navTargetPoint;

        if (TryGetNavPoint(targetPoint, NavRecoverGap, out navTargetPoint) == false)
        {
            return false;
        }

        if (NeedPathRefresh(navTargetPoint, stopDistance))
        {
            _navMeshAgent.stoppingDistance = stopDistance;

            if (TrySetPath(navTargetPoint) == false)
            {
                return false;
            }

            _pathTargetPoint = navTargetPoint;
            _pathStopDistance = stopDistance;
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

        if (_navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
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

        if (_navMeshAgent.pathPending)
        {
            return false;
        }

        if (_navMeshAgent.hasPath == false)
        {
            return true;
        }

        if (GetFlatDistanceSqr(_pathTargetPoint, targetPoint) >= PathRefreshGap * PathRefreshGap)
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

        if (_navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            return true;
        }

        if (_navMeshAgent.isPathStale)
        {
            return true;
        }

        return false;
    }

    private bool TrySetPath(Vector3 targetPoint)
    {
        bool hasPath = _navMeshAgent.CalculatePath(targetPoint, _navMeshPath);

        if (hasPath == false)
        {
            return false;
        }

        if (_navMeshPath.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        Vector3[] pathCorners = _navMeshPath.corners;

        if (pathCorners == null)
        {
            return false;
        }

        if (pathCorners.Length == 0)
        {
            return false;
        }

        return _navMeshAgent.SetPath(_navMeshPath);
    }

    private Vector3 GetMoveDirection(Vector3 currentPoint)
    {
        Vector3 pathDirection = GetPathDirection(currentPoint);
        Vector3 moveDirection = GetFlatDirection(_navMeshAgent.desiredVelocity);

        if (pathDirection.sqrMagnitude > MinDistance)
        {
            if (moveDirection.sqrMagnitude > MinDistance)
            {
                float directionDot = Vector3.Dot(pathDirection, moveDirection);

                if (directionDot > 0f)
                {
                    return GetFlatDirection(pathDirection + moveDirection);
                }
            }

            return pathDirection;
        }

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

    private Vector3 GetPathDirection(Vector3 currentPoint)
    {
        if (_navMeshAgent.hasPath == false)
        {
            return Vector3.zero;
        }

        Vector3[] pathCorners = _navMeshAgent.path.corners;

        if (pathCorners == null)
        {
            return Vector3.zero;
        }

        if (pathCorners.Length == 0)
        {
            return Vector3.zero;
        }

        float lookAheadDistance = Mathf.Max(PathLookAheadDistance, _probeRadius * 2f);
        Vector3 segmentStart = currentPoint;
        Vector3 lookPoint = currentPoint;
        float remainingDistance = lookAheadDistance;
        int cornerIndex = 0;

        while (cornerIndex < pathCorners.Length)
        {
            Vector3 cornerPoint = GetFlatPoint(pathCorners[cornerIndex]);
            Vector3 segment = cornerPoint - segmentStart;
            float segmentLength = segment.magnitude;

            if (segmentLength <= MinDistance)
            {
                cornerIndex += 1;

                continue;
            }

            if (remainingDistance <= segmentLength)
            {
                Vector3 segmentDirection = segment / segmentLength;
                lookPoint = segmentStart + (segmentDirection * remainingDistance);

                break;
            }

            remainingDistance -= segmentLength;
            segmentStart = cornerPoint;
            lookPoint = cornerPoint;
            cornerIndex += 1;
        }

        return GetFlatDirection(lookPoint - currentPoint);
    }

    private void ClearPath()
    {
        _pathTargetPoint = Vector3.zero;
        _pathStopDistance = 0f;
        _hasPathTarget = false;

        if (_navMeshAgent == null)
        {
            return;
        }

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
}
