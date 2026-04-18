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
        private float _boomTurnSpeed;
        private float _stickTurnSpeed;
        private float _bucketTurnSpeed;
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
            ResetTurnSpeeds();
            CacheCurrentPose();
        }

        public void Tick()
        {
            ValidateDependencies();

            if (_isLocked)
            {
                ResetTurnSpeeds();

                return;
            }

            if (_boss.IsDead)
            {
                ResetTurnSpeeds();

                return;
            }

            float deltaTime = Time.deltaTime;
            RotatePart(_boom, _boomTarget, _config.ArmBoomSpeed * _poseSpeedMult, ref _boomTurnSpeed, deltaTime);
            RotatePart(_stick, _stickTarget, _config.ArmStickSpeed * _poseSpeedMult, ref _stickTurnSpeed, deltaTime);
            RotatePart(_bucket, _bucketTarget, _config.ArmBucketSpeed * _poseSpeedMult, ref _bucketTurnSpeed, deltaTime);
        }

        public void SetLocked(bool isLocked)
        {
            _isLocked = isLocked;

            if (isLocked)
            {
                ResetTurnSpeeds();
            }
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

        public float GetPoseTravelTime(BossExcavatorArmPose pose, float poseSpeedMult)
        {
            if (poseSpeedMult <= 0f)
            {
                throw new InvalidOperationException(nameof(poseSpeedMult));
            }

            ValidateDependencies();

            Vector3 boomLocalEuler = _config.ArmNeutralBoomEuler;
            Vector3 stickLocalEuler = _config.ArmNeutralStickEuler;
            Vector3 bucketLocalEuler = _config.ArmNeutralBucketEuler;

            ResolvePose(pose, out boomLocalEuler, out stickLocalEuler, out bucketLocalEuler);

            return GetPoseTravelTime(boomLocalEuler, stickLocalEuler, bucketLocalEuler, poseSpeedMult);
        }

        public float GetPoseTravelTime(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler, float poseSpeedMult)
        {
            if (poseSpeedMult <= 0f)
            {
                throw new InvalidOperationException(nameof(poseSpeedMult));
            }

            ValidateDependencies();

            Quaternion boomTarget = BuildJointRotation(boomLocalEuler, _config.ArmBoomAxis, _config.ArmBoomAxisInvert);
            Quaternion stickTarget = BuildJointRotation(stickLocalEuler, _config.ArmStickAxis, _config.ArmStickAxisInvert);
            Quaternion bucketTarget = BuildJointRotation(bucketLocalEuler, _config.ArmBucketAxis, _config.ArmBucketAxisInvert);
            float boomTime = GetJointTravelTime(_boom.localRotation, boomTarget, _boomTurnSpeed, _config.ArmBoomSpeed * poseSpeedMult);
            float stickTime = GetJointTravelTime(_stick.localRotation, stickTarget, _stickTurnSpeed, _config.ArmStickSpeed * poseSpeedMult);
            float bucketTime = GetJointTravelTime(_bucket.localRotation, bucketTarget, _bucketTurnSpeed, _config.ArmBucketSpeed * poseSpeedMult);

            return Mathf.Max(boomTime, Mathf.Max(stickTime, bucketTime));
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

                return;
            }

            if (pose == BossExcavatorArmPose.TrailScrape)
            {
                boomLocalEuler = _config.ArmTrailScrapeBoomEuler;
                stickLocalEuler = _config.ArmTrailScrapeStickEuler;
                bucketLocalEuler = _config.ArmTrailScrapeBucketEuler;
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
            ResetTurnSpeeds();
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

        private void RotatePart(
            Transform part,
            Quaternion targetRotation,
            float turnSpeed,
            ref float currentTurnSpeed,
            float deltaTime)
        {
            Quaternion currentRotation = part.localRotation;
            Quaternion nextRotation = BossExcavatorMotionProfile.StepRotation(
                currentRotation,
                targetRotation,
                ref currentTurnSpeed,
                turnSpeed,
                _config.ArmTurnAcceleration,
                _config.ArmTurnDeceleration,
                _config.ArmTurnSlowAngle,
                _config.ArmTurnMinSpeedFactor,
                deltaTime);
            part.localRotation = nextRotation;
        }

        private float GetJointTravelTime(
            Quaternion currentRotation,
            Quaternion targetRotation,
            float currentTurnSpeed,
            float turnSpeed)
        {
            if (turnSpeed <= 0f)
            {
                throw new InvalidOperationException(nameof(turnSpeed));
            }

            return BossExcavatorMotionProfile.EstimateTravelTime(
                currentRotation,
                targetRotation,
                currentTurnSpeed,
                turnSpeed,
                _config.ArmTurnAcceleration,
                _config.ArmTurnDeceleration,
                _config.ArmTurnSlowAngle,
                _config.ArmTurnMinSpeedFactor);
        }

        private void CacheCurrentPose()
        {
            _boomTarget = _boom.localRotation;
            _stickTarget = _stick.localRotation;
            _bucketTarget = _bucket.localRotation;
        }

        private void ResetTurnSpeeds()
        {
            _boomTurnSpeed = 0f;
            _stickTurnSpeed = 0f;
            _bucketTurnSpeed = 0f;
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
