using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private BossExcavatorAttack SelectGuaranteedExecutableAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            BossExcavatorAttack bestAttack = BossExcavatorAttack.None;
            float bestScore = float.MinValue;
            BossExcavatorAttack candidate = BossExcavatorAttack.BucketStrike;

            while (candidate <= BossExcavatorAttack.Sweep)
            {
                if (IsAttackGuaranteed(candidate))
                {
                    if (CanExecuteAttack(candidate, targetDistance, baseAngle, cabinAngle))
                    {
                        float candidateScore = GetAttackQueueScore(candidate, targetDistance);

                        if (candidateScore > bestScore)
                        {
                            bestScore = candidateScore;
                            bestAttack = candidate;
                        }
                    }
                }

                candidate = (BossExcavatorAttack)((int)candidate + 1);
            }

            return bestAttack;
        }

        private BossExcavatorAttack SelectGuaranteedQueuedAttack(float targetDistance)
        {
            BossExcavatorAttack bestAttack = BossExcavatorAttack.None;
            float bestScore = float.MinValue;
            BossExcavatorAttack candidate = BossExcavatorAttack.BucketStrike;

            while (candidate <= BossExcavatorAttack.Sweep)
            {
                if (IsAttackGuaranteed(candidate))
                {
                    if (CanUseAttackForQueue(candidate, false))
                    {
                        float candidateScore = GetAttackQueueScore(candidate, targetDistance);

                        if (candidateScore > bestScore)
                        {
                            bestScore = candidateScore;
                            bestAttack = candidate;
                        }
                    }
                }

                candidate = (BossExcavatorAttack)((int)candidate + 1);
            }

            return bestAttack;
        }

        private bool IsAttackGuaranteed(BossExcavatorAttack attack)
        {
            return GetAttacksSinceUsed(attack) >= AttackGuaranteeThreshold;
        }

        private int GetAttacksSinceUsed(BossExcavatorAttack attack)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                return _attacksSinceBucketStrike;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                return _attacksSinceThrowScrap;
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                return _attacksSinceCharge;
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                return _attacksSinceSweep;
            }

            return 0;
        }

        private BossExcavatorAttack SelectOpportunisticAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (IsClosePriorityDistance(targetDistance))
            {
                if (CanUseBucket(targetDistance, baseAngle, cabinAngle))
                {
                    return BossExcavatorAttack.BucketStrike;
                }

                if (CanUseSweep(targetDistance, cabinAngle))
                {
                    return BossExcavatorAttack.Sweep;
                }
            }

            else
            {
                if (CanUseCharge(targetDistance, baseAngle))
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

            if (CanUseBucket(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (CanUseSweep(targetDistance, cabinAngle))
            {
                return BossExcavatorAttack.Sweep;
            }

            if (CanUseCharge(targetDistance, baseAngle))
            {
                return BossExcavatorAttack.Charge;
            }

            if (CanUseThrow(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            return BossExcavatorAttack.None;
        }

        private BossExcavatorAttack SelectBestFallbackAttack(float targetDistance, bool allowRepeat)
        {
            BossExcavatorAttack bestAttack = BossExcavatorAttack.None;
            float bestScore = float.MinValue;
            BossExcavatorAttack candidate = BossExcavatorAttack.BucketStrike;

            while (candidate <= BossExcavatorAttack.Sweep)
            {
                if (CanUseAttackForQueue(candidate, allowRepeat))
                {
                    float candidateScore = GetAttackQueueScore(candidate, targetDistance);

                    if (candidateScore > bestScore)
                    {
                        bestScore = candidateScore;
                        bestAttack = candidate;
                    }
                }

                candidate = (BossExcavatorAttack)((int)candidate + 1);
            }

            return bestAttack;
        }

        private BossExcavatorAttack ResolveQueuedAttackFallback(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (_queuedAttack != BossExcavatorAttack.BucketStrike)
            {
                return BossExcavatorAttack.None;
            }

            if (ShouldKeepBucketFallbackQueued(targetDistance) == false)
            {
                return BossExcavatorAttack.None;
            }

            if (ShouldUseBucketWhiff(baseAngle))
            {
                return BossExcavatorAttack.BucketStrike;
            }

            BossExcavatorAttack replacementAttack = SelectBucketReplacementAttack(targetDistance, baseAngle, cabinAngle);

            if (replacementAttack == BossExcavatorAttack.None)
            {
                return BossExcavatorAttack.None;
            }

            SetQueuedAttack(replacementAttack);

            return replacementAttack;
        }

        private bool ShouldUseBucketWhiff(float baseAngle)
        {
            float bucketWhiffAngle = _boss.Config.RepositionBaseAngle;

            if (baseAngle > bucketWhiffAngle)
            {
                return false;
            }

            return true;
        }

        private BossExcavatorAttack SelectBucketReplacementAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (CanUseSweep(targetDistance, cabinAngle))
            {
                return BossExcavatorAttack.Sweep;
            }

            if (CanUseCharge(targetDistance, baseAngle))
            {
                return BossExcavatorAttack.Charge;
            }

            if (CanUseThrow(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            return BossExcavatorAttack.None;
        }

        private bool CanUseAttackForQueue(BossExcavatorAttack attack, bool allowRepeat)
        {
            if (CanQueueAttack(attack) == false)
            {
                return false;
            }

            if (allowRepeat == false)
            {
                if (IsAttackBlockedByRhythm(attack))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsAttackBlockedByRhythm(BossExcavatorAttack attack)
        {
            if (attack == BossExcavatorAttack.None)
            {
                return true;
            }

            if (attack == _lastAttack)
            {
                return true;
            }

            return false;
        }

        private bool CanQueueAttack(BossExcavatorAttack attack)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                return _bucketCooldownTimer <= 0f;
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                return _sweepCooldownTimer <= 0f;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                return _throwCooldownTimer <= 0f;
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                return _chargeCooldownTimer <= 0f;
            }

            return false;
        }

        private float GetCloseQueuePressureDistance()
        {
            return _boss.Config.BucketMaxDistance + (_boss.Config.DistanceTolerance * 0.35f);
        }

        private bool CanExecuteAttack(BossExcavatorAttack attack, float targetDistance, float baseAngle, float cabinAngle)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                return CanUseBucket(targetDistance, baseAngle, cabinAngle);
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                return CanUseSweep(targetDistance, cabinAngle);
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                return CanUseThrow(targetDistance, baseAngle, cabinAngle);
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                return CanUseCharge(targetDistance, baseAngle);
            }

            return false;
        }

        private void SetQueuedAttack(BossExcavatorAttack attack)
        {
            _queuedAttack = attack;

            if (attack == BossExcavatorAttack.None)
            {
                _queuedAttackTimer = 0f;

                return;
            }

            _queuedAttackTimer = AttackQueueCommitTime / GetDecisionSpeedMult();
        }

        private void ClearQueuedAttack()
        {
            _queuedAttack = BossExcavatorAttack.None;
            _queuedAttackTimer = 0f;
        }
    }
}
