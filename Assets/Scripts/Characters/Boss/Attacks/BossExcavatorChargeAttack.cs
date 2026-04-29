using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorChargeAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const float AlignFinishAngle = 4f;
        private const float ChargeSkin = 0.05f;
        private const float ChargeCatchRadiusPadding = 1.55f;
        private const float ChargeSideCatchMinLateralFactor = 0.35f;
        private const float ChargeSideCatchForwardPadding = 0.65f;
        private const int HitBufferCount = 24;
        private const float InnerSweepRadiusFactor = 0.72f;
        private const float PhaseTwoAlignTimeMult = 0.2f;
        private const float PhaseTwoTelegraphTimeMult = 0.3f;
        private const float PhaseTwoCabinSpinSlowdown = 1.5f;
        private const float ChargeDriveStartAngle = 6f;
        private const float ChargeDriveStopAngle = 28f;
        private const float ChargeTurnSpeedMult = 1.6f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly Collider[] _hitBuffer;
        private readonly HashSet<int> _hitHealthIds;
        private readonly HashSet<int> _comboHitHealthIds;

        private float _alignTimer;
        private float _telegraphTimer;
        private float _recoverTimer;
        private float _comboDamageTickTimer;
        private float _spinDirectionSign;
        private Vector3 _chargeDirection;
        private RigidbodyConstraints _baseConstraints;
        private bool _hasBaseConstraints;
        private bool _isDashActive;
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
            _recoverTimer = 0f;
            _comboDamageTickTimer = 0f;
            _spinDirectionSign = 1f;
            _chargeDirection = Vector3.forward;
            _baseConstraints = RigidbodyConstraints.None;
            _hasBaseConstraints = false;
            _isDashActive = false;
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
            _recoverTimer = GetRecoverTime();
            _comboDamageTickTimer = 0f;
            _spinDirectionSign = 1f;
            _chargeDirection = ResolveTargetDirection();
            _isDashActive = false;
            _isRunning = true;
            _isRecovering = false;
            _isComboSweep = isComboSweep;
            _hitHealthIds.Clear();
            _comboHitHealthIds.Clear();
            CaptureBaseConstraints();

            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(true);
            _boss.SetArmLocked(false);
            _boss.SetArmPose(BossExcavatorArmPose.ChargeBrace, GetAttackPoseSpeedMult());
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

            if (_isDashActive)
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

            if (_alignTimer > 0f)
            {
                RotateBaseTowards(_chargeDirection, Time.fixedDeltaTime);

                return;
            }

            if (_telegraphTimer > 0f)
            {
                RotateBaseTowards(_chargeDirection, Time.fixedDeltaTime);

                return;
            }

            if (_isDashActive == false)
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
            _recoverTimer = 0f;
            _comboDamageTickTimer = 0f;
            _isDashActive = false;
            _isRecovering = false;
            _isComboSweep = false;
            _hitHealthIds.Clear();
            _comboHitHealthIds.Clear();
            _boss.SetAimLocked(false);
            RestoreBaseConstraints();
            ResetPlanarVelocity();

            if (restoreNeutralPose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
            }
        }

        private void TickAlign()
        {
            _chargeDirection = ResolveTargetDirection();

            float angleToCharge = GetChargeFacingAngle(_chargeDirection);
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
            _telegraphTimer = Mathf.Max(0f, _telegraphTimer - Time.deltaTime);

            if (_telegraphTimer <= 0f)
            {
                BeginDash();
            }
        }

        private void BeginDash()
        {
            _chargeDirection = ResolveChargeDashDirection();
            _isDashActive = true;
            _boss.NotifyChargeDashed();

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
            _isDashActive = false;
            _boss.SetAimLocked(false);
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
            _boss.Move.InvalidatePath();
            _comboDamageTickTimer = 0f;
            _comboHitHealthIds.Clear();
            RestoreBaseConstraints();
            ResetPlanarVelocity();
        }

        private void EndAttack()
        {
            _isRunning = false;
            _isRecovering = false;
            _isComboSweep = false;
            _comboDamageTickTimer = 0f;
            _comboHitHealthIds.Clear();
            _isDashActive = false;
            _boss.SetAimLocked(false);
            RestoreBaseConstraints();
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

        private float GetChargeFacingAngle(Vector3 direction)
        {
            return GetAngleToDirection(direction);
        }

        private void RotateBaseTowards(Vector3 direction, float deltaTime)
        {
            float turnSpeed = _config.BaseTurnSpeed * ChargeTurnSpeedMult * GetPhaseAttackSpeedMult();
            _boss.Move.RotateBaseTowardsDirection(direction, turnSpeed);
        }

        private float GetChargeDriveSpeedFactor(Vector3 moveDirection)
        {
            float facingAngle = GetChargeFacingAngle(moveDirection);
            float driveBlend = Mathf.InverseLerp(ChargeDriveStartAngle, ChargeDriveStopAngle, facingAngle);

            return 1f - driveBlend;
        }

        private void ResetPlanarVelocity()
        {
            Vector3 currentVelocity = _boss.BaseRigidbody.linearVelocity;
            currentVelocity.x = 0f;
            currentVelocity.z = 0f;
            _boss.BaseRigidbody.linearVelocity = currentVelocity;
        }

        private void CaptureBaseConstraints()
        {
            if (_hasBaseConstraints)
            {
                return;
            }

            _baseConstraints = _boss.BaseRigidbody.constraints;
            RigidbodyConstraints constraints = _baseConstraints;
            constraints |= RigidbodyConstraints.FreezeRotationX;
            constraints |= RigidbodyConstraints.FreezeRotationZ;
            constraints &= ~RigidbodyConstraints.FreezeRotationY;
            _boss.BaseRigidbody.constraints = constraints;
            _hasBaseConstraints = true;
        }

        private void RestoreBaseConstraints()
        {
            if (_hasBaseConstraints == false)
            {
                return;
            }

            _boss.BaseRigidbody.constraints = _baseConstraints;
            _hasBaseConstraints = false;
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
            return _boss.GetPhaseAttackSpeedMult();
        }

        private float GetPhaseDamageMult()
        {
            return _boss.GetPhaseDamageMult();
        }

        private float GetAttackPoseSpeedMult()
        {
            return _config.AttackPoseSpeedMult * GetPhaseAttackSpeedMult();
        }

        private float GetChargeSpeed()
        {
            return _config.ChargeSpeed * _boss.GetPhaseChargeSpeedMult();
        }

        private float GetAlignTime()
        {
            float alignTime = _config.ChargeAlignTime / GetPhaseAttackSpeedMult();

            if (_boss.IsAdvancedPhase)
            {
                alignTime *= PhaseTwoAlignTimeMult;
            }

            return alignTime;
        }

        private float GetTelegraphTime()
        {
            float telegraphTime = _config.ChargeTelegraphTime / GetPhaseAttackSpeedMult();

            if (_boss.IsAdvancedPhase)
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

            if (_boss.IsAdvancedPhase)
            {
                spinSpeed *= _boss.GetPhaseSweepSpinSpeedMult();
                spinSpeed *= _boss.GetPhaseComboSweepSpinSpeedMult();
                spinSpeed /= PhaseTwoCabinSpinSlowdown;
            }

            return spinSpeed;
        }
    }
}
