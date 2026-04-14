using UnityEngine;

public sealed class Bullet : Ammo
{
    private readonly DeveloperCheatSave _developerCheatSave = new DeveloperCheatSave();

    [Header("РќР°СЃС‚СЂРѕР№РєРё")]
    [SerializeField] private float _impulseStrength = 3f;

    protected override void OnHitTarget(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();

        if (health != null)
        {
            float damage = Damage;

            if (IsPlayerOwned() && _developerCheatSave.LoadInfiniteDamage())
            {
                damage = health.MaxValue;
            }

            health.Decrease(damage);
        }

        Rigidbody rigidbody = other.attachedRigidbody;

        if (rigidbody != null)
        {
            rigidbody.AddForce(transform.forward * _impulseStrength, ForceMode.Impulse);
        }
    }
}
