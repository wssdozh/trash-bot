using System;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossScrapTrailBlockSpawner : Spawner<BossScrapTrailBlock>
    {
        public BossScrapTrailBlock Spawn(Vector3 position, Quaternion rotation, Vector3 size, float lifetime, Collider[] ignoredColliders)
        {
            if (lifetime <= 0f)
            {
                throw new InvalidOperationException(nameof(lifetime));
            }

            if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
            {
                throw new InvalidOperationException(nameof(size));
            }

            BossScrapTrailBlock block = Pool.Get();
            block.BindSpawner(this);
            block.Activate(position, rotation, size, lifetime, ignoredColliders);

            return block;
        }

        protected override void Awake()
        {
            base.Awake();

            for (int i = 0; i < PoolSize; i++)
            {
                BossScrapTrailBlock block = Pool.Get();
                Pool.Release(block);
            }
        }

        public override BossScrapTrailBlock Spawn(Vector3 position)
        {
            return Spawn(position, Quaternion.identity, Vector3.one, 1f, null);
        }

        public override void Despawn(BossScrapTrailBlock block)
        {
            if (block == null)
            {
                throw new InvalidOperationException(nameof(block));
            }

            Pool.Release(block);
        }
    }
}
