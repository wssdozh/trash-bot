using UnityEngine;

public sealed class Bullet : Ammo
{
    [Header("Настройки")]
    [SerializeField] private float _impulseStrength = 3f;

    protected override void OnHitTarget(Collider other)
    {
        if (other.gameObject.TryGetComponent<Health>(out Health health))
        {
            health.Decrease(Damage);
        }

        if (other.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
        {
            rigidbody.AddForce(transform.forward * _impulseStrength, ForceMode.Impulse);
        }
    }
}
