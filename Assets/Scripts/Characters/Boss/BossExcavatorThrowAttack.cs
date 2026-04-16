using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JunkyardBoss
{
    public sealed class BossExcavatorThrowAttack
    {
        private const float MinDirectionSqr = 0.0001f;
        private const string ProjectileResourcePath = "Prefabs/Bullet";

        private readonly BossExcavator _boss;
        private readonly BossExcavatorConfig _config;
        private readonly GameObject _projectilePrefab;
        private readonly List<Bullet> _projectiles;

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
            _projectilePrefab = Resources.Load<GameObject>(ProjectileResourcePath);

            if (_projectilePrefab == null)
            {
                throw new InvalidOperationException(nameof(_projectilePrefab));
            }

            _projectiles = new List<Bullet>(8);
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
            Vector3 spawnPosition = bucket.position + launchForward * _config.ThrowSpawnOffset;
            int projectileCount = _config.ThrowProjectileCount;
            int projectileIndex = 0;

            while (projectileIndex < projectileCount)
            {
                float angleOffset = GetProjectileAngleOffset(projectileIndex, projectileCount);
                Quaternion projectileRotation = Quaternion.LookRotation(
                    RotateDirection(launchForward, angleOffset),
                    Vector3.up);
                Bullet projectile = GetProjectile();

                projectile.transform.SetPositionAndRotation(spawnPosition, projectileRotation);
                projectile.gameObject.SetActive(true);
                projectile.SetIgnoredRoot(_boss.transform);
                projectile.SetLayers(_config.ThrowHitMask);
                projectile.SetDamage(_config.ThrowProjectileDamage);
                projectile.SetSpeedMultiplier(_config.ThrowProjectileSpeedMult);

                projectileIndex += 1;
            }
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

        private Bullet GetProjectile()
        {
            int projectileIndex = 0;

            while (projectileIndex < _projectiles.Count)
            {
                Bullet projectile = _projectiles[projectileIndex];

                if (projectile != null)
                {
                    if (projectile.gameObject.activeSelf == false)
                    {
                        return projectile;
                    }
                }

                projectileIndex += 1;
            }

            GameObject createdObject = Object.Instantiate(_projectilePrefab);
            Bullet createdProjectile = createdObject.GetComponent<Bullet>();

            if (createdProjectile == null)
            {
                throw new InvalidOperationException(nameof(createdProjectile));
            }

            createdProjectile.gameObject.SetActive(false);
            _projectiles.Add(createdProjectile);

            return createdProjectile;
        }

        private Vector3 ResolveLaunchForward()
        {
            Transform bucket = _boss.Bucket;
            Transform target = _boss.Target;

            if (bucket != null && target != null)
            {
                Vector3 targetPoint = target.position + Vector3.up * _config.ThrowAimHeight;
                Vector3 targetDirection = targetPoint - bucket.position;

                if (targetDirection.sqrMagnitude > MinDirectionSqr)
                {
                    return BuildLaunchDirection(targetDirection);
                }
            }

            if (bucket != null)
            {
                Vector3 bucketForward = bucket.forward;
                bucketForward.y = Mathf.Max(0f, bucketForward.y);

                if (bucketForward.sqrMagnitude > MinDirectionSqr)
                {
                    return BuildLaunchDirection(bucketForward);
                }
            }

            Transform cabin = _boss.Cabin;

            if (cabin != null)
            {
                Vector3 cabinForward = cabin.forward;
                cabinForward.y = Mathf.Max(0f, cabinForward.y);

                if (cabinForward.sqrMagnitude > MinDirectionSqr)
                {
                    return BuildLaunchDirection(cabinForward);
                }
            }

            Transform baseTransform = _boss.Base;

            if (baseTransform != null)
            {
                Vector3 baseForward = baseTransform.forward;
                baseForward.y = 0f;

                if (baseForward.sqrMagnitude > MinDirectionSqr)
                {
                    return BuildLaunchDirection(baseForward);
                }
            }

            return BuildLaunchDirection(Vector3.forward);
        }

        private Vector3 BuildLaunchDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= MinDirectionSqr)
            {
                return Vector3.forward;
            }

            Vector3 launchDirection = direction.normalized;
            launchDirection += Vector3.up * _config.ThrowUpwardBias;

            if (launchDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return Vector3.forward;
            }

            return launchDirection.normalized;
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
