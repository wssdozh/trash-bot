using UnityEngine;

public sealed partial class EnemyMeleeBrain
{
    private void ProcessVisibleTarget(Transform currentTarget)
    {
        _isIdleWalking = false;
        _hasSearchPoint = false;
        _isSafeMove = false;
        _searchStep = 0;
        _isSearchIdle = false;

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 targetPoint = GetTargetPoint(currentTarget);
        float distance = Vector3.Distance(currentPoint, targetPoint);
        FireExecutor fireExecutor = GetFireExecutor();

        if (_isAttackWindup)
        {
            if (ProcessAttackWindup(currentTarget, targetPoint))
            {
                return;
            }
        }

        if (_isAttackInProgress)
        {
            _state = EnemyState.Fight;
            StopAttackMove();
            _enemySteering.LookToPoint(targetPoint);

            return;
        }

        if (_suicideAttack != null)
        {
            ProcessSuicideTarget(currentPoint, targetPoint, distance);

            return;
        }

        if (fireExecutor != null)
        {
            ProcessRangeTarget(currentTarget, currentPoint, targetPoint, distance, fireExecutor);

            return;
        }

        if (TryAttack(currentTarget, targetPoint))
        {
            _state = EnemyState.Fight;
            ResetMoveStuck();

            return;
        }

        ProcessCombatMove(currentTarget, currentPoint, targetPoint, distance);
    }

    private void ProcessHiddenTarget()
    {
        StopFire();

        if (_isAttackWindup)
        {
            ResetAttackState();
        }

        if (_isAttackInProgress)
        {
            _state = EnemyState.Fight;
            StopAttackMove();

            return;
        }

        if (IsSuicideActive())
        {
            ProcessSuicideFuse(GetFlatPoint(transform.position));

            return;
        }

        if (_hasLastSeenPoint == false)
        {
            _enemyMove.SetRun(false);
            ProcessIdle();

            return;
        }

        _isIdleWalking = false;
        FireExecutor fireExecutor = GetFireExecutor();

        if (fireExecutor != null)
        {
            ProcessHiddenRange(GetFlatPoint(transform.position));

            return;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);

        if (_state != EnemyState.Search)
        {
            if (TryLostPoint(currentPoint))
            {
                return;
            }
        }

        _state = EnemyState.Search;
        _enemyMove.SetRun(false);
        ProcessSearch(currentPoint);
    }

    private void ProcessSuicideTarget(Vector3 currentPoint, Vector3 targetPoint, float distance)
    {
        if (_suicideAttack.IsStartNeeded(distance))
        {
            _suicideAttack.StartFuse();
        }

        if (_suicideAttack.IsActive)
        {
            _state = EnemyState.Fight;
        }

        else
        {
            _state = EnemyState.Chase;
        }

        _enemyMove.SetRun(true);

        if (TrySuicideMove(currentPoint, targetPoint))
        {
            _suicideAttack.Tick();

            return;
        }

        _enemySteering.LookToPoint(targetPoint);
        _suicideAttack.Tick();
    }

    private void ProcessSuicideFuse(Vector3 currentPoint)
    {
        _state = EnemyState.Fight;
        _enemyMove.SetRun(true);

        if (_hasLastSeenPoint)
        {
            TrySuicideMove(currentPoint, _lastSeenPoint);
        }

        else
        {
            _enemySteering.ForceStop();
        }

        _suicideAttack.Tick();
    }

    private bool TrySuicideMove(Vector3 currentPoint, Vector3 targetPoint)
    {
        bool isMoving = _enemySteering.MoveToPoint(GetMovePoint(targetPoint), GetCombatStopDistance(), targetPoint);

        return TryMove(isMoving, currentPoint);
    }

    private void ProcessHiddenRange(Vector3 currentPoint)
    {
        _state = EnemyState.Search;
        _enemyMove.SetRun(false);
        _hasSearchPoint = false;
        _isSafeMove = false;
        _searchStep = 0;
        _isSearchIdle = true;

        Vector3 searchPoint = GetMovePoint(_lastSeenPoint);
        float distance = Vector3.Distance(currentPoint, searchPoint);

        if (distance > _searchPointDistance)
        {
            if (TryRangeMove(searchPoint, _searchPointDistance, _lastSeenPoint, currentPoint))
            {
                return;
            }
        }

        FinishSearch();
    }

    private void ProcessCombatMove(Transform currentTarget, Vector3 currentPoint, Vector3 targetPoint, float distance)
    {
        _state = EnemyState.Chase;
        _enemyMove.SetRun(IsRunNeeded(distance));

        if (TryCombatChase(currentPoint, targetPoint))
        {
            return;
        }

        if (TryCombatOrbit(currentTarget, currentPoint, distance))
        {
            _state = EnemyState.Fight;

            return;
        }

        _enemyMove.SetRun(false);
        ResetMoveStuck();
        _enemySteering.LookToPoint(targetPoint);
    }

    private void ProcessRangeTarget(Transform currentTarget, Vector3 currentPoint, Vector3 targetPoint, float distance, FireExecutor fireExecutor)
    {
        bool hasFireLine = HasFireLine(targetPoint);
        fireExecutor.SetAimPoint(targetPoint);

        if (_state != EnemyState.Chase && _state != EnemyState.Fight)
        {
            SetRangeMode(GetInitialRangeMode(distance));
        }

        TickRangeModeTimer();
        UpdateRangeMode(distance);

        if (_rangeMode == RangeChase)
        {
            ProcessRangeChase(currentPoint, targetPoint, distance);
        }

        else
        {
            if (_rangeMode == RangeBack)
            {
                ProcessRangeBack(currentTarget, currentPoint, targetPoint);
            }

            else
            {
                ProcessRangeFight(currentTarget, currentPoint, targetPoint, hasFireLine);
            }
        }

        TryShoot(fireExecutor, targetPoint, distance, hasFireLine);
    }

    private void ProcessRangeChase(Vector3 currentPoint, Vector3 targetPoint, float distance)
    {
        _state = EnemyState.Chase;
        _enemyMove.SetRun(IsRangeRunNeeded(distance));

        if (TryRangeChase(currentPoint, targetPoint))
        {
            return;
        }

        _enemyMove.SetRun(false);
        _enemySteering.LookToPoint(targetPoint);
    }

    private void ProcessRangeBack(Transform currentTarget, Vector3 currentPoint, Vector3 targetPoint)
    {
        _state = EnemyState.Fight;
        _enemyMove.SetRun(false);

        if (TryRangeRecover(currentTarget, currentPoint))
        {
            return;
        }

        _enemySteering.LookToPoint(targetPoint);
    }

    private void ProcessRangeFight(Transform currentTarget, Vector3 currentPoint, Vector3 targetPoint, bool hasFireLine)
    {
        _state = EnemyState.Fight;
        _enemyMove.SetRun(false);

        if (hasFireLine)
        {
            if (TryRangeOrbit(currentTarget, currentPoint))
            {
                return;
            }

            ResetMoveStuck();
            _enemySteering.Stop();
            _enemyRotator.RotateToPoint(targetPoint);

            return;
        }

        if (TryRangeChase(currentPoint, targetPoint))
        {
            return;
        }

        _enemySteering.LookToPoint(targetPoint);
    }

    private bool TryCombatOrbit(Transform currentTarget, Vector3 currentPoint, float distance)
    {
        if (currentTarget == null)
        {
            return false;
        }

        if (distance > GetCombatOrbitStart())
        {
            return false;
        }

        bool isMoving = _enemySteering.OrbitTarget(currentTarget, GetCombatOrbitDistance(), GetCombatOrbitGap(), _isCombatClockwise);

        return TrySteeringMove(isMoving, currentPoint, true);
    }

    private bool TryCombatChase(Vector3 currentPoint, Vector3 targetPoint)
    {
        bool isMoving = _enemySteering.ChaseTarget(targetPoint, GetCombatChaseDistance(), GetCombatStopDistance(), _chaseLookBlend);

        return TrySteeringMove(isMoving, currentPoint, false);
    }

    private bool TryRangeChase(Vector3 currentPoint, Vector3 targetPoint)
    {
        bool isMoving = _enemySteering.ChaseTarget(targetPoint, GetRangeHoldDistance(), GetRangeHoldGap(), 1f);

        return TrySteeringMove(isMoving, currentPoint, false);
    }

    private bool TryRangeRecover(Transform currentTarget, Vector3 currentPoint)
    {
        if (currentTarget == null)
        {
            return false;
        }

        bool isMoving = _enemySteering.RecoverTarget(currentTarget, _isCombatClockwise);

        return TrySteeringMove(isMoving, currentPoint, true);
    }

    private bool TryRangeOrbit(Transform currentTarget, Vector3 currentPoint)
    {
        if (currentTarget == null)
        {
            return false;
        }

        bool isMoving = _enemySteering.OrbitTarget(currentTarget, GetRangeHoldDistance(), GetRangeHoldGap(), _isCombatClockwise);

        return TrySteeringMove(isMoving, currentPoint, true);
    }

    private bool TrySteeringMove(bool isMoving, Vector3 currentPoint, bool isSideMove)
    {
        if (TryMove(isMoving, currentPoint))
        {
            return true;
        }

        if (isSideMove)
        {
            FlipCombatDirection();
        }

        return false;
    }

    private bool TryRangeMove(Vector3 movePoint, float stopDistance, Vector3 lookPoint, Vector3 currentPoint)
    {
        bool isMoving = _enemySteering.MoveToPoint(movePoint, stopDistance, lookPoint);

        return TrySteeringMove(isMoving, currentPoint, false);
    }

    private int GetInitialRangeMode(float distance)
    {
        if (distance > _fightMaxDistance)
        {
            return RangeChase;
        }

        if (distance < _fightMinDistance)
        {
            return RangeBack;
        }

        return RangeFight;
    }

    private void UpdateRangeMode(float distance)
    {
        if (_rangeModeTimer > 0f)
        {
            return;
        }

        if (_rangeMode == RangeChase)
        {
            if (distance > GetRangeEnterMax())
            {
                return;
            }

            SetRangeMode(RangeFight);

            return;
        }

        if (_rangeMode == RangeBack)
        {
            if (distance < GetRangeEnterMin())
            {
                return;
            }

            SetRangeMode(RangeFight);

            return;
        }

        if (distance > GetRangeExitMax())
        {
            SetRangeMode(RangeChase);

            return;
        }

        if (distance < GetRangeExitMin())
        {
            SetRangeMode(RangeBack);
        }
    }

    private void TryShoot(FireExecutor fireExecutor, Vector3 targetPoint, float distance, bool hasFireLine)
    {
        if (distance > _fireMaxDistance)
        {
            fireExecutor.StopFiring();

            return;
        }

        if (distance > GetRangeShootDistance())
        {
            fireExecutor.StopFiring();

            return;
        }

        if (hasFireLine == false)
        {
            fireExecutor.StopFiring();

            return;
        }

        fireExecutor.SetAimPoint(targetPoint);
        fireExecutor.StartFiring();
    }

    private bool TryAttack(Transform currentTarget, Vector3 targetPoint)
    {
        if (_isAttackWindup)
        {
            return true;
        }

        if (_isAttackInProgress)
        {
            StopAttackMove();
            _enemySteering.LookToPoint(targetPoint);

            return true;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 attackDirection = targetPoint - currentPoint;

        if (CanStartAttack(currentTarget, attackDirection) == false)
        {
            return false;
        }

        StopAttackMove();
        _enemySteering.LookToPoint(targetPoint);

        if (_attacker.CanStartAttack() == false)
        {
            return true;
        }

        _attackDirection = attackDirection;
        StartAttackWindup();

        return true;
    }

    private bool ProcessAttackWindup(Transform currentTarget, Vector3 targetPoint)
    {
        if (currentTarget == null)
        {
            ResetAttackState();

            return false;
        }

        Vector3 currentPoint = GetFlatPoint(transform.position);
        Vector3 attackDirection = targetPoint - currentPoint;

        if (CanKeepAttack(currentTarget, attackDirection) == false)
        {
            ResetAttackState();

            return false;
        }

        _state = EnemyState.Fight;
        StopAttackMove();
        _enemySteering.LookToPoint(targetPoint);
        _attackDirection = attackDirection;

        if (_attackWindupTimer > 0f)
        {
            _attackWindupTimer -= Time.fixedDeltaTime;
        }

        if (_attackWindupTimer > 0f)
        {
            return true;
        }

        CommitAttack();

        return true;
    }

    private void StartAttackWindup()
    {
        if (_attackWindupTime <= 0f)
        {
            CommitAttack();

            return;
        }

        _isAttackWindup = true;
        _attackWindupTimer = _attackWindupTime;
    }

    private void CommitAttack()
    {
        _isAttackWindup = false;
        _attackWindupTimer = 0f;
        _isAttackInProgress = true;
        _isHitPending = true;
        _enemyRotator.SnapToDirection(_attackDirection);
        _animation.TriggerAttack();
    }

    private bool IsSuicideActive()
    {
        if (_suicideAttack == null)
        {
            return false;
        }

        return _suicideAttack.IsActive;
    }

    private bool IsRunNeeded(float distance)
    {
        if (_enemyMove.IsRunRequested)
        {
            if (distance > _runStopDistance)
            {
                return true;
            }

            return false;
        }

        if (distance >= _runStartDistance)
        {
            return true;
        }

        return false;
    }

    private bool IsRangeRunNeeded(float distance)
    {
        if (_enemyMove.IsRunRequested)
        {
            if (distance > _rangeRunStop)
            {
                return true;
            }

            return false;
        }

        if (distance >= _rangeRunStart)
        {
            return true;
        }

        return false;
    }

    private bool TryLostPoint(Vector3 currentPoint)
    {
        Vector3 lostPoint = GetLostChasePoint();
        float distance = Vector3.Distance(currentPoint, lostPoint);
        float stopDistance = Mathf.Max(_lostStopDistance, _searchPointDistance);

        if (distance <= stopDistance)
        {
            return false;
        }

        _state = EnemyState.Chase;
        _enemyMove.SetRun(IsRunNeeded(distance));

        return TryMove(_enemySteering.MoveToPoint(lostPoint, stopDistance), currentPoint);
    }

    private bool CanHitTarget(Transform currentTarget, Vector3 attackDirection)
    {
        if (currentTarget == null)
        {
            return false;
        }

        return _attacker.CanHitTarget(currentTarget, attackDirection);
    }

    private bool CanStartAttack(Transform currentTarget, Vector3 attackDirection)
    {
        float maxDistance = GetAttackNearDistance(_attackStartNearScale) + GetAttackExtraDistance(_attackStartExtraScale);

        if (IsAttackNear(attackDirection, maxDistance) == false)
        {
            return false;
        }

        if (CanHitTarget(currentTarget, attackDirection))
        {
            return true;
        }

        return CanReachAttack(currentTarget, attackDirection, maxDistance);
    }

    private bool CanKeepAttack(Transform currentTarget, Vector3 attackDirection)
    {
        float maxDistance = GetAttackNearDistance(_attackKeepNearScale) + GetAttackExtraDistance(_attackKeepExtraScale);

        if (IsAttackNear(attackDirection, maxDistance) == false)
        {
            return false;
        }

        if (CanHitTarget(currentTarget, attackDirection))
        {
            return true;
        }

        return CanReachAttack(currentTarget, attackDirection, maxDistance);
    }

    private bool IsAttackNear(Vector3 attackDirection, float maxDistance)
    {
        Vector3 flatAttackDirection = attackDirection;
        flatAttackDirection.y = 0f;

        if (flatAttackDirection.sqrMagnitude <= ZeroThreshold)
        {
            return false;
        }

        float attackDistance = flatAttackDirection.magnitude;

        if (attackDistance > maxDistance)
        {
            return false;
        }

        return true;
    }

    private bool CanReachAttack(Transform currentTarget, Vector3 attackDirection, float maxDistance)
    {
        if (currentTarget == null)
        {
            return false;
        }

        Vector3 flatAttackDirection = attackDirection;
        flatAttackDirection.y = 0f;

        if (flatAttackDirection.sqrMagnitude <= ZeroThreshold)
        {
            return false;
        }

        float attackDistance = flatAttackDirection.magnitude;

        if (attackDistance > maxDistance)
        {
            return false;
        }

        flatAttackDirection /= attackDistance;
        Vector3 forwardDirection = GetStartDirection();
        float attackDot = Vector3.Dot(forwardDirection, flatAttackDirection);

        if (attackDot < _attackMinDot)
        {
            return false;
        }

        return true;
    }

    private float GetAttackExtraDistance(float extraScale)
    {
        return _attacker.AttackData.AttackRange * extraScale;
    }

    private float GetAttackNearDistance(float nearScale)
    {
        return _attacker.AttackData.AttackRange * nearScale;
    }

    private bool HasFireLine(Vector3 targetPoint)
    {
        if (_targetVision != null)
        {
            return _targetVision.IsPointVisible(targetPoint);
        }

        return _enemySteering.IsLineBlocked(targetPoint) == false;
    }

    private void OnAttackingFrame()
    {
        if (_isAttackInProgress == false)
        {
            return;
        }

        if (_isHitPending == false)
        {
            return;
        }

        StopAttackMove();
        _isHitPending = false;
        WeaponModifierContext weaponModifierContext = _animation.BuildAttackContext();
        _attacker.PerformAttack(_attackDirection, weaponModifierContext);
    }

    private void OnAttackEnded()
    {
        if (_isAttackInProgress == false)
        {
            return;
        }

        ResetAttackState();
    }
}
