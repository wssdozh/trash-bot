using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
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
            if (CanUseBucketCore(baseAngle, cabinAngle) == false)
            {
                return false;
            }

            if (targetDistance <= GetBucketAttackStartDistance())
            {
                return true;
            }

            return CanUsePhaseTwoRangedBucket(targetDistance, baseAngle, cabinAngle);
        }

        private float GetBucketAttackStartDistance()
        {
            float bucketAttackDistance = _boss.Config.BucketMaxDistance * 0.82f;
            float minBucketAttackDistance = _boss.Config.StopDistance + 1.35f;

            return Mathf.Max(minBucketAttackDistance, bucketAttackDistance);
        }

        private bool CanUseBucketCore(float baseAngle, float cabinAngle)
        {
            if (_postAttackTimer > 0f)
            {
                return false;
            }

            if (_bucketCooldownTimer > 0f)
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

        private bool CanUsePhaseTwoRangedBucket(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (_boss.IsAdvancedPhase == false)
            {
                return false;
            }

            if (CanUseBucketCore(baseAngle, cabinAngle) == false)
            {
                return false;
            }

            if (targetDistance <= GetCloseQueuePressureDistance())
            {
                return false;
            }

            if (targetDistance > GetPhaseTwoRangedBucketMaxDistance())
            {
                return false;
            }

            return true;
        }

        private float GetPhaseTwoRangedBucketMaxDistance()
        {
            float rangedBucketMaxDistance = Mathf.Min(_boss.Config.ThrowMaxDistance, _boss.Config.ScrapTrailMaxDistance);
            float minRangedBucketMaxDistance = GetCloseQueuePressureDistance() + 0.5f;

            return Mathf.Max(minRangedBucketMaxDistance, rangedBucketMaxDistance);
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

            if (GetChargeApproachAngle(baseAngle) > GetChargeStartAngle())
            {
                return false;
            }

            return true;
        }

        private float GetChargeApproachAngle(float baseAngle)
        {
            return baseAngle;
        }

        private float GetChargeStartAngle()
        {
            float chargeStartAngle = _boss.Config.ChargeBaseAngle;

            if (_boss.IsAdvancedPhase)
            {
                chargeStartAngle = Mathf.Max(chargeStartAngle, _boss.Config.RepositionBaseAngle);
            }

            return chargeStartAngle;
        }

        private bool IsClosePriorityDistance(float targetDistance)
        {
            return targetDistance <= GetCloseQueuePressureDistance();
        }

        private BossExcavatorAttack GetClosePriorityAttack()
        {
            if (_lastAttack == BossExcavatorAttack.BucketStrike)
            {
                return BossExcavatorAttack.Sweep;
            }

            if (_lastAttack == BossExcavatorAttack.Sweep)
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (_previousAttack == BossExcavatorAttack.BucketStrike)
            {
                return BossExcavatorAttack.Sweep;
            }

            return BossExcavatorAttack.BucketStrike;
        }

        private BossExcavatorAttack GetFarPriorityAttack()
        {
            if (_lastAttack == BossExcavatorAttack.Charge)
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            if (_lastAttack == BossExcavatorAttack.ThrowScrap)
            {
                return BossExcavatorAttack.Charge;
            }

            if (_previousAttack == BossExcavatorAttack.Charge)
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            return BossExcavatorAttack.Charge;
        }

        private BossExcavatorAttack GetPairedPriorityAttack(BossExcavatorAttack attack)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                return BossExcavatorAttack.Sweep;
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                return BossExcavatorAttack.Charge;
            }

            return BossExcavatorAttack.None;
        }
    }
}
