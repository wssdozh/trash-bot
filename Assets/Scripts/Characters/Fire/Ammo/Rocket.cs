using System.Collections;
using UnityEngine;

public class Rocket : Ammo
{
    [Header("Зависимости")]
    [SerializeField] private TrailRenderer _trailRenderer;

    [Header("Настройки")]
    [SerializeField] private float _minDamage = 3f;
    [SerializeField] private float _maxDamage = 6f;
    [SerializeField] private float _impulseStrength = 3f;
    [SerializeField] private float _radiusImpulse = 3f;
    [SerializeField] private float _upwardsModifier = 0.5f;

    private Coroutine _trailRoutine;

    protected override void OnAmmoEnabled()
    {
        if (_trailRoutine == null == false)
        {
            StopCoroutine(_trailRoutine);
            _trailRoutine = null;
        }

        if (_trailRenderer == null == false)
        {
            _trailRenderer.emitting = false;
            _trailRenderer.Clear();
            _trailRoutine = StartCoroutine(EnableTrailNextFrame());
        }
    }

    private IEnumerator EnableTrailNextFrame()
    {
        yield return null;

        _trailRenderer.Clear();
        _trailRenderer.emitting = true;
        _trailRoutine = null;
    }

    protected override void OnHitTarget(Collider other)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radiusImpulse, TargetLayers);

        Rigidbody tempRigidbody;

        foreach (Collider collider in colliders)
        {

            if (collider.gameObject.TryGetComponent<Health>(out Health health))
            {
                health.Decrease(Random.Range(_minDamage, _maxDamage));
            }

            tempRigidbody = collider.attachedRigidbody;

            if (tempRigidbody == null)
                continue;

            tempRigidbody.AddExplosionForce(_impulseStrength, transform.position, _radiusImpulse, _upwardsModifier, ForceMode.Impulse);
        }
    }

    protected override void OnLifeEnding()
    {
        if (_trailRoutine == null == false)
        {
            StopCoroutine(_trailRoutine);
            _trailRoutine = null;
        }

        if (_trailRenderer == null == false)
        {
            _trailRenderer.emitting = false;
            _trailRenderer.Clear();
        }
    }
}
