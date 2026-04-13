using UnityEngine;

public sealed partial class EnemyMeleeBrain
{
    private float GetIdleDistance()
    {
        float idleDistance = GetRandomRange(_idleMoveMin, _idleMoveMax);
        float strideScale = GetRandomRange(IdleStrideMinScale, IdleStrideMaxScale);

        return idleDistance * strideScale;
    }

    private int GetIdleChainCount()
    {
        return Mathf.FloorToInt(GetRandomRange(IdleChainMin, IdleChainMax + 1f));
    }

    private float GetIdleWait()
    {
        return GetRandomRange(_idleWaitMin, _idleWaitMax) * _idleWaitScale;
    }

    private int GetRandomPatrolDirection()
    {
        if (GetRandomBool())
        {
            return 1;
        }

        return -1;
    }

    private Vector3 GetPatrolForward()
    {
        if (_idleDirection.sqrMagnitude > ZeroThreshold)
        {
            return _idleDirection;
        }

        return GetStartDirection();
    }

    private Vector3 GetNextIdleDirection()
    {
        float turnDegrees = GetIdleTurn();
        Quaternion rotation = Quaternion.Euler(0f, turnDegrees, 0f);
        Vector3 nextDirection = rotation * _idleDirection;
        nextDirection.y = 0f;

        if (nextDirection.sqrMagnitude <= 0.0001f)
        {
            nextDirection = Vector3.forward;
        }

        nextDirection.Normalize();
        _idleDirection = nextDirection;

        return nextDirection;
    }

    private Vector3 GetIdleLookDirection()
    {
        float turnDegrees = GetIdleLookTurn();
        Vector3 lookDirection = RotateDirection(_idleDirection, turnDegrees);

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return GetStartDirection();
        }

        return lookDirection;
    }

    private float GetIdleTurn()
    {
        float minTurn = _idleTurnMin;
        float maxTurn = _idleTurnMax;

        if (Next01() >= IdleWideTurnChance)
        {
            minTurn *= IdleSoftTurnScale;
            maxTurn *= IdleSoftTurnScale;
        }

        return GetRandomTurn(minTurn, maxTurn);
    }

    private float GetIdleLookTurn()
    {
        if (Next01() < IdleFrontChance)
        {
            return 0f;
        }

        return GetRandomTurn(0f, _idleLookAngle);
    }

    private Vector3 GetSearchPoint()
    {
        Vector3 baseDirection = RotateDirection(GetSearchDirection(), GetSearchAngle());
        Vector3 startPoint = GetSearchStartPoint();
        float searchDistance = GetSearchDistance();

        return startPoint + (baseDirection * searchDistance);
    }

    private Vector3 GetLostChasePoint()
    {
        Vector3 startPoint = GetSearchStartPoint();
        Vector3 chaseDirection = GetSearchDirection();
        Vector3 chasePoint = startPoint + (chaseDirection * _lostChaseDistance);

        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        if (enemyRoomLock != null)
        {
            chasePoint = enemyRoomLock.ClampMovePoint(chasePoint);
        }

        return GetMovePoint(GetFlatPoint(chasePoint));
    }

    private float GetCombatStopDistance()
    {
        return Mathf.Max(MinFightRadius, _fightGapDistance * 0.5f);
    }

    private float GetCombatOrbitDistance()
    {
        return Mathf.Max(GetCombatStopDistance(), _probeRadius * 2f);
    }

    private float GetCombatOrbitGap()
    {
        return Mathf.Max(_fightGapDistance, GetCombatStopDistance());
    }

    private float GetCombatOrbitStart()
    {
        return Mathf.Max(_slotRadius + _fightGapDistance, _probeDistance * 2f);
    }

    private float GetMoveWallGap()
    {
        return Mathf.Max(_idleWallGap, _probeRadius * 2f);
    }

    private Vector3 GetMovePoint(Vector3 point)
    {
        return _enemySteering.GetSafePoint(point, GetMoveWallGap());
    }

    private Vector3 GetSearchStartPoint()
    {
        return GetMovePoint(_lastSeenPoint);
    }

    private float GetSearchAngle()
    {
        if (_searchStep == 1)
        {
            return SearchSideAngle;
        }

        if (_searchStep == 2)
        {
            return -SearchSideAngle;
        }

        if (_searchStep >= SearchStepsCount - 1)
        {
            return 180f;
        }

        return 0f;
    }

    private float GetSearchDistance()
    {
        float searchStride = GetSearchStride();

        if (_searchStep >= SearchStepsCount - 1)
        {
            return searchStride * SearchBackScale;
        }

        return searchStride;
    }

    private Transform GetVisibleTarget()
    {
        if (_targetVision.IsTargetVisible == false)
        {
            return null;
        }

        Transform currentTarget = _targetVision.CurrentTarget;

        if (currentTarget == null)
        {
            return null;
        }

        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();

        if (enemyRoomLock == null)
        {
            return currentTarget;
        }

        Vector3 targetPoint = GetTargetPoint(currentTarget);

        if (enemyRoomLock.ContainsRoomPoint(currentTarget.position) == false && enemyRoomLock.ContainsRoomPoint(targetPoint) == false)
        {
            return null;
        }

        return currentTarget;
    }

    private Vector3 GetTargetPoint(Transform currentTarget)
    {
        if (currentTarget == null)
        {
            return GetFlatPoint(transform.position);
        }

        if (_targetVision != null)
        {
            if (currentTarget == _targetVision.CurrentTarget)
            {
                return GetFlatPoint(_targetVision.CurrentTargetPoint);
            }
        }

        return GetFlatPoint(currentTarget.position);
    }

    private EnemyRoomLock GetEnemyRoomLock()
    {
        if (_enemyRoomLock != null)
        {
            return _enemyRoomLock;
        }

        _enemyRoomLock = GetComponent<EnemyRoomLock>();

        return _enemyRoomLock;
    }

    private FireExecutor GetFireExecutor()
    {
        return _weaponHolder.FireExecutor;
    }

    private void RotateTarget(Transform currentTarget)
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 targetPoint = GetTargetPoint(currentTarget);
        _enemyRotator.RotateToPoint(targetPoint);
    }

    private float GetSearchStride()
    {
        return Mathf.Max(_lostChaseDistance, _probeDistance);
    }

    private float GetRandomRange(float minValue, float maxValue)
    {
        if (maxValue - minValue <= 0.0001f)
        {
            return minValue;
        }

        return Mathf.Lerp(minValue, maxValue, Next01());
    }

    private float GetRandomTurn(float minAngle, float maxAngle)
    {
        float angle = GetRandomRange(minAngle, maxAngle);

        if (GetRandomBool())
        {
            return angle;
        }

        return -angle;
    }

    private bool GetRandomBool()
    {
        return Next01() >= 0.5f;
    }

    private float Next01()
    {
        if (_random == null)
        {
            return 0.5f;
        }

        return (float)_random.NextDouble();
    }

    private int GetSeed()
    {
        int seed = gameObject.GetInstanceID();

        if (seed == int.MinValue)
        {
            return int.MaxValue;
        }

        if (seed < 0)
        {
            seed = -seed;
        }

        return seed;
    }

    private Vector3 GetRandomDirection()
    {
        float turnDegrees = GetRandomRange(0f, 360f);
        Quaternion rotation = Quaternion.Euler(0f, turnDegrees, 0f);
        Vector3 direction = rotation * Vector3.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        direction.Normalize();

        return direction;
    }

    private float GetRangeHoldDistance()
    {
        float holdDistance = _fightMinDistance + (_fightGapDistance * 0.5f) - _rangeNearExtra;

        return Mathf.Max(MinFightRadius, holdDistance);
    }

    private float GetRangeEnterMax()
    {
        float rangeEnterMax = GetRangeHoldDistance() + GetRangeHoldGap();

        return Mathf.Min(_fightMaxDistance, rangeEnterMax);
    }

    private float GetRangeEnterMin()
    {
        float rangeEnterMin = GetRangeHoldDistance() - GetRangeHoldGap();

        return Mathf.Max(MinFightRadius, rangeEnterMin);
    }

    private float GetRangeExitMax()
    {
        float rangeExitMax = GetRangeEnterMax() + _fightGapDistance;

        return Mathf.Min(_fireMaxDistance, rangeExitMax);
    }

    private float GetRangeExitMin()
    {
        float rangeExitMin = GetRangeEnterMin() - _fightGapDistance;

        return Mathf.Max(MinFightRadius, rangeExitMin);
    }

    private float GetRangeShootDistance()
    {
        return Mathf.Min(_fireMaxDistance, _fightMaxDistance);
    }

    private float GetRangeHoldGap()
    {
        float holdGap = (_fightMaxDistance - _fightMinDistance) * RangeHoldScale;

        if (holdGap < _fightGapDistance)
        {
            holdGap = _fightGapDistance;
        }

        return holdGap;
    }

    private Vector3 GetSearchDirection()
    {
        if (_lastSeenDirection.sqrMagnitude <= 0.0001f)
        {
            return GetStartDirection();
        }

        return _lastSeenDirection;
    }

    private Vector3 GetStartDirection()
    {
        if (_enemyRotator != null)
        {
            return _enemyRotator.ForwardDirection;
        }

        Vector3 direction = transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        return direction;
    }

    private Vector3 GetFlatPoint(Vector3 point)
    {
        point.y = transform.position.y;

        return point;
    }

    private Vector3 RotateDirection(Vector3 direction, float angle)
    {
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        Vector3 rotatedDirection = rotation * direction;
        rotatedDirection.y = 0f;

        if (rotatedDirection.sqrMagnitude <= 0.0001f)
        {
            return direction;
        }

        rotatedDirection.Normalize();

        return rotatedDirection;
    }
}
