using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private void UpdateStallRuntime(BossExcavatorState moveState, float targetDistance)
        {
            if (ShouldTrackStall(moveState, targetDistance) == false)
            {
                ResetStallRuntime();

                return;
            }

            Vector3 basePoint = _boss.Base.position;
            basePoint.y = 0f;

            if (_hasStallSample == false)
            {
                _hasStallSample = true;
                _stallTimer = 0f;
                _lastStallBasePoint = basePoint;
                _lastStallTargetDistance = targetDistance;

                return;
            }

            float movedDistance = Vector3.Distance(basePoint, _lastStallBasePoint);
            float targetDistanceDelta = Mathf.Abs(targetDistance - _lastStallTargetDistance);
            bool hasProgress = _boss.Move.CurrentPlanarSpeed > StallSpeedThreshold;

            if (hasProgress == false)
            {
                if (movedDistance > StallMoveDistanceThreshold)
                {
                    hasProgress = true;
                }
            }

            if (hasProgress == false)
            {
                if (targetDistanceDelta > StallTargetDistanceThreshold)
                {
                    hasProgress = true;
                }
            }

            if (hasProgress)
            {
                _stallTimer = 0f;
            }

            else
            {
                _stallTimer += Time.deltaTime;
            }

            _lastStallBasePoint = basePoint;
            _lastStallTargetDistance = targetDistance;
        }

        private bool ShouldTrackStall(BossExcavatorState moveState, float targetDistance)
        {
            if (moveState != BossExcavatorState.Chase)
            {
                if (moveState != BossExcavatorState.Reposition)
                {
                    return false;
                }
            }

            if (_boss.Move.TargetPoint == BossExcavatorTargetPoint.ArenaCenter)
            {
                return false;
            }

            if (_currentAttack != BossExcavatorAttack.None)
            {
                return false;
            }

            if (_queuedAttack != BossExcavatorAttack.None)
            {
                return true;
            }

            if (targetDistance <= _boss.Move.MinMoveDistance)
            {
                return false;
            }

            return true;
        }

        private bool ShouldBreakStall()
        {
            return _stallTimer >= GetStallRecoverTime();
        }

        private BossExcavatorAttack SelectAggressiveStallAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (IsClosePriorityDistance(targetDistance))
            {
                if (CanUseAggressiveBucket(targetDistance, baseAngle, cabinAngle))
                {
                    return BossExcavatorAttack.BucketStrike;
                }

                if (CanUseAggressiveSweep(targetDistance, cabinAngle))
                {
                    return BossExcavatorAttack.Sweep;
                }
            }

            else
            {
                if (CanUseAggressiveCharge(targetDistance, baseAngle))
                {
                    return BossExcavatorAttack.Charge;
                }

                if (CanUsePhaseTwoRangedBucket(targetDistance, baseAngle, cabinAngle))
                {
                    return BossExcavatorAttack.BucketStrike;
                }

                if (CanUseThrow(targetDistance, baseAngle, cabinAngle))
                {
                    return BossExcavatorAttack.ThrowScrap;
                }
            }

            if (CanUseAggressiveBucket(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (CanUseAggressiveSweep(targetDistance, cabinAngle))
            {
                return BossExcavatorAttack.Sweep;
            }

            if (CanUseAggressiveCharge(targetDistance, baseAngle))
            {
                return BossExcavatorAttack.Charge;
            }

            if (CanUseThrow(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            return BossExcavatorAttack.None;
        }

        private bool CanUseAggressiveBucket(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (CanQueueAttack(BossExcavatorAttack.BucketStrike) == false)
            {
                return false;
            }

            if (targetDistance > GetBucketAttackStartDistance() + _boss.Config.DistanceTolerance)
            {
                return false;
            }

            float aggressiveBaseAngle = Mathf.Max(_boss.Config.BucketBaseAngle, _boss.Config.RepositionBaseAngle);
            float aggressiveCabinAngle = Mathf.Max(_boss.Config.BucketCabinAngle, _boss.Config.SweepCabinAngle);

            if (baseAngle > aggressiveBaseAngle)
            {
                return false;
            }

            if (cabinAngle > aggressiveCabinAngle)
            {
                return false;
            }

            return true;
        }

        private bool CanUseAggressiveSweep(float targetDistance, float cabinAngle)
        {
            if (CanQueueAttack(BossExcavatorAttack.Sweep) == false)
            {
                return false;
            }

            if (targetDistance > _boss.Config.SweepMaxDistance + _boss.Config.DistanceTolerance)
            {
                return false;
            }

            float aggressiveCabinAngle = Mathf.Max(_boss.Config.SweepCabinAngle, _boss.Config.RepositionBaseAngle + 10f);

            if (cabinAngle > aggressiveCabinAngle)
            {
                return false;
            }

            return true;
        }

        private bool CanUseAggressiveCharge(float targetDistance, float baseAngle)
        {
            if (CanQueueAttack(BossExcavatorAttack.Charge) == false)
            {
                return false;
            }

            if (targetDistance < _boss.Config.ChargeMinDistance)
            {
                return false;
            }

            if (targetDistance > _boss.Config.ChargeMaxDistance)
            {
                return false;
            }

            float aggressiveChargeAngle = Mathf.Max(GetChargeStartAngle(), _boss.Config.RepositionBaseAngle);

            if (GetChargeApproachAngle(baseAngle) > aggressiveChargeAngle)
            {
                return false;
            }

            return true;
        }

        private void ForceStallRecovery()
        {
            ClearQueuedAttack();
            _boss.Move.InvalidatePath();
            _forcedChaseTimer = 0f;
            SetMoveState(BossExcavatorState.Chase);
            ResetStallRuntime();
        }
    }
}
