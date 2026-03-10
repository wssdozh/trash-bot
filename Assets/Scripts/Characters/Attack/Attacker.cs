using UnityEngine;
using System.Collections;

public class Attacker : MonoBehaviour
{
    private const int TargetBufferSize = 16;
    private const float DirectionThreshold = 0.0001f;

    [SerializeField] private AttackData _attackData;
    [SerializeField] private float _hitForce = 6f;
    [SerializeField] private ForceMode _hitForceMode = ForceMode.Impulse;
    [SerializeField] private bool _isGizmoVisible = true;
    [SerializeField] private float _gizmoPointSize = 0.12f;

    private bool _isOnCooldown = false;
    private readonly Collider[] _targetBuffer = new Collider[TargetBufferSize];

    public AttackData AttackData => _attackData;

    public bool CanStartAttack()
    {
        return _isOnCooldown == false;
    }

    public bool PerformAttack()
    {
        return PerformAttack(transform.forward);
    }

    public bool PerformAttack(Vector3 attackDirection)
    {
        if (_isOnCooldown)
        {
            return false;
        }

        int damage = _attackData.GetDamage();
        int hitCount = FillTargets(attackDirection);

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

    public bool CanHitTarget(Transform targetTransform, Vector3 attackDirection)
    {
        if (targetTransform == null)
        {
            return false;
        }

        int hitCount = FillTargets(attackDirection);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hit = _targetBuffer[hitIndex];

            if (hit != null)
            {
                if (IsTargetMatch(targetTransform, hit.transform))
                {
                    return true;
                }
            }

            hitIndex += 1;
        }

        return false;
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
        Vector3 attackDirection = GetAttackDirection(transform.forward);
        _attackData.AttackShape.DrawGizmos(transform.position, attackDirection, _attackData.AttackRange);

        Vector3 attackPoint = transform.position + (attackDirection * _attackData.AttackRange);

        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawLine(transform.position, attackPoint);
        Gizmos.DrawWireSphere(attackPoint, _gizmoPointSize);
    }

    private int FillTargets(Vector3 attackDirection)
    {
        Vector3 resolvedDirection = GetAttackDirection(attackDirection);

        return _attackData.AttackShape.GetTargets(
            transform.position,
            resolvedDirection,
            _attackData.AttackRange,
            _attackData.HitLayers,
            _targetBuffer);
    }

    private bool IsTargetMatch(Transform targetTransform, Transform hitTransform)
    {
        if (hitTransform == targetTransform)
        {
            return true;
        }

        if (hitTransform.IsChildOf(targetTransform))
        {
            return true;
        }

        if (targetTransform.IsChildOf(hitTransform))
        {
            return true;
        }

        return false;
    }

    private Vector3 GetAttackDirection(Vector3 attackDirection)
    {
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude <= DirectionThreshold)
        {
            Vector3 transformForward = transform.forward;
            transformForward.y = 0f;

            if (transformForward.sqrMagnitude <= DirectionThreshold)
            {
                return Vector3.forward;
            }

            transformForward.Normalize();

            return transformForward;
        }

        attackDirection.Normalize();

        return attackDirection;
    }
}
