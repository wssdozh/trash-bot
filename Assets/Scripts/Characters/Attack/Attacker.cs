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

        Transform targetTransform = null;
        Rigidbody targetRigidbody = null;
        Health targetHealth = null;
        float closestDistance = float.MaxValue;

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

            if (hitHealth == null && hitRigidbody == null)
            {
                hitIndex += 1;

                continue;
            }

            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetTransform = hit.transform;
                targetRigidbody = hitRigidbody;
                targetHealth = hitHealth;
            }

            hitIndex += 1;
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
