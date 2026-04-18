using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorSweepAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const int HitBufferCount = 24;
        private const float InnerSweepRadiusFactor = 0.72f;
        private const float SweepKnockbackDuration = 0.16f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly Collider[] _hitBuffer;
        private readonly HashSet<int> _hitHealthIds;

        private float _prepareTimer;
        private float _attackTimer;
        private float _recoverTimer;
        private float _damageTickTimer;
        private float _spinDirectionSign;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public BossExcavatorSweepAttack(BossExcavator boss, BossExcavatorConfig config)
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
            _hitHealthIds = new HashSet<int>();
            Reset();
        }

        public void Reset()
        {
            _prepareTimer = 0f;
            _attackTimer = 0f;
            _recoverTimer = 0f;
            _damageTickTimer = 0f;
            _spinDirectionSign = 1f;
            _isRunning = false;
            _hitHealthIds.Clear();
        }

        public void StartAttack()
        {
            ValidateDependencies();

            _prepareTimer = GetPrepareTime();
            _attackTimer = GetAttackDuration();
            _recoverTimer = GetRecoverTime();
            _damageTickTimer = 0f;
            _spinDirectionSign = ResolveSpinDirectionSign();
            _isRunning = true;
            _hitHealthIds.Clear();

            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(true);
            _boss.SetArmLocked(false);
            SetSweepPose();
        }

        public bool Tick()
        {
            if (_isRunning == false)
            {
                return false;
            }

            if (_prepareTimer > 0f)
            {
                _prepareTimer = Mathf.Max(0f, _prepareTimer - Time.deltaTime);

                if (_prepareTimer <= 0f)
                {
                    BeginSpin();
                }

                return true;
            }

            if (_attackTimer > 0f)
            {
                RotateCabin(Time.deltaTime);
                TickDamage(Time.deltaTime);
                _attackTimer = Mathf.Max(0f, _attackTimer - Time.deltaTime);

                if (_attackTimer <= 0f)
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

            _prepareTimer = 0f;
            _attackTimer = 0f;
            _recoverTimer = 0f;
            _damageTickTimer = 0f;
            _isRunning = false;
            _hitHealthIds.Clear();
            _boss.SetAimLocked(false);

            if (restoreNeutralPose)
            {
                SetRecoverPose();
            }
        }

        private void BeginSpin()
        {
            _damageTickTimer = 0f;
        }

        private void BeginRecover()
        {
            SetRecoverPose();
        }

        private void EndAttack()
        {
            _isRunning = false;
            _boss.SetAimLocked(false);
        }

        private void SetSweepPose()
        {
            _boss.SetArmPose(
                _config.ArmSweepBoomEuler,
                _config.ArmSweepStickEuler,
                _config.ArmSweepBucketEuler,
                GetAttackPoseSpeedMult());
        }

        private void SetRecoverPose()
        {
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
        }

        private void RotateCabin(float deltaTime)
        {
            Transform cabin = _boss.Cabin;

            if (cabin == null)
            {
                throw new InvalidOperationException(nameof(cabin));
            }

            float spinAngle = GetSweepSpinSpeed() * _spinDirectionSign * deltaTime;
            Quaternion spinRotation = Quaternion.AngleAxis(spinAngle, Vector3.up);
            cabin.rotation = spinRotation * cabin.rotation;
        }

        private void TickDamage(float deltaTime)
        {
            float interval = Mathf.Max(_config.SweepDamageInterval / GetPhaseAttackSpeedMult(), 0.05f);
            _damageTickTimer -= deltaTime;

            while (_damageTickTimer <= 0f)
            {
                ApplyDamagePulse();
                _damageTickTimer += interval;
            }
        }

        private void ApplyDamagePulse()
        {
            Transform bucket = _boss.Bucket;

            if (bucket == null)
            {
                throw new InvalidOperationException(nameof(bucket));
            }

            Vector3 hitForward = ResolveHitForward();
            Vector3 hitCenter = bucket.position + hitForward * _config.SweepHitOffset;
            Vector3 innerCenter = ResolveInnerHitCenter(hitCenter);
            float hitRadius = GetSweepHitRadius();
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                innerCenter,
                hitCenter,
                hitRadius,
                _hitBuffer,
                _config.BucketHitMask,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
            {
                return;
            }

            _hitHealthIds.Clear();

            int hitIndex = 0;

            while (hitIndex < hitCount)
            {
                Collider hitCollider = _hitBuffer[hitIndex];
                _hitBuffer[hitIndex] = null;
                hitIndex += 1;

                if (hitCollider == null)
                {
                    continue;
                }

                if (hitCollider.transform.IsChildOf(_boss.transform))
                {
                    continue;
                }

                Health hitHealth = hitCollider.GetComponentInParent<Health>();

                if (hitHealth == null)
                {
                    continue;
                }

                int healthId = hitHealth.GetInstanceID();

                if (_hitHealthIds.Contains(healthId))
                {
                    continue;
                }

                _hitHealthIds.Add(healthId);
                hitHealth.Decrease(_config.SweepHitDamage * GetPhaseDamageMult());
                TryPushPlayer(hitCollider, hitForward);
            }
        }

        private void TryPushPlayer(Collider hitCollider, Vector3 hitForward)
        {
            if (hitCollider == null)
            {
                return;
            }

            Player hitPlayer = hitCollider.GetComponentInParent<Player>();

            if (hitPlayer == null)
            {
                return;
            }

            PlayerMovement playerMovement = hitPlayer.Movement;

            if (playerMovement == null)
            {
                return;
            }

            Rigidbody hitRigidbody = hitCollider.attachedRigidbody;

            if (hitRigidbody == null)
            {
                return;
            }

            if (hitRigidbody.isKinematic)
            {
                return;
            }

            Vector3 pushOrigin = _boss.Base.position;
            pushOrigin.y = hitRigidbody.worldCenterOfMass.y;
            Vector3 pushDirection = hitRigidbody.worldCenterOfMass - pushOrigin;
            pushDirection.y = 0f;

            if (pushDirection.sqrMagnitude <= MinDirectionSqr)
            {
                pushDirection = hitForward;
            }

            else
            {
                pushDirection.Normalize();
            }

            playerMovement.ApplyKnockback(
                pushDirection,
                _config.SweepPushForce,
                SweepKnockbackDuration,
                _config.SweepPushLift);
        }

        private Vector3 ResolveInnerHitCenter(Vector3 outerHitCenter)
        {
            Transform cabin = _boss.Cabin;

            if (cabin == null)
            {
                return outerHitCenter;
            }

            Vector3 innerCenter = cabin.position;
            innerCenter.y = outerHitCenter.y;

            Vector3 toOuter = outerHitCenter - innerCenter;
            toOuter.y = 0f;

            if (toOuter.sqrMagnitude <= MinDirectionSqr)
            {
                return outerHitCenter;
            }

            return innerCenter + toOuter * InnerSweepRadiusFactor;
        }

        private float GetSweepHitRadius()
        {
            return _config.SweepHitRadius;
        }

        private Vector3 ResolveHitForward()
        {
            Transform bucket = _boss.Bucket;

            if (bucket != null)
            {
                Vector3 bucketForward = bucket.forward;
                bucketForward.y = 0f;

                if (bucketForward.sqrMagnitude > MinDirectionSqr)
                {
                    return bucketForward.normalized;
                }
            }

            Transform cabin = _boss.Cabin;

            if (cabin != null)
            {
                Vector3 cabinForward = cabin.forward;
                cabinForward.y = 0f;

                if (cabinForward.sqrMagnitude > MinDirectionSqr)
                {
                    return cabinForward.normalized;
                }
            }

            Transform baseTransform = _boss.Base;

            if (baseTransform != null)
            {
                Vector3 baseForward = baseTransform.forward;
                baseForward.y = 0f;

                if (baseForward.sqrMagnitude > MinDirectionSqr)
                {
                    return baseForward.normalized;
                }
            }

            return Vector3.forward;
        }

        private float ResolveSpinDirectionSign()
        {
            Transform cabin = _boss.Cabin;
            Transform target = _boss.Target;

            if (cabin == null)
            {
                return 1f;
            }

            if (target == null)
            {
                return 1f;
            }

            Vector3 cabinForward = cabin.forward;
            cabinForward.y = 0f;
            Vector3 targetDirection = target.position - cabin.position;
            targetDirection.y = 0f;

            if (cabinForward.sqrMagnitude <= MinDirectionSqr)
            {
                return 1f;
            }

            if (targetDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return 1f;
            }

            float signedAngle = Vector3.SignedAngle(cabinForward.normalized, targetDirection.normalized, Vector3.up);

            if (Mathf.Abs(signedAngle) <= 1f)
            {
                return 1f;
            }

            if (signedAngle < 0f)
            {
                return -1f;
            }

            return 1f;
        }

        private void ValidateDependencies()
        {
            if (_boss.Cabin == null)
            {
                throw new InvalidOperationException(nameof(_boss.Cabin));
            }

            if (_boss.Bucket == null)
            {
                throw new InvalidOperationException(nameof(_boss.Bucket));
            }

            if (_boss.Base == null)
            {
                throw new InvalidOperationException(nameof(_boss.Base));
            }
        }

        private float GetAttackDuration()
        {
            float attackTime = _config.SweepAttackTime / GetPhaseAttackSpeedMult();
            float spinDuration = (360f * _config.SweepSpinTurns) / Mathf.Max(GetSweepSpinSpeed(), 1f);

            if (spinDuration > attackTime)
            {
                return spinDuration;
            }

            return attackTime;
        }

        private float GetPhaseAttackSpeedMult()
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.PhaseTwoAttackSpeedMult;
            }

            return 1f;
        }

        private float GetPhaseDamageMult()
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.PhaseTwoDamageMult;
            }

            return 1f;
        }

        private float GetAttackPoseSpeedMult()
        {
            return _config.AttackPoseSpeedMult * GetPhaseAttackSpeedMult();
        }

        private float GetSweepSpinSpeed()
        {
            float spinSpeed = _config.SweepSpinSpeed;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                spinSpeed *= _config.PhaseTwoSweepSpinSpeedMult;
            }

            return spinSpeed;
        }

        private float GetPrepareTime()
        {
            return _config.SweepPrepareTime / GetPhaseAttackSpeedMult();
        }

        private float GetRecoverTime()
        {
            return _config.AttackRecoveryTime / GetPhaseAttackSpeedMult();
        }
    }
}
