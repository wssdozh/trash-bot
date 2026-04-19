using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavator
    {
        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                throw new InvalidOperationException(nameof(target));
            }

            if (IsTargetSelf(target))
            {
                throw new InvalidOperationException(nameof(target));
            }

            _target = target;
            _move.SetTarget(target);
            _aim.SetTarget(target);
        }

        public void SetChargeAlign(bool isChargeAlign)
        {
            _move.SetChargeAlign(isChargeAlign);
        }

        public void SetMoveAttackIntent(BossExcavatorAttack attackIntent)
        {
            _move.SetAttackIntent(attackIntent);
        }

        public void SetAimLocked(bool isLocked)
        {
            _aim.SetLocked(isLocked);
        }

        public void SetArmLocked(bool isLocked)
        {
            _arm.SetLocked(isLocked);
        }

        public void SetArmDefaultPose()
        {
            _arm.SetDefaultPose();
        }

        public BossExcavatorArmPose GetArmPose()
        {
            return _arm.CurrentPose;
        }

        public void SetArmNeutralPose()
        {
            _arm.SetNeutralPose();
        }

        public void SetArmBucketPreparePose()
        {
            _arm.SetBucketPreparePose();
        }

        public void SetArmBucketStrikePose()
        {
            _arm.SetBucketStrikePose();
        }

        public void SetArmGrabScrapPose()
        {
            _arm.SetGrabScrapPose();
        }

        public void SetArmThrowScrapPose()
        {
            _arm.SetThrowScrapPose();
        }

        public void SetArmPose(BossExcavatorArmPose pose)
        {
            _arm.SetPose(pose);
        }

        public void SetArmPose(BossExcavatorArmPose pose, float poseSpeedMult)
        {
            _arm.SetPose(pose, poseSpeedMult);
        }

        public void SetArmPose(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler)
        {
            _arm.SetPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler);
        }

        public void SetArmPose(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler, float poseSpeedMult)
        {
            _arm.SetPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler, poseSpeedMult);
        }

        public void SetArmPoseImmediate(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler)
        {
            _arm.SetPoseImmediate(boomLocalEuler, stickLocalEuler, bucketLocalEuler);
        }
    }
}
