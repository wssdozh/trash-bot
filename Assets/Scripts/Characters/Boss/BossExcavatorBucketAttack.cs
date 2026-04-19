using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorBucketAttack
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

        private void TryApplyHit()
        {
            if (_isHitApplied)
            {
                return;
            }

            _isHitApplied = true;
            BeginHitStop();
            _damagedHealthIds.Clear();

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

            Vector3 hitCenter = ResolveImpactCenter(bucket.position, strikeForward, _config.BucketHitOffset, _config.BucketHitRadius);

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

                if (_boss.IsFriendlyMinion(hitHealth))
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
                ApplyShockwave(hitCenter);
                TrySpawnPhaseTwoMegaTrench(bucket.position, strikeForward);

                return;
            }

            _damagedHealthIds.Add(nearestHealth.GetInstanceID());
            nearestHealth.Decrease(_config.BucketHitDamage * GetPhaseDamageMult());
            ApplyShockwave(hitCenter);
            TrySpawnPhaseTwoMegaTrench(bucket.position, strikeForward);
        }

        private void BeginHitStop()
        {
            _hitStopTimer = HitStopDuration;
            _boss.SetArmLocked(true);
        }

        private void TickHitStop()
        {
            _hitStopTimer = Mathf.Max(0f, _hitStopTimer - Time.deltaTime);

            if (_hitStopTimer <= 0f)
            {
                _boss.SetArmLocked(false);
            }
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

        private void ApplyShockwave(Vector3 hitCenter)
        {
            Vector3 shockwaveCenter = ResolveImpactCenter(hitCenter, _strikeForward, _config.BucketShockwaveOffset, _config.BucketShockwaveRadius);
            int hitCount = Physics.OverlapSphereNonAlloc(
                shockwaveCenter,
                _config.BucketShockwaveRadius,
                _hitBuffer,
                _config.BucketHitMask,
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

                if (_damagedHealthIds.Contains(healthId))
                {
                    continue;
                }

                _damagedHealthIds.Add(healthId);
                hitHealth.Decrease(_config.BucketShockwaveDamage * GetPhaseDamageMult());
            }
        }

        private void TrySpawnPhaseTwoMegaTrench(Vector3 bucketPosition, Vector3 strikeForward)
        {
            if (_boss.IsAdvancedPhase == false)
            {
                return;
            }

            Vector3 trenchDirection = ResolveMegaTrenchDirection(strikeForward);

            if (trenchDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return;
            }

            ValidateScrapTrailDependencies();

            Vector3 startPoint;

            if (TryResolveGroundPoint(bucketPosition, out startPoint) == false)
            {
                return;
            }

            Vector3 endPoint = ResolveRoomEdgePoint(startPoint, trenchDirection);
            float trenchLength = Vector3.Distance(startPoint, endPoint);

            if (trenchLength <= 0f)
            {
                return;
            }

            float spawnSpacing = Mathf.Max(0.1f, _config.ScrapTrailSpawnSpacing);
            int spawnCount = Mathf.Max(1, Mathf.CeilToInt(trenchLength / spawnSpacing));
            int spawnIndex = 0;

            while (spawnIndex <= spawnCount)
            {
                float progress = spawnCount > 0 ? (float)spawnIndex / spawnCount : 0f;
                Vector3 samplePoint = Vector3.Lerp(startPoint, endPoint, progress);
                Vector3 spawnPoint;

                if (TryResolveGroundPoint(samplePoint, out spawnPoint))
                {
                    SpawnTrenchBlock(spawnPoint);
                }

                spawnIndex += 1;
            }
        }

        private void SpawnTrenchBlock(Vector3 spawnPoint)
        {
            Quaternion spawnRotation = Quaternion.Euler(
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f));

            _scrapTrailBlockSpawner.Spawn(
                spawnPoint,
                spawnRotation,
                _config.ScrapTrailBlockSize,
                _config.ScrapTrailBlockLifetime,
                _boss.Move.BodyColliders);
        }

        private Vector3 ResolveMegaTrenchDirection(Vector3 strikeForward)
        {
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

            Vector3 planarStrikeForward = strikeForward;
            planarStrikeForward.y = 0f;

            if (planarStrikeForward.sqrMagnitude > MinDirectionSqr)
            {
                return planarStrikeForward.normalized;
            }

            return Vector3.zero;
        }

        private bool TryResolveGroundPoint(Vector3 samplePoint, out Vector3 groundPoint)
        {
            Vector3 rayOrigin = samplePoint + Vector3.up * _config.ScrapTrailGroundProbeHeight;
            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                _groundHitBuffer,
                _config.ScrapTrailGroundProbeDistance,
                _config.ScrapTrailGroundMask,
                QueryTriggerInteraction.Ignore);
            int hitIndex = 0;

            while (hitIndex < hitCount)
            {
                RaycastHit groundHit = _groundHitBuffer[hitIndex];
                hitIndex += 1;

                if (groundHit.collider == null)
                {
                    continue;
                }

                if (groundHit.collider.transform.IsChildOf(_boss.transform))
                {
                    continue;
                }

                groundPoint = groundHit.point;

                return true;
            }

            groundPoint = samplePoint;
            groundPoint.y = _boss.Base.position.y;

            return true;
        }

        private Vector3 ResolveRoomEdgePoint(Vector3 startPoint, Vector3 strikeForward)
        {
            RoomRuntimeState roomRuntimeState = _boss.Base.GetComponentInParent<RoomRuntimeState>();

            if (roomRuntimeState == null)
            {
                return startPoint + strikeForward.normalized * _config.ScrapTrailMaxDistance;
            }

            Bounds roomBounds = roomRuntimeState.GetRoomBounds();
            Vector3 planarDirection = strikeForward.normalized;
            float bestDistance = float.MaxValue;

            TryUpdateEdgeDistance(startPoint.x, planarDirection.x, roomBounds.min.x, startPoint.z, planarDirection.z, roomBounds.min.z, roomBounds.max.z, ref bestDistance);
            TryUpdateEdgeDistance(startPoint.x, planarDirection.x, roomBounds.max.x, startPoint.z, planarDirection.z, roomBounds.min.z, roomBounds.max.z, ref bestDistance);
            TryUpdateEdgeDistance(startPoint.z, planarDirection.z, roomBounds.min.z, startPoint.x, planarDirection.x, roomBounds.min.x, roomBounds.max.x, ref bestDistance);
            TryUpdateEdgeDistance(startPoint.z, planarDirection.z, roomBounds.max.z, startPoint.x, planarDirection.x, roomBounds.min.x, roomBounds.max.x, ref bestDistance);

            if (bestDistance == float.MaxValue)
            {
                return roomBounds.ClosestPoint(startPoint + planarDirection * Mathf.Max(roomBounds.size.x, roomBounds.size.z));
            }

            Vector3 edgePoint = startPoint + planarDirection * bestDistance;
            edgePoint.y = startPoint.y;

            return edgePoint;
        }

        private void TryUpdateEdgeDistance(
            float originAxis,
            float directionAxis,
            float planeAxis,
            float originOtherAxis,
            float directionOtherAxis,
            float minOtherAxis,
            float maxOtherAxis,
            ref float bestDistance)
        {
            if (Mathf.Abs(directionAxis) <= MinDirectionSqr)
            {
                return;
            }

            float distance = (planeAxis - originAxis) / directionAxis;

            if (distance <= 0f)
            {
                return;
            }

            float otherAxis = originOtherAxis + (directionOtherAxis * distance);

            if (otherAxis < minOtherAxis || otherAxis > maxOtherAxis)
            {
                return;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
            }
        }

        private void ValidateScrapTrailDependencies()
        {
            if (_boss.Move == null)
            {
                throw new InvalidOperationException(nameof(_boss.Move));
            }

            if (_boss.Move.BodyColliders == null)
            {
                throw new InvalidOperationException(nameof(_boss.Move.BodyColliders));
            }

            if (_scrapTrailBlockSpawner == null)
            {
                _scrapTrailBlockSpawner = SpawnerServiceLocator.Find<BossScrapTrailBlock>(ScrapTrailSpawnerKey) as BossScrapTrailBlockSpawner;
            }

            if (_scrapTrailBlockSpawner == null)
            {
                throw new InvalidOperationException(nameof(_scrapTrailBlockSpawner));
            }
        }

        private Vector3 ResolveImpactCenter(Vector3 origin, Vector3 forward, float offset, float radius)
        {
            Vector3 impactCenter = origin + forward * offset;
            float impactHeight = _boss.Base.position.y + Mathf.Min(radius * 0.3f, 0.8f);
            impactCenter.y = impactHeight;

            return impactCenter;
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
