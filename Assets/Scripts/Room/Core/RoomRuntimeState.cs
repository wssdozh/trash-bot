using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomRuntimeState : MonoBehaviour
{
    private const float MinBoundsSize = 0.1f;

    private Bounds _roomBounds;
    private Bounds _moveBounds;
    private bool _isReady;

    public void Setup(Bounds roomBounds, float enemyBorderGap)
    {
        _roomBounds = NormalizeBounds(roomBounds);
        _moveBounds = BuildMoveBounds(_roomBounds, enemyBorderGap);
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

        return _moveBounds.Contains(GetFlatPoint(point, _moveBounds.center.y));
    }

    public Vector3 ClampMovePoint(Vector3 point)
    {
        if (_isReady == false)
        {
            return point;
        }

        return ClampPoint(_moveBounds, point);
    }

    public Vector3 ClampSnapPoint(Vector3 point)
    {
        if (_isReady == false)
        {
            return point;
        }

        return ClampPoint(_moveBounds, point);
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

    public void SetRoomActive(bool isActive)
    {
        if (gameObject.activeSelf == isActive)
        {
            return;
        }

        gameObject.SetActive(isActive);
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

    private Vector3 GetFlatPoint(Vector3 point, float centerY)
    {
        point.y = centerY;

        return point;
    }

    private Vector3 ClampPoint(Bounds bounds, Vector3 point)
    {
        Vector3 flatPoint = GetFlatPoint(point, bounds.center.y);
        Vector3 clampedPoint = bounds.ClosestPoint(flatPoint);
        clampedPoint.y = point.y;

        return clampedPoint;
    }
}
