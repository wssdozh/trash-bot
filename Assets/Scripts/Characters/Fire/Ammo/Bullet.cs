using UnityEngine;

public sealed class Bullet : Ammo
{
    [Header("Настройки")]
    [SerializeField] private float _impulseStrength = 3f;

    protected override void OnHitTarget(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();

        if (health != null)
        {
            health.Decrease(Damage);
        }

        Rigidbody rigidbody = other.attachedRigidbody;

        if (rigidbody != null)
        {
            rigidbody.AddForce(transform.forward * _impulseStrength, ForceMode.Impulse);
        }
    }
}
