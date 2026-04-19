using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
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
                _chargeCooldownTimer = GetChargeCooldownValue();
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
            if (nextState != BossExcavatorState.Move)
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
                if (_boss.IsAdvancedPhase == false)
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

            if (_boss.IsAdvancedPhase)
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

            if (_boss.IsAdvancedPhase)
            {
                requiredBaseAngle += 34f;
            }

            if (baseAngle > requiredBaseAngle)
            {
                return false;
            }

            float requiredMoveSpeed = _boss.Config.ScrapTrailMinMoveSpeed;

            if (_boss.IsAdvancedPhase)
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
            if (_boss.IsAdvancedPhase == false)
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
            if (_boss.IsAdvancedPhase == false)
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
            UpdateAttackUsageRuntime(attack);
        }

        private void UpdateAttackUsageRuntime(BossExcavatorAttack attack)
        {
            IncrementAttackUsageCounter(BossExcavatorAttack.BucketStrike, attack);
            IncrementAttackUsageCounter(BossExcavatorAttack.ThrowScrap, attack);
            IncrementAttackUsageCounter(BossExcavatorAttack.Charge, attack);
            IncrementAttackUsageCounter(BossExcavatorAttack.Sweep, attack);
        }

        private void IncrementAttackUsageCounter(BossExcavatorAttack candidate, BossExcavatorAttack usedAttack)
        {
            int nextValue = GetAttacksSinceUsed(candidate) + 1;

            if (candidate == usedAttack)
            {
                nextValue = 0;
            }

            SetAttacksSinceUsed(candidate, nextValue);
        }

        private void SetAttacksSinceUsed(BossExcavatorAttack attack, int value)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                _attacksSinceBucketStrike = value;

                return;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                _attacksSinceThrowScrap = value;

                return;
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                _attacksSinceCharge = value;

                return;
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                _attacksSinceSweep = value;
            }
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
            if (_boss.Phase == BossExcavatorPhase.PhaseThree)
            {
                return baseCooldown * _boss.Config.PhaseThreeCooldownMult;
            }

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return baseCooldown * _boss.Config.PhaseTwoCooldownMult;
            }

            return baseCooldown;
        }

        private float GetChargeCooldownValue()
        {
            float chargeCooldown = _boss.Config.ChargeAttackCooldown;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return chargeCooldown;
            }

            return GetCooldownValue(chargeCooldown);
        }

        private float GetShortPostAttackDelay()
        {
            float postAttackDelay = _boss.Config.AttackRecoveryTime;

            if (_boss.Phase == BossExcavatorPhase.PhaseThree)
            {
                postAttackDelay /= _boss.Config.PhaseThreeAttackSpeedMult;
            }

            else if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
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
            if (_boss.IsAdvancedPhase == false)
            {
                return false;
            }

            return true;
        }
    }
}
