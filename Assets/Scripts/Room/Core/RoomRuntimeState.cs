using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class RoomRuntimeState : MonoBehaviour
{
    private const float MinBoundsSize = 0.1f;
    private const int RandomPointTryCount = 16;
    private const int PatrolPointCount = 12;
    private const int PatrolRingCount = 2;
    private const float PatrolInsetGap = 1.2f;
    private const float PatrolRadiusScale = 0.72f;
    private const float PatrolInnerScale = 0.48f;

    private Bounds _roomBounds;
    private Bounds _moveBounds;
    private float _cornerGap;
    private Vector3[] _patrolPoints = Array.Empty<Vector3>();
    private Vector3[] _groundPatrolPoints = Array.Empty<Vector3>();
    private bool _isReady;

    public void Setup(Bounds roomBounds, float enemyBorderGap)
    {
        _roomBounds = NormalizeBounds(roomBounds);
        _moveBounds = BuildMoveBounds(_roomBounds, enemyBorderGap);
        _cornerGap = BuildCornerGap(_moveBounds, enemyBorderGap);
        _patrolPoints = BuildPatrolPoints(_moveBounds, _cornerGap);
        _groundPatrolPoints = _patrolPoints;
        _isReady = true;
    }

    public bool ContainsRoomPoint(Vector3 point)
    {
        if (_isReady == false)
        {
            return true;
        }

        return _roomBounds.Contains(GetFlatPoint(point, _roomBounds.center.y));
    }

    public bool ContainsMovePoint(Vector3 point)
    {
        if (_isReady == false)
        {
            return true;
        }

        return ContainsRoundedPoint(_moveBounds, _cornerGap, point);
    }

    public Vector3 ClampMovePoint(Vector3 point)
    {
        if (_isReady == false)
        {
            return point;
        }

        return ClampRoundedPoint(_moveBounds, _cornerGap, point);
    }

    public Vector3 ClampSnapPoint(Vector3 point)
    {
        if (_isReady == false)
        {
            return point;
        }

        return ClampRoundedPoint(_moveBounds, _cornerGap, point);
    }

    public float GetMoveTop()
    {
        if (_isReady == false)
        {
            return 0f;
        }

        return _moveBounds.max.y;
    }

    public float GetMoveBottom()
    {
        if (_isReady == false)
        {
            return 0f;
        }

        return _moveBounds.min.y;
    }

    public int GetPatrolCount()
    {
        if (_isReady == false)
        {
            return 0;
        }

        return _patrolPoints.Length;
    }

    public Vector3 GetPatrolPoint(int patrolIndex, float height)
    {
        if (_isReady == false)
        {
            return new Vector3(0f, height, 0f);
        }

        if (_patrolPoints.Length == 0)
        {
            Vector3 centerPoint = _moveBounds.center;
            centerPoint.y = height;

            return centerPoint;
        }

        int loopIndex = GetLoopIndex(patrolIndex, _patrolPoints.Length);
        Vector3 patrolPoint = _patrolPoints[loopIndex];
        patrolPoint.y = height;

        return patrolPoint;
    }

    public int GetGroundPatrolCount()
    {
        if (_isReady == false)
        {
            return 0;
        }

        return _groundPatrolPoints.Length;
    }

    public Vector3 GetGroundPatrolPoint(int patrolIndex, float height)
    {
        if (_isReady == false)
        {
            return new Vector3(0f, height, 0f);
        }

        if (_groundPatrolPoints.Length == 0)
        {
            Vector3 centerPoint = _moveBounds.center;
            centerPoint.y = height;

            return centerPoint;
        }

        int loopIndex = GetLoopIndex(patrolIndex, _groundPatrolPoints.Length);
        Vector3 patrolPoint = _groundPatrolPoints[loopIndex];
        patrolPoint.y = height;

        return patrolPoint;
    }

    public int GetNearestPatrolIndex(Vector3 point)
    {
        if (_isReady == false)
        {
            return 0;
        }

        if (_patrolPoints.Length == 0)
        {
            return 0;
        }

        Vector3 flatPoint = ClampRoundedPoint(_moveBounds, _cornerGap, point);
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;
        int pointIndex = 0;

        while (pointIndex < _patrolPoints.Length)
        {
            Vector3 patrolPoint = _patrolPoints[pointIndex];
            float pointDistance = (patrolPoint - flatPoint).sqrMagnitude;

            if (pointDistance < nearestDistance)
            {
                nearestDistance = pointDistance;
                nearestIndex = pointIndex;
            }

            pointIndex += 1;
        }

        return nearestIndex;
    }

    public int GetNearestGroundPatrolIndex(Vector3 point)
    {
        if (_isReady == false)
        {
            return 0;
        }

        if (_groundPatrolPoints.Length == 0)
        {
            return 0;
        }

        Vector3 flatPoint = ClampRoundedPoint(_moveBounds, _cornerGap, point);
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;
        int pointIndex = 0;

        while (pointIndex < _groundPatrolPoints.Length)
        {
            Vector3 patrolPoint = _groundPatrolPoints[pointIndex];
            float pointDistance = (patrolPoint - flatPoint).sqrMagnitude;

            if (pointDistance < nearestDistance)
            {
                nearestDistance = pointDistance;
                nearestIndex = pointIndex;
            }

            pointIndex += 1;
        }

        return nearestIndex;
    }

    public Vector3 GetRandomMovePoint(float height, System.Random random)
    {
        if (_isReady == false)
        {
            return new Vector3(0f, height, 0f);
        }

        if (random == null)
        {
            throw new InvalidOperationException(nameof(random));
        }

        float xProgress = (float)random.NextDouble();
        float zProgress = (float)random.NextDouble();
        int tryIndex = 0;

        while (tryIndex < RandomPointTryCount)
        {
            float pointX = Mathf.Lerp(_moveBounds.min.x, _moveBounds.max.x, xProgress);
            float pointZ = Mathf.Lerp(_moveBounds.min.z, _moveBounds.max.z, zProgress);
            Vector3 point = new Vector3(pointX, height, pointZ);

            if (ContainsRoundedPoint(_moveBounds, _cornerGap, point))
            {
                return point;
            }

            xProgress = (float)random.NextDouble();
            zProgress = (float)random.NextDouble();
            tryIndex += 1;
        }

        float fallbackX = Mathf.Lerp(_moveBounds.min.x, _moveBounds.max.x, xProgress);
        float fallbackZ = Mathf.Lerp(_moveBounds.min.z, _moveBounds.max.z, zProgress);
        Vector3 fallbackPoint = new Vector3(fallbackX, height, fallbackZ);

        return ClampRoundedPoint(_moveBounds, _cornerGap, fallbackPoint);
    }

    public void SetPatrolPoints(IReadOnlyList<Vector3> patrolPoints)
    {
        if (_isReady == false)
        {
            return;
        }

        _patrolPoints = BuildCustomPatrolPoints(patrolPoints);
    }

    public void SetGroundPatrolPoints(IReadOnlyList<Vector3> patrolPoints)
    {
        if (_isReady == false)
        {
            return;
        }

        _groundPatrolPoints = BuildCustomPatrolPoints(patrolPoints);
    }

    public void ClearGroundPatrolPoints()
    {
        if (_isReady == false)
        {
            return;
        }

        _groundPatrolPoints = Array.Empty<Vector3>();
    }

    public float GetDistanceSqr(Vector3 point)
    {
        if (_isReady == false)
        {
            return 0f;
        }

        Vector3 flatPoint = GetFlatPoint(point, _roomBounds.center.y);
        Vector3 closestPoint = _roomBounds.ClosestPoint(flatPoint);
        float distanceX = flatPoint.x - closestPoint.x;
        float distanceZ = flatPoint.z - closestPoint.z;

        return (distanceX * distanceX) + (distanceZ * distanceZ);
    }

    public Bounds GetRoomBounds()
    {
        if (_isReady == false)
        {
            return new Bounds(transform.position, Vector3.zero);
        }

        return _roomBounds;
    }

    public void SetRoomActive(bool isActive)
    {
        if (gameObject.activeSelf == isActive)
        {
            return;
        }

        if (isActive == false)
        {
            DisableNavMeshAgents();
        }

        gameObject.SetActive(isActive);
    }

    private void DisableNavMeshAgents()
    {
        NavMeshAgent[] navMeshAgents = GetComponentsInChildren<NavMeshAgent>(true);

        for (int agentIndex = 0; agentIndex < navMeshAgents.Length; agentIndex++)
        {
            NavMeshAgent navMeshAgent = navMeshAgents[agentIndex];

            if (navMeshAgent == null)
            {
                continue;
            }

            navMeshAgent.enabled = false;
        }
    }

    private Bounds NormalizeBounds(Bounds roomBounds)
    {
        Bounds normalizedBounds = roomBounds;
        Vector3 size = normalizedBounds.size;

        if (size.y < MinBoundsSize)
        {
            size.y = MinBoundsSize;
        }

        normalizedBounds.size = size;

        return normalizedBounds;
    }

    private Bounds BuildMoveBounds(Bounds roomBounds, float enemyBorderGap)
    {
        Bounds moveBounds = roomBounds;
        Vector3 size = moveBounds.size;
        float shrinkSize = Mathf.Max(0f, enemyBorderGap) * 2f;

        size.x = Mathf.Max(MinBoundsSize, size.x - shrinkSize);
        size.z = Mathf.Max(MinBoundsSize, size.z - shrinkSize);
        moveBounds.size = size;

        return moveBounds;
    }

    private Vector3[] BuildCustomPatrolPoints(IReadOnlyList<Vector3> patrolPoints)
    {
        if (patrolPoints == null)
        {
            return BuildPatrolPoints(_moveBounds, _cornerGap);
        }

        if (patrolPoints.Count == 0)
        {
            return BuildPatrolPoints(_moveBounds, _cornerGap);
        }

        Vector3[] nextPatrolPoints = new Vector3[patrolPoints.Count];
        int pointIndex = 0;

        while (pointIndex < patrolPoints.Count)
        {
            Vector3 patrolPoint = ClampRoundedPoint(_moveBounds, _cornerGap, patrolPoints[pointIndex]);
            patrolPoint.y = _moveBounds.center.y;
            nextPatrolPoints[pointIndex] = patrolPoint;
            pointIndex += 1;
        }

        return nextPatrolPoints;
    }

    private Vector3[] BuildPatrolPoints(Bounds moveBounds, float cornerGap)
    {
        Vector3[] patrolPoints = new Vector3[PatrolPointCount];
        float outerRadiusX = GetPatrolRadius(moveBounds.extents.x, cornerGap);
        float outerRadiusZ = GetPatrolRadius(moveBounds.extents.z, cornerGap);
        float innerRadiusX = GetInnerPatrolRadius(outerRadiusX);
        float innerRadiusZ = GetInnerPatrolRadius(outerRadiusZ);
        int ringPointCount = PatrolPointCount / PatrolRingCount;
        float angleStep = (Mathf.PI * 2f) / ringPointCount;
        int pointIndex = 0;

        while (pointIndex < PatrolPointCount)
        {
            bool isOuterPoint = pointIndex % PatrolRingCount == 0;
            int ringIndex = pointIndex / PatrolRingCount;
            float angle = angleStep * ringIndex;

            if (isOuterPoint == false)
            {
                angle += angleStep * 0.5f;
            }

            float radiusX = outerRadiusX;
            float radiusZ = outerRadiusZ;

            if (isOuterPoint == false)
            {
                radiusX = innerRadiusX;
                radiusZ = innerRadiusZ;
            }

            float pointX = moveBounds.center.x + (Mathf.Cos(angle) * radiusX);
            float pointZ = moveBounds.center.z + (Mathf.Sin(angle) * radiusZ);
            Vector3 patrolPoint = new Vector3(pointX, moveBounds.center.y, pointZ);
            patrolPoints[pointIndex] = ClampRoundedPoint(moveBounds, cornerGap, patrolPoint);
            pointIndex += 1;
        }

        return patrolPoints;
    }

    private float GetPatrolRadius(float extent, float cornerGap)
    {
        float patrolInset = Mathf.Max(PatrolInsetGap, cornerGap + (PatrolInsetGap * 0.5f));
        float maxRadius = Mathf.Max(MinBoundsSize * 0.5f, extent - patrolInset);
        float scaledRadius = extent * PatrolRadiusScale;

        return Mathf.Max(MinBoundsSize * 0.5f, Mathf.Min(maxRadius, scaledRadius));
    }

    private float GetInnerPatrolRadius(float outerRadius)
    {
        float innerRadius = Mathf.Max(MinBoundsSize * 0.5f, outerRadius * PatrolInnerScale);

        return Mathf.Min(outerRadius, innerRadius);
    }

    private float BuildCornerGap(Bounds moveBounds, float enemyBorderGap)
    {
        float maxCornerGap = Mathf.Min(moveBounds.extents.x, moveBounds.extents.z) - (MinBoundsSize * 0.5f);

        if (maxCornerGap <= 0f)
        {
            return 0f;
        }

        float cornerGap = Mathf.Max(0f, enemyBorderGap) * 2f;

        return Mathf.Min(cornerGap, maxCornerGap);
    }

    private Vector3 GetFlatPoint(Vector3 point, float centerY)
    {
        point.y = centerY;

        return point;
    }

    private bool ContainsRoundedPoint(Bounds bounds, float cornerGap, Vector3 point)
    {
        Vector3 flatPoint = GetFlatPoint(point, bounds.center.y);

        if (bounds.Contains(flatPoint) == false)
        {
            return false;
        }

        if (cornerGap <= 0f)
        {
            return true;
        }

        Vector3 localPoint = flatPoint - bounds.center;
        float absX = Mathf.Abs(localPoint.x);
        float absZ = Mathf.Abs(localPoint.z);
        float innerX = Mathf.Max(0f, bounds.extents.x - cornerGap);
        float innerZ = Mathf.Max(0f, bounds.extents.z - cornerGap);

        if (absX <= innerX)
        {
            return true;
        }

        if (absZ <= innerZ)
        {
            return true;
        }

        float offsetX = absX - innerX;
        float offsetZ = absZ - innerZ;

        return (offsetX * offsetX) + (offsetZ * offsetZ) <= cornerGap * cornerGap;
    }

    private Vector3 ClampRoundedPoint(Bounds bounds, float cornerGap, Vector3 point)
    {
        Vector3 flatPoint = GetFlatPoint(point, bounds.center.y);

        if (cornerGap <= 0f)
        {
            Vector3 clampedBoundsPoint = bounds.ClosestPoint(flatPoint);
            clampedBoundsPoint.y = point.y;

            return clampedBoundsPoint;
        }

        Vector3 clampedPoint = bounds.ClosestPoint(flatPoint);
        Vector3 localPoint = clampedPoint - bounds.center;
        float signX = Mathf.Sign(localPoint.x);
        float signZ = Mathf.Sign(localPoint.z);

        if (Mathf.Abs(signX) <= Mathf.Epsilon)
        {
            signX = 1f;
        }

        if (Mathf.Abs(signZ) <= Mathf.Epsilon)
        {
            signZ = 1f;
        }

        float absX = Mathf.Abs(localPoint.x);
        float absZ = Mathf.Abs(localPoint.z);
        float innerX = Mathf.Max(0f, bounds.extents.x - cornerGap);
        float innerZ = Mathf.Max(0f, bounds.extents.z - cornerGap);

        if (absX > innerX && absZ > innerZ)
        {
            Vector2 cornerOffset = new Vector2(absX - innerX, absZ - innerZ);

            if (cornerOffset.sqrMagnitude > cornerGap * cornerGap)
            {
                cornerOffset.Normalize();
                absX = innerX + (cornerOffset.x * cornerGap);
                absZ = innerZ + (cornerOffset.y * cornerGap);
                clampedPoint.x = bounds.center.x + (absX * signX);
                clampedPoint.z = bounds.center.z + (absZ * signZ);
            }
        }

        clampedPoint.y = point.y;

        return clampedPoint;
    }

    private int GetLoopIndex(int pointIndex, int pointCount)
    {
        if (pointCount <= 0)
        {
            return 0;
        }

        int loopIndex = pointIndex % pointCount;

        if (loopIndex < 0)
        {
            loopIndex += pointCount;
        }

        return loopIndex;
    }
}
