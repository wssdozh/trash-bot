using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorThrowAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const string ScrapCubeSpawnerKey = "BossScrapCubeProjectile";
        private const float SpawnForwardPadding = 0.55f;
        private const float SpawnHeightOffset = 0.75f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;

        private BossScrapCubeSpawner _scrapCubeSpawner;
        private float _grabTimer;
        private float _throwTimer;
        private float _recoverTimer;
        private bool _isRunning;
        private bool _isProjectileLaunched;

        public bool IsRunning => _isRunning;

        public BossExcavatorThrowAttack(BossExcavator boss, BossExcavatorConfig config)
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
            Reset();
        }

        public void Reset()
        {
            _grabTimer = 0f;
            _throwTimer = 0f;
            _recoverTimer = 0f;
            _isRunning = false;
            _isProjectileLaunched = false;
        }

        public void StartAttack()
        {
            ValidateDependencies();

            _grabTimer = _config.ThrowGrabTime;
            _throwTimer = _config.ThrowReleaseTime;
            _recoverTimer = _config.AttackRecoveryTime;
            _isRunning = true;
            _isProjectileLaunched = false;

            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
            _boss.SetArmPose(BossExcavatorArmPose.GrabScrap, _config.AttackPoseSpeedMult);
        }

        public bool Tick()
        {
            if (_isRunning == false)
            {
                return false;
            }

            if (_grabTimer > 0f)
            {
                _grabTimer = Mathf.Max(0f, _grabTimer - Time.deltaTime);

                if (_grabTimer <= 0f)
                {
                    BeginThrow();
                }

                return true;
            }

            if (_throwTimer > 0f)
            {
                if (_isProjectileLaunched == false)
                {
                    LaunchProjectiles();
                }

                _throwTimer = Mathf.Max(0f, _throwTimer - Time.deltaTime);

                if (_throwTimer <= 0f)
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
            _grabTimer = 0f;
            _throwTimer = 0f;
            _recoverTimer = 0f;
            _boss.SetAimLocked(false);

            if (restoreNeutralPose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, _config.AttackPoseSpeedMult);
            }
        }

        private void BeginThrow()
        {
            _boss.SetAimLocked(true);
            _boss.SetArmPose(BossExcavatorArmPose.ThrowScrap, _config.AttackPoseSpeedMult);
        }

        private void BeginRecover()
        {
            _boss.SetAimLocked(false);
            _boss.SetArmPose(BossExcavatorArmPose.Neutral, _config.AttackPoseSpeedMult);
        }

        private void EndAttack()
        {
            _isRunning = false;
            _boss.SetAimLocked(false);
        }

        private void LaunchProjectiles()
        {
            _isProjectileLaunched = true;

            Transform bucket = _boss.Bucket;
            Vector3 launchForward = ResolveLaunchForward();
            Vector3 spawnPosition = ResolveSpawnPosition(bucket.position, launchForward);
            int projectileCount = _config.ThrowProjectileCount;
            int projectileIndex = 0;

            while (projectileIndex < projectileCount)
            {
                float angleOffset = GetProjectileAngleOffset(projectileIndex, projectileCount);
                Vector3 projectileDirection = RotateDirection(launchForward, angleOffset);

                _scrapCubeSpawner.Spawn(
                    spawnPosition,
                    projectileDirection,
                    _config.ThrowProjectileDamage,
                    _config.ThrowProjectileSpeedMult,
                    _config.ThrowHitMask,
                    _boss.transform);

                projectileIndex += 1;
            }
        }

        private Vector3 ResolveSpawnPosition(Vector3 bucketPosition, Vector3 launchForward)
        {
            Vector3 spawnPosition = bucketPosition + launchForward * (_config.ThrowSpawnOffset + SpawnForwardPadding);
            float minSpawnHeight = _boss.Base.position.y + SpawnHeightOffset;

            if (spawnPosition.y < minSpawnHeight)
            {
                spawnPosition.y = minSpawnHeight;
            }

            return spawnPosition;
        }

        private float GetProjectileAngleOffset(int projectileIndex, int projectileCount)
        {
            if (projectileCount <= 1)
            {
                return 0f;
            }

            float spreadAngle = _config.ThrowProjectileSpreadAngle;
            float step = spreadAngle / (projectileCount - 1);
            float minAngle = spreadAngle * -0.5f;

            return minAngle + step * projectileIndex;
        }

        private Vector3 ResolveLaunchForward()
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

        private Vector3 RotateDirection(Vector3 direction, float angle)
        {
            if (direction.sqrMagnitude <= MinDirectionSqr)
            {
                return Vector3.forward;
            }

            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);

            return (rotation * direction.normalized).normalized;
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

            if (_boss.Stick == null)
            {
                throw new InvalidOperationException(nameof(_boss.Stick));
            }

            if (_scrapCubeSpawner == null)
            {
                _scrapCubeSpawner = SpawnerServiceLocator.Find<BossScrapCubeProjectile>(ScrapCubeSpawnerKey) as BossScrapCubeSpawner;
            }

            if (_scrapCubeSpawner == null)
            {
                throw new InvalidOperationException(nameof(_scrapCubeSpawner));
            }
        }
    }
}
