using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossScrapCubeSpawner : Spawner<BossScrapCubeProjectile>, IAmmoSpawner
    {
        private const float MinDirectionSqr = 0.0001f;

        private readonly Dictionary<int, AmmoReturner> _returnersByProjectileId = new Dictionary<int, AmmoReturner>(32);

        public BossScrapCubeProjectile Spawn(
            Vector3 position,
            Vector3 moveDirection,
            float damage,
            float speedMultiplier,
            LayerMask hitMask,
            Transform ignoredRoot)
        {
            if (speedMultiplier <= 0f)
            {
                throw new InvalidOperationException(nameof(speedMultiplier));
            }

            if (damage <= 0f)
            {
                throw new InvalidOperationException(nameof(damage));
            }

            Vector3 planarDirection = moveDirection;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= MinDirectionSqr)
            {
                planarDirection = Vector3.forward;
            }

            BossScrapCubeProjectile projectile = Pool.Get();
            projectile.transform.SetPositionAndRotation(position, Quaternion.LookRotation(planarDirection.normalized, Vector3.up));
            projectile.SetLayers(hitMask);
            projectile.SetIgnoredRoot(ignoredRoot);
            GetAmmoReturner(projectile).Initialize(this);
            projectile.gameObject.SetActive(true);
            projectile.SetDamage(damage);
            projectile.SetSpeedMultiplier(speedMultiplier);

            return projectile;
        }

        protected override void Awake()
        {
            base.Awake();

            for (int i = 0; i < PoolSize; i++)
            {
                BossScrapCubeProjectile projectile = Pool.Get();
                Pool.Release(projectile);
            }
        }

        public override BossScrapCubeProjectile Spawn(Vector3 position)
        {
            return Spawn(position, Vector3.forward, 1f, 1f, default, null);
        }

        public override void Despawn(BossScrapCubeProjectile projectile)
        {
            if (projectile == null)
            {
                throw new InvalidOperationException(nameof(projectile));
            }

            Pool.Release(projectile);
        }

        void IAmmoSpawner.Despawn(Ammo ammo)
        {
            BossScrapCubeProjectile projectile = ammo as BossScrapCubeProjectile;

            if (projectile == null)
            {
                throw new InvalidOperationException(nameof(ammo));
            }

            Despawn(projectile);
        }

        private AmmoReturner GetAmmoReturner(BossScrapCubeProjectile projectile)
        {
            int projectileId = projectile.GetInstanceID();

            if (_returnersByProjectileId.TryGetValue(projectileId, out AmmoReturner cachedReturner))
            {
                if (cachedReturner != null)
                {
                    return cachedReturner;
                }
            }

            AmmoReturner ammoReturner = projectile.GetComponent<AmmoReturner>();

            if (ammoReturner == null)
            {
                throw new InvalidOperationException(nameof(ammoReturner));
            }

            _returnersByProjectileId[projectileId] = ammoReturner;

            return ammoReturner;
        }

        protected override void ActionOnGet(BossScrapCubeProjectile projectile)
        {
        }

        protected override void ActionOnRelease(BossScrapCubeProjectile projectile)
        {
            projectile.gameObject.SetActive(false);
        }
    }
}
