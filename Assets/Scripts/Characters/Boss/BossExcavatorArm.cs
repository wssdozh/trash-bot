using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorArm : MonoBehaviour
    {
        private BossExcavator _boss;
        private BossExcavatorConfig _config;
        private Transform _boom;
        private Transform _stick;
        private Transform _bucket;
        private Quaternion _boomTarget;
        private Quaternion _stickTarget;
        private Quaternion _bucketTarget;
        private float _poseSpeedMult;
        private bool _isLocked;
        private BossExcavatorArmPose _currentPose;

        public bool IsLocked => _isLocked;
        public BossExcavatorArmPose CurrentPose => _currentPose;

        public void Setup(BossExcavator boss, BossExcavatorConfig config, Transform boom, Transform stick, Transform bucket)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            if (config == null)
            {
                throw new InvalidOperationException(nameof(config));
            }

            if (boom == null)
            {
                throw new InvalidOperationException(nameof(boom));
            }

            if (stick == null)
            {
                throw new InvalidOperationException(nameof(stick));
            }

            if (bucket == null)
            {
                throw new InvalidOperationException(nameof(bucket));
            }

            _boss = boss;
            _config = config;
            _boom = boom;
            _stick = stick;
            _bucket = bucket;
            _poseSpeedMult = 1f;
            _isLocked = false;
            _currentPose = BossExcavatorArmPose.Custom;
            CacheCurrentPose();
        }

        public void Tick()
        {
            ValidateDependencies();

            if (_isLocked)
            {
                return;
            }

            if (_boss.IsDead)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            RotatePart(_boom, _boomTarget, _config.ArmBoomSpeed * _poseSpeedMult, deltaTime);
            RotatePart(_stick, _stickTarget, _config.ArmStickSpeed * _poseSpeedMult, deltaTime);
            RotatePart(_bucket, _bucketTarget, _config.ArmBucketSpeed * _poseSpeedMult, deltaTime);
        }

        public void SetLocked(bool isLocked)
        {
            _isLocked = isLocked;
        }

        public void SetDefaultPose()
        {
            SetNeutralPose();
        }

        public void SetDefaultPoseImmediate()
        {
            SetNeutralPoseImmediate();
        }

        public void SetNeutralPose()
        {
            SetPose(BossExcavatorArmPose.Neutral, 1f);
        }

        public void SetNeutralPoseImmediate()
        {
            SetPoseImmediate(BossExcavatorArmPose.Neutral);
        }

        public void SetBucketPreparePose()
        {
            SetPose(BossExcavatorArmPose.BucketPrepare, 1f);
        }

        public void SetBucketStrikePose()
        {
            SetPose(BossExcavatorArmPose.BucketStrike, 1f);
        }

        public void SetGrabScrapPose()
        {
            SetPose(BossExcavatorArmPose.GrabScrap, 1f);
        }

        public void SetThrowScrapPose()
        {
            SetPose(BossExcavatorArmPose.ThrowScrap, 1f);
        }

        public void SetPose(BossExcavatorArmPose pose)
        {
            SetPose(pose, 1f);
        }

        public void SetPose(BossExcavatorArmPose pose, float poseSpeedMult)
        {
            if (poseSpeedMult <= 0f)
            {
                throw new InvalidOperationException(nameof(poseSpeedMult));
            }

            Vector3 boomLocalEuler = _config.ArmNeutralBoomEuler;
            Vector3 stickLocalEuler = _config.ArmNeutralStickEuler;
            Vector3 bucketLocalEuler = _config.ArmNeutralBucketEuler;

            ResolvePose(pose, out boomLocalEuler, out stickLocalEuler, out bucketLocalEuler);
            _currentPose = pose;
            ApplyPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler, poseSpeedMult);
        }

        public void SetPoseImmediate(BossExcavatorArmPose pose)
        {
            Vector3 boomLocalEuler = _config.ArmNeutralBoomEuler;
            Vector3 stickLocalEuler = _config.ArmNeutralStickEuler;
            Vector3 bucketLocalEuler = _config.ArmNeutralBucketEuler;

            ResolvePose(pose, out boomLocalEuler, out stickLocalEuler, out bucketLocalEuler);
            _currentPose = pose;
            ApplyPoseImmediate(boomLocalEuler, stickLocalEuler, bucketLocalEuler);
        }

        public void SetPose(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler)
        {
            SetPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler, 1f);
        }

        public void SetPose(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler, float poseSpeedMult)
        {
            if (poseSpeedMult <= 0f)
            {
                throw new InvalidOperationException(nameof(poseSpeedMult));
            }

            _currentPose = BossExcavatorArmPose.Custom;
            ApplyPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler, poseSpeedMult);
        }

        public void SetPoseImmediate(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler)
        {
            _currentPose = BossExcavatorArmPose.Custom;
            ApplyPoseImmediate(boomLocalEuler, stickLocalEuler, bucketLocalEuler);
        }

        private void ResolvePose(BossExcavatorArmPose pose, out Vector3 boomLocalEuler, out Vector3 stickLocalEuler, out Vector3 bucketLocalEuler)
        {
            boomLocalEuler = _config.ArmNeutralBoomEuler;
            stickLocalEuler = _config.ArmNeutralStickEuler;
            bucketLocalEuler = _config.ArmNeutralBucketEuler;

            if (pose == BossExcavatorArmPose.BucketPrepare)
            {
                boomLocalEuler = _config.ArmBucketPrepareBoomEuler;
                stickLocalEuler = _config.ArmBucketPrepareStickEuler;
                bucketLocalEuler = _config.ArmBucketPrepareBucketEuler;

                return;
            }

            if (pose == BossExcavatorArmPose.BucketStrike)
            {
                boomLocalEuler = _config.ArmBucketStrikeBoomEuler;
                stickLocalEuler = _config.ArmBucketStrikeStickEuler;
                bucketLocalEuler = _config.ArmBucketStrikeBucketEuler;

                return;
            }

            if (pose == BossExcavatorArmPose.GrabScrap)
            {
                boomLocalEuler = _config.ArmGrabScrapBoomEuler;
                stickLocalEuler = _config.ArmGrabScrapStickEuler;
                bucketLocalEuler = _config.ArmGrabScrapBucketEuler;

                return;
            }

            if (pose == BossExcavatorArmPose.ThrowScrap)
            {
                boomLocalEuler = _config.ArmThrowScrapBoomEuler;
                stickLocalEuler = _config.ArmThrowScrapStickEuler;
                bucketLocalEuler = _config.ArmThrowScrapBucketEuler;
            }
        }

        private void ApplyPose(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler, float poseSpeedMult)
        {
            _boomTarget = BuildJointRotation(boomLocalEuler, _config.ArmBoomAxis, _config.ArmBoomAxisInvert);
            _stickTarget = BuildJointRotation(stickLocalEuler, _config.ArmStickAxis, _config.ArmStickAxisInvert);
            _bucketTarget = BuildJointRotation(bucketLocalEuler, _config.ArmBucketAxis, _config.ArmBucketAxisInvert);
            _poseSpeedMult = poseSpeedMult;
        }

        private void ApplyPoseImmediate(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler)
        {
            ApplyPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler, 1f);
            _boom.localRotation = _boomTarget;
            _stick.localRotation = _stickTarget;
            _bucket.localRotation = _bucketTarget;
        }

        private Quaternion BuildJointRotation(Vector3 sourceEuler, BossExcavatorAxis axis, bool isInverted)
        {
            float sourceAngle = GetDominantAngle(sourceEuler);

            if (isInverted)
            {
                sourceAngle = -sourceAngle;
            }

            Vector3 axisVector = GetAxisVector(axis);

            return Quaternion.AngleAxis(sourceAngle, axisVector);
        }

        private float GetDominantAngle(Vector3 sourceEuler)
        {
            float absX = Mathf.Abs(sourceEuler.x);
            float absY = Mathf.Abs(sourceEuler.y);
            float absZ = Mathf.Abs(sourceEuler.z);

            if (absX >= absY)
            {
                if (absX >= absZ)
                {
                    return sourceEuler.x;
                }
            }

            if (absY >= absX)
            {
                if (absY >= absZ)
                {
                    return sourceEuler.y;
                }
            }

            return sourceEuler.z;
        }

        private Vector3 GetAxisVector(BossExcavatorAxis axis)
        {
            if (axis == BossExcavatorAxis.X)
            {
                return Vector3.right;
            }

            if (axis == BossExcavatorAxis.Y)
            {
                return Vector3.up;
            }

            return Vector3.forward;
        }

        private void RotatePart(Transform part, Quaternion targetRotation, float turnSpeed, float deltaTime)
        {
            Quaternion currentRotation = part.localRotation;
            Quaternion nextRotation = Quaternion.RotateTowards(currentRotation, targetRotation, turnSpeed * deltaTime);
            part.localRotation = nextRotation;
        }

        private void CacheCurrentPose()
        {
            _boomTarget = _boom.localRotation;
            _stickTarget = _stick.localRotation;
            _bucketTarget = _bucket.localRotation;
        }

        private void ValidateDependencies()
        {
            if (_boss == null)
            {
                throw new InvalidOperationException(nameof(_boss));
            }

            if (_config == null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            if (_boom == null)
            {
                throw new InvalidOperationException(nameof(_boom));
            }

            if (_stick == null)
            {
                throw new InvalidOperationException(nameof(_stick));
            }

            if (_bucket == null)
            {
                throw new InvalidOperationException(nameof(_bucket));
            }
        }
    }
}
