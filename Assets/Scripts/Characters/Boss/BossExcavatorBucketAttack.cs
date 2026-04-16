using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorBucketAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const int HitBufferCount = 20;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly Collider[] _hitBuffer;

        private float _telegraphTimer;
        private float _strikeTimer;
        private float _recoverTimer;
        private Vector3 _strikeForward;
        private bool _isRunning;
        private bool _isHitApplied;

        public bool IsRunning => _isRunning;

        public float Duration => _config.BucketPrepareTime + _config.BucketStrikeTime + _config.AttackRecoveryTime;

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
            Reset();
        }

        public void Reset()
        {
            _telegraphTimer = 0f;
            _strikeTimer = 0f;
            _recoverTimer = 0f;
            _isRunning = false;
            _isHitApplied = false;
            _strikeForward = Vector3.forward;
        }

        public void StartAttack()
        {
            ValidateDependencies();

            _telegraphTimer = _config.BucketPrepareTime;
            _strikeTimer = _config.BucketStrikeTime;
            _recoverTimer = _config.AttackRecoveryTime;
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
                if (_isHitApplied == false)
                {
                    TryApplyHit();
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
            _boss.SetAimLocked(false);

            if (restoreNeutralPose)
            {
                SetRecoverPose();
            }
        }

        private void BeginStrike()
        {
            _boss.SetAimLocked(false);
            _strikeForward = ResolveStrikeForward();
            SetStrikePose();
            TryApplyHit();
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

        private void SetTelegraphPose()
        {
            _boss.SetArmPose(
                _config.ArmBucketPrepareBoomEuler,
                _config.ArmBucketPrepareStickEuler,
                _config.ArmBucketPrepareBucketEuler,
                _config.BucketPrepareSpeedMult);
        }

        private void SetStrikePose()
        {
            _boss.SetArmPose(
                _config.ArmBucketStrikeBoomEuler,
                _config.ArmBucketStrikeStickEuler,
                _config.ArmBucketStrikeBucketEuler,
                _config.BucketStrikeSpeedMult);
        }

        private void SetRecoverPose()
        {
            _boss.SetArmPose(
                _config.ArmNeutralBoomEuler,
                _config.ArmNeutralStickEuler,
                _config.ArmNeutralBucketEuler,
                _config.BucketRecoverSpeedMult);
        }

        private void TryApplyHit()
        {
            if (_isHitApplied)
            {
                return;
            }

            _isHitApplied = true;

            Transform bucket = _boss.Bucket;

            if (bucket == null)
            {
                throw new InvalidOperationException(nameof(bucket));
            }

            Vector3 strikeForward = _strikeForward;

            if (strikeForward.sqrMagnitude <= MinDirectionSqr)
            {
                strikeForward = ResolveStrikeForward();
            }

            Vector3 hitCenter = bucket.position + strikeForward * _config.BucketHitOffset;

            int hitCount = Physics.OverlapSphereNonAlloc(
                hitCenter,
                _config.BucketHitRadius,
                _hitBuffer,
                _config.BucketHitMask,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
            {
                return;
            }

            Health nearestHealth = null;
            float nearestDistance = float.MaxValue;
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

                Vector3 targetPoint = hitHealth.transform.position;

                if (IsInsideHitSector(hitCenter, strikeForward, targetPoint) == false)
                {
                    continue;
                }

                float targetDistance = Vector3.Distance(hitCenter, targetPoint);

                if (targetDistance < nearestDistance)
                {
                    nearestDistance = targetDistance;
                    nearestHealth = hitHealth;
                }
            }

            if (nearestHealth == null)
            {
                return;
            }

            nearestHealth.Decrease(_config.BucketHitDamage);
        }

        private bool IsInsideHitSector(Vector3 hitCenter, Vector3 strikeForward, Vector3 targetPoint)
        {
            Vector3 directionToTarget = targetPoint - hitCenter;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude <= MinDirectionSqr)
            {
                return true;
            }

            Vector3 planarForward = strikeForward;
            planarForward.y = 0f;

            if (planarForward.sqrMagnitude <= MinDirectionSqr)
            {
                return true;
            }

            float halfAngle = _config.BucketHitAngle * 0.5f;
            float targetAngle = Vector3.Angle(planarForward.normalized, directionToTarget.normalized);

            if (targetAngle > halfAngle)
            {
                return false;
            }

            return true;
        }

        private Vector3 ResolveStrikeForward()
        {
            Transform bucket = _boss.Bucket;
            Transform target = _boss.Target;

            if (bucket != null && target != null)
            {
                Vector3 targetDirection = target.position - bucket.position;
                targetDirection.y = 0f;

                if (targetDirection.sqrMagnitude > MinDirectionSqr)
                {
                    return targetDirection.normalized;
                }
            }

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

        private void ValidateDependencies()
        {
            if (_boss.Bucket == null)
            {
                throw new InvalidOperationException(nameof(_boss.Bucket));
            }

            if (_boss.Boom == null)
            {
                throw new InvalidOperationException(nameof(_boss.Boom));
            }

            if (_boss.Stick == null)
            {
                throw new InvalidOperationException(nameof(_boss.Stick));
            }
        }
    }
}
