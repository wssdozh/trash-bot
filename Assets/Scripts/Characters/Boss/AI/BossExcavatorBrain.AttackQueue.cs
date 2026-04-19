using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private BossExcavatorAttack SelectAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            _pendingAttack = BossExcavatorAttack.None;

            UpdateQueuedAttack(targetDistance);

            BossExcavatorAttack guaranteedAttack = SelectGuaranteedExecutableAttack(targetDistance, baseAngle, cabinAngle);

            if (guaranteedAttack != BossExcavatorAttack.None)
            {
                SetQueuedAttack(guaranteedAttack);
                _pendingAttack = guaranteedAttack;

                return _pendingAttack;
            }

            if (CanExecuteAttack(_queuedAttack, targetDistance, baseAngle, cabinAngle))
            {
                _pendingAttack = _queuedAttack;

                return _pendingAttack;
            }

            BossExcavatorAttack queuedFallbackAttack = ResolveQueuedAttackFallback(targetDistance, baseAngle, cabinAngle);

            if (queuedFallbackAttack != BossExcavatorAttack.None)
            {
                _pendingAttack = queuedFallbackAttack;

                return _pendingAttack;
            }

            if (ShouldReserveWindowForScrapTrail(targetDistance))
            {
                return BossExcavatorAttack.None;
            }

            BossExcavatorAttack opportunisticAttack = SelectOpportunisticAttack(targetDistance, baseAngle, cabinAngle);

            if (opportunisticAttack != BossExcavatorAttack.None)
            {
                SetQueuedAttack(opportunisticAttack);
                _pendingAttack = opportunisticAttack;

                return _pendingAttack;
            }

            return BossExcavatorAttack.None;
        }

        private void UpdateQueuedAttack(float targetDistance)
        {
            if (ShouldKeepQueuedAttack(targetDistance))
            {
                return;
            }

            BossExcavatorAttack queuedAttack = SelectQueuedAttack(targetDistance);
            SetQueuedAttack(queuedAttack);
        }

        private bool ShouldKeepQueuedAttack(float targetDistance)
        {
            if (_queuedAttack == BossExcavatorAttack.None)
            {
                return false;
            }

            if (_queuedAttackTimer <= 0f)
            {
                return ShouldHoldExpiredQueuedAttack(targetDistance);
            }

            if (CanQueueAttack(_queuedAttack) == false)
            {
                return false;
            }

            if (ShouldDropQueuedAttackForClosePressure(targetDistance))
            {
                return false;
            }

            return true;
        }

        private bool ShouldDropQueuedAttackForClosePressure(float targetDistance)
        {
            if (_queuedAttack == BossExcavatorAttack.BucketStrike || _queuedAttack == BossExcavatorAttack.Sweep)
            {
                return false;
            }

            float closePressureDistance = GetCloseQueuePressureDistance();

            if (targetDistance <= closePressureDistance)
            {
                return true;
            }

            return false;
        }

        private bool ShouldHoldExpiredQueuedAttack(float targetDistance)
        {
            if (_queuedAttack == BossExcavatorAttack.BucketStrike)
            {
                return ShouldKeepBucketFallbackQueued(targetDistance);
            }

            return false;
        }

        private bool ShouldKeepBucketFallbackQueued(float targetDistance)
        {
            if (targetDistance > GetBucketAttackStartDistance())
            {
                return false;
            }

            return true;
        }

        private BossExcavatorAttack SelectQueuedAttack(float targetDistance)
        {
            BossExcavatorAttack guaranteedAttack = SelectGuaranteedQueuedAttack(targetDistance);

            if (guaranteedAttack != BossExcavatorAttack.None)
            {
                return guaranteedAttack;
            }

            BossExcavatorAttack preferredAttack = GetPreferredRhythmAttack(targetDistance);
            BossExcavatorAttack secondaryAttack = GetSecondaryRhythmAttack(preferredAttack, targetDistance);
            BossExcavatorAttack queuedAttack = SelectRhythmAttackCandidate(preferredAttack, secondaryAttack, targetDistance, false);

            if (queuedAttack != BossExcavatorAttack.None)
            {
                return queuedAttack;
            }

            queuedAttack = SelectBestFallbackAttack(targetDistance, false);

            if (queuedAttack != BossExcavatorAttack.None)
            {
                return queuedAttack;
            }

            return SelectBestFallbackAttack(targetDistance, true);
        }

        private BossExcavatorAttack GetPreferredRhythmAttack(float targetDistance)
        {
            if (IsClosePriorityDistance(targetDistance))
            {
                return GetClosePriorityAttack();
            }

            return GetFarPriorityAttack();
        }

        private BossExcavatorAttack GetSecondaryRhythmAttack(BossExcavatorAttack preferredAttack, float targetDistance)
        {
            BossExcavatorAttack pairedAttack = GetPairedPriorityAttack(preferredAttack);

            if (pairedAttack != BossExcavatorAttack.None)
            {
                return pairedAttack;
            }

            if (IsClosePriorityDistance(targetDistance))
            {
                return BossExcavatorAttack.Sweep;
            }

            return BossExcavatorAttack.ThrowScrap;
        }

        private BossExcavatorAttack SelectRhythmAttackCandidate(
            BossExcavatorAttack preferredAttack,
            BossExcavatorAttack secondaryAttack,
            float targetDistance,
            bool allowRepeat)
        {
            bool canUsePreferred = CanUseAttackForQueue(preferredAttack, allowRepeat);
            bool canUseSecondary = CanUseAttackForQueue(secondaryAttack, allowRepeat);

            if (canUsePreferred == false && canUseSecondary == false)
            {
                return BossExcavatorAttack.None;
            }

            if (canUsePreferred && canUseSecondary)
            {
                float preferredScore = GetAttackQueueScore(preferredAttack, targetDistance);
                float secondaryScore = GetAttackQueueScore(secondaryAttack, targetDistance);

                if (preferredScore >= secondaryScore)
                {
                    return preferredAttack;
                }

                return secondaryAttack;
            }

            if (canUsePreferred)
            {
                return preferredAttack;
            }

            return secondaryAttack;
        }

        private float GetAttackRangeScore(BossExcavatorAttack attack, float targetDistance)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                return GetDistanceScore(targetDistance, _boss.Config.BucketMaxDistance * 0.78f, 0.95f);
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                return GetDistanceScore(targetDistance, _boss.Config.SweepMaxDistance * 0.7f, 1.3f);
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                float idealDistance = (_boss.Config.ThrowMinDistance + _boss.Config.ThrowMaxDistance) * 0.5f;

                return GetDistanceScore(targetDistance, idealDistance, 0.72f);
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                float idealDistance = Mathf.Lerp(_boss.Config.ChargeMinDistance, _boss.Config.ChargeMaxDistance, 0.58f);

                return GetDistanceScore(targetDistance, idealDistance, 0.55f);
            }

            return 0f;
        }

        private float GetDistanceScore(float targetDistance, float idealDistance, float distanceWeight)
        {
            float distanceGap = Mathf.Abs(targetDistance - idealDistance);
            float score = 3.8f - distanceGap * distanceWeight;

            return Mathf.Clamp(score, -3.5f, 3.8f);
        }

        private float GetAttackQueueScore(BossExcavatorAttack attack, float targetDistance)
        {
            float score = GetAttackRangeScore(attack, targetDistance);
            bool isClosePriorityDistance = IsClosePriorityDistance(targetDistance);
            int attacksSinceUsed = GetAttacksSinceUsed(attack);

            if (attack == _previousAttack)
            {
                score -= PreviousAttackRepeatPenalty;
            }

            score += attacksSinceUsed * AttackUsageScoreBonusStep;

            if (IsAttackGuaranteed(attack))
            {
                score += AttackGuaranteeScoreBonus;
            }

            if (_boss.IsAdvancedPhase)
            {
                if (attack == BossExcavatorAttack.Charge)
                {
                    score -= 0.6f;
                }

                if (attack == BossExcavatorAttack.Sweep)
                {
                    score += 0.3f;
                }
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                if (targetDistance >= _boss.Config.ChargeMinDistance)
                {
                    if (targetDistance <= _boss.Config.ChargeMaxDistance)
                    {
                        score += 0.85f;
                    }
                }
            }

            if (isClosePriorityDistance)
            {
                if (attack == BossExcavatorAttack.BucketStrike)
                {
                    score += 3.25f;
                }

                if (attack == BossExcavatorAttack.Sweep)
                {
                    score += 2.8f;
                }

                if (attack == BossExcavatorAttack.ThrowScrap)
                {
                    score -= 4.2f;
                }

                if (attack == BossExcavatorAttack.Charge)
                {
                    score -= 5f;
                }
            }

            else
            {
                if (attack == BossExcavatorAttack.Charge)
                {
                    score += 3.15f;
                }

                if (attack == BossExcavatorAttack.ThrowScrap)
                {
                    score += 2.45f;
                }

                if (attack == BossExcavatorAttack.BucketStrike)
                {
                    score -= 3.2f;
                }

                if (attack == BossExcavatorAttack.Sweep)
                {
                    score -= 3.8f;
                }
            }

            return score;
        }
    }
}
