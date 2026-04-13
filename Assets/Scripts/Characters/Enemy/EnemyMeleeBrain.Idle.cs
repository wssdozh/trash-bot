using UnityEngine;

public sealed partial class EnemyMeleeBrain
{
    private void ProcessIdle()
    {
        _enemyMove.SetRun(false);
        _hasSearchPoint = false;
        _searchStep = 0;

        if (_isIdleWalking)
        {
            _state = EnemyState.Patrol;
            ProcessIdleWalk();

            return;
        }

        _state = EnemyState.Watch;
        ProcessIdleLook();
    }

    private void ProcessIdleWalk()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);
        float distance = Vector3.Distance(currentPoint, _idleTargetPoint);
        float stopDistance = GetIdleStopDistance();

        TickIdleStuck(distance, stopDistance);

        if (_idleStuckTimer >= IdleStuckSeconds)
        {
            if (_enemySteering.ResolveOverlap())
            {
                _idleLastDistance = -1f;
                _idleStuckTimer = 0f;
                ResetMoveStuck();

                return;
            }

            StartIdleWalk();

            return;
        }

        if (distance > stopDistance)
        {
            bool isMoving = _enemySteering.MoveToPoint(_idleTargetPoint, stopDistance);

            if (isMoving)
            {
                if (_enemySteering.CanKeepMove(currentPoint, Time.fixedDeltaTime))
                {
                    return;
                }
            }

            StartIdleWalk();

            return;
        }

        if (_idleChain > 0)
        {
            _idleChain -= 1;
        }

        if (_idleChain > 0)
        {
            StartIdleWalk();

            return;
        }

        StartIdleLook();
    }

    private void ProcessIdleLook()
    {
        ResetMoveStuck();
        _enemySteering.LookToPoint(_idleLookPoint);
        _idleTimer -= Time.fixedDeltaTime;

        if (_idleTimer > 0f)
        {
            return;
        }

        StartIdleWalk();
    }

    private void ProcessSearch(Vector3 currentPoint)
    {
        _enemyMove.SetRun(false);
        _isSearchIdle = true;

        if (_hasSearchPoint == false)
        {
            if (StartSearchPoint() == false)
            {
                FinishSearch();

                return;
            }
        }

        float distance = Vector3.Distance(currentPoint, _searchTargetPoint);

        if (distance > _searchPointDistance)
        {
            bool isMoving = false;

            if (_isSafeMove)
            {
                isMoving = _enemySteering.MoveDirect(_searchTargetPoint, _searchPointDistance, _safeMoveSpeed, _searchTargetPoint);
            }

            else
            {
                isMoving = _enemySteering.MoveToPoint(_searchTargetPoint, _searchPointDistance);
            }

            if (TrySearchMove(isMoving, currentPoint))
            {
                return;
            }

            if (_searchStep < SearchStepsCount)
            {
                return;
            }

            FinishSearch();

            return;
        }

        AdvanceSearchPoint();
    }

    private void StartIdleWalk()
    {
        bool isNewChain = _idleChain <= 0;

        if (isNewChain)
        {
            _idleChain = GetIdleChainCount();
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);

        if (TryStartWallWalk(currentPoint))
        {
            return;
        }

        if (TryStartGuardWalk(currentPoint))
        {
            return;
        }

        if (TryStartNavWalk(currentPoint))
        {
            return;
        }

        if (HasPatrolRoute() == false)
        {
            int attemptIndex = 0;

            while (attemptIndex < IdlePointTryCount)
            {
                Vector3 nextDirection = _idleDirection;

                if (attemptIndex > 0 || isNewChain == false)
                {
                    nextDirection = GetNextIdleDirection();
                }

                float idleDistance = GetIdleDistance();

                if (TrySetIdlePoint(currentPoint, nextDirection, idleDistance))
                {
                    return;
                }

                attemptIndex += 1;
            }

            if (TrySetIdlePoint(currentPoint, _idleDirection, IdleFallbackDistance))
            {
                return;
            }
        }

        StartIdleLook();
    }

    private void StartIdleLook()
    {
        _isIdleWalking = false;
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        _idleChain = 0;
        ResetMoveStuck();
        _enemyMove.SetRun(false);
        _idleTimer = GetIdleWait();

        if (_isSearchIdle)
        {
            _idleLookPoint = transform.position + (_idleDirection * LookDistance);
        }

        else
        {
            _idleLookPoint = transform.position + (GetIdleLookDirection() * LookDistance);
        }

        _enemySteering.Stop();
    }

    private bool StartSearchPoint()
    {
        Vector3 currentPoint = GetFlatPoint(transform.position);
        float wallGap = GetMoveWallGap();

        if (TryEscapeSearchPoint(currentPoint, wallGap))
        {
            return true;
        }

        while (_searchStep < SearchStepsCount)
        {
            Vector3 candidatePoint = _enemySteering.GetSafePoint(GetFlatPoint(GetSearchPoint()), wallGap);
            float distance = Vector3.Distance(currentPoint, candidatePoint);

            if (distance <= _searchPointDistance)
            {
                _searchStep += 1;

                continue;
            }

            if (_enemySteering.HasPointClearance(candidatePoint) == false)
            {
                _searchStep += 1;

                continue;
            }

            _searchTargetPoint = candidatePoint;
            _hasSearchPoint = true;
            _isSafeMove = false;

            return true;
        }

        return false;
    }

    private void AdvanceSearchPoint()
    {
        _hasSearchPoint = false;
        _isSafeMove = false;
        _searchStep += 1;
        ResetMoveStuck();
    }

    private void FinishSearch()
    {
        Vector3 searchDirection = GetSearchDirection();

        ClearSearch();
        _isSearchIdle = true;

        _state = EnemyState.Watch;
        _idleDirection = searchDirection;
        _isIdleWalking = false;
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        _idleChain = 0;
        _idleTimer = GetIdleWait();
        _idleLookPoint = transform.position + (_idleDirection * LookDistance);
        _enemySteering.Stop();
    }

    private bool TryEscapeSearchPoint(Vector3 currentPoint, float wallGap)
    {
        if (_enemySteering.HasWallGap(currentPoint, wallGap))
        {
            return false;
        }

        Vector3 escapePoint;

        if (TryGetEscapePoint(currentPoint, wallGap, out escapePoint) == false)
        {
            return false;
        }

        escapePoint = _enemySteering.GetSafePoint(escapePoint, wallGap);

        float reachDistance = Vector3.Distance(currentPoint, escapePoint);

        if (reachDistance <= _searchPointDistance)
        {
            return false;
        }

        if (_enemySteering.HasPointClearance(escapePoint) == false)
        {
            return false;
        }

        if (_enemySteering.HasWallGap(escapePoint, wallGap) == false)
        {
            return false;
        }

        _searchTargetPoint = escapePoint;
        _hasSearchPoint = true;
        _isSafeMove = true;

        return true;
    }

    private bool TrySearchMove(bool isMoving, Vector3 currentPoint)
    {
        if (TryMove(isMoving, currentPoint))
        {
            return true;
        }

        _hasSearchPoint = false;
        _searchStep += 1;

        return false;
    }

    private float GetIdleStopDistance()
    {
        return _idleReachDistance;
    }

    private bool TrySetIdlePoint(Vector3 currentPoint, Vector3 nextDirection, float idleDistance)
    {
        Vector3 nextPoint = currentPoint + (nextDirection * idleDistance);
        nextPoint.y = transform.position.y;

        return TrySetIdleTarget(currentPoint, nextPoint);
    }

    private bool TrySetPatrolPoint(Vector3 currentPoint, Vector3 patrolPoint)
    {
        return TrySetIdleTarget(currentPoint, GetFlatPoint(patrolPoint));
    }

    private bool TrySetIdleTarget(Vector3 currentPoint, Vector3 nextPoint)
    {
        if (TrySetIdleTarget(currentPoint, nextPoint, _idleWallGap))
        {
            return true;
        }

        if (_idleWallGap <= _probeRadius)
        {
            return false;
        }

        return TrySetIdleTarget(currentPoint, nextPoint, _probeRadius);
    }

    private bool TrySetIdleTarget(Vector3 currentPoint, Vector3 nextPoint, float wallGap)
    {
        Vector3 safePoint = _enemySteering.GetSafePoint(nextPoint, wallGap);
        float reachDistance = Vector3.Distance(currentPoint, safePoint);

        if (reachDistance < _idleReachDistance)
        {
            return false;
        }

        if (_enemySteering.HasPointClearance(safePoint) == false)
        {
            return false;
        }

        if (_enemySteering.HasWallGap(safePoint, wallGap) == false)
        {
            return false;
        }

        Vector3 nextDirection = safePoint - currentPoint;
        nextDirection.y = 0f;

        if (nextDirection.sqrMagnitude > ZeroThreshold)
        {
            nextDirection.Normalize();
            _idleDirection = nextDirection;
        }

        _idleTargetPoint = safePoint;
        _isIdleWalking = true;
        _isSearchIdle = false;
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        _enemySteering.ResetMoveStuck();

        return true;
    }

    private bool TryStartWallWalk(Vector3 currentPoint)
    {
        if (_enemySteering.HasWallGap(currentPoint, _idleWallGap))
        {
            return false;
        }

        if (TrySetIdleTarget(currentPoint, currentPoint, _idleWallGap))
        {
            return true;
        }

        if (TrySetEscapeTarget(currentPoint, _idleWallGap))
        {
            return true;
        }

        if (_idleWallGap <= _probeRadius)
        {
            return false;
        }

        if (TrySetEscapeTarget(currentPoint, _probeRadius))
        {
            return true;
        }

        return TrySetIdleTarget(currentPoint, currentPoint, _probeRadius);
    }

    private bool TryStartGuardWalk(Vector3 currentPoint)
    {
        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        if (enemyRoomLock == null)
        {
            return false;
        }

        if (enemyRoomLock.GetPatrolCount() <= 0)
        {
            return false;
        }

        Vector3 patrolPoint;
        float minDistance = Mathf.Max(_probeDistance, GetIdleDistance());
        Vector3 fallbackPoint = currentPoint;
        fallbackPoint.y = transform.position.y;

        if (_idlePatrolPicker.TryPickPoint(enemyRoomLock, currentPoint, GetPatrolForward(), fallbackPoint, transform.position.y, minDistance, IdlePointTryCount, GetRandomPatrolDirection, out patrolPoint) == false)
        {
            return false;
        }

        return TrySetPatrolPoint(currentPoint, patrolPoint);
    }

    private bool TryStartNavWalk(Vector3 currentPoint)
    {
        float minDistance = Mathf.Max(_probeDistance, _idleReachDistance * 2f);
        float maxDistance = GetIdleDistance();

        if (maxDistance < minDistance)
        {
            maxDistance = minDistance;
        }

        Vector3 navPoint;

        if (_enemySteering.TryPickNavPoint(currentPoint, GetPatrolForward(), minDistance, maxDistance, _idleWallGap, IdlePointTryCount, Next01, out navPoint))
        {
            return TrySetIdleTarget(currentPoint, navPoint, _idleWallGap);
        }

        if (_idleWallGap <= _probeRadius)
        {
            return false;
        }

        if (_enemySteering.TryPickNavPoint(currentPoint, GetPatrolForward(), minDistance, maxDistance, _probeRadius, IdlePointTryCount, Next01, out navPoint))
        {
            return TrySetIdleTarget(currentPoint, navPoint, _probeRadius);
        }

        return false;
    }

    private bool HasPatrolRoute()
    {
        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        if (enemyRoomLock == null)
        {
            return false;
        }

        if (enemyRoomLock.GetPatrolCount() <= 0)
        {
            return false;
        }

        return true;
    }

    private void TickIdleStuck(float distance, float stopDistance)
    {
        if (distance <= stopDistance)
        {
            _idleLastDistance = distance;
            _idleStuckTimer = 0f;

            return;
        }

        if (_idleLastDistance < 0f)
        {
            _idleLastDistance = distance;
            _idleStuckTimer = 0f;

            return;
        }

        if (_idleLastDistance - distance > IdleProgressMin)
        {
            _idleLastDistance = distance;
            _idleStuckTimer = 0f;

            return;
        }

        _idleStuckTimer += Time.fixedDeltaTime;
        _idleLastDistance = distance;
    }

    private bool TrySetEscapeTarget(Vector3 currentPoint, float wallGap)
    {
        Vector3 escapePoint;

        if (TryGetEscapePoint(currentPoint, wallGap, out escapePoint) == false)
        {
            return false;
        }

        return TrySetIdleTarget(currentPoint, escapePoint, wallGap);
    }

    private bool TryGetEscapePoint(Vector3 currentPoint, float wallGap, out Vector3 escapePoint)
    {
        Vector3 safePoint = _enemySteering.GetSafePoint(currentPoint, wallGap);
        Vector3 escapeDirection = safePoint - currentPoint;
        escapeDirection.y = 0f;
        float escapeDistance = escapeDirection.magnitude;

        if (escapeDistance <= ZeroThreshold)
        {
            escapePoint = Vector3.zero;

            return false;
        }

        escapeDirection /= escapeDistance;
        float minEscapeDistance = _idleReachDistance + _probeRadius;
        float targetDistance = Mathf.Max(escapeDistance, minEscapeDistance);
        escapePoint = currentPoint + (escapeDirection * targetDistance);
        escapePoint.y = transform.position.y;

        return true;
    }
}
