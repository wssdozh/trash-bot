using System;
using UnityEngine;

public sealed class EnemyPatrolPicker
{
    private const float ZeroThreshold = 0.0001f;

    private int _patrolIndex;
    private int _patrolDirection;
    private bool _hasPatrolIndex;

    public void Clear()
    {
        _hasPatrolIndex = false;
    }

    public bool TryPickPoint(EnemyRoomLock enemyRoomLock, Vector3 currentPoint, Vector3 forwardDirection, Vector3 fallbackPoint, float height, float minDistance, int maxTryCount, Func<int> getFallbackDirection, out Vector3 patrolPoint)
    {
        if (getFallbackDirection == null)
        {
            throw new InvalidOperationException(nameof(getFallbackDirection));
        }

        patrolPoint = fallbackPoint;
        patrolPoint.y = height;

        if (enemyRoomLock == null)
        {
            return false;
        }

        int patrolCount = enemyRoomLock.GetPatrolCount();

        if (patrolCount <= 0)
        {
            return false;
        }

        EnsurePatrolIndex(enemyRoomLock, currentPoint, forwardDirection, height, getFallbackDirection);

        Vector3 bestPoint = enemyRoomLock.ClampMovePoint(fallbackPoint);
        bestPoint.y = height;
        float bestDistance = -1f;
        int bestIndex = _patrolIndex;
        int tryIndex = 0;
        int patrolIndex = _patrolIndex;

        while (tryIndex < patrolCount && tryIndex < maxTryCount)
        {
            patrolIndex = enemyRoomLock.GetNextPatrolIndex(patrolIndex, _patrolDirection);
            Vector3 nextPoint = enemyRoomLock.GetPatrolPoint(patrolIndex, height);
            nextPoint = enemyRoomLock.ClampMovePoint(nextPoint);
            nextPoint.y = height;
            float nextDistance = GetFlatDistance(currentPoint, nextPoint);

            if (nextDistance > bestDistance)
            {
                bestDistance = nextDistance;
                bestPoint = nextPoint;
                bestIndex = patrolIndex;
            }

            if (nextDistance >= minDistance)
            {
                _patrolIndex = patrolIndex;
                patrolPoint = nextPoint;

                return true;
            }

            tryIndex += 1;
        }

        _patrolIndex = bestIndex;
        patrolPoint = bestPoint;

        return bestDistance > ZeroThreshold;
    }

    public bool TryPickNextPoint(EnemyRoomLock enemyRoomLock, Vector3 currentPoint, Vector3 forwardDirection, float height, int maxTryCount, Func<int> getFallbackDirection, Func<Vector3, bool> isPointAccepted)
    {
        if (getFallbackDirection == null)
        {
            throw new InvalidOperationException(nameof(getFallbackDirection));
        }

        if (isPointAccepted == null)
        {
            throw new InvalidOperationException(nameof(isPointAccepted));
        }

        if (enemyRoomLock == null)
        {
            return false;
        }

        int patrolCount = enemyRoomLock.GetPatrolCount();

        if (patrolCount <= 1)
        {
            return false;
        }

        EnsurePatrolIndex(enemyRoomLock, currentPoint, forwardDirection, height, getFallbackDirection);

        int tryIndex = 0;
        int patrolIndex = _patrolIndex;

        while (tryIndex < patrolCount && tryIndex < maxTryCount)
        {
            patrolIndex = enemyRoomLock.GetNextPatrolIndex(patrolIndex, _patrolDirection);
            Vector3 patrolPoint = enemyRoomLock.GetPatrolPoint(patrolIndex, height);

            if (isPointAccepted(patrolPoint))
            {
                _patrolIndex = patrolIndex;

                return true;
            }

            tryIndex += 1;
        }

        return false;
    }

    private void EnsurePatrolIndex(EnemyRoomLock enemyRoomLock, Vector3 currentPoint, Vector3 forwardDirection, float height, Func<int> getFallbackDirection)
    {
        if (_hasPatrolIndex)
        {
            return;
        }

        _patrolIndex = enemyRoomLock.GetNearestPatrolIndex(currentPoint);
        _patrolDirection = GetPatrolDirection(enemyRoomLock, currentPoint, forwardDirection, height, getFallbackDirection);
        _hasPatrolIndex = true;
    }

    private int GetPatrolDirection(EnemyRoomLock enemyRoomLock, Vector3 currentPoint, Vector3 forwardDirection, float height, Func<int> getFallbackDirection)
    {
        Vector3 flatForwardDirection = GetFlatDirection(forwardDirection);
        int forwardIndex = enemyRoomLock.GetNextPatrolIndex(_patrolIndex, 1);
        int backwardIndex = enemyRoomLock.GetNextPatrolIndex(_patrolIndex, -1);
        Vector3 forwardPoint = enemyRoomLock.GetPatrolPoint(forwardIndex, height);
        Vector3 backwardPoint = enemyRoomLock.GetPatrolPoint(backwardIndex, height);
        float forwardDot = GetPatrolDot(currentPoint, flatForwardDirection, forwardPoint);
        float backwardDot = GetPatrolDot(currentPoint, flatForwardDirection, backwardPoint);

        if (Mathf.Abs(forwardDot - backwardDot) <= ZeroThreshold)
        {
            return GetDirection(getFallbackDirection());
        }

        if (forwardDot >= backwardDot)
        {
            return 1;
        }

        return -1;
    }

    private float GetPatrolDot(Vector3 currentPoint, Vector3 forwardDirection, Vector3 patrolPoint)
    {
        Vector3 patrolDirection = patrolPoint - currentPoint;
        patrolDirection.y = 0f;

        if (patrolDirection.sqrMagnitude <= ZeroThreshold)
        {
            return -1f;
        }

        patrolDirection.Normalize();

        return Vector3.Dot(forwardDirection, patrolDirection);
    }

    private float GetFlatDistance(Vector3 firstPoint, Vector3 secondPoint)
    {
        firstPoint.y = 0f;
        secondPoint.y = 0f;

        return Vector3.Distance(firstPoint, secondPoint);
    }

    private Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.zero;
        }

        direction.Normalize();

        return direction;
    }

    private int GetDirection(int patrolDirection)
    {
        if (patrolDirection >= 0)
        {
            return 1;
        }

        return -1;
    }
}
