using UnityEngine;
using System.Collections;

public class Attacker : MonoBehaviour
{
    private const int TargetBufferSize = 16;

    [SerializeField] private AttackData _attackData;
    [SerializeField] private float _hitForce = 6f;
    [SerializeField] private ForceMode _hitForceMode = ForceMode.Impulse;
    [SerializeField] private bool _isGizmoVisible = true;
    [SerializeField] private float _gizmoPointSize = 0.12f;

    private bool _isOnCooldown = false;
    private readonly Collider[] _targetBuffer = new Collider[TargetBufferSize];

    public AttackData AttackData => _attackData;

    public bool PerformAttack()
    {
        if (_isOnCooldown)
        {
            return false;
        }

        int damage = _attackData.GetDamage();

        int hitCount = _attackData.AttackShape.GetTargets(transform, _attackData.AttackRange, _attackData.HitLayers, _targetBuffer);

        if (hitCount == 0)
        {
            StartCoroutine(StartCooldown());

            return true;
        }

        Transform nearestHealthTransform = null;
        Rigidbody nearestHealthRigidbody = null;
        Health nearestHealth = null;
        float nearestHealthDistance = float.MaxValue;
        Transform nearestRigidbodyTransform = null;
        Rigidbody nearestRigidbody = null;
        float nearestRigidbodyDistance = float.MaxValue;

        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hit = _targetBuffer[hitIndex];

            if (hit == null)
            {
                hitIndex += 1;

                continue;
            }

            if (hit.transform.IsChildOf(transform))
            {
                hitIndex += 1;

                continue;
            }

            Health hitHealth = hit.GetComponentInParent<Health>();
            Rigidbody hitRigidbody = hit.attachedRigidbody;

            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (hitHealth != null)
            {
                if (distance < nearestHealthDistance)
                {
                    nearestHealthDistance = distance;
                    nearestHealthTransform = hit.transform;
                    nearestHealthRigidbody = hitRigidbody;
                    nearestHealth = hitHealth;
                }
            }

            else if (hitRigidbody != null)
            {
                if (distance < nearestRigidbodyDistance)
                {
                    nearestRigidbodyDistance = distance;
                    nearestRigidbodyTransform = hit.transform;
                    nearestRigidbody = hitRigidbody;
                }
            }

            hitIndex += 1;
        }

        Transform targetTransform = nearestHealthTransform;
        Rigidbody targetRigidbody = nearestHealthRigidbody;
        Health targetHealth = nearestHealth;

        if (targetTransform == null)
        {
            targetTransform = nearestRigidbodyTransform;
            targetRigidbody = nearestRigidbody;
        }

        if (targetTransform == null)
        {
            StartCoroutine(StartCooldown());

            return true;
        }

        if (targetRigidbody != null)
        {
            Vector3 direction = (targetTransform.position - transform.position).normalized;
            targetRigidbody.AddForce(direction * _hitForce, _hitForceMode);
        }

        if (targetHealth != null)
        {
            targetHealth.Decrease(damage);
        }

        StartCoroutine(StartCooldown());

        return true;
    }

    private IEnumerator StartCooldown()
    {
        _isOnCooldown = true;

        yield return new WaitForSeconds(_attackData.AttackCooldown);

        _isOnCooldown = false;
    }

    private void OnDrawGizmos()
    {
        if (_isGizmoVisible == false)
        {
            return;
        }

        if (_attackData == null)
        {
            return;
        }

        if (_attackData.AttackShape == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        _attackData.AttackShape.DrawGizmos(transform, _attackData.AttackRange);

        Vector3 attackPoint = transform.position + (transform.forward * _attackData.AttackRange);

        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawLine(transform.position, attackPoint);
        Gizmos.DrawWireSphere(attackPoint, _gizmoPointSize);
    }
}
