using UnityEngine;

public class Rocket : Ammo
{
    [Header("Настройки")]
    [SerializeField] private float _minDamage = 3f;
    [SerializeField] private float _maxDamage = 6f;
    [SerializeField] private float _impulseStrength = 3f;
    [SerializeField] private float _radiusImpulse = 3f;
    [SerializeField] private float _upwardsModifier = 0.5f;

    protected override void OnHitTarget(Collider other)
    {
        Vector3 explosionPosition = transform.position;

        Collider[] colliders = GetAffectedColliders(explosionPosition);

        foreach (Collider collider in colliders)
        {
            float damageMultiplier = GetDamageMultiplier(explosionPosition, collider);

            TryDealDamage(collider, damageMultiplier);

            TryApplyExplosionForce(collider, explosionPosition);
        }
    }

    private Collider[] GetAffectedColliders(Vector3 explosionPosition)
    {
        return Physics.OverlapSphere(explosionPosition, _radiusImpulse, TargetLayers);
    }

    private float GetDamageMultiplier(Vector3 explosionPosition, Collider collider)
    {
        float distance = Vector3.Distance(explosionPosition, collider.transform.position);
        float normalizedDistance = Mathf.Clamp01(distance / _radiusImpulse);

        return 1f - normalizedDistance;
    }

    private void TryDealDamage(Collider collider, float damageMultiplier)
    {
        if (collider.gameObject.TryGetComponent<Health>(out Health health) == false)
        {
            return;
        }

        float baseDamage = Random.Range(_minDamage, _maxDamage);
        float finalDamage = baseDamage * damageMultiplier;

        if (finalDamage <= 0f)
        {
            return;
        }

        health.Decrease(finalDamage);
    }

    private void TryApplyExplosionForce(Collider collider, Vector3 explosionPosition)
    {
        Rigidbody rigidbody = collider.attachedRigidbody;

        if (rigidbody == null)
        {
            return;
        }

        rigidbody.AddExplosionForce(
            _impulseStrength,
            explosionPosition,
            _radiusImpulse,
            _upwardsModifier,
            ForceMode.Impulse);
    }
}
