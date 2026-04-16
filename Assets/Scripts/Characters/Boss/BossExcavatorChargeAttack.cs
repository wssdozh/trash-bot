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

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly Collider[] _hitBuffer;
        private readonly HashSet<int> _hitHealthIds;

        private float _alignTimer;
        private float _telegraphTimer;
        private float _dashTimer;
        private float _recoverTimer;
        private Vector3 _chargeDirection;
        private bool _isRunning;
        private bool _isRecovering;

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
            Reset();
        }

        public void Reset()
        {
            _alignTimer = 0f;
            _telegraphTimer = 0f;
            _dashTimer = 0f;
            _recoverTimer = 0f;
            _chargeDirection = Vector3.forward;
            _isRunning = false;
            _isRecovering = false;
            _hitHealthIds.Clear();
        }

        public void StartAttack()
        {
            ValidateDependencies();

            _alignTimer = _config.ChargeAlignTime;
            _telegraphTimer = _config.ChargeTelegraphTime;
            _dashTimer = _config.ChargeAttackTime;
            _recoverTimer = _config.ChargeRecoveryTime;
            _chargeDirection = ResolveTargetDirection();
            _isRunning = true;
            _isRecovering = false;
            _hitHealthIds.Clear();

            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(true);
            _boss.SetArmLocked(false);
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, _config.AttackPoseSpeedMult);
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
                _dashTimer = Mathf.Max(0f, _dashTimer - Time.deltaTime);

                if (_dashTimer <= 0f)
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
            _isRecovering = false;
            _hitHealthIds.Clear();
            _boss.SetAimLocked(false);

            if (restoreNeutralPose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, _config.AttackPoseSpeedMult);
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
            _chargeDirection = GetBaseForward();

            if (_chargeDirection.sqrMagnitude <= MinDirectionSqr)
            {
                _chargeDirection = ResolveTargetDirection();
            }
        }

        private void TickTelegraph()
        {
            RotateBaseTowards(_chargeDirection, Time.deltaTime);
            _telegraphTimer = Mathf.Max(0f, _telegraphTimer - Time.deltaTime);

            if (_telegraphTimer <= 0f)
            {
                BeginDash();
            }
        }

        private void BeginDash()
        {
            _chargeDirection = GetBaseForward();

            if (_chargeDirection.sqrMagnitude <= MinDirectionSqr)
            {
                _chargeDirection = ResolveTargetDirection();
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
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, _config.AttackPoseSpeedMult);
            _boss.Move.InvalidatePath();
        }

        private void EndAttack()
        {
            _isRunning = false;
            _isRecovering = false;
            _boss.SetAimLocked(false);
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

            float stepDistance = _config.ChargeSpeed * deltaTime;

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
                ApplyChargeDamage(nextPosition, moveDirection);
            }

            if (isObstacleHit)
            {
                BeginRecover();
            }
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
                hitHealth.Decrease(_config.ChargeHitDamage);
            }
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

            return GetBaseForward();
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
                _config.BaseTurnSpeed * deltaTime);

            _boss.BaseRigidbody.MoveRotation(nextRotation);
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
    }
}
