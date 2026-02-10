using System;
using UnityEngine;

public sealed class Rocket : Ammo
{
    [Header("Настройки")]
    [SerializeField] private float _impulseStrength = 3f;
    [SerializeField] private float _radiusImpulse = 3f;
    [SerializeField] private float _upwardsModifier = 0.5f;

    private float _radiusMultiplier = 1f;

    protected override void OnAmmoEnabled()
    {
        _radiusMultiplier = 1f;
    }

    public void SetExplosionRadiusMultiplier(float radiusMultiplier)
    {
        if (radiusMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(radiusMultiplier));
        }

        _radiusMultiplier = radiusMultiplier;
    }

    protected override void OnHitTarget(Collider other)
    {
        Vector3 explosionPosition = transform.position;

        Collider[] colliders = GetAffectedColliders(explosionPosition);

        for (int i = 0; i < colliders.Length; i++)
        {

            Collider collider = colliders[i];

            float damageMultiplier = GetDamageMultiplier(explosionPosition, collider);

            TryDealDamage(collider, damageMultiplier);

            TryApplyExplosionForce(collider, explosionPosition);

        }
    }

    private Collider[] GetAffectedColliders(Vector3 explosionPosition)
    {
        float radius = _radiusImpulse * _radiusMultiplier;

        return Physics.OverlapSphere(explosionPosition, radius, TargetLayers);
    }

    private float GetDamageMultiplier(Vector3 explosionPosition, Collider collider)
    {
        float radius = _radiusImpulse * _radiusMultiplier;

        float distance = Vector3.Distance(explosionPosition, collider.transform.position);
        float normalizedDistance = Mathf.Clamp01(distance / radius);

        return 1f - normalizedDistance;
    }

    private void TryDealDamage(Collider collider, float damageMultiplier)
    {
        if (collider.gameObject.TryGetComponent<Health>(out Health health) == false)
        {
            return;
        }

        float finalDamage = Damage * damageMultiplier;

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

        float radius = _radiusImpulse * _radiusMultiplier;

        rigidbody.AddExplosionForce(
            _impulseStrength,
            explosionPosition,
            radius,
            _upwardsModifier,
            ForceMode.Impulse);
    }
}
