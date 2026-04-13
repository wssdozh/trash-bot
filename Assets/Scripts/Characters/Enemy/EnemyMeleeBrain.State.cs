using UnityEngine;

public sealed partial class EnemyMeleeBrain
{
    private void UpdateLastSeenPoint()
    {
        Transform currentTarget = GetVisibleTarget();

        if (currentTarget == null)
        {
            return;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 targetPoint = GetTargetPoint(currentTarget);
        Vector3 direction = targetPoint - currentPoint;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
            _lastSeenDirection = direction;
        }

        _lastSeenPoint = targetPoint;
        _hasLastSeenPoint = true;
    }

    private void ResetState()
    {
        _state = EnemyState.Idle;
        _rangeMode = RangeFight;
        _rangeModeTimer = 0f;
        _idleTimer = 0f;
        _hasLastSeenPoint = false;
        _hasSearchPoint = false;
        _isSafeMove = false;
        _isIdleWalking = false;
        _isSearchIdle = false;
        ResetAttackState();
        StopFire();
        _idleLastDistance = -1f;
        _idleStuckTimer = 0f;
        ResetMoveStuck();
        _idleChain = 0;
        _searchStep = 0;
        _idlePatrolPicker.Clear();
        _lastSeenDirection = GetRandomDirection();
        _isCombatClockwise = GetRandomBool();
        _idleDirection = _lastSeenDirection;
        _idleLookPoint = transform.position + (_idleDirection * LookDistance);
        _enemyMove.SetRun(false);
        _enemyRotator.SnapToDirection(_idleDirection);
        _enemySteering.Stop();

        if (_suicideAttack != null)
        {
            _suicideAttack.ResetState();
        }

        StartIdleWalk();

        if (_isIdleWalking)
        {
            _state = EnemyState.Patrol;
        }

        else
        {
            _state = EnemyState.Watch;
        }
    }

    private void RefreshRoomLock()
    {
        EnemyRoomLock enemyRoomLock = GetEnemyRoomLock();
        _enemySteering.SetRoomLock(enemyRoomLock);
    }

    private void ResetAttackState()
    {
        _attackDirection = Vector3.zero;
        _isAttackWindup = false;
        _attackWindupTimer = 0f;
        _isAttackInProgress = false;
        _isHitPending = false;
    }

    private void StopAttackMove()
    {
        _enemyMove.SetRun(false);
        _enemyMove.ForceStop();
    }

    private void StopFire()
    {
        FireExecutor fireExecutor = GetFireExecutor();

        if (fireExecutor == null)
        {
            return;
        }

        fireExecutor.StopFiring();
        fireExecutor.ClearAimPoint();
    }

    private void FlipCombatDirection()
    {
        _isCombatClockwise = _isCombatClockwise == false;
    }

    private bool CanKeepMove(Vector3 currentPoint)
    {
        return _enemySteering.CanKeepMove(currentPoint, Time.fixedDeltaTime);
    }

    private void ResetMoveStuck()
    {
        _enemySteering.ResetMoveStuck();
    }

    private bool TryMove(bool isMoving, Vector3 currentPoint)
    {
        if (isMoving == false)
        {
            _enemySteering.ForceStop();

            return false;
        }

        if (CanKeepMove(currentPoint))
        {
            return true;
        }

        _enemySteering.ForceStop();

        return false;
    }

    private void TickRangeModeTimer()
    {
        if (_rangeModeTimer <= 0f)
        {
            return;
        }

        _rangeModeTimer -= Time.fixedDeltaTime;

        if (_rangeModeTimer < 0f)
        {
            _rangeModeTimer = 0f;
        }
    }

    private void SetRangeMode(int rangeMode)
    {
        _rangeMode = rangeMode;
        _rangeModeTimer = RangeModeHoldTime;
    }

    private void ClearSearch()
    {
        _hasLastSeenPoint = false;
        _hasSearchPoint = false;
        _isSafeMove = false;
        _searchStep = 0;
        _isSearchIdle = false;
        _idlePatrolPicker.Clear();
        ResetMoveStuck();
    }

    private void OnDied()
    {
        StopFire();
        _enemyMove.SetRun(false);
        ResetMoveStuck();
        ResetAttackState();
        _enemySteering.Stop();

        if (_suicideAttack != null)
        {
            _suicideAttack.ResetState();
        }

        enabled = false;
    }
}
