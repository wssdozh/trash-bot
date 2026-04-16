namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private BossExcavatorAttack SelectAttack(float targetDistance, float baseAngle, float cabinAngle)
        {
            _pendingAttack = BossExcavatorAttack.None;

            if (CanUseBucket(targetDistance, baseAngle, cabinAngle))
            {
                _pendingAttack = BossExcavatorAttack.BucketStrike;

                return _pendingAttack;
            }

            if (CanUseCharge(targetDistance, baseAngle))
            {
                _pendingAttack = BossExcavatorAttack.Charge;

                return _pendingAttack;
            }

            if (CanUseThrow(targetDistance, baseAngle, cabinAngle))
            {
                _pendingAttack = BossExcavatorAttack.ThrowScrap;

                return _pendingAttack;
            }

            return BossExcavatorAttack.None;
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

            if (targetDistance > _boss.Config.BucketMaxDistance)
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
            _currentAttack = attack;
            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);

            if (attack == BossExcavatorAttack.BucketStrike)
            {
                _bucketAttack.StartAttack();

                return;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                _throwAttack.StartAttack();

                return;
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                _chargeAttack.StartAttack();
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
            if (_currentAttack == BossExcavatorAttack.BucketStrike)
            {
                _bucketCooldownTimer = GetCooldownValue(_boss.Config.BucketAttackCooldown);
                _postAttackTimer = _boss.Config.AttackRecoveryTime;
            }

            if (_currentAttack == BossExcavatorAttack.ThrowScrap)
            {
                _throwCooldownTimer = GetCooldownValue(_boss.Config.ThrowAttackCooldown);
                _postAttackTimer = _boss.Config.AttackRecoveryTime;
            }

            if (_currentAttack == BossExcavatorAttack.Charge)
            {
                _chargeCooldownTimer = GetCooldownValue(_boss.Config.ChargeAttackCooldown);
                _postAttackTimer = 0f;
            }

            ClearAttackRuntime(true);
        }

        private void ClearAttackRuntime(bool restorePose)
        {
            if (_currentAttack == BossExcavatorAttack.None)
            {
                return;
            }

            _currentAttack = BossExcavatorAttack.None;
            _pendingAttack = BossExcavatorAttack.None;
            _bucketAttack.Cancel(restorePose);
            _throwAttack.Cancel(restorePose);
            _chargeAttack.Cancel(restorePose);
            ApplyCombatDefaults();

            if (restorePose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, _boss.Config.AttackPoseSpeedMult);
            }
        }

        private void ApplyCombatDefaults()
        {
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
    }
}
