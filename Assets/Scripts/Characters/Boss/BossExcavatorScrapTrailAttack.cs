using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorScrapTrailAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const int GroundHitBufferCount = 8;
        private const string ScrapTrailSpawnerKey = "BossScrapTrailBlock";

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly RaycastHit[] _groundHitBuffer;

        private BossScrapTrailBlockSpawner _scrapTrailBlockSpawner;
        private float _activeTimer;
        private Vector3 _lastSpawnPoint;
        private bool _isRunning;
        private bool _hasLastSpawnPoint;

        public bool IsRunning => _isRunning;

        public BossExcavatorScrapTrailAttack(BossExcavator boss, BossExcavatorConfig config)
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
            _groundHitBuffer = new RaycastHit[GroundHitBufferCount];
            Reset();
        }

        public void Reset()
        {
            _activeTimer = 0f;
            _lastSpawnPoint = Vector3.zero;
            _isRunning = false;
            _hasLastSpawnPoint = false;
        }

        public void StartAttack()
        {
            ValidateDependencies();

            _activeTimer = GetActiveDuration();
            _lastSpawnPoint = Vector3.zero;
            _isRunning = true;
            _hasLastSpawnPoint = false;

            _boss.SetArmPose(BossExcavatorArmPose.TrailScrape, GetAttackPoseSpeedMult());
        }

        public bool Tick()
        {
            if (_isRunning == false)
            {
                return false;
            }

            _activeTimer = Mathf.Max(0f, _activeTimer - Time.deltaTime);
            TrySpawnBlock();

            if (_activeTimer > 0f)
            {
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

            _activeTimer = 0f;
            _lastSpawnPoint = Vector3.zero;
            _isRunning = false;
            _hasLastSpawnPoint = false;

            if (restoreNeutralPose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
            }
        }

        private void EndAttack()
        {
            _activeTimer = 0f;
            _lastSpawnPoint = Vector3.zero;
            _isRunning = false;
            _hasLastSpawnPoint = false;
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, GetAttackPoseSpeedMult());
        }

        private void TrySpawnBlock()
        {
            Vector3 spawnPoint;

            if (TryResolveSpawnPoint(out spawnPoint) == false)
            {
                return;
            }

            if (_hasLastSpawnPoint)
            {
                float spawnDistance = Vector3.Distance(_lastSpawnPoint, spawnPoint);

                if (spawnDistance < GetSpawnSpacing())
                {
                    return;
                }
            }

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

            _lastSpawnPoint = spawnPoint;
            _hasLastSpawnPoint = true;
        }

        private bool TryResolveSpawnPoint(out Vector3 spawnPoint)
        {
            spawnPoint = Vector3.zero;

            Vector3 moveDirection = GetMoveDirection();

            if (moveDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return false;
            }

            Transform bucket = _boss.Bucket;

            if (bucket == null)
            {
                throw new InvalidOperationException(nameof(bucket));
            }

            Vector3 bucketPoint = bucket.position - moveDirection * _config.ScrapTrailSpawnBackOffset;
            Vector3 rayOrigin = bucketPoint + Vector3.up * _config.ScrapTrailGroundProbeHeight;
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

                spawnPoint = groundHit.point;

                return true;
            }

            spawnPoint = bucketPoint;
            spawnPoint.y = _boss.Base.position.y;

            return true;
        }

        private Vector3 GetMoveDirection()
        {
            Vector3 moveDirection = _boss.Move.CurrentMoveDirection;

            if (moveDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return Vector3.zero;
            }

            return moveDirection.normalized;
        }

        private void ValidateDependencies()
        {
            if (_boss.Bucket == null)
            {
                throw new InvalidOperationException(nameof(_boss.Bucket));
            }

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

        private float GetActiveDuration()
        {
            float activeDuration = _config.ScrapTrailDuration;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                activeDuration *= 1.55f;
            }

            return activeDuration / GetPhaseAttackSpeedMult();
        }

        private float GetSpawnSpacing()
        {
            float spawnSpacing = _config.ScrapTrailSpawnSpacing;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                spawnSpacing *= 0.68f;
            }

            return spawnSpacing;
        }

        private float GetAttackPoseSpeedMult()
        {
            return _config.AttackPoseSpeedMult * GetPhaseAttackSpeedMult();
        }

        private float GetPhaseAttackSpeedMult()
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.PhaseTwoAttackSpeedMult;
            }

            return 1f;
        }
    }
}
