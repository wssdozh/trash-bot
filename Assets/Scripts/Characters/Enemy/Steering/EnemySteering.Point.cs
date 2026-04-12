using System;
using UnityEngine;
using UnityEngine.AI;

public sealed partial class EnemySteering
{
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

    public bool HasWallGap(Vector3 point, float wallGap)
    {
        if (wallGap <= MinDistance)
        {
            return true;
        }

        Vector3 flatPoint = ClampMovePoint(point);
        float sampleDistance = Mathf.Max(GetNavSampleGap(), wallGap * 4f);
        Vector3 navPoint;

        if (TryGetNavPoint(flatPoint, sampleDistance, out navPoint) == false)
        {
            return false;
        }

        if (ContainsMovePoint(navPoint) == false)
        {
            return false;
        }

        NavMeshHit edgeHit;
        bool hasEdge = NavMesh.FindClosestEdge(navPoint, out edgeHit, NavMesh.AllAreas);

        if (hasEdge == false)
        {
            return true;
        }

        if (edgeHit.distance < wallGap)
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

        Vector3 safePoint = navPoint;
        int pushIndex = 0;

        while (pushIndex < SafePointPushCount)
        {
            NavMeshHit edgeHit;
            bool hasEdge = NavMesh.FindClosestEdge(safePoint, out edgeHit, NavMesh.AllAreas);

            if (hasEdge == false)
            {
                return ClampMovePoint(safePoint);
            }

            if (edgeHit.distance >= wallGap)
            {
                return ClampMovePoint(safePoint);
            }

            Vector3 edgeDirection = GetFlatDirection(edgeHit.normal);

            if (edgeDirection.sqrMagnitude <= MinDistance)
            {
                return ClampMovePoint(safePoint);
            }

            float pushDistance = (wallGap - edgeHit.distance) + SafePointPushGap;
            Vector3 pushedPoint = safePoint + (edgeDirection * pushDistance);
            Vector3 nextPoint;

            if (TryGetNavPoint(pushedPoint, sampleDistance, out nextPoint) == false)
            {
                return ClampMovePoint(safePoint);
            }

            safePoint = ClampMovePoint(nextPoint);
            pushIndex += 1;
        }

        return ClampMovePoint(safePoint);
    }

    public bool TryPickNavPoint(Vector3 currentPoint, Vector3 forwardDirection, float minDistance, float maxDistance, float wallGap, int tryCount, Func<float> getProgress, out Vector3 navPoint)
    {
        if (getProgress == null)
        {
            throw new InvalidOperationException(nameof(getProgress));
        }

        navPoint = ClampMovePoint(currentPoint);

        if (tryCount <= 0)
        {
            return false;
        }

        if (minDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(minDistance));
        }

        if (maxDistance < minDistance)
        {
            throw new InvalidOperationException(nameof(maxDistance));
        }

        Vector3 flatCurrentPoint = GetFlatPoint(currentPoint);
        Vector3 baseDirection = GetFlatDirection(forwardDirection);

        if (baseDirection.sqrMagnitude <= MinDistance)
        {
            baseDirection = _enemyRotator.ForwardDirection;
        }

        float sampleGap = Mathf.Max(GetNavSampleGap(), maxDistance);
        int tryIndex = 0;

        while (tryIndex < tryCount)
        {
            float distance = Mathf.Lerp(minDistance, maxDistance, getProgress());
            float angle = Mathf.Lerp(-NavPickAngle, NavPickAngle, getProgress());
            Vector3 pickDirection = RotateDirection(baseDirection, angle);

            if (pickDirection.sqrMagnitude <= MinDistance)
            {
                tryIndex += 1;

                continue;
            }

            Vector3 candidatePoint = flatCurrentPoint + (pickDirection * distance);
            candidatePoint = ClampMovePoint(candidatePoint);
            Vector3 samplePoint;

            if (TryGetNavPoint(candidatePoint, sampleGap, out samplePoint) == false)
            {
                tryIndex += 1;

                continue;
            }

            Vector3 safePoint = GetSafePoint(samplePoint, wallGap);
            float reachDistance = Vector3.Distance(flatCurrentPoint, safePoint);

            if (reachDistance < minDistance)
            {
                tryIndex += 1;

                continue;
            }

            if (HasWallGap(safePoint, wallGap) == false)
            {
                tryIndex += 1;

                continue;
            }

            if (HasPointClearance(safePoint) == false)
            {
                tryIndex += 1;

                continue;
            }

            navPoint = safePoint;

            return true;
        }

        return false;
    }
}
