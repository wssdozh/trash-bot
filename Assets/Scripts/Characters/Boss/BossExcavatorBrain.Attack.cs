using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private BossExcavatorAttack SelectAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            _pendingAttack = BossExcavatorAttack.None;

            UpdateQueuedAttack(targetDistance);

            if (CanExecuteAttack(_queuedAttack, targetDistance, baseAngle, cabinAngle))
            {
                _pendingAttack = _queuedAttack;

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
                return false;
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

        private BossExcavatorAttack SelectQueuedAttack(float targetDistance)
        {
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
            if (_lastAttack == BossExcavatorAttack.ThrowScrap)
            {
                return BossExcavatorAttack.Sweep;
            }

            if (_lastAttack == BossExcavatorAttack.Sweep)
            {
                return BossExcavatorAttack.Charge;
            }

            if (_lastAttack == BossExcavatorAttack.Charge)
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (_lastAttack == BossExcavatorAttack.BucketStrike)
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            if (targetDistance <= GetCloseQueuePressureDistance())
            {
                return BossExcavatorAttack.BucketStrike;
            }

            return BossExcavatorAttack.ThrowScrap;
        }

        private BossExcavatorAttack GetSecondaryRhythmAttack(BossExcavatorAttack preferredAttack, float targetDistance)
        {
            if (preferredAttack == BossExcavatorAttack.ThrowScrap)
            {
                return BossExcavatorAttack.Charge;
            }

            if (preferredAttack == BossExcavatorAttack.Charge)
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            if (preferredAttack == BossExcavatorAttack.BucketStrike)
            {
                return BossExcavatorAttack.Sweep;
            }

            if (preferredAttack == BossExcavatorAttack.Sweep)
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (targetDistance <= GetCloseQueuePressureDistance())
            {
                return BossExcavatorAttack.Sweep;
            }

            return BossExcavatorAttack.Charge;
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

            if (attack == _previousAttack)
            {
                score -= PreviousAttackRepeatPenalty;
            }

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                if (attack == BossExcavatorAttack.Charge)
                {
                    score += 0.45f;
                }

                if (attack == BossExcavatorAttack.Sweep)
                {
                    score += 0.3f;
                }
            }

            float closePressureDistance = GetCloseQueuePressureDistance();

            if (targetDistance <= closePressureDistance)
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

            return score;
        }

        private BossExcavatorAttack SelectOpportunisticAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (CanUseBucket(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (CanUseSweep(targetDistance, cabinAngle))
            {
                return BossExcavatorAttack.Sweep;
            }

            if (_queuedAttack != BossExcavatorAttack.None)
            {
                return BossExcavatorAttack.None;
            }

            if (CanUseThrow(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            if (CanUseCharge(targetDistance, baseAngle))
            {
                return BossExcavatorAttack.Charge;
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

            _queuedAttackTimer = AttackQueueCommitTime;
        }

        private void ClearQueuedAttack()
        {
            _queuedAttack = BossExcavatorAttack.None;
            _queuedAttackTimer = 0f;
        }

        private bool CanUseSweep(float targetDistance, float cabinAngle)
        {
            if (_postAttackTimer > 0f)
            {
                return false;
            }

            if (_sweepCooldownTimer > 0f)
            {
                return false;
            }

            if (targetDistance > _boss.Config.SweepMaxDistance)
            {
                return false;
            }

            if (cabinAngle > _boss.Config.SweepCabinAngle)
            {
                return false;
            }

            return true;
        }

        private bool CanUseBucket(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (_postAttackTimer > 0f)
            {
                return false;
            }

            if (_bucketCooldownTimer > 0f)
            {
                return false;
            }

            if (targetDistance > GetBucketAttackStartDistance())
            {
                return false;
            }

            if (baseAngle > _boss.Config.BucketBaseAngle)
            {
                return false;
            }

            if (cabinAngle > _boss.Config.BucketCabinAngle)
            {
                return false;
            }

            return true;
        }

        private float GetBucketAttackStartDistance()
        {
            float bucketAttackDistance = _boss.Config.BucketMaxDistance * 0.82f;
            float minBucketAttackDistance = _boss.Config.StopDistance + 1.35f;

            return Mathf.Max(minBucketAttackDistance, bucketAttackDistance);
        }

        private bool CanUseThrow(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (_postAttackTimer > 0f)
            {
                return false;
            }

            if (_throwCooldownTimer > 0f)
            {
                return false;
            }

            if (targetDistance < _boss.Config.ThrowMinDistance)
            {
                return false;
            }

            if (targetDistance > _boss.Config.ThrowMaxDistance)
            {
                return false;
            }

            if (baseAngle > _boss.Config.ThrowBaseAngle)
            {
                return false;
            }

            if (cabinAngle > _boss.Config.ThrowCabinAngle)
            {
                return false;
            }

            return true;
        }

        private bool CanUseCharge(float targetDistance, float baseAngle)
        {
            if (_postAttackTimer > 0f)
            {
                return false;
            }

            if (_chargeCooldownTimer > 0f)
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

            if (baseAngle > _boss.Config.ChargeBaseAngle)
            {
                return false;
            }

            return true;
        }

        private void StartAttack(BossExcavatorAttack attack)
        {
            CancelScrapTrail(false);
            _currentAttack = attack;
            ClearQueuedAttack();
            _boss.SetMoveAttackIntent(attack);
            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);

            if (attack == BossExcavatorAttack.BucketStrike)
            {
                _bucketAttack.StartAttack();

                return;
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                _sweepAttack.StartAttack();

                return;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                _throwAttack.StartAttack();

                return;
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                _chargeAttack.StartAttack(ShouldUseChargeSweepCombo());
            }
        }

        private bool UpdateAttackRuntime()
        {
            if (_currentAttack == BossExcavatorAttack.BucketStrike)
            {
                if (_bucketAttack.Tick())
                {
                    return true;
                }

                FinishAttack();

                return false;
            }

            if (_currentAttack == BossExcavatorAttack.Sweep)
            {
                if (_sweepAttack.Tick())
                {
                    return true;
                }

                FinishAttack();

                return false;
            }

            if (_currentAttack == BossExcavatorAttack.ThrowScrap)
            {
                if (_throwAttack.Tick())
                {
                    return true;
                }

                FinishAttack();

                return false;
            }

            if (_currentAttack == BossExcavatorAttack.Charge)
            {
                if (_chargeAttack.Tick())
                {
                    return true;
                }

                FinishAttack();

                return false;
            }

            return false;
        }

        private void FinishAttack()
        {
            BossExcavatorAttack completedAttack = _currentAttack;

            if (_currentAttack == BossExcavatorAttack.Sweep)
            {
                _sweepCooldownTimer = GetCooldownValue(_boss.Config.SweepAttackCooldown);
                _postAttackTimer = GetShortPostAttackDelay();
                _forcedChaseTimer = GetClosePressureTime();
            }

            if (_currentAttack == BossExcavatorAttack.BucketStrike)
            {
                _bucketCooldownTimer = GetCooldownValue(_boss.Config.BucketAttackCooldown);
                _postAttackTimer = GetShortPostAttackDelay();
                _forcedChaseTimer = GetClosePressureTime();
            }

            if (_currentAttack == BossExcavatorAttack.ThrowScrap)
            {
                _throwCooldownTimer = GetCooldownValue(_boss.Config.ThrowAttackCooldown);
                _postAttackTimer = GetShortPostAttackDelay();
                _forcedChaseTimer = _boss.Config.MovePressureTime;
                TryPrimeScrapTrailPressure();
            }

            if (_currentAttack == BossExcavatorAttack.Charge)
            {
                _chargeCooldownTimer = GetCooldownValue(_boss.Config.ChargeAttackCooldown);
                _postAttackTimer = 0f;
                _forcedChaseTimer = GetClosePressureTime();
                TryPrimeScrapTrailPressure();
            }

            ClearAttackRuntime(true);
            RegisterCompletedAttack(completedAttack);

            if (CanUseCombatData())
            {
                UpdateQueuedAttack(GetTargetDistance());
            }
        }

        private void ClearAttackRuntime(bool restorePose)
        {
            if (_currentAttack == BossExcavatorAttack.None)
            {
                return;
            }

            _currentAttack = BossExcavatorAttack.None;
            _pendingAttack = BossExcavatorAttack.None;
            _boss.SetMoveAttackIntent(BossExcavatorAttack.None);
            _sweepAttack.Cancel(restorePose);
            _bucketAttack.Cancel(restorePose);
            _throwAttack.Cancel(restorePose);
            _chargeAttack.Cancel(restorePose);
            ApplyCombatDefaults();

            if (restorePose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, _boss.Config.AttackPoseSpeedMult);
            }
        }

        private void UpdateScrapTrailRuntime(BossExcavatorState nextState)
        {
            if (nextState != BossExcavatorState.Chase && nextState != BossExcavatorState.Reposition)
            {
                CancelScrapTrail(true);

                return;
            }

            if (_scrapTrailAttack.IsRunning)
            {
                if (_scrapTrailAttack.Tick())
                {
                    return;
                }

                _scrapTrailCooldownTimer = GetCooldownValue(_boss.Config.ScrapTrailCooldown);

                return;
            }

            if (CanUseScrapTrail())
            {
                _scrapTrailAttack.StartAttack();
            }
        }

        private void CancelScrapTrail(bool restorePose)
        {
            if (_scrapTrailAttack.IsRunning == false)
            {
                return;
            }

            _scrapTrailAttack.Cancel(restorePose);
        }

        private bool CanUseScrapTrail()
        {
            if (_postAttackTimer > 0f)
            {
                if (_boss.Phase != BossExcavatorPhase.PhaseTwo)
                {
                    return false;
                }
            }

            if (_scrapTrailCooldownTimer > 0f)
            {
                return false;
            }

            float targetDistance = GetTargetDistance();
            float minDistance = _boss.Config.ScrapTrailMinDistance;
            float maxDistance = _boss.Config.ScrapTrailMaxDistance;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                minDistance = Mathf.Max(_boss.Move.MinMoveDistance, minDistance - 1.35f);
                maxDistance += 3.4f;
            }

            if (targetDistance < minDistance)
            {
                return false;
            }

            if (targetDistance > maxDistance)
            {
                return false;
            }

            float baseAngle = GetTargetAngle(_boss.Base);
            float requiredBaseAngle = _boss.Config.ScrapTrailBaseAngle;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                requiredBaseAngle += 34f;
            }

            if (baseAngle > requiredBaseAngle)
            {
                return false;
            }

            float requiredMoveSpeed = _boss.Config.ScrapTrailMinMoveSpeed;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                requiredMoveSpeed *= 0.3f;
            }

            if (_boss.Move.CurrentPlanarSpeed < requiredMoveSpeed)
            {
                return false;
            }

            return true;
        }

        private bool ShouldReserveWindowForScrapTrail(float targetDistance)
        {
            if (_boss.Phase != BossExcavatorPhase.PhaseTwo)
            {
                return false;
            }

            if (_scrapTrailAttack.IsRunning)
            {
                return ShouldHoldScrapTrailPressure(targetDistance);
            }

            if (CanUseScrapTrail() == false)
            {
                return false;
            }

            return ShouldHoldScrapTrailPressure(targetDistance);
        }

        private bool ShouldHoldScrapTrailPressure(float targetDistance)
        {
            float closeMeleeDistance = _boss.Config.BucketMaxDistance * 0.92f;

            if (targetDistance <= closeMeleeDistance)
            {
                return false;
            }

            return true;
        }

        private void TryPrimeScrapTrailPressure()
        {
            if (_boss.Phase != BossExcavatorPhase.PhaseTwo)
            {
                return;
            }

            _scrapTrailCooldownTimer = 0f;
        }

        private void RegisterCompletedAttack(BossExcavatorAttack attack)
        {
            if (attack == BossExcavatorAttack.None)
            {
                return;
            }

            _previousAttack = _lastAttack;
            _lastAttack = attack;
        }

        private void ApplyCombatDefaults()
        {
            _boss.SetMoveAttackIntent(BossExcavatorAttack.None);
            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
        }

        private float GetCooldownValue(float baseCooldown)
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return baseCooldown * _boss.Config.PhaseTwoCooldownMult;
            }

            return baseCooldown;
        }

        private float GetShortPostAttackDelay()
        {
            float postAttackDelay = _boss.Config.AttackRecoveryTime;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                postAttackDelay /= _boss.Config.PhaseTwoAttackSpeedMult;
            }

            return Mathf.Min(postAttackDelay, 0.08f);
        }

        private float GetClosePressureTime()
        {
            return Mathf.Max(_boss.Config.MovePressureTime * 0.35f, 0.25f);
        }

        private bool ShouldUseChargeSweepCombo()
        {
            if (_boss.Phase != BossExcavatorPhase.PhaseTwo)
            {
                return false;
            }

            return true;
        }
    }
}
