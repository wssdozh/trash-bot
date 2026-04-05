using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class RoomRuntimeState : MonoBehaviour
{
    private const float MinBoundsSize = 0.1f;
    private const int RandomPointTryCount = 16;

    private Bounds _roomBounds;
    private Bounds _moveBounds;
    private float _cornerGap;
    private bool _isReady;

    public void Setup(Bounds roomBounds, float enemyBorderGap)
    {
        _roomBounds = NormalizeBounds(roomBounds);
        _moveBounds = BuildMoveBounds(_roomBounds, enemyBorderGap);
        _cornerGap = BuildCornerGap(_moveBounds, enemyBorderGap);
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
}
