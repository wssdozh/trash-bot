using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavator
    {
        private void DrawBucketAttackGizmos()
        {
            if (_bucket == null)
            {
                return;
            }

            if (_base == null)
            {
                return;
            }

            Vector3 strikeForward = ResolveAttackForward();
            Vector3 hitCenter = ResolveImpactCenter(_bucket.position, strikeForward, _config.BucketHitOffset, _config.BucketHitRadius);
            Vector3 shockwaveCenter = ResolveImpactCenter(hitCenter, strikeForward, _config.BucketShockwaveOffset, _config.BucketShockwaveRadius);

            Gizmos.color = new Color(1f, 0.22f, 0.12f, 0.95f);
            Gizmos.DrawLine(_bucket.position, hitCenter);
            Gizmos.DrawWireSphere(hitCenter, _config.BucketHitRadius);
            DrawAttackSector(hitCenter, strikeForward, _config.BucketHitRadius, _config.BucketHitAngle);

            Gizmos.color = new Color(1f, 0.66f, 0.18f, 0.9f);
            Gizmos.DrawLine(hitCenter, shockwaveCenter);
            Gizmos.DrawWireSphere(shockwaveCenter, _config.BucketShockwaveRadius);
        }

        private void DrawSweepAttackGizmos()
        {
            if (_bucket == null)
            {
                return;
            }

            if (_cabin == null)
            {
                return;
            }

            Vector3 sweepForward = ResolveAttackForward();
            Vector3 sweepCenter = _bucket.position + sweepForward * _config.SweepHitOffset;
            Vector3 innerSweepCenter = _cabin.position;
            innerSweepCenter.y = sweepCenter.y;
            Vector3 toOuter = sweepCenter - innerSweepCenter;
            toOuter.y = 0f;

            if (toOuter.sqrMagnitude > 0.0001f)
            {
                innerSweepCenter += toOuter * 0.72f;
            }

            Gizmos.color = new Color(0.2f, 1f, 1f, 0.9f);
            Gizmos.DrawLine(innerSweepCenter, sweepCenter);
            Gizmos.DrawWireSphere(innerSweepCenter, _config.SweepHitRadius);
            Gizmos.DrawWireSphere(sweepCenter, _config.SweepHitRadius);
        }

        private void DrawChargeAttackGizmos()
        {
            if (_base == null)
            {
                return;
            }

            Vector3 chargeDirection = GetPlanarDirection(_base.forward);

            if (chargeDirection.sqrMagnitude <= 0.0001f)
            {
                chargeDirection = Vector3.forward;
            }

            Vector3 basePosition = _base.position;
            Vector3 hitCenter = basePosition + chargeDirection * _config.ChargeHitOffset;
            float chargeDistance = GetCurrentChargeSpeed() * GetCurrentChargeAttackTime();
            Vector3 chargeEndPoint = basePosition + chargeDirection * chargeDistance;

            Gizmos.color = new Color(1f, 0.1f, 0.85f, 0.9f);
            Gizmos.DrawLine(basePosition, chargeEndPoint);
            Gizmos.DrawWireSphere(hitCenter, _config.ChargeHitRadius);
            Gizmos.DrawWireSphere(chargeEndPoint, 0.22f);
        }

        private void DrawThrowAttackGizmos()
        {
            if (_bucket == null)
            {
                return;
            }

            if (_base == null)
            {
                return;
            }

            Vector3 launchForward = ResolveAttackForward();
            Vector3 spawnPosition = ResolveThrowSpawnPosition(_bucket.position, launchForward);
            int projectileCount = GetCurrentThrowProjectileCount();
            int projectileIndex = 0;

            Gizmos.color = new Color(0.35f, 1f, 0.35f, 0.92f);
            Gizmos.DrawWireSphere(spawnPosition, 0.18f);

            while (projectileIndex < projectileCount)
            {
                float angleOffset = GetThrowAngleOffset(projectileIndex, projectileCount);
                Vector3 projectileDirection = Quaternion.Euler(0f, angleOffset, 0f) * launchForward.normalized;
                Vector3 previewPoint = spawnPosition + projectileDirection * 3.5f;

                Gizmos.DrawLine(spawnPosition, previewPoint);
                Gizmos.DrawWireSphere(previewPoint, 0.1f);
                projectileIndex += 1;
            }
        }

        private void DrawAttackSector(Vector3 origin, Vector3 forward, float radius, float angle)
        {
            Vector3 planarForward = GetPlanarDirection(forward);

            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float halfAngle = angle * 0.5f;
            Vector3 leftDirection = Quaternion.AngleAxis(halfAngle, Vector3.up) * planarForward;
            Vector3 rightDirection = Quaternion.AngleAxis(halfAngle * -1f, Vector3.up) * planarForward;

            Gizmos.DrawLine(origin, origin + planarForward * radius);
            Gizmos.DrawLine(origin, origin + leftDirection * radius);
            Gizmos.DrawLine(origin, origin + rightDirection * radius);
        }

        private Vector3 ResolveAttackForward()
        {
            if (_bucket != null)
            {
                Vector3 bucketForward = GetPlanarDirection(_bucket.forward);

                if (bucketForward.sqrMagnitude > 0.0001f)
                {
                    return bucketForward;
                }
            }

            if (_cabin != null)
            {
                Vector3 cabinForward = GetPlanarDirection(_cabin.forward);

                if (cabinForward.sqrMagnitude > 0.0001f)
                {
                    return cabinForward;
                }
            }

            if (_base != null)
            {
                Vector3 baseForward = GetPlanarDirection(_base.forward);

                if (baseForward.sqrMagnitude > 0.0001f)
                {
                    return baseForward;
                }
            }

            return Vector3.forward;
        }

        private Vector3 ResolveImpactCenter(Vector3 origin, Vector3 forward, float offset, float radius)
        {
            Vector3 impactCenter = origin + forward * offset;
            float impactHeight = _base.position.y + Mathf.Min(radius * 0.3f, 0.8f);
            impactCenter.y = impactHeight;

            return impactCenter;
        }

        private Vector3 ResolveThrowSpawnPosition(Vector3 bucketPosition, Vector3 launchForward)
        {
            Vector3 spawnPosition = bucketPosition + launchForward * (_config.ThrowSpawnOffset + 0.55f);
            float minSpawnHeight = _base.position.y + 0.75f;

            if (spawnPosition.y < minSpawnHeight)
            {
                spawnPosition.y = minSpawnHeight;
            }

            return spawnPosition;
        }

        private float GetThrowAngleOffset(int projectileIndex, int projectileCount)
        {
            if (projectileCount <= 1)
            {
                return 0f;
            }

            float spreadAngle = GetCurrentThrowProjectileSpreadAngle();
            float step = spreadAngle / (projectileCount - 1);
            float minAngle = spreadAngle * -0.5f;

            return minAngle + step * projectileIndex;
        }

        private Vector3 GetPlanarDirection(Vector3 direction)
        {
            Vector3 planarDirection = direction;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return planarDirection.normalized;
        }

        private float GetCurrentChargeSpeed()
        {
            return _config.ChargeSpeed * GetPhaseChargeSpeedMult();
        }

        private float GetCurrentChargeAttackTime()
        {
            return _config.ChargeAttackTime / GetPhaseAttackSpeedMult();
        }

        private int GetCurrentThrowProjectileCount()
        {
            return GetPhaseThrowProjectileCount();
        }

        private float GetCurrentThrowProjectileSpreadAngle()
        {
            return GetPhaseThrowProjectileSpreadAngle();
        }
    }
}
