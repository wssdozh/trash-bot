using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossScrapCubeProjectile : Ammo
    {
        protected override void OnHitTarget(Collider other)
        {
            Health health = other.GetComponentInParent<Health>();

            if (health == null)
            {
                return;
            }

            health.Decrease(Damage);
        }
    }
}
