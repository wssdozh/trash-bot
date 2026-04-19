using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorChargeAttack
    {
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

            float stepDistance = GetChargeSpeed() * GetChargeDriveSpeedFactor(moveDirection) * deltaTime;

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
                Vector3 catchStopPosition;

                if (TryResolveTargetCatchStop(currentPosition, moveDirection, stepDistance, out catchStopPosition))
                {
                    MoveChargeTo(catchStopPosition, moveDirection);
                    BeginRecover();

                    return;
                }

                Vector3 nextPosition = currentPosition + moveDirection * stepDistance;

                MoveChargeTo(nextPosition, moveDirection);
            }

            if (isObstacleHit)
            {
                BeginRecover();
            }
        }

        private bool TryResolveTargetCatchStop(Vector3 currentPosition, Vector3 moveDirection, float stepDistance, out Vector3 stopPosition)
        {
            stopPosition = currentPosition;

            Transform target = _boss.Target;

            if (target == null)
            {
                return false;
            }

            Vector3 targetPoint = target.position;
            targetPoint.y = currentPosition.y;
            Vector3 toTarget = targetPoint - currentPosition;
            float forwardDistance = Vector3.Dot(toTarget, moveDirection);

            if (forwardDistance < 0f)
            {
                return false;
            }

            if (forwardDistance > stepDistance + ChargeSideCatchForwardPadding)
            {
                return false;
            }

            float closestDistanceAlongPath = Mathf.Clamp(forwardDistance, 0f, stepDistance);
            Vector3 closestPoint = currentPosition + moveDirection * closestDistanceAlongPath;
            float catchRadius = _config.ChargeHitRadius + ChargeCatchRadiusPadding;
            float lateralDistance = Vector3.Distance(closestPoint, targetPoint);
            float minSideCatchDistance = _config.ChargeHitRadius * ChargeSideCatchMinLateralFactor;

            if (lateralDistance > catchRadius)
            {
                return false;
            }

            if (lateralDistance < minSideCatchDistance)
            {
                return false;
            }

            stopPosition = closestPoint;

            return true;
        }

        private void MoveChargeTo(Vector3 nextPosition, Vector3 moveDirection)
        {
            _boss.BaseRigidbody.MovePosition(nextPosition);
            ResetPlanarVelocity();
            ApplyChargeDamage(nextPosition, moveDirection);
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

                if (_boss.IsFriendlyMinion(hitHealth))
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

                if (_boss.IsFriendlyMinion(hitHealth))
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
    }
}
