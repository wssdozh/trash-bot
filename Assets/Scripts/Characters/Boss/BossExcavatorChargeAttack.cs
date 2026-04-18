using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorChargeAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const float AlignFinishAngle = 4f;
        private const float ChargeSkin = 0.05f;
        private const int HitBufferCount = 24;
        private const float InnerSweepRadiusFactor = 0.72f;
        private const float PhaseTwoAlignTimeMult = 0.2f;
        private const float PhaseTwoTelegraphTimeMult = 0.3f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly Collider[] _hitBuffer;
        private readonly HashSet<int> _hitHealthIds;
        private readonly HashSet<int> _comboHitHealthIds;

        private float _alignTimer;
        private float _telegraphTimer;
        private float _dashTimer;
        private float _recoverTimer;
        private float _comboDamageTickTimer;
        private float _spinDirectionSign;
        private Vector3 _chargeDirection;
        private bool _isRunning;
        private bool _isRecovering;
        private bool _isComboSweep;

        public bool IsRunning => _isRunning;

        public BossExcavatorChargeAttack(BossExcavator boss, BossExcavatorConfig config)
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
            _comboHitHealthIds = new HashSet<int>();
            Reset();
        }

        public void Reset()
        {
            _alignTimer = 0f;
            _telegraphTimer = 0f;
            _dashTimer = 0f;
            _recoverTimer = 0f;
            _comboDamageTickTimer = 0f;
            _spinDirectionSign = 1f;
            _chargeDirection = Vector3.forward;
            _isRunning = false;
            _isRecovering = false;
            _isComboSweep = false;
            _hitHealthIds.Clear();
            _comboHitHealthIds.Clear();
        }

        public void StartAttack(bool isComboSweep)
        {
            ValidateDependencies();

            _alignTimer = GetAlignTime();
            _telegraphTimer = GetTelegraphTime();
            _dashTimer = 0f;
            _recoverTimer = GetRecoverTime();
            _comboDamageTickTimer = 0f;
            _spinDirectionSign = 1f;
            _chargeDirection = ResolveTargetDirection();
            _isRunning = true;
            _isRecovering = false;
            _isComboSweep = isComboSweep;
            _hitHealthIds.Clear();
            _comboHitHealthIds.Clear();

            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(true);
            _boss.SetArmLocked(false);
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
        }

        public bool Tick()
        {
            if (_isRunning == false)
            {
                return false;
            }

            if (_alignTimer > 0f)
            {
                TickAlign();

                return true;
            }

            if (_telegraphTimer > 0f)
            {
                TickTelegraph();

                return true;
            }

            if (_dashTimer > 0f)
            {
                TickDash(Time.deltaTime);

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

        public void FixedTick()
        {
            if (_isRunning == false)
            {
                return;
            }

            if (_dashTimer <= 0f)
            {
                return;
            }

            MoveCharge(Time.fixedDeltaTime);
        }

        public void Cancel(bool restoreNeutralPose)
        {
            if (_isRunning == false)
            {
                return;
            }

            _isRunning = false;
            _alignTimer = 0f;
            _telegraphTimer = 0f;
            _dashTimer = 0f;
            _recoverTimer = 0f;
            _comboDamageTickTimer = 0f;
            _isRecovering = false;
            _isComboSweep = false;
            _hitHealthIds.Clear();
            _comboHitHealthIds.Clear();
            _boss.SetAimLocked(false);
            ResetPlanarVelocity();

            if (restoreNeutralPose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
            }
        }

        private void TickAlign()
        {
            _chargeDirection = ResolveTargetDirection();
            RotateBaseTowards(_chargeDirection, Time.deltaTime);

            float angleToCharge = GetAngleToDirection(_chargeDirection);
            _alignTimer = Mathf.Max(0f, _alignTimer - Time.deltaTime);

            if (angleToCharge <= AlignFinishAngle)
            {
                _alignTimer = 0f;
            }

            if (_alignTimer <= 0f)
            {
                BeginTelegraph();
            }
        }

        private void BeginTelegraph()
        {
            _chargeDirection = ResolveChargeDashDirection();
        }

        private void TickTelegraph()
        {
            _chargeDirection = ResolveChargeDashDirection();
            RotateBaseTowards(_chargeDirection, Time.deltaTime);
            _telegraphTimer = Mathf.Max(0f, _telegraphTimer - Time.deltaTime);

            if (_telegraphTimer <= 0f)
            {
                BeginDash();
            }
        }

        private void BeginDash()
        {
            _chargeDirection = ResolveChargeDashDirection();
            _dashTimer = 1f;

            if (_isComboSweep)
            {
                _comboDamageTickTimer = 0f;
                _spinDirectionSign = ResolveSpinDirectionSign();
                SetComboSweepPose();
            }
        }

        private void BeginRecover()
        {
            if (_isRecovering)
            {
                return;
            }

            _isRecovering = true;
            _dashTimer = 0f;
            _boss.SetAimLocked(false);
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
            _boss.Move.InvalidatePath();
            _comboDamageTickTimer = 0f;
            _comboHitHealthIds.Clear();
            ResetPlanarVelocity();
        }

        private void EndAttack()
        {
            _isRunning = false;
            _isRecovering = false;
            _isComboSweep = false;
            _comboDamageTickTimer = 0f;
            _comboHitHealthIds.Clear();
            _boss.SetAimLocked(false);
            ResetPlanarVelocity();
        }

        private void TickDash(float deltaTime)
        {
            if (_isComboSweep == false)
            {
                return;
            }

            RotateCabin(deltaTime);
            TickComboDamage(deltaTime);
        }

        private void MoveCharge(float deltaTime)
        {
            Vector3 moveDirection = _chargeDirection;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude <= MinDirectionSqr)
            {
                BeginRecover();

                return;
            }

            moveDirection.Normalize();
            RotateBaseTowards(moveDirection, deltaTime);

            float stepDistance = GetChargeSpeed() * deltaTime;

            if (stepDistance <= 0f)
            {
                return;
            }

            Vector3 currentPosition = _boss.BaseRigidbody.position;
            Vector3 castOrigin = currentPosition + Vector3.up * _config.ProbeHeight;
            RaycastHit obstacleHit;
            bool isObstacleHit = Physics.SphereCast(
                castOrigin,
                _config.ChargeHitRadius,
                moveDirection,
                out obstacleHit,
                stepDistance + ChargeSkin,
                _boss.Move.ObstacleMask,
                QueryTriggerInteraction.Ignore);

            if (isObstacleHit)
            {
                stepDistance = Mathf.Max(0f, obstacleHit.distance - ChargeSkin);
            }

            if (stepDistance > 0f)
            {
                Vector3 nextPosition = currentPosition + moveDirection * stepDistance;

                _boss.BaseRigidbody.MovePosition(nextPosition);
                ResetPlanarVelocity();
                ApplyChargeDamage(nextPosition, moveDirection);
            }

            if (isObstacleHit)
            {
                BeginRecover();
            }
        }

        private void RotateCabin(float deltaTime)
        {
            Transform cabin = _boss.Cabin;

            if (cabin == null)
            {
                throw new InvalidOperationException(nameof(cabin));
            }

            float spinAngle = GetComboSweepSpinSpeed() * _spinDirectionSign * deltaTime;
            Quaternion spinRotation = Quaternion.AngleAxis(spinAngle, Vector3.up);
            cabin.rotation = spinRotation * cabin.rotation;
        }

        private void ApplyChargeDamage(Vector3 position, Vector3 moveDirection)
        {
            Vector3 hitCenter = position + moveDirection * _config.ChargeHitOffset;
            int hitCount = Physics.OverlapSphereNonAlloc(
                hitCenter,
                _config.ChargeHitRadius,
                _hitBuffer,
                _config.ChargeHitMask,
                QueryTriggerInteraction.Ignore);
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
                hitHealth.Decrease(_config.ChargeHitDamage * GetPhaseDamageMult());
            }
        }

        private void TickComboDamage(float deltaTime)
        {
            float interval = Mathf.Max(_config.SweepDamageInterval / GetPhaseAttackSpeedMult(), 0.05f);
            _comboDamageTickTimer -= deltaTime;

            while (_comboDamageTickTimer <= 0f)
            {
                ApplyComboDamagePulse();
                _comboDamageTickTimer += interval;
            }
        }

        private void ApplyComboDamagePulse()
        {
            Transform bucket = _boss.Bucket;

            if (bucket == null)
            {
                throw new InvalidOperationException(nameof(bucket));
            }

            Vector3 hitForward = ResolveSweepHitForward();
            Vector3 outerHitCenter = bucket.position + hitForward * _config.SweepHitOffset;
            Vector3 innerHitCenter = ResolveComboInnerHitCenter(outerHitCenter);
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                innerHitCenter,
                outerHitCenter,
                _config.SweepHitRadius,
                _hitBuffer,
                _config.BucketHitMask,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
            {
                return;
            }

            _comboHitHealthIds.Clear();

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

                if (_comboHitHealthIds.Contains(healthId))
                {
                    continue;
                }

                _comboHitHealthIds.Add(healthId);
                hitHealth.Decrease(_config.SweepHitDamage * GetPhaseDamageMult());
            }
        }

        private Vector3 ResolveComboInnerHitCenter(Vector3 outerHitCenter)
        {
            Transform cabin = _boss.Cabin;

            if (cabin == null)
            {
                return outerHitCenter;
            }

            Vector3 innerHitCenter = cabin.position;
            innerHitCenter.y = outerHitCenter.y;

            Vector3 toOuter = outerHitCenter - innerHitCenter;
            toOuter.y = 0f;

            if (toOuter.sqrMagnitude <= MinDirectionSqr)
            {
                return outerHitCenter;
            }

            return innerHitCenter + toOuter * InnerSweepRadiusFactor;
        }

        private Vector3 ResolveTargetDirection()
        {
            Transform target = _boss.Target;
            Transform baseTransform = _boss.Base;

            if (target != null && baseTransform != null)
            {
                Vector3 targetDirection = target.position - baseTransform.position;
                targetDirection.y = 0f;

                if (targetDirection.sqrMagnitude > MinDirectionSqr)
                {
                    return targetDirection.normalized;
                }
            }

            Vector3 moveDirection = ResolveMovementDirection();

            if (moveDirection.sqrMagnitude > MinDirectionSqr)
            {
                return moveDirection;
            }

            return GetBaseForward();
        }

        private Vector3 ResolveChargeDashDirection()
        {
            Vector3 targetDirection = ResolveTargetDirection();

            if (targetDirection.sqrMagnitude > MinDirectionSqr)
            {
                return targetDirection;
            }

            Vector3 moveDirection = ResolveMovementDirection();

            if (moveDirection.sqrMagnitude > MinDirectionSqr)
            {
                return moveDirection;
            }

            return GetBaseForward();
        }

        private Vector3 ResolveMovementDirection()
        {
            Vector3 moveDirection = _boss.Move.CurrentMoveDirection;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return Vector3.zero;
            }

            return moveDirection.normalized;
        }

        private Vector3 GetBaseForward()
        {
            Transform baseTransform = _boss.Base;

            if (baseTransform == null)
            {
                return Vector3.forward;
            }

            Vector3 baseForward = baseTransform.forward;
            baseForward.y = 0f;

            if (baseForward.sqrMagnitude <= MinDirectionSqr)
            {
                return Vector3.forward;
            }

            return baseForward.normalized;
        }

        private Vector3 ResolveSweepHitForward()
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

            return GetBaseForward();
        }

        private float GetAngleToDirection(Vector3 direction)
        {
            Vector3 baseForward = GetBaseForward();

            if (direction.sqrMagnitude <= MinDirectionSqr)
            {
                return 0f;
            }

            return Vector3.Angle(baseForward, direction.normalized);
        }

        private void RotateBaseTowards(Vector3 direction, float deltaTime)
        {
            Vector3 planarDirection = direction;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return;
            }

            Quaternion currentRotation = _boss.BaseRigidbody.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(
                currentRotation,
                targetRotation,
                _config.BaseTurnSpeed * GetPhaseAttackSpeedMult() * deltaTime);

            _boss.BaseRigidbody.MoveRotation(nextRotation);
        }

        private void ResetPlanarVelocity()
        {
            Vector3 currentVelocity = _boss.BaseRigidbody.linearVelocity;
            currentVelocity.x = 0f;
            currentVelocity.z = 0f;
            _boss.BaseRigidbody.linearVelocity = currentVelocity;
        }

        private void SetComboSweepPose()
        {
            _boss.SetArmPose(
                _config.ArmSweepBoomEuler,
                _config.ArmSweepStickEuler,
                _config.ArmSweepBucketEuler,
                GetAttackPoseSpeedMult());
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
            if (_boss.Base == null)
            {
                throw new InvalidOperationException(nameof(_boss.Base));
            }

            if (_boss.BaseRigidbody == null)
            {
                throw new InvalidOperationException(nameof(_boss.BaseRigidbody));
            }

            if (_boss.Move == null)
            {
                throw new InvalidOperationException(nameof(_boss.Move));
            }
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

        private float GetChargeSpeed()
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.ChargeSpeed * _config.PhaseTwoChargeSpeedMult;
            }

            return _config.ChargeSpeed;
        }

        private float GetAlignTime()
        {
            float alignTime = _config.ChargeAlignTime / GetPhaseAttackSpeedMult();

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                alignTime *= PhaseTwoAlignTimeMult;
            }

            return alignTime;
        }

        private float GetTelegraphTime()
        {
            float telegraphTime = _config.ChargeTelegraphTime / GetPhaseAttackSpeedMult();

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                telegraphTime *= PhaseTwoTelegraphTimeMult;
            }

            return telegraphTime;
        }

        private float GetRecoverTime()
        {
            return _config.ChargeRecoveryTime / GetPhaseAttackSpeedMult();
        }

        private float GetComboSweepSpinSpeed()
        {
            float spinSpeed = _config.SweepSpinSpeed;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                spinSpeed *= _config.PhaseTwoSweepSpinSpeedMult;
                spinSpeed *= _config.PhaseTwoComboSweepSpinSpeedMult;
            }

            return spinSpeed;
        }
    }
}
