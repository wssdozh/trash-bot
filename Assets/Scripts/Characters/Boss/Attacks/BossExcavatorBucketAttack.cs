using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBucketAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const int HitBufferCount = 20;
        private const int GroundHitBufferCount = 8;
        private const string ScrapTrailSpawnerKey = "BossScrapTrailBlock";
        private const float StrikeSpeedBoostMult = 5f;
        private const float StrikePoseSnapSpeedMult = 1.65f;
        private const float StrikeImpactDelayFactor = 0.5f;
        private const float MinStrikeImpactDelay = 0.1f;
        private const float MaxStrikeImpactDelay = 0.22f;
        private const float HitStopDuration = 0.05f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly Collider[] _hitBuffer;
        private readonly RaycastHit[] _groundHitBuffer;
        private readonly HashSet<int> _damagedHealthIds;

        private BossScrapTrailBlockSpawner _scrapTrailBlockSpawner;
        private float _telegraphTimer;
        private float _strikeTimer;
        private float _recoverTimer;
        private float _hitDelayTimer;
        private float _hitStopTimer;
        private Vector3 _strikeForward;
        private bool _isRunning;
        private bool _isHitApplied;

        public bool IsRunning => _isRunning;

        public float Duration => GetBucketPrepareTime() + GetBucketStrikeTime() + GetRecoverTime();

        public BossExcavatorBucketAttack(BossExcavator boss, BossExcavatorConfig config)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            if (config == null)
            {
                throw new InvalidOperationException(nameof(config));
            }

            _boss = boss;
            _config = config;
            _hitBuffer = new Collider[HitBufferCount];
            _groundHitBuffer = new RaycastHit[GroundHitBufferCount];
            _damagedHealthIds = new HashSet<int>();
            Reset();
        }

        public void Reset()
        {
            _telegraphTimer = 0f;
            _strikeTimer = 0f;
            _recoverTimer = 0f;
            _hitDelayTimer = 0f;
            _hitStopTimer = 0f;
            _isRunning = false;
            _isHitApplied = false;
            _strikeForward = Vector3.forward;
            _damagedHealthIds.Clear();
        }

        public void StartAttack()
        {
            ValidateDependencies();

            _telegraphTimer = Mathf.Max(GetBucketPrepareTime(), GetPreparePoseTravelTime());
            _strikeTimer = 0f;
            _recoverTimer = GetRecoverTime();
            _hitDelayTimer = 0f;
            _hitStopTimer = 0f;
            _isRunning = true;
            _isHitApplied = false;
            _strikeForward = ResolveStrikeForward();

            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(true);
            _boss.SetArmLocked(false);
            SetTelegraphPose();
        }

        public bool Tick()
        {
            if (_isRunning == false)
            {
                return false;
            }

            if (_hitStopTimer > 0f)
            {
                TickHitStop();

                return true;
            }

            if (_telegraphTimer > 0f)
            {
                _telegraphTimer = Mathf.Max(0f, _telegraphTimer - Time.deltaTime);

                if (_telegraphTimer <= 0f)
                {
                    BeginStrike();
                }

                return true;
            }

            if (_strikeTimer > 0f)
            {
                _hitDelayTimer = Mathf.Max(0f, _hitDelayTimer - Time.deltaTime);

                if (_isHitApplied == false)
                {
                    if (_hitDelayTimer <= 0f)
                    {
                        TryApplyHit();
                    }
                }

                _strikeTimer = Mathf.Max(0f, _strikeTimer - Time.deltaTime);

                if (_strikeTimer <= 0f)
                {
                    BeginRecover();
                }

                return true;
            }

            if (_recoverTimer > 0f)
            {
                _recoverTimer = Mathf.Max(0f, _recoverTimer - Time.deltaTime);

                if (_recoverTimer <= 0f)
                {
                    EndAttack();
                }

                return true;
            }

            EndAttack();

            return false;
        }

        public void Cancel(bool restoreNeutralPose)
        {
            if (_isRunning == false)
            {
                return;
            }

            _isRunning = false;
            _telegraphTimer = 0f;
            _strikeTimer = 0f;
            _recoverTimer = 0f;
            _hitDelayTimer = 0f;
            _hitStopTimer = 0f;
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
            _damagedHealthIds.Clear();

            if (restoreNeutralPose)
            {
                SetRecoverPose();
            }
        }

        private void BeginStrike()
        {
            _boss.SetAimLocked(false);
            _strikeForward = ResolveStrikeForward();
            float strikePoseTravelTime = GetStrikePoseTravelTime();
            _strikeTimer = Mathf.Max(GetBucketStrikeTime(), strikePoseTravelTime);
            _hitDelayTimer = GetStrikeImpactDelay(_strikeTimer);
            SetStrikePose();
        }

        private void BeginRecover()
        {
            _boss.SetArmLocked(false);
            SetRecoverPose();
        }

        private void EndAttack()
        {
            _isRunning = false;
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
        }

        private void SetTelegraphPose()
        {
            _boss.SetArmPose(
                _config.ArmBucketPrepareBoomEuler,
                _config.ArmBucketPrepareStickEuler,
                _config.ArmBucketPrepareBucketEuler,
                _config.BucketPrepareSpeedMult * GetPhaseAttackSpeedMult());
        }

        private void SetStrikePose()
        {
            _boss.SetArmPose(
                _config.ArmBucketStrikeBoomEuler,
                _config.ArmBucketStrikeStickEuler,
                _config.ArmBucketStrikeBucketEuler,
                GetBucketStrikePoseSpeedMult() * StrikePoseSnapSpeedMult);
        }

        private void SetRecoverPose()
        {
            _boss.SetArmPose(
                _config.ArmNeutralBoomEuler,
                _config.ArmNeutralStickEuler,
                _config.ArmNeutralBucketEuler,
                _config.BucketRecoverSpeedMult * GetPhaseAttackSpeedMult());
        }

        private void ValidateDependencies()
        {
            if (_boss.Bucket == null)
            {
                throw new InvalidOperationException(nameof(_boss.Bucket));
            }

            if (_boss.Base == null)
            {
                throw new InvalidOperationException(nameof(_boss.Base));
            }

            if (_boss.Boom == null)
            {
                throw new InvalidOperationException(nameof(_boss.Boom));
            }

            if (_boss.Arm == null)
            {
                throw new InvalidOperationException(nameof(_boss.Arm));
            }

            if (_boss.Stick == null)
            {
                throw new InvalidOperationException(nameof(_boss.Stick));
            }
        }

        private float GetPhaseAttackSpeedMult()
        {
            return _boss.GetPhaseAttackSpeedMult();
        }

        private float GetPhaseDamageMult()
        {
            return _boss.GetPhaseDamageMult();
        }

        private float GetBucketPrepareTime()
        {
            return _config.BucketPrepareTime / GetPhaseAttackSpeedMult();
        }

        private float GetPreparePoseTravelTime()
        {
            float poseSpeedMult = _config.BucketPrepareSpeedMult * GetPhaseAttackSpeedMult();

            return _boss.Arm.GetPoseTravelTime(
                _config.ArmBucketPrepareBoomEuler,
                _config.ArmBucketPrepareStickEuler,
                _config.ArmBucketPrepareBucketEuler,
                poseSpeedMult);
        }

        private float GetBucketStrikeTime()
        {
            return _config.BucketStrikeTime / GetPhaseAttackSpeedMult() / StrikeSpeedBoostMult;
        }

        private float GetStrikePoseTravelTime()
        {
            float poseSpeedMult = GetBucketStrikePoseSpeedMult();

            return _boss.Arm.GetPoseTravelTime(
                _config.ArmBucketStrikeBoomEuler,
                _config.ArmBucketStrikeStickEuler,
                _config.ArmBucketStrikeBucketEuler,
                poseSpeedMult);
        }

        private float GetBucketStrikePoseSpeedMult()
        {
            return _config.BucketStrikeSpeedMult * GetPhaseAttackSpeedMult() * StrikeSpeedBoostMult;
        }

        private float GetStrikeImpactDelay(float strikeTime)
        {
            float minStrikeImpactDelay = MinStrikeImpactDelay / StrikeSpeedBoostMult;
            float hitDelay = Mathf.Clamp(strikeTime * StrikeImpactDelayFactor, minStrikeImpactDelay, MaxStrikeImpactDelay);

            return Mathf.Min(hitDelay, strikeTime);
        }

        private float GetRecoverTime()
        {
            return _config.AttackRecoveryTime / GetPhaseAttackSpeedMult();
        }
    }
}
